using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Resolves source channel layout for normal-map textures.
    /// </summary>
    public static class NormalMapSourceLayoutDetector
    {
        /// <summary>
        /// Resolves source normal-map layout from format and, for DXT5/BC7,
        /// pixel heuristics when the source layout is ambiguous.
        /// </summary>
        public static NormalMapPreprocessor.SourceLayout Resolve(
            Texture2D originalTexture,
            Texture2D resizedTexture,
            TextureFormat format
        )
        {
            var detectionTexture =
                originalTexture != null && originalTexture.isReadable
                    ? originalTexture
                    : resizedTexture;

            switch (format)
            {
                case TextureFormat.BC5:
                    return NormalMapPreprocessor.SourceLayout.RG;
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC7:
                    return DetectDXTnmLike(detectionTexture);
                default:
                    return NormalMapPreprocessor.SourceLayout.RGB;
            }
        }

        /// <summary>
        /// Detects whether DXT5/BC7 source pixels are in AG (DXTnm), RG, or RGB layout.
        /// Defaults to AG unless non-DXTnm evidence is strong to preserve compatibility.
        /// </summary>
        public static NormalMapPreprocessor.SourceLayout DetectDXTnmLike(Texture2D texture)
        {
            if (texture == null || !texture.isReadable)
            {
                return NormalMapPreprocessor.SourceLayout.AG;
            }

            try
            {
                var pixels = texture.GetPixels32();
                if (pixels.Length == 0)
                {
                    return NormalMapPreprocessor.SourceLayout.AG;
                }

                int sampleCount = Mathf.Min(pixels.Length, 4096);
                int step = Mathf.Max(1, pixels.Length / sampleCount);

                int validRgCount = 0;
                int validAgCount = 0;
                int rbNearOneCount = 0;
                int rgbSignedConsistentCount = 0;
                int rgbAbsConsistentCount = 0;
                int alphaNonOpaqueCount = 0;
                int alphaNearOneCount = 0;
                int zNegativeCount = 0;
                int zPositiveCount = 0;
                int total = 0;

                for (int i = 0; i < pixels.Length; i += step)
                {
                    var p = pixels[i];

                    float xFromR = (p.r / 255f) * 2f - 1f;
                    float yFromG = (p.g / 255f) * 2f - 1f;
                    float zFromB = (p.b / 255f) * 2f - 1f;
                    float xFromA = (p.a / 255f) * 2f - 1f;

                    if (zFromB <= -0.2f)
                    {
                        zNegativeCount++;
                    }
                    else if (zFromB >= 0.2f)
                    {
                        zPositiveCount++;
                    }

                    if (xFromR * xFromR + yFromG * yFromG <= 1.02f)
                    {
                        validRgCount++;
                    }

                    if (xFromA * xFromA + yFromG * yFromG <= 1.02f)
                    {
                        validAgCount++;
                    }

                    if (p.r >= 250 && p.b >= 250)
                    {
                        rbNearOneCount++;
                    }

                    float zFromRgSq = 1f - xFromR * xFromR - yFromG * yFromG;
                    float zFromRg = zFromRgSq > 0f ? Mathf.Sqrt(zFromRgSq) : 0f;
                    if (zFromRgSq >= -0.02f && Mathf.Abs(zFromB - zFromRg) <= 0.25f)
                    {
                        rgbSignedConsistentCount++;
                    }
                    if (zFromRgSq >= -0.02f && Mathf.Abs(Mathf.Abs(zFromB) - zFromRg) <= 0.2f)
                    {
                        rgbAbsConsistentCount++;
                    }

                    if (p.a < AnalysisConstants.SignificantAlphaThreshold)
                    {
                        alphaNonOpaqueCount++;
                    }
                    else
                    {
                        alphaNearOneCount++;
                    }

                    total++;
                }

                float validRgRatio = (float)validRgCount / total;
                float validAgRatio = (float)validAgCount / total;
                float rbNearOneRatio = (float)rbNearOneCount / total;
                float rgbSignedConsistencyRatio = (float)rgbSignedConsistentCount / total;
                float rgbAbsConsistencyRatio = (float)rgbAbsConsistentCount / total;
                float alphaNonOpaqueRatio = (float)alphaNonOpaqueCount / total;
                float alphaNearOneRatio = (float)alphaNearOneCount / total;
                float zNegativeRatio = (float)zNegativeCount / total;
                float zPositiveRatio = (float)zPositiveCount / total;
                float rgAdvantage = validRgRatio - validAgRatio;
                bool mixedSignedZ = zNegativeRatio >= 0.2f && zPositiveRatio >= 0.2f;
                bool strongSingleNegativeSignedZ =
                    zNegativeRatio >= 0.9f
                    && zPositiveRatio <= 0.05f
                    && rgbAbsConsistencyRatio >= 0.9f
                    && rgAdvantage <= 0.05f;
                bool strongRgbEvidence =
                    rbNearOneRatio < 0.9f && rgbSignedConsistencyRatio >= 0.85f;

                // Strong DXTnm AG signature: R/B are near 1 while XY validity matches AG.
                if (rbNearOneRatio >= 0.9f && validAgRatio >= 0.75f)
                {
                    return NormalMapPreprocessor.SourceLayout.AG;
                }

                // AG is substantially more plausible than RG.
                if (validAgRatio >= 0.9f && rgAdvantage <= -0.1f)
                {
                    return NormalMapPreprocessor.SourceLayout.AG;
                }

                // RGB object-space data can contain both positive and negative signed Z in B.
                // Evaluate this before opaque-alpha RG preference to avoid discarding signed Z.
                if (
                    rbNearOneRatio < 0.9f
                    && mixedSignedZ
                    && rgbAbsConsistencyRatio >= 0.7f
                    && rgAdvantage <= 0.05f
                )
                {
                    return NormalMapPreprocessor.SourceLayout.RGB;
                }

                // Some object-space RGB normals can be single-sign negative-Z (all back-facing)
                // while alpha remains fully opaque. Preserve signed Z in this case.
                if (rbNearOneRatio < 0.9f && strongSingleNegativeSignedZ)
                {
                    return NormalMapPreprocessor.SourceLayout.RGB;
                }

                // RG layout/BC5-style data frequently keeps alpha near 1.0.
                // Prefer RG when alpha is consistently opaque and RG is not less plausible than AG.
                // Skip this preference when explicit RGB Z evidence is strong.
                if (
                    alphaNearOneRatio >= 0.9f
                    && validRgRatio >= 0.75f
                    && rgAdvantage >= -0.05f
                    && !strongRgbEvidence
                )
                {
                    return NormalMapPreprocessor.SourceLayout.RG;
                }

                // RG is clearly more plausible than AG and avoids treating undefined B as signed Z.
                if (validRgRatio >= 0.85f && rgAdvantage >= 0.12f)
                {
                    return NormalMapPreprocessor.SourceLayout.RG;
                }

                // If explicit signed Z in B is plausible and alpha contains meaningful non-opaque values,
                // prioritize RGB to preserve semantic alpha (e.g., cutout/mask textures).
                if (
                    rbNearOneRatio < 0.9f
                    && rgbSignedConsistencyRatio >= 0.7f
                    && alphaNonOpaqueRatio >= 0.05f
                )
                {
                    return NormalMapPreprocessor.SourceLayout.RGB;
                }

                // RGB stores an explicit Z in B that should roughly match reconstructed Z from RG.
                if (rbNearOneRatio < 0.9f && rgbSignedConsistencyRatio >= 0.7f)
                {
                    return NormalMapPreprocessor.SourceLayout.RGB;
                }

                return NormalMapPreprocessor.SourceLayout.AG;
            }
            catch
            {
                return NormalMapPreprocessor.SourceLayout.AG;
            }
        }
    }
}
