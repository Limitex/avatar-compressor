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
    /// sampling the source yielded.
    /// </summary>
    public class CpuAreaAverageResizer : ITextureResizer
    {
        public Texture2D Resize(Texture2D source, int targetWidth, int targetHeight)
        {
            if (source == null)
                return null;

            int srcW = source.width;
            int srcH = source.height;
            bool isSRGB = source.isDataSRGB;

            var pixels = GetStoredPixels(source);
            if (pixels == null || pixels.Length == 0)
                return null;

            if (targetWidth == srcW && targetHeight == srcH)
            {
                return CreateOutput(pixels, targetWidth, targetHeight, source, isSRGB);
            }

            var decode = BuildDecodeTable(isSRGB);
            var planX = BuildPlan(srcW, targetWidth);
            var planY = BuildPlan(srcH, targetHeight);

            var intermediate = new Vector4[targetWidth * srcH];

            Parallel.For(
                0,
                srcH,
                y =>
                {
                    int srcRow = y * srcW;
                    int dstRow = y * targetWidth;
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

                        intermediate[dstRow + i] = sum;
                    }
                }
            );

            var result = new Color32[targetWidth * targetHeight];

            Parallel.For(
                0,
                targetWidth,
                x =>
                {
                    int dstN = planY.Start.Length;
                    for (int i = 0; i < dstN; i++)
                    {
                        var sum = Vector4.zero;
                        int off = planY.Offset[i];
                        int count = planY.Len[i];
                        int s = planY.Start[i];

                        for (int t = 0; t < count; t++)
                        {
                            sum += intermediate[(s + t) * targetWidth + x] * planY.Weights[off + t];
                        }

                        result[i * targetWidth + x] = EncodePixel(sum, isSRGB);
                    }
                }
            );

            return CreateOutput(result, targetWidth, targetHeight, source, isSRGB);
        }

        public struct AxisPlan
        {
            public int[] Start;
            public int[] Len;
            public int[] Offset;
            public float[] Weights;
        }

        public static AxisPlan BuildPlan(int srcN, int dstN)
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

        private static Color32 EncodePixel(Vector4 linear, bool isSRGB)
        {
            float r = linear.x;
            float g = linear.y;
            float b = linear.z;

            if (isSRGB)
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
                try
                {
                    return texture.GetPixels32();
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] GetPixels32 failed for '{texture.name}', "
                            + $"falling back to RenderTexture readback: {e.Message}"
                    );
                }
            }

            var colorSpace = texture.isDataSRGB
                ? RenderTextureReadWrite.sRGB
                : RenderTextureReadWrite.Linear;

            RenderTexture rt = null;
            Texture2D readable = null;
            var previous = RenderTexture.active;
            try
            {
                rt = new RenderTexture(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    colorSpace
                );
                rt.Create();
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;

                readable = new Texture2D(
                    texture.width,
                    texture.height,
                    TextureFormat.RGBA32,
                    false,
                    linear: !texture.isDataSRGB
                );
                readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                readable.Apply(false);

                return readable.GetPixels32();
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[TextureCompressor] Failed to read pixels from '{texture.name}': {e.Message}"
                );
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (readable != null)
                    UnityEngine.Object.DestroyImmediate(readable);
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
            }
        }

        private static Texture2D CreateOutput(
            Color32[] pixels,
            int width,
            int height,
            Texture2D source,
            bool isSRGB
        )
        {
            var output = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                source.mipmapCount > 1,
                linear: !isSRGB
            );
            output.SetPixels32(pixels);
            output.Apply(source.mipmapCount > 1);
            TextureProcessor.CopyTextureSettings(source, output);
            return output;
        }
    }
}
