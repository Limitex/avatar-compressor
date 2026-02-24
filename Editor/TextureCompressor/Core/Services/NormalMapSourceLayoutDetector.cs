using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Resolves source channel layout for normal-map textures.
    /// For unambiguous formats (BC5, uncompressed), the layout is deterministic.
    /// For ambiguous formats (DXT5, BC7), pixel heuristics are used to distinguish
    /// between AG (DXTnm), RG, and RGB channel layouts.
    /// </summary>
    public static class NormalMapSourceLayoutDetector
    {
        /// <summary>Maximum number of pixels to sample for heuristic analysis.</summary>
        private const int MaxSamplePixels = 4096;

        /// <summary>
        /// Tolerance for unit vector length² check (x²+y² ≤ this value).
        /// Slightly above 1.0 to account for 8-bit quantization error.
        /// </summary>
        private const float UnitVectorLengthSqTolerance = 1.02f;

        /// <summary>
        /// Tolerance for Z² being slightly negative due to 8-bit quantization.
        /// When x²+y² slightly exceeds 1.0, Z² becomes negative; this tolerance
        /// allows such pixels to still be considered for Z consistency checks.
        /// </summary>
        private const float NegativeZSqTolerance = -0.02f;

        /// <summary>
        /// Byte threshold for R/B channels to be considered "near 1.0".
        /// DXTnm format pads R and B to ~255 as constants.
        /// </summary>
        private const byte NearOneByteThreshold = 250;

        /// <summary>
        /// Minimum |Z| value to classify a pixel as definitively positive or negative Z.
        /// Values in [-threshold, +threshold] are considered indeterminate.
        /// </summary>
        private const float ZSignThreshold = 0.2f;

        /// <summary>
        /// Maximum allowed difference between B-channel Z and reconstructed Z (signed)
        /// for a pixel to be considered consistent with RGB layout.
        /// </summary>
        private const float SignedZMatchTolerance = 0.25f;

        /// <summary>
        /// Maximum allowed difference between |B-channel Z| and reconstructed |Z|
        /// for a pixel to be considered consistent with RGB layout (ignoring sign).
        /// </summary>
        private const float AbsoluteZMatchTolerance = 0.2f;

        /// <summary>
        /// Minimum range (max - min) of Z values in the B channel to confirm
        /// it carries actual Z data rather than a constant (e.g., B=0 in RG-layout).
        /// </summary>
        private const float MinBChannelVariance = 0.1f;

        /// <summary>
        /// Resolves source normal-map layout from format and, for DXT5/BC7,
        /// pixel heuristics when the source layout is ambiguous.
        /// </summary>
        /// <param name="originalTexture">The original texture (preferred for detection if readable)</param>
        /// <param name="resizedTexture">The resized texture (fallback for detection)</param>
        /// <param name="format">The original texture format</param>
        public static NormalMapPreprocessor.SourceLayout Resolve(
            Texture2D originalTexture,
            Texture2D resizedTexture,
            TextureFormat format
        )
        {
            switch (format)
            {
                case TextureFormat.BC5:
                    return NormalMapPreprocessor.SourceLayout.RG;
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC7:
                    // Heuristic detection requires reading pixel data directly.
                    // Non-readable textures cannot be sampled, so fall back to AG
                    // which Unity's normal map import always produces for DXT5/BC7.
                    if (originalTexture != null && originalTexture.isReadable)
                        return DetectDXTnmLike(originalTexture);
                    return NormalMapPreprocessor.SourceLayout.AG;
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

                int sampleCount = Mathf.Min(pixels.Length, MaxSamplePixels);
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
                float zFromBMin = float.MaxValue;
                float zFromBMax = float.MinValue;
                int total = 0;

                for (int i = 0; i < pixels.Length; i += step)
                {
                    var p = pixels[i];

                    float xFromR = (p.r / 255f) * 2f - 1f;
                    float yFromG = (p.g / 255f) * 2f - 1f;
                    float zFromB = (p.b / 255f) * 2f - 1f;
                    float xFromA = (p.a / 255f) * 2f - 1f;

                    zFromBMin = Mathf.Min(zFromBMin, zFromB);
                    zFromBMax = Mathf.Max(zFromBMax, zFromB);

                    if (zFromB <= -ZSignThreshold)
                    {
                        zNegativeCount++;
                    }
                    else if (zFromB >= ZSignThreshold)
                    {
                        zPositiveCount++;
                    }

                    if (xFromR * xFromR + yFromG * yFromG <= UnitVectorLengthSqTolerance)
                    {
                        validRgCount++;
                    }

                    if (xFromA * xFromA + yFromG * yFromG <= UnitVectorLengthSqTolerance)
                    {
                        validAgCount++;
                    }

                    if (p.r >= NearOneByteThreshold && p.b >= NearOneByteThreshold)
                    {
                        rbNearOneCount++;
                    }

                    float zFromRgSq = 1f - xFromR * xFromR - yFromG * yFromG;
                    float zFromRg = zFromRgSq > 0f ? Mathf.Sqrt(zFromRgSq) : 0f;
                    if (
                        zFromRgSq >= NegativeZSqTolerance
                        && Mathf.Abs(zFromB - zFromRg) <= SignedZMatchTolerance
                    )
                    {
                        rgbSignedConsistentCount++;
                    }
                    if (
                        zFromRgSq >= NegativeZSqTolerance
                        && Mathf.Abs(Mathf.Abs(zFromB) - zFromRg) <= AbsoluteZMatchTolerance
                    )
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
                bool mixedSignedZ =
                    zNegativeRatio >= ZSignThreshold && zPositiveRatio >= ZSignThreshold;
                // When alpha is consistently opaque, xFromA=1 makes AG validation fail
                // for most non-flat normals, inflating rgAdvantage. In this case rgAdvantage
                // is not a reliable discriminator, so we relax the guard.
                bool alphaInflatesRgAdvantage = alphaNearOneRatio >= 0.9f; // ≥90% opaque
                bool bChannelHasVariance = (zFromBMax - zFromBMin) >= MinBChannelVariance;
                bool strongSingleNegativeSignedZ =
                    zNegativeRatio >= 0.9f // ≥90% negative Z (all back-facing)
                    && zPositiveRatio <= 0.05f // ≤5% positive Z (near-zero front-facing)
                    && rgbAbsConsistencyRatio >= 0.9f // ≥90% Z consistent with XY
                    && (rgAdvantage <= 0.05f || (alphaInflatesRgAdvantage && bChannelHasVariance));
                bool strongRgbEvidence =
                    rbNearOneRatio < 0.9f // R/B are NOT DXTnm constants
                    && rgbSignedConsistencyRatio >= 0.85f; // ≥85% Z matches reconstruction

                // Decision tree (evaluated in priority order)
                // Threshold rationale:
                //   ≥0.90 = strong/dominant evidence (high confidence)
                //   ≥0.85 = strong evidence
                //   ≥0.75 = moderate evidence
                //   ≥0.70 = moderate-weak evidence (with corroborating signals)
                //   ≥0.05 = minimal presence (enough to be non-negligible)

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
                if (
                    rbNearOneRatio < 0.9f
                    && mixedSignedZ
                    && rgbAbsConsistencyRatio >= 0.7f
                    && (rgAdvantage <= 0.05f || alphaInflatesRgAdvantage)
                )
                {
                    return NormalMapPreprocessor.SourceLayout.RGB;
                }

                // Some object-space RGB normals can be single-sign negative-Z (all back-facing)
                // while alpha remains fully opaque.
                if (rbNearOneRatio < 0.9f && strongSingleNegativeSignedZ)
                {
                    return NormalMapPreprocessor.SourceLayout.RGB;
                }

                // RG layout/BC5-style data frequently keeps alpha near 1.0.
                // Prefer RG when alpha is consistently opaque and RG is not less plausible than AG.
                if (
                    alphaNearOneRatio >= 0.9f
                    && validRgRatio >= 0.75f
                    && rgAdvantage >= -0.05f
                    && !strongRgbEvidence
                )
                {
                    return NormalMapPreprocessor.SourceLayout.RG;
                }

                // RG is clearly more plausible than AG (≥12% advantage exceeds quantization noise).
                if (validRgRatio >= 0.85f && rgAdvantage >= 0.12f)
                {
                    return NormalMapPreprocessor.SourceLayout.RG;
                }

                // If explicit signed Z in B is plausible and alpha contains meaningful non-opaque values,
                // prioritize RGB to preserve semantic alpha.
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
            catch (System.Exception)
            {
                return NormalMapPreprocessor.SourceLayout.AG;
            }
        }
    }
}
