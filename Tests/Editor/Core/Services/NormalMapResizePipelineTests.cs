using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Tests for verifying that normal map data is preserved through the resize pipeline.
    /// These tests generate textures, pass them through the resize pipeline at the same size,
    /// and verify that the normal vector data is not corrupted.
    /// </summary>
    [TestFixture]
    public class NormalMapResizePipelineTests
    {
        private TextureProcessor _processor;

        /// <summary>
        /// Maximum allowed difference per channel after round-trip through resize pipeline.
        /// This accounts for floating-point precision loss during RenderTexture operations.
        /// </summary>
        private const float MaxChannelDifference = 0.02f;

        /// <summary>
        /// Maximum allowed angular difference (in degrees) between original and processed normals.
        /// </summary>
        private const float MaxAngularDifferenceDegrees = 3f;

        [SetUp]
        public void SetUp()
        {
            _processor = new TextureProcessor(32, 2048, false);
        }

        #region Normal Map Pattern Tests

        public enum NormalMapPattern
        {
            // Tangent Space
            Flat,
            Sphere,
            Gradient,
            HighFrequency,
            Checker,
            ExtremeAngle,

            // Object Space
            NegativeZ,
            MixedZ,
            FullSphere,

            // Edge Cases
            Degenerate,
            SharpEdge,
        }

        [Test]
        [TestCase(NormalMapPattern.Flat)]
        [TestCase(NormalMapPattern.Sphere)]
        [TestCase(NormalMapPattern.Gradient)]
        [TestCase(NormalMapPattern.HighFrequency)]
        [TestCase(NormalMapPattern.Checker)]
        [TestCase(NormalMapPattern.ExtremeAngle)]
        [TestCase(NormalMapPattern.NegativeZ)]
        [TestCase(NormalMapPattern.MixedZ)]
        [TestCase(NormalMapPattern.FullSphere)]
        [TestCase(NormalMapPattern.Degenerate)]
        [TestCase(NormalMapPattern.SharpEdge)]
        public void ResizeSameSize_AllPatterns_PreservesData(NormalMapPattern pattern)
        {
            var source = CreateTextureForPattern(pattern, 256);

            var result = _processor.ResizeTo(source, 256, 256, isNormalMap: true);

            AssertNormalMapPreserved(source, result, pattern.ToString());

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        private static Texture2D CreateTextureForPattern(NormalMapPattern pattern, int size)
        {
            return pattern switch
            {
                NormalMapPattern.Flat => NormalMapTestTextureFactory.CreateFlatNormal(size),
                NormalMapPattern.Sphere => NormalMapTestTextureFactory.CreateSphereNormal(size),
                NormalMapPattern.Gradient => NormalMapTestTextureFactory.CreateGradientNormal(size),
                NormalMapPattern.HighFrequency =>
                    NormalMapTestTextureFactory.CreateHighFrequencyNormal(size),
                NormalMapPattern.Checker => NormalMapTestTextureFactory.CreateCheckerNormal(size),
                NormalMapPattern.ExtremeAngle =>
                    NormalMapTestTextureFactory.CreateExtremeAngleNormal(size),
                NormalMapPattern.NegativeZ => NormalMapTestTextureFactory.CreateNegativeZNormal(
                    size
                ),
                NormalMapPattern.MixedZ => NormalMapTestTextureFactory.CreateMixedZNormal(size),
                NormalMapPattern.FullSphere => NormalMapTestTextureFactory.CreateFullSphereNormal(
                    size
                ),
                NormalMapPattern.Degenerate => NormalMapTestTextureFactory.CreateDegenerateNormal(
                    size
                ),
                NormalMapPattern.SharpEdge => NormalMapTestTextureFactory.CreateSharpEdgeNormal(
                    size
                ),
                _ => throw new System.ArgumentException($"Unknown pattern: {pattern}"),
            };
        }

        #endregion

        #region Different Size Tests

        [Test]
        [TestCase(64)]
        [TestCase(128)]
        [TestCase(256)]
        [TestCase(512)]
        public void ResizeSameSize_VariousSizes_PreservesData(int size)
        {
            var source = NormalMapTestTextureFactory.CreateSphereNormal(size);

            var result = _processor.ResizeTo(source, size, size, isNormalMap: true);

            AssertNormalMapPreserved(source, result, $"SphereNormal_{size}");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        [TestCase(512, 256)]
        [TestCase(512, 128)]
        [TestCase(1024, 256)]
        [TestCase(256, 64)]
        public void Downscale_SphereNormal_PreservesNormalDirection(int sourceSize, int targetSize)
        {
            // Bilinear interpolation produces non-normalized vectors (magnitude < 1).
            // This is expected behavior - shaders should call normalize() when sampling normal maps.
            // We only verify that the direction is preserved and Z remains non-negative.
            const float MaxLengthDeviationForDownscale = 0.2f;

            var source = NormalMapTestTextureFactory.CreateSphereNormal(sourceSize);

            var result = _processor.ResizeTo(source, targetSize, targetSize, isNormalMap: true);

            var resultPixels = result.GetPixels();
            int sampleCount = Mathf.Min(500, resultPixels.Length);
            int step = Mathf.Max(1, resultPixels.Length / sampleCount);

            float maxLengthDeviation = 0f;
            int negativeZCount = 0;

            for (int i = 0; i < resultPixels.Length; i += step)
            {
                var normal = NormalMapTestTextureFactory.DecodeNormal(resultPixels[i]);
                float length = normal.magnitude;
                float deviation = Mathf.Abs(length - 1f);

                maxLengthDeviation = Mathf.Max(maxLengthDeviation, deviation);

                if (normal.z < 0f)
                {
                    negativeZCount++;
                }
            }

            Debug.Log(
                $"[Downscale {sourceSize}->{targetSize}] Max length deviation: {maxLengthDeviation:F4}, Negative Z: {negativeZCount}"
            );

            Assert.That(
                maxLengthDeviation,
                Is.LessThanOrEqualTo(MaxLengthDeviationForDownscale),
                $"Downscale {sourceSize}->{targetSize}: Length deviation exceeded tolerance (bilinear interpolation expected)"
            );
            Assert.AreEqual(
                0,
                negativeZCount,
                $"Downscale {sourceSize}->{targetSize}: Tangent space normals should have Z >= 0"
            );

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Non-NormalMap Comparison Tests

        [Test]
        public void ResizeSameSize_WithoutNormalMapFlag_MayCorruptData()
        {
            // This test documents the behavior difference when isNormalMap is false
            // The sRGB color space conversion can corrupt normal map data
            var source = NormalMapTestTextureFactory.CreateSphereNormal(256);

            var resultWithFlag = _processor.ResizeTo(source, 256, 256, isNormalMap: true);
            var resultWithoutFlag = _processor.ResizeTo(source, 256, 256, isNormalMap: false);

            // Compare the two results - they should be different
            var pixelsWithFlag = resultWithFlag.GetPixels();
            var pixelsWithoutFlag = resultWithoutFlag.GetPixels();

            bool hasDifference = false;
            for (int i = 0; i < pixelsWithFlag.Length; i++)
            {
                float diffR = Mathf.Abs(pixelsWithFlag[i].r - pixelsWithoutFlag[i].r);
                float diffG = Mathf.Abs(pixelsWithFlag[i].g - pixelsWithoutFlag[i].g);
                float diffB = Mathf.Abs(pixelsWithFlag[i].b - pixelsWithoutFlag[i].b);

                if (diffR > 0.01f || diffG > 0.01f || diffB > 0.01f)
                {
                    hasDifference = true;
                    break;
                }
            }

            // Note: This may or may not show difference depending on Unity's color space settings
            // The important thing is that isNormalMap=true preserves data correctly
            Debug.Log(
                $"[NormalMapResizePipelineTests] WithFlag vs WithoutFlag difference detected: {hasDifference}"
            );

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(resultWithFlag);
            Object.DestroyImmediate(resultWithoutFlag);
        }

        #endregion


        #region Assertion Helpers

        private void AssertNormalMapPreserved(Texture2D source, Texture2D result, string testName)
        {
            Assert.AreEqual(source.width, result.width, $"{testName}: Width should be preserved");
            Assert.AreEqual(
                source.height,
                result.height,
                $"{testName}: Height should be preserved"
            );

            var sourcePixels = source.GetPixels();
            var resultPixels = result.GetPixels();

            Assert.AreEqual(
                sourcePixels.Length,
                resultPixels.Length,
                $"{testName}: Pixel count should match"
            );

            int sampleCount = Mathf.Min(1000, sourcePixels.Length);
            int step = sourcePixels.Length / sampleCount;

            float maxDiffR = 0f,
                maxDiffG = 0f,
                maxDiffB = 0f;
            float maxAngularDiff = 0f;
            int errorCount = 0;

            for (int i = 0; i < sourcePixels.Length; i += step)
            {
                float diffR = Mathf.Abs(sourcePixels[i].r - resultPixels[i].r);
                float diffG = Mathf.Abs(sourcePixels[i].g - resultPixels[i].g);
                float diffB = Mathf.Abs(sourcePixels[i].b - resultPixels[i].b);

                maxDiffR = Mathf.Max(maxDiffR, diffR);
                maxDiffG = Mathf.Max(maxDiffG, diffG);
                maxDiffB = Mathf.Max(maxDiffB, diffB);

                // Check angular difference
                var sourceNormal = NormalMapTestTextureFactory.DecodeNormal(sourcePixels[i]);
                var resultNormal = NormalMapTestTextureFactory.DecodeNormal(resultPixels[i]);

                float dot = Vector3.Dot(sourceNormal.normalized, resultNormal.normalized);
                dot = Mathf.Clamp(dot, -1f, 1f);
                float angleDiff = Mathf.Acos(dot) * Mathf.Rad2Deg;
                maxAngularDiff = Mathf.Max(maxAngularDiff, angleDiff);

                if (
                    diffR > MaxChannelDifference
                    || diffG > MaxChannelDifference
                    || diffB > MaxChannelDifference
                )
                {
                    errorCount++;
                }
            }

            Debug.Log(
                $"[{testName}] Max channel diff: R={maxDiffR:F4}, G={maxDiffG:F4}, B={maxDiffB:F4}, Max angular diff: {maxAngularDiff:F2}°"
            );

            Assert.That(
                maxDiffR,
                Is.LessThanOrEqualTo(MaxChannelDifference),
                $"{testName}: Red channel difference too large"
            );
            Assert.That(
                maxDiffG,
                Is.LessThanOrEqualTo(MaxChannelDifference),
                $"{testName}: Green channel difference too large"
            );
            Assert.That(
                maxDiffB,
                Is.LessThanOrEqualTo(MaxChannelDifference),
                $"{testName}: Blue channel difference too large"
            );
            Assert.That(
                maxAngularDiff,
                Is.LessThanOrEqualTo(MaxAngularDifferenceDegrees),
                $"{testName}: Angular difference too large"
            );

            Assert.AreEqual(0, errorCount, $"{testName}: {errorCount} pixels exceeded tolerance");
        }

        #endregion
    }
}
