using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Tests that normal map textures survive the resize pipeline
    /// without corruption from gamma correction or quantization.
    /// </summary>
    [TestFixture]
    public class NormalMapResizePipelineTests
    {
        private static bool IsSoftwareRenderer =>
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private TextureProcessor _processor;
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _processor = new TextureProcessor(32, 2048, false);
            _createdObjects = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        #region Linear Color Space

        [Test]
        public void Resize_NormalMap_SameSize_PreservesChannels()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(64);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, 64, 64, isNormalMap: true);
            _createdObjects.Add(result);

            var sourcePixels = source.GetPixels32();
            var resultPixels = result.GetPixels32();

            // Flat normal should be preserved through same-size resize
            Assert.That(resultPixels[0].r, Is.InRange((byte)124, (byte)132));
            Assert.That(resultPixels[0].g, Is.InRange((byte)124, (byte)132));
            Assert.That(resultPixels[0].b, Is.InRange((byte)248, (byte)255));
        }

        [Test]
        public void Resize_NormalMap_HalfSize_PreservesApproximateValues()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(64);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(result);

            var resultPixels = result.GetPixels32();

            // Downsampled flat normal should still be approximately flat
            Assert.That(resultPixels[0].r, Is.InRange((byte)120, (byte)136));
            Assert.That(resultPixels[0].g, Is.InRange((byte)120, (byte)136));
            Assert.That(resultPixels[0].b, Is.InRange((byte)240, (byte)255));
        }

        [Test]
        public void Resize_NormalMap_SpherePattern_ProducesValidVectors()
        {
            var source = NormalMapTestTextureFactory.CreateSphere(64);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(result);

            var pixels = result.GetPixels32();
            int validCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                float x = (pixels[i].r / 255f) * 2f - 1f;
                float y = (pixels[i].g / 255f) * 2f - 1f;
                float z = (pixels[i].b / 255f) * 2f - 1f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                // Allow wider tolerance for resize interpolation
                if (length >= 0.7f && length <= 1.3f)
                {
                    validCount++;
                }
            }

            float validRatio = (float)validCount / pixels.Length;
            Assert.That(
                validRatio,
                Is.GreaterThan(0.8f),
                $"At least 80% of vectors should be roughly unit length, got {validRatio:P0}"
            );
        }

        #endregion

        #region Resize via Analysis Path

        [Test]
        public void Resize_NormalMap_ViaAnalysis_Works()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(64);
            _createdObjects.Add(source);

            var analysis = new TextureAnalysisResult(0.5f, 2, new Vector2Int(32, 32));

            var result = _processor.Resize(source, analysis, isNormalMap: true);
            _createdObjects.Add(result);

            Assert.AreEqual(32, result.width);
            Assert.AreEqual(32, result.height);

            var pixels = result.GetPixels32();
            Assert.That(pixels[0].r, Is.InRange((byte)120, (byte)136));
            Assert.That(pixels[0].g, Is.InRange((byte)120, (byte)136));
        }

        [Test]
        public void Resize_NormalMap_NoDivisor_StillUsesLinearSpace()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(64);
            _createdObjects.Add(source);

            var analysis = new TextureAnalysisResult(0.8f, 1, new Vector2Int(64, 64));

            var result = _processor.Resize(source, analysis, isNormalMap: true);
            _createdObjects.Add(result);

            Assert.AreEqual(64, result.width);
            Assert.AreEqual(64, result.height);

            var pixels = result.GetPixels32();
            // Should be close to source values (not gamma-corrupted)
            Assert.That(pixels[0].r, Is.InRange((byte)124, (byte)132));
            Assert.That(pixels[0].g, Is.InRange((byte)124, (byte)132));
            Assert.That(pixels[0].b, Is.InRange((byte)248, (byte)255));
        }

        #endregion

        #region Non-Normal Map (Regression)

        [Test]
        public void Resize_NonNormalMap_StillWorks()
        {
            var source = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            _createdObjects.Add(source);
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(200, 100, 50, 255);
            }
            source.SetPixels32(pixels);
            source.Apply();

            var result = _processor.ResizeTo(source, 32, 32, isNormalMap: false);
            _createdObjects.Add(result);

            Assert.AreEqual(32, result.width);
            Assert.AreEqual(32, result.height);
        }

        [Test]
        public void Resize_DefaultParameter_TreatsAsNonNormalMap()
        {
            var source = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            _createdObjects.Add(source);
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(200, 100, 50, 255);
            }
            source.SetPixels32(pixels);
            source.Apply();

            // No isNormalMap parameter -> defaults to false
            var result = _processor.ResizeTo(source, 32, 32);
            _createdObjects.Add(result);

            Assert.AreEqual(32, result.width);
            Assert.AreEqual(32, result.height);
        }

        #endregion

        #region Pattern Coverage

        public enum NormalMapPattern
        {
            Flat,
            Sphere,
            Gradient,
            HighFrequency,
            Checker,
            ExtremeAngle,
            NegativeZ,
            MixedZ,
            FullSphere,
            Degenerate,
            SharpEdge,
        }

        [Test]
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
            if (IsSoftwareRenderer)
                Assert.Ignore("Same-size blit precision test requires a GPU renderer.");

            var source = CreateTextureForPattern(pattern, 128);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, 128, 128, isNormalMap: true);
            _createdObjects.Add(result);

            var sourcePixels = source.GetPixels32();
            var resultPixels = result.GetPixels32();

            int sampleCount = Mathf.Min(500, sourcePixels.Length);
            int step = Mathf.Max(1, sourcePixels.Length / sampleCount);
            float maxDiff = 0f;

            for (int i = 0; i < sourcePixels.Length; i += step)
            {
                float diffR = Mathf.Abs(sourcePixels[i].r / 255f - resultPixels[i].r / 255f);
                float diffG = Mathf.Abs(sourcePixels[i].g / 255f - resultPixels[i].g / 255f);
                float diffB = Mathf.Abs(sourcePixels[i].b / 255f - resultPixels[i].b / 255f);
                maxDiff = Mathf.Max(maxDiff, Mathf.Max(diffR, Mathf.Max(diffG, diffB)));
            }

            Assert.That(
                maxDiff,
                Is.LessThanOrEqualTo(0.02f),
                $"{pattern}: Max channel difference should be within tolerance"
            );
        }

        private static Texture2D CreateTextureForPattern(NormalMapPattern pattern, int size)
        {
            switch (pattern)
            {
                case NormalMapPattern.Flat:
                    return NormalMapTestTextureFactory.CreateFlat(size);
                case NormalMapPattern.Sphere:
                    return NormalMapTestTextureFactory.CreateSphere(size);
                case NormalMapPattern.Gradient:
                    return NormalMapTestTextureFactory.CreateGradient(size);
                case NormalMapPattern.HighFrequency:
                    return NormalMapTestTextureFactory.CreateHighFrequency(size);
                case NormalMapPattern.Checker:
                    return NormalMapTestTextureFactory.CreateChecker(size);
                case NormalMapPattern.ExtremeAngle:
                    return NormalMapTestTextureFactory.CreateExtremeAngle(size);
                case NormalMapPattern.NegativeZ:
                    return NormalMapTestTextureFactory.CreateNegativeZ(size);
                case NormalMapPattern.MixedZ:
                    return NormalMapTestTextureFactory.CreateMixedZ(size);
                case NormalMapPattern.FullSphere:
                    return NormalMapTestTextureFactory.CreateFullSphere(size);
                case NormalMapPattern.Degenerate:
                    return NormalMapTestTextureFactory.CreateDegenerate(size);
                case NormalMapPattern.SharpEdge:
                    return NormalMapTestTextureFactory.CreateSharpEdge(size);
                default:
                    throw new System.ArgumentException($"Unknown pattern: {pattern}");
            }
        }

        #endregion

        #region Various Sizes

        [Test]
        [TestCase(64)]
        [TestCase(128)]
        [TestCase(256)]
        [TestCase(512)]
        public void ResizeSameSize_VariousSizes_PreservesData(int size)
        {
            if (IsSoftwareRenderer)
                Assert.Ignore("Same-size blit precision test requires a GPU renderer.");

            var source = NormalMapTestTextureFactory.CreateSphere(size);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, size, size, isNormalMap: true);
            _createdObjects.Add(result);

            var sourcePixels = source.GetPixels32();
            var resultPixels = result.GetPixels32();

            int sampleCount = Mathf.Min(500, sourcePixels.Length);
            int step = Mathf.Max(1, sourcePixels.Length / sampleCount);
            float maxDiff = 0f;

            for (int i = 0; i < sourcePixels.Length; i += step)
            {
                float diffR = Mathf.Abs(sourcePixels[i].r / 255f - resultPixels[i].r / 255f);
                float diffG = Mathf.Abs(sourcePixels[i].g / 255f - resultPixels[i].g / 255f);
                float diffB = Mathf.Abs(sourcePixels[i].b / 255f - resultPixels[i].b / 255f);
                maxDiff = Mathf.Max(maxDiff, Mathf.Max(diffR, Mathf.Max(diffG, diffB)));
            }

            Assert.That(
                maxDiff,
                Is.LessThanOrEqualTo(0.02f),
                $"Sphere {size}x{size}: Max channel difference should be within tolerance"
            );
        }

        [Test]
        public void ResizeSameSize_SphereNormal_WhenARGBFloatSupported_PreservesData()
        {
            if (IsSoftwareRenderer)
                Assert.Ignore("Same-size blit precision test requires a GPU renderer.");

            if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
            {
                Assert.Ignore("ARGBFloat RenderTexture is not supported on this environment.");
            }

            var source = NormalMapTestTextureFactory.CreateSphere(128);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, 128, 128, isNormalMap: true);
            _createdObjects.Add(result);

            var sourcePixels = source.GetPixels32();
            var resultPixels = result.GetPixels32();

            int sampleCount = Mathf.Min(500, sourcePixels.Length);
            int step = Mathf.Max(1, sourcePixels.Length / sampleCount);
            float maxDiff = 0f;

            for (int i = 0; i < sourcePixels.Length; i += step)
            {
                float diffR = Mathf.Abs(sourcePixels[i].r / 255f - resultPixels[i].r / 255f);
                float diffG = Mathf.Abs(sourcePixels[i].g / 255f - resultPixels[i].g / 255f);
                float diffB = Mathf.Abs(sourcePixels[i].b / 255f - resultPixels[i].b / 255f);
                maxDiff = Mathf.Max(maxDiff, Mathf.Max(diffR, Mathf.Max(diffG, diffB)));
            }

            Assert.That(
                maxDiff,
                Is.LessThanOrEqualTo(0.02f),
                "ARGBFloat path: Max channel difference should be within tolerance"
            );
        }

        #endregion

        #region Downscale Quality Validation

        [Test]
        [TestCase(512, 256)]
        [TestCase(512, 128)]
        [TestCase(1024, 256)]
        [TestCase(256, 64)]
        public void Downscale_SphereNormal_PreservesNormalDirection(int sourceSize, int targetSize)
        {
            if (IsSoftwareRenderer)
                Assert.Ignore("Downscale precision test requires a GPU renderer.");

            var source = NormalMapTestTextureFactory.CreateSphere(sourceSize);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, targetSize, targetSize, isNormalMap: true);
            _createdObjects.Add(result);

            var resultPixels = result.GetPixels32();
            int sampleCount = Mathf.Min(500, resultPixels.Length);
            int step = Mathf.Max(1, resultPixels.Length / sampleCount);

            float maxLengthDeviation = 0f;

            for (int i = 0; i < resultPixels.Length; i += step)
            {
                float x = (resultPixels[i].r / 255f) * 2f - 1f;
                float y = (resultPixels[i].g / 255f) * 2f - 1f;
                float z = (resultPixels[i].b / 255f) * 2f - 1f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                float deviation = Mathf.Abs(length - 1f);
                maxLengthDeviation = Mathf.Max(maxLengthDeviation, deviation);
            }

            Assert.That(
                maxLengthDeviation,
                Is.LessThanOrEqualTo(0.2f),
                $"Downscale {sourceSize}->{targetSize}: Length deviation exceeded tolerance"
            );
        }

        #endregion

        #region Non-NormalMap Comparison

        [Test]
        public void ResizeSameSize_WithoutNormalMapFlag_MayCorruptData()
        {
            var source = NormalMapTestTextureFactory.CreateSphere(128);
            _createdObjects.Add(source);

            var resultWithFlag = _processor.ResizeTo(source, 128, 128, isNormalMap: true);
            _createdObjects.Add(resultWithFlag);

            var resultWithoutFlag = _processor.ResizeTo(source, 128, 128, isNormalMap: false);
            _createdObjects.Add(resultWithoutFlag);

            var sourcePixels = source.GetPixels32();
            var pixelsWithFlag = resultWithFlag.GetPixels32();
            var pixelsWithoutFlag = resultWithoutFlag.GetPixels32();

            // Compute angular errors for both paths
            float withFlagErrorSum = 0f;
            float withoutFlagErrorSum = 0f;
            int count = 0;
            int sampleCount = Mathf.Min(500, sourcePixels.Length);
            int step = Mathf.Max(1, sourcePixels.Length / sampleCount);

            for (int i = 0; i < sourcePixels.Length; i += step)
            {
                var srcN = NormalMapTestTextureFactory.DecodeNormal(sourcePixels[i]);
                var withN = NormalMapTestTextureFactory.DecodeNormal(pixelsWithFlag[i]);
                var withoutN = NormalMapTestTextureFactory.DecodeNormal(pixelsWithoutFlag[i]);

                float dot1 = Mathf.Clamp(Vector3.Dot(srcN.normalized, withN.normalized), -1f, 1f);
                float dot2 = Mathf.Clamp(
                    Vector3.Dot(srcN.normalized, withoutN.normalized),
                    -1f,
                    1f
                );
                withFlagErrorSum += Mathf.Acos(dot1) * Mathf.Rad2Deg;
                withoutFlagErrorSum += Mathf.Acos(dot2) * Mathf.Rad2Deg;
                count++;
            }

            float withFlagAvg = count > 0 ? withFlagErrorSum / count : 0f;
            float withoutFlagAvg = count > 0 ? withoutFlagErrorSum / count : 0f;

            // The normal-map path must not be less accurate than the default path
            Assert.That(
                withFlagAvg,
                Is.LessThanOrEqualTo(withoutFlagAvg + 0.1f),
                "isNormalMap=true path should be at least as accurate as default resize path"
            );
        }

        #endregion

        #region High Precision

        [Test]
        public void Resize_NormalMap_GradientPattern_MaintainsPrecision()
        {
            var source = NormalMapTestTextureFactory.CreateGradient(64);
            _createdObjects.Add(source);

            var result = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(result);

            var sourcePixels = source.GetPixels32();
            var resultPixels = result.GetPixels32();

            // Check that gradient is preserved (left side vs right side)
            int leftIdx = 0;
            int rightIdx = 31;
            int midY = 16;

            byte leftR = resultPixels[midY * 32 + leftIdx].r;
            byte rightR = resultPixels[midY * 32 + rightIdx].r;

            // Gradient goes from negative X to positive X
            Assert.That(rightR, Is.GreaterThan(leftR), "Gradient direction should be preserved");
        }

        #endregion
    }
}
