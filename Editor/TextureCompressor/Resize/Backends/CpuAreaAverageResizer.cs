using System;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    public class CpuAreaAverageResizer : ITextureResizer
    {
        public Texture2D Resize(
            Texture2D source,
            int targetWidth,
            int targetHeight,
            bool isNormalMap
        )
        {
            if (source == null)
                return null;

            int srcW = source.width;
            int srcH = source.height;

            if (targetWidth == srcW && targetHeight == srcH)
            {
                return CopyTexture(source, isNormalMap);
            }

            var pixels = GetRawPixels(source, isNormalMap);
            if (pixels == null || pixels.Length == 0)
                return null;

            SplitChannels(pixels, srcW * srcH, out var r, out var g, out var b, out var a);
            pixels = null;

            var planX = BuildPlan(srcW, targetWidth);
            var planY = BuildPlan(srcH, targetHeight);

            var rH = new float[targetWidth * srcH];
            var gH = new float[targetWidth * srcH];
            var bH = new float[targetWidth * srcH];
            var aH = new float[targetWidth * srcH];

            Parallel.For(
                0,
                srcH,
                y =>
                {
                    int srcRow = y * srcW;
                    int dstRow = y * targetWidth;
                    ApplyPlanToRow(planX, r, srcRow, rH, dstRow);
                    ApplyPlanToRow(planX, g, srcRow, gH, dstRow);
                    ApplyPlanToRow(planX, b, srcRow, bH, dstRow);
                    ApplyPlanToRow(planX, a, srcRow, aH, dstRow);
                }
            );

            var rF = new float[targetWidth * targetHeight];
            var gF = new float[targetWidth * targetHeight];
            var bF = new float[targetWidth * targetHeight];
            var aF = new float[targetWidth * targetHeight];

            Parallel.For(
                0,
                targetWidth,
                x =>
                {
                    ApplyPlanToColumn(planY, rH, x, targetWidth, rF, x, targetWidth);
                    ApplyPlanToColumn(planY, gH, x, targetWidth, gF, x, targetWidth);
                    ApplyPlanToColumn(planY, bH, x, targetWidth, bF, x, targetWidth);
                    ApplyPlanToColumn(planY, aH, x, targetWidth, aF, x, targetWidth);
                }
            );

            var result = MergeChannels(rF, gF, bF, aF, targetWidth * targetHeight);

            var output = new Texture2D(
                targetWidth,
                targetHeight,
                TextureFormat.RGBA32,
                source.mipmapCount > 1,
                isNormalMap
            );
            output.SetPixels(result);
            output.Apply(source.mipmapCount > 1);
            TextureProcessor.CopyTextureSettings(source, output);

            return output;
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

        private static void ApplyPlanToRow(
            AxisPlan plan,
            float[] src,
            int srcOffset,
            float[] dst,
            int dstOffset
        )
        {
            int dstN = plan.Start.Length;
            for (int i = 0; i < dstN; i++)
            {
                float sum = 0f;
                int off = plan.Offset[i];
                int count = plan.Len[i];
                int s = plan.Start[i];

                for (int t = 0; t < count; t++)
                {
                    sum += src[srcOffset + s + t] * plan.Weights[off + t];
                }

                dst[dstOffset + i] = sum;
            }
        }

        private static void ApplyPlanToColumn(
            AxisPlan plan,
            float[] src,
            int srcCol,
            int srcStride,
            float[] dst,
            int dstCol,
            int dstStride
        )
        {
            int dstN = plan.Start.Length;
            for (int i = 0; i < dstN; i++)
            {
                float sum = 0f;
                int off = plan.Offset[i];
                int count = plan.Len[i];
                int s = plan.Start[i];

                for (int t = 0; t < count; t++)
                {
                    sum += src[(s + t) * srcStride + srcCol] * plan.Weights[off + t];
                }

                dst[i * dstStride + dstCol] = sum;
            }
        }

        private static Color[] GetRawPixels(Texture2D texture, bool isNormalMap = false)
        {
            if (texture.isReadable)
            {
                try
                {
                    return texture.GetPixels();
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] GetPixels failed for '{texture.name}', "
                            + $"falling back to RenderTexture readback: {e.Message}"
                    );
                }
            }

            var colorSpace = isNormalMap
                ? RenderTextureReadWrite.Linear
                : RenderTextureReadWrite.sRGB;

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
                    isNormalMap
                );
                readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                readable.Apply(false);

                return readable.GetPixels();
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

        private static void SplitChannels(
            Color[] pixels,
            int count,
            out float[] r,
            out float[] g,
            out float[] b,
            out float[] a
        )
        {
            r = new float[count];
            g = new float[count];
            b = new float[count];
            a = new float[count];

            for (int i = 0; i < count; i++)
            {
                r[i] = pixels[i].r;
                g[i] = pixels[i].g;
                b[i] = pixels[i].b;
                a[i] = pixels[i].a;
            }
        }

        private static Color[] MergeChannels(float[] r, float[] g, float[] b, float[] a, int count)
        {
            var result = new Color[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = new Color(
                    Mathf.Clamp01(r[i]),
                    Mathf.Clamp01(g[i]),
                    Mathf.Clamp01(b[i]),
                    Mathf.Clamp01(a[i])
                );
            }
            return result;
        }

        private static Texture2D CopyTexture(Texture2D source, bool isNormalMap)
        {
            var pixels = GetRawPixels(source, isNormalMap);
            if (pixels == null)
                return null;

            var output = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                source.mipmapCount > 1,
                isNormalMap
            );
            output.SetPixels(pixels);
            output.Apply(source.mipmapCount > 1);
            TextureProcessor.CopyTextureSettings(source, output);
            return output;
        }
    }
}
