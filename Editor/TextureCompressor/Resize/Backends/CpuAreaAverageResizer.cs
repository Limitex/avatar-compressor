using System;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// CPU implementation of separable two-pass area averaging.
    /// Color space policy: readback obtains the stored bytes unchanged,
    /// sRGB sources are decoded to linear before averaging and re-encoded
    /// afterwards, and the output texture keeps the source's color space
    /// flag — so sampling the result yields the area average of what
    /// sampling the source yielded. With forceLinearOutput the re-encode
    /// is skipped and the output is flagged linear instead.
    /// </summary>
    public class CpuAreaAverageResizer : ITextureResizer
    {
        public Texture2D Resize(
            Texture2D source,
            int targetWidth,
            int targetHeight,
            bool forceLinearOutput
        )
        {
            if (source == null)
                return null;

            int srcW = source.width;
            int srcH = source.height;
            bool isSRGB = source.isDataSRGB;
            bool outputSRGB = isSRGB && !forceLinearOutput;

            var pixels = GetStoredPixels(source);
            if (pixels == null || pixels.Length == 0)
                return null;

            if (source.filterMode == FilterMode.Point)
            {
                return ResizeNearest(
                    pixels,
                    srcW,
                    srcH,
                    targetWidth,
                    targetHeight,
                    source,
                    isSRGB,
                    outputSRGB
                );
            }

            // A same-size target needs no filtering: copy the bytes, decoding
            // sRGB to linear first when the output is forced linear (normal
            // maps). Skipping the two-pass filter also skips its full-image
            // intermediate buffer.
            if (targetWidth == srcW && targetHeight == srcH)
            {
                if (outputSRGB != isSRGB)
                    DecodeToLinear(pixels);
                return CreateOutput(pixels, targetWidth, targetHeight, source, outputSRGB);
            }

            var decode = BuildDecodeTable(isSRGB);
            var planX = BuildPlan(srcW, targetWidth);
            var planY = BuildPlan(srcH, targetHeight);

            var result = new Color32[targetWidth * targetHeight];

            // Output rows are processed in contiguous chunks, each streaming
            // the horizontal pass through a ring buffer that holds only one
            // vertical tap window. Peak memory is O(targetWidth * tap window)
            // instead of the full-image O(targetWidth * srcH) intermediate,
            // which exceeded the 2 GiB managed-array limit for 16K sources.
            // Chunks re-run the horizontal pass only for the few tap-window
            // rows they share with a neighbour. Per-pixel tap order is
            // unchanged, so the output is bit-identical to the two-pass form.
            int ringRows = 0;
            for (int i = 0; i < targetHeight; i++)
                ringRows = Math.Max(ringRows, planY.Len[i]);

            int chunkCount = Math.Min(Environment.ProcessorCount, targetHeight);

            Parallel.For(
                0,
                chunkCount,
                c =>
                {
                    int rowStart = (int)((long)targetHeight * c / chunkCount);
                    int rowEnd = (int)((long)targetHeight * (c + 1) / chunkCount);
                    var ring = new Vector4[ringRows * targetWidth];
                    int nextSourceRow = planY.Start[rowStart];

                    for (int i = rowStart; i < rowEnd; i++)
                    {
                        int off = planY.Offset[i];
                        int count = planY.Len[i];
                        int s = planY.Start[i];

                        for (int y = Math.Max(nextSourceRow, s); y < s + count; y++)
                        {
                            ResampleRowHorizontally(
                                pixels,
                                srcW,
                                y,
                                decode,
                                planX,
                                ring,
                                ringRows,
                                targetWidth
                            );
                        }
                        nextSourceRow = Math.Max(nextSourceRow, s + count);

                        int dstRow = i * targetWidth;
                        for (int x = 0; x < targetWidth; x++)
                        {
                            var sum = Vector4.zero;
                            for (int t = 0; t < count; t++)
                            {
                                sum +=
                                    ring[((s + t) % ringRows) * targetWidth + x]
                                    * planY.Weights[off + t];
                            }

                            result[dstRow + x] = EncodePixel(sum, outputSRGB);
                        }
                    }
                }
            );

            return CreateOutput(result, targetWidth, targetHeight, source, outputSRGB);
        }

        /// <summary>
        /// Horizontal pass for one source row: box/bilinear taps from planX,
        /// decoded into the averaging space, written to the row's ring slot
        /// (source row index modulo the ring height).
        /// </summary>
        private static void ResampleRowHorizontally(
            Color32[] pixels,
            int srcW,
            int y,
            float[] decode,
            AxisPlan planX,
            Vector4[] ring,
            int ringRows,
            int targetWidth
        )
        {
            int srcRow = y * srcW;
            int dstRow = (y % ringRows) * targetWidth;
            for (int i = 0; i < targetWidth; i++)
            {
                var sum = Vector4.zero;
                int off = planX.Offset[i];
                int count = planX.Len[i];
                int s = planX.Start[i];

                for (int t = 0; t < count; t++)
                {
                    var p = pixels[srcRow + s + t];
                    float w = planX.Weights[off + t];
                    sum.x += decode[p.r] * w;
                    sum.y += decode[p.g] * w;
                    sum.z += decode[p.b] * w;
                    sum.w += (p.a / 255f) * w;
                }

                ring[dstRow + i] = sum;
            }
        }

        /// <summary>
        /// Point-filtered sources are resampled with nearest neighbor to keep
        /// exact texel values (pixel art): averaging would introduce blended
        /// colors that the Point filter then displays crisply. Matches the old
        /// blit path, which sampled with the source's own filter mode.
        /// </summary>
        private static Texture2D ResizeNearest(
            Color32[] pixels,
            int srcW,
            int srcH,
            int dstW,
            int dstH,
            Texture2D source,
            bool isSRGB,
            bool outputSRGB
        )
        {
            float scaleX = (float)srcW / dstW;
            float scaleY = (float)srcH / dstH;

            var result = new Color32[dstW * dstH];
            for (int y = 0; y < dstH; y++)
            {
                int sy = Math.Min((int)((y + 0.5f) * scaleY), srcH - 1);
                int srcRow = sy * srcW;
                int dstRow = y * dstW;
                for (int x = 0; x < dstW; x++)
                {
                    int sx = Math.Min((int)((x + 0.5f) * scaleX), srcW - 1);
                    result[dstRow + x] = pixels[srcRow + sx];
                }
            }

            if (isSRGB && !outputSRGB)
            {
                // Forced-linear output (normal maps) still needs the sRGB decode.
                DecodeToLinear(result);
            }

            return CreateOutput(result, dstW, dstH, source, outputSRGB);
        }

        public struct AxisPlan
        {
            public int[] Start;
            public int[] Len;
            public int[] Offset;
            public float[] Weights;
        }

        /// <summary>
        /// Builds per-output-pixel taps for one axis: a box filter over the
        /// covered source interval when downscaling, 2-tap bilinear
        /// interpolation when upscaling (a box interval covers at most one
        /// source pixel there, which would degenerate to nearest-neighbor
        /// replication). Must match ResampleAxis in AreaAverageResize.compute.
        /// </summary>
        public static AxisPlan BuildPlan(int srcN, int dstN)
        {
            return dstN > srcN ? BuildBilinearPlan(srcN, dstN) : BuildBoxPlan(srcN, dstN);
        }

        private static AxisPlan BuildBilinearPlan(int srcN, int dstN)
        {
            float scale = (float)srcN / dstN;

            var start = new int[dstN];
            var len = new int[dstN];
            var offset = new int[dstN];

            int totalTaps = 0;
            for (int i = 0; i < dstN; i++)
            {
                float c = (i + 0.5f) * scale - 0.5f;
                int i0 = Mathf.FloorToInt(c);
                int s0 = Mathf.Clamp(i0, 0, srcN - 1);
                int s1 = Mathf.Clamp(i0 + 1, 0, srcN - 1);

                start[i] = s0;
                len[i] = s1 > s0 ? 2 : 1;
                offset[i] = totalTaps;
                totalTaps += len[i];
            }

            var weights = new float[totalTaps];
            for (int i = 0; i < dstN; i++)
            {
                float c = (i + 0.5f) * scale - 0.5f;
                float f = c - Mathf.FloorToInt(c);

                if (len[i] == 2)
                {
                    weights[offset[i]] = 1f - f;
                    weights[offset[i] + 1] = f;
                }
                else
                {
                    weights[offset[i]] = 1f;
                }
            }

            return new AxisPlan
            {
                Start = start,
                Len = len,
                Offset = offset,
                Weights = weights,
            };
        }

        private static AxisPlan BuildBoxPlan(int srcN, int dstN)
        {
            float scale = (float)srcN / dstN;

            var start = new int[dstN];
            var len = new int[dstN];
            var offset = new int[dstN];

            int totalTaps = 0;
            for (int i = 0; i < dstN; i++)
            {
                float a = i * scale;
                float b = (i + 1) * scale;
                int s = (int)a;
                int e = Math.Min((int)Math.Ceiling(b), srcN);
                if (e <= s)
                    e = Math.Min(s + 1, srcN);

                start[i] = s;
                len[i] = e - s;
                offset[i] = totalTaps;
                totalTaps += len[i];
            }

            var weights = new float[totalTaps];
            for (int i = 0; i < dstN; i++)
            {
                float a = i * scale;
                float b = (i + 1) * scale;

                float totalWeight = 0f;
                int off = offset[i];
                int count = len[i];

                for (int t = 0; t < count; t++)
                {
                    int sx = start[i] + t;
                    float w = Math.Max(0f, Math.Min(sx + 1, b) - Math.Max(sx, a));
                    weights[off + t] = w;
                    totalWeight += w;
                }

                if (totalWeight > 1e-12f)
                {
                    float inv = 1f / totalWeight;
                    for (int t = 0; t < count; t++)
                    {
                        weights[off + t] *= inv;
                    }
                }
            }

            return new AxisPlan
            {
                Start = start,
                Len = len,
                Offset = offset,
                Weights = weights,
            };
        }

        /// <summary>
        /// In-place sRGB→linear decode of the RGB channels (alpha is stored
        /// linearly). Used by the 1:1 paths where no filtering runs.
        /// </summary>
        private static void DecodeToLinear(Color32[] pixels)
        {
            var decode = BuildDecodeTable(true);
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                pixels[i] = new Color32(
                    ToByte(decode[p.r]),
                    ToByte(decode[p.g]),
                    ToByte(decode[p.b]),
                    p.a
                );
            }
        }

        /// <summary>
        /// Maps stored byte values to the averaging space: sRGB sources are
        /// decoded to linear (exact sRGB curve, matching the GPU's hardware
        /// decode), linear sources pass through unchanged.
        /// </summary>
        private static float[] BuildDecodeTable(bool isSRGB)
        {
            var table = new float[256];
            for (int i = 0; i < 256; i++)
            {
                float v = i / 255f;
                table[i] = isSRGB ? Mathf.GammaToLinearSpace(v) : v;
            }
            return table;
        }

        private static Color32 EncodePixel(Vector4 linear, bool encodeSRGB)
        {
            float r = linear.x;
            float g = linear.y;
            float b = linear.z;

            if (encodeSRGB)
            {
                r = Mathf.LinearToGammaSpace(r);
                g = Mathf.LinearToGammaSpace(g);
                b = Mathf.LinearToGammaSpace(b);
            }

            return new Color32(ToByte(r), ToByte(g), ToByte(b), ToByte(linear.w));
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        }

        /// <summary>
        /// Returns the texture's stored bytes unchanged. Non-readable textures
        /// are read back through a RenderTexture whose color space matches the
        /// source, so the hardware decode/encode round-trip preserves the bytes.
        /// </summary>
        private static Color32[] GetStoredPixels(Texture2D texture)
        {
            if (texture.isReadable)
            {
                return texture.GetPixels32();
            }

            var colorSpace = texture.isDataSRGB
                ? RenderTextureReadWrite.sRGB
                : RenderTextureReadWrite.Linear;

            var readable = TextureReadback.BlitToReadable(
                texture,
                colorSpace,
                linearFlag: !texture.isDataSRGB
            );
            if (readable == null)
                return null;
            try
            {
                return readable.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        private static Texture2D CreateOutput(
            Color32[] pixels,
            int width,
            int height,
            Texture2D source,
            bool outputSRGB
        )
        {
            var output = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                source.mipmapCount > 1,
                linear: !outputSRGB
            );
            output.SetPixels32(pixels);
            output.Apply(source.mipmapCount > 1);
            TextureReadback.CopyTextureSettings(source, output);
            return output;
        }
    }
}
