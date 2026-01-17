using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Specialized analyzer for Normal Map textures.
    /// Measures normal vector variation instead of color variance.
    /// </summary>
    public class NormalMapAnalyzer : ITextureComplexityAnalyzer
    {
        public TextureComplexityResult Analyze(ProcessedPixelData data)
        {
            float score = CalculateNormalMapComplexity(data.OpaquePixels, data.Width, data.Height);
            return new TextureComplexityResult(
                score,
                "Normal map complexity based on vector variation"
            );
        }

        private float CalculateNormalMapComplexity(Color[] pixels, int width, int height)
        {
            if (
                width < AnalysisConstants.MinNormalMapDimension
                || height < AnalysisConstants.MinNormalMapDimension
            )
            {
                return AnalysisConstants.DefaultComplexityScore;
            }

            if (pixels.Length != width * height)
            {
                return AnalysisConstants.DefaultComplexityScore;
            }

            float totalVariation = 0f;
            int count = 0;

            for (int y = 1; y < height - 1; y += AnalysisConstants.NormalMapSampleStep)
            {
                for (int x = 1; x < width - 1; x += AnalysisConstants.NormalMapSampleStep)
                {
                    int idx = y * width + x;
                    int idxRight = idx + 1;
                    int idxLeft = idx - 1;
                    int idxDown = idx + width;
                    int idxUp = idx - width;

                    if (idxDown >= pixels.Length || idxUp < 0)
                        continue;

                    Vector3 n0 = DecodeNormal(pixels[idx]);
                    Vector3 n1 = DecodeNormal(pixels[idxRight]);
                    Vector3 n2 = DecodeNormal(pixels[idxDown]);
                    Vector3 n3 = DecodeNormal(pixels[idxLeft]);
                    Vector3 n4 = DecodeNormal(pixels[idxUp]);

                    float variation = 0f;
                    variation += 1f - Vector3.Dot(n0, n1);
                    variation += 1f - Vector3.Dot(n0, n2);
                    variation += 1f - Vector3.Dot(n0, n3);
                    variation += 1f - Vector3.Dot(n0, n4);

                    totalVariation += variation / 4f;
                    count++;
                }
            }

            float avgVariation = count > 0 ? totalVariation / count : 0f;

            return Mathf.Clamp01(avgVariation * AnalysisConstants.NormalMapVariationMultiplier);
        }

        private Vector3 DecodeNormal(Color c)
        {
            Vector3 n = new Vector3(c.r * 2f - 1f, c.g * 2f - 1f, c.b * 2f - 1f);
            return n.normalized;
        }
    }
}
