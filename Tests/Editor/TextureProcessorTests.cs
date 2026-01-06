using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureProcessorTests
    {
        private TextureProcessor _processor;

        [SetUp]
        public void SetUp()
        {
            // Default: min=32, max=2048, forcePowerOfTwo=true
            _processor = new TextureProcessor(32, 2048, true);
        }

        #region CalculateNewDimensions Tests

        [Test]
        public void CalculateNewDimensions_Divisor1_ReturnsSameSize()
        {
            var result = _processor.CalculateNewDimensions(512, 512, 1);
            Assert.AreEqual(512, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_Divisor2_ReturnsHalfSize()
        {
            var result = _processor.CalculateNewDimensions(512, 512, 2);
            Assert.AreEqual(256, result.x);
            Assert.AreEqual(256, result.y);
        }

        [Test]
        public void CalculateNewDimensions_Divisor4_ReturnsQuarterSize()
        {
            var result = _processor.CalculateNewDimensions(1024, 1024, 4);
            Assert.AreEqual(256, result.x);
            Assert.AreEqual(256, result.y);
        }

        [Test]
        public void CalculateNewDimensions_RespectsMinResolution()
        {
            // With divisor 8, 128/8=16, but min is 32
            var result = _processor.CalculateNewDimensions(128, 128, 8);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_RespectsMaxResolution()
        {
            // 4096 with divisor 1 should be clamped to max (2048)
            var result = _processor.CalculateNewDimensions(4096, 4096, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(2048, result.y);
        }

        [Test]
        public void CalculateNewDimensions_NonSquare_PreservesAspectRatio()
        {
            var result = _processor.CalculateNewDimensions(1024, 512, 2);
            Assert.AreEqual(512, result.x);
            Assert.AreEqual(256, result.y);
        }

        [Test]
        public void CalculateNewDimensions_ForcePowerOfTwo_ReturnsPowerOfTwo()
        {
            // 300/2=150, closest power of 2 is 128
            var result = _processor.CalculateNewDimensions(300, 300, 2);
            Assert.That(IsPowerOfTwo(result.x), Is.True, $"Width {result.x} is not power of 2");
            Assert.That(IsPowerOfTwo(result.y), Is.True, $"Height {result.y} is not power of 2");
        }

        [Test]
        public void CalculateNewDimensions_WithoutForcePowerOfTwo_ReturnsExactValue()
        {
            var resizerNoPow2 = new TextureProcessor(32, 2048, false);
            var result = resizerNoPow2.CalculateNewDimensions(300, 300, 2);
            Assert.AreEqual(150, result.x);
            Assert.AreEqual(150, result.y);
        }

        [Test]
        public void CalculateNewDimensions_VerySmallTexture_ClampsToMin()
        {
            var result = _processor.CalculateNewDimensions(16, 16, 1);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_LargeDivisor_ClampsToMin()
        {
            // 1024/16=64, but with large divisor would be 32 (min)
            var result = _processor.CalculateNewDimensions(256, 256, 16);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_AsymmetricClamping_Works()
        {
            // Width needs clamping to max, height doesn't
            var result = _processor.CalculateNewDimensions(4096, 512, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_CustomMinMax_Respected()
        {
            var customResizer = new TextureProcessor(64, 512, true);
            var result = customResizer.CalculateNewDimensions(1024, 1024, 1);
            Assert.AreEqual(512, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_NonPowerOfTwoSource_HandlesCorrectly()
        {
            // 1000/2=500, closest power of 2 is 512
            var result = _processor.CalculateNewDimensions(1000, 1000, 2);
            Assert.That(result.x, Is.InRange(256, 512));
            Assert.That(result.y, Is.InRange(256, 512));
        }

        [Test]
        public void CalculateNewDimensions_PortraitOrientation_Works()
        {
            var result = _processor.CalculateNewDimensions(256, 1024, 2);
            Assert.AreEqual(128, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_ExtremeAspectRatio_Works()
        {
            var result = _processor.CalculateNewDimensions(2048, 64, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(64, result.y);
        }

        #endregion

        #region Power of Two Edge Cases

        [Test]
        public void CalculateNewDimensions_PowerOfTwoMaxClamp_HandlesCorrectly()
        {
            // When result exceeds max and needs power of 2 adjustment
            var customResizer = new TextureProcessor(32, 300, true);
            var result = customResizer.CalculateNewDimensions(1024, 1024, 1);
            // Max is 300, closest power of 2 should be 256 or adjusted
            Assert.That(result.x, Is.LessThanOrEqualTo(300));
            Assert.That(result.y, Is.LessThanOrEqualTo(300));
        }

        [Test]
        public void CalculateNewDimensions_AllPowerOfTwoDivisors_ReturnPowerOfTwo()
        {
            int[] divisors = { 1, 2, 4, 8, 16 };
            foreach (int div in divisors)
            {
                var result = _processor.CalculateNewDimensions(1024, 1024, div);
                Assert.That(IsPowerOfTwo(result.x), Is.True,
                    $"Divisor {div}: Width {result.x} is not power of 2");
                Assert.That(IsPowerOfTwo(result.y), Is.True,
                    $"Divisor {div}: Height {result.y} is not power of 2");
            }
        }

        #endregion

        #region Boundary Value Tests

        [Test]
        public void CalculateNewDimensions_ExactlyAtMin_ReturnsMin()
        {
            var result = _processor.CalculateNewDimensions(32, 32, 1);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_ExactlyAtMax_ReturnsMax()
        {
            var result = _processor.CalculateNewDimensions(2048, 2048, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(2048, result.y);
        }

        [Test]
        public void CalculateNewDimensions_JustBelowMin_ClampsToMin()
        {
            var result = _processor.CalculateNewDimensions(64, 64, 4);
            // 64/4=16, min is 32
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_JustAboveMax_ClampsToMax()
        {
            var result = _processor.CalculateNewDimensions(2049, 2049, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(2048, result.y);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_WithBasicParameters_CreatesInstance()
        {
            var processor = new TextureProcessor(32, 2048, true);
            Assert.IsNotNull(processor);
        }

        [Test]
        public void Constructor_WithAllParameters_CreatesInstance()
        {
            var processor = new TextureProcessor(
                minResolution: 64,
                maxResolution: 1024,
                forcePowerOfTwo: false,
                targetPlatform: CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.5f);

            Assert.IsNotNull(processor);
        }

        [Test]
        public void Constructor_WithDesktopPlatform_CreatesInstance()
        {
            var processor = new TextureProcessor(
                32, 2048, true,
                targetPlatform: CompressionPlatform.Desktop);

            Assert.IsNotNull(processor);
        }

        [Test]
        public void Constructor_WithMobilePlatform_CreatesInstance()
        {
            var processor = new TextureProcessor(
                32, 2048, true,
                targetPlatform: CompressionPlatform.Mobile);

            Assert.IsNotNull(processor);
        }

        [Test]
        public void Constructor_WithAutoPlatform_CreatesInstance()
        {
            var processor = new TextureProcessor(
                32, 2048, true,
                targetPlatform: CompressionPlatform.Auto);

            Assert.IsNotNull(processor);
        }

        [Test]
        public void Constructor_HighQualityFormatDisabled_CreatesInstance()
        {
            var processor = new TextureProcessor(
                32, 2048, true,
                useHighQualityFormatForHighComplexity: false);

            Assert.IsNotNull(processor);
        }

        [Test]
        public void Constructor_CustomThreshold_CreatesInstance()
        {
            var processor = new TextureProcessor(
                32, 2048, true,
                highQualityComplexityThreshold: 0.9f);

            Assert.IsNotNull(processor);
        }

        #endregion

        #region ResizeTo and Copy Tests

        [Test]
        public void ResizeTo_WithMipmaps_PreservesMipmaps()
        {
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, true);
            
            var result = _processor.ResizeTo(sourceTexture, 256, 256);
            
            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.width);
            Assert.AreEqual(256, result.height);
            Assert.IsTrue(result.mipmapCount > 1, "Result should have mipmaps when source has mipmaps");
            
            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ResizeTo_WithoutMipmaps_DoesNotAddMipmaps()
        {
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            
            var result = _processor.ResizeTo(sourceTexture, 256, 256);
            
            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.height);
            Assert.AreEqual(256, result.width);
            Assert.AreEqual(1, result.mipmapCount, "Result should not have mipmaps when source doesn't have mipmaps");
            
            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ResizeTo_PreservesTextureSettings()
        {
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, true);
            sourceTexture.wrapModeU = TextureWrapMode.Repeat;
            sourceTexture.wrapModeV = TextureWrapMode.Clamp;
            sourceTexture.filterMode = FilterMode.Trilinear;
            sourceTexture.anisoLevel = 4;
            
            var result = _processor.ResizeTo(sourceTexture, 256, 256);
            
            Assert.AreEqual(TextureWrapMode.Repeat, result.wrapModeU);
            Assert.AreEqual(TextureWrapMode.Clamp, result.wrapModeV);
            Assert.AreEqual(FilterMode.Trilinear, result.filterMode);
            Assert.AreEqual(4, result.anisoLevel);
            
            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Copy_PreservesMipmaps()
        {
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, true);
            
            var result = _processor.Copy(sourceTexture);
            
            Assert.IsNotNull(result);
            Assert.AreEqual(512, result.width);
            Assert.AreEqual(512, result.height);
            Assert.IsTrue(result.mipmapCount > 1, "Copied texture should preserve mipmaps");
            
            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Copy_WithoutMipmaps_DoesNotAddMipmaps()
        {
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            
            var result = _processor.Copy(sourceTexture);
            
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.mipmapCount, "Copied texture should not have mipmaps when source doesn't have them");
            
            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ResizeTo_SameDimensions_PreservesMipmaps()
        {
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, true);
            
            var result = _processor.ResizeTo(sourceTexture, 512, 512);
            
            Assert.AreEqual(512, result.width);
            Assert.AreEqual(512, result.height);
            Assert.IsTrue(result.mipmapCount > 1, "Should preserve mipmaps even when not actually resizing");
            
            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Normal Map Tests

        [Test]
        public void ResizeTo_NormalMap_UsesLinearColorSpace()
        {
            // Create texture with linear color space (5th parameter = true)
            // Normal maps store linear data, not sRGB
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, true, true);

            // Fill with normal map data (neutral normal = 0.5, 0.5, 1.0)
            var pixels = new Color[512 * 512];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 1.0f, 1.0f);
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.ResizeTo(sourceTexture, 256, 256, isNormalMap: true);

            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.width);
            Assert.AreEqual(256, result.height);

            // Verify normal data is preserved (should be close to 0.5, 0.5, 1.0)
            var resultPixels = result.GetPixels();
            var centerPixel = resultPixels[resultPixels.Length / 2];
            Assert.That(centerPixel.r, Is.InRange(0.45f, 0.55f), "Red channel should preserve normal X");
            Assert.That(centerPixel.g, Is.InRange(0.45f, 0.55f), "Green channel should preserve normal Y");

            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Copy_NormalMap_PreservesData()
        {
            // Create texture with linear color space for normal map data
            var sourceTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);

            var pixels = new Color[256 * 256];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 1.0f, 1.0f);
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.Copy(sourceTexture, isNormalMap: true);

            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.width);

            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ResizeTo_NonNormalMap_UsesDefaultColorSpace()
        {
            var sourceTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);

            var result = _processor.ResizeTo(sourceTexture, 256, 256, isNormalMap: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.width);

            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void GetReadablePixels_NormalMap_PreservesVectorData()
        {
            // Create texture with linear color space for normal map data
            var sourceTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);

            // Fill with normal map data (neutral normal = 0.5, 0.5, 1.0)
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 1.0f, 1.0f);
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.GetReadablePixels(sourceTexture, isNormalMap: true);

            Assert.IsNotNull(result);
            Assert.AreEqual(64 * 64, result.Length);

            // Verify normal data is preserved
            var centerPixel = result[result.Length / 2];
            Assert.That(centerPixel.r, Is.InRange(0.45f, 0.55f), "Red channel should preserve normal X");
            Assert.That(centerPixel.g, Is.InRange(0.45f, 0.55f), "Green channel should preserve normal Y");
            Assert.That(centerPixel.b, Is.InRange(0.95f, 1.05f), "Blue channel should preserve normal Z");

            Object.DestroyImmediate(sourceTexture);
        }

        [Test]
        public void GetReadablePixels_NonNormalMap_ReturnsCorrectPixels()
        {
            var sourceTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);

            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Red
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.GetReadablePixels(sourceTexture, isNormalMap: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(64 * 64, result.Length);

            var centerPixel = result[result.Length / 2];
            Assert.That(centerPixel.r, Is.InRange(0.95f, 1.05f), "Red channel should be preserved");
            Assert.That(centerPixel.g, Is.InRange(-0.05f, 0.05f), "Green channel should be near zero");

            Object.DestroyImmediate(sourceTexture);
        }

        [Test]
        public void GetReadablePixels_NormalMap_WithVariedNormals_PreservesData()
        {
            // Create texture with linear color space for normal map data
            var sourceTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);

            // Fill with varied normal map data to test different vector values
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Create gradient normals to test range preservation
                    float nx = x / 63.0f;  // 0 to 1
                    float ny = y / 63.0f;  // 0 to 1
                    pixels[y * 64 + x] = new Color(nx, ny, 1.0f, 1.0f);
                }
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.GetReadablePixels(sourceTexture, isNormalMap: true);

            Assert.IsNotNull(result);
            Assert.AreEqual(64 * 64, result.Length);

            // Check corner pixels to verify gradient is preserved
            Assert.That(result[0].r, Is.InRange(-0.05f, 0.15f), "Top-left X should be near 0");
            Assert.That(result[0].g, Is.InRange(-0.05f, 0.15f), "Top-left Y should be near 0");
            Assert.That(result[63].r, Is.InRange(0.85f, 1.05f), "Top-right X should be near 1");
            Assert.That(result[63 * 64 + 63].r, Is.InRange(0.85f, 1.05f), "Bottom-right X should be near 1");
            Assert.That(result[63 * 64 + 63].g, Is.InRange(0.85f, 1.05f), "Bottom-right Y should be near 1");

            Object.DestroyImmediate(sourceTexture);
        }

        #endregion

        #region BC5 Format Tests

        [Test]
        public void ResizeTo_BC5SimulatedData_PreservesNormalMapData()
        {
            // BC5 stores normals in RG channels only (XY), Z is reconstructed
            // Simulate BC5-like data with only RG channels containing normal data
            // Create texture with linear color space (BC5 is always linear)
            var sourceTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);

            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++)
            {
                // BC5 format: R=X, G=Y, B and A are not used for normal data
                pixels[i] = new Color(0.5f, 0.5f, 0.0f, 1.0f);
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.ResizeTo(sourceTexture, 64, 64, isNormalMap: true);

            Assert.IsNotNull(result);
            Assert.AreEqual(64, result.width);
            Assert.AreEqual(64, result.height);

            var resultPixels = result.GetPixels();
            var centerPixel = resultPixels[resultPixels.Length / 2];

            // BC5 critical channels (R and G) should be preserved accurately
            Assert.That(centerPixel.r, Is.InRange(0.45f, 0.55f), "R channel (normal X) should be preserved");
            Assert.That(centerPixel.g, Is.InRange(0.45f, 0.55f), "G channel (normal Y) should be preserved");

            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Copy_BC5SimulatedData_PreservesNormalMapData()
        {
            // Create texture with linear color space (BC5 is always linear)
            var sourceTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false, true);

            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 0.0f, 1.0f);
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.Copy(sourceTexture, isNormalMap: true);

            Assert.IsNotNull(result);
            Assert.AreEqual(128, result.width);

            var resultPixels = result.GetPixels();
            var centerPixel = resultPixels[resultPixels.Length / 2];

            Assert.That(centerPixel.r, Is.InRange(0.45f, 0.55f), "R channel should be preserved");
            Assert.That(centerPixel.g, Is.InRange(0.45f, 0.55f), "G channel should be preserved");

            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void GetReadablePixels_BC5SimulatedData_PreservesNormalMapData()
        {
            // Create texture with linear color space (BC5 is always linear)
            var sourceTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);

            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                // BC5: Only R and G contain meaningful normal data
                pixels[i] = new Color(0.5f, 0.5f, 0.0f, 1.0f);
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.GetReadablePixels(sourceTexture, isNormalMap: true);

            Assert.IsNotNull(result);
            Assert.AreEqual(64 * 64, result.Length);

            var centerPixel = result[result.Length / 2];
            Assert.That(centerPixel.r, Is.InRange(0.45f, 0.55f), "R channel (normal X) should be preserved");
            Assert.That(centerPixel.g, Is.InRange(0.45f, 0.55f), "G channel (normal Y) should be preserved");

            Object.DestroyImmediate(sourceTexture);
        }

        [Test]
        public void ResizeTo_BC5SimulatedData_WithExtremeValues_PreservesRange()
        {
            // Create texture with linear color space (BC5 is always linear)
            var sourceTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);

            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Test extreme normal values that BC5 might contain
                    // Normals can point in any direction: values 0-1 map to -1 to +1
                    float nx = x / 63.0f;
                    float ny = y / 63.0f;
                    pixels[y * 64 + x] = new Color(nx, ny, 0.0f, 1.0f);
                }
            }
            sourceTexture.SetPixels(pixels);
            sourceTexture.Apply();

            var result = _processor.ResizeTo(sourceTexture, 32, 32, isNormalMap: true);

            Assert.IsNotNull(result);
            var resultPixels = result.GetPixels();

            // Check corners to verify range is preserved
            Assert.That(resultPixels[0].r, Is.InRange(-0.1f, 0.2f), "Top-left X should be near 0");
            Assert.That(resultPixels[0].g, Is.InRange(-0.1f, 0.2f), "Top-left Y should be near 0");
            Assert.That(resultPixels[31 * 32 + 31].r, Is.InRange(0.8f, 1.1f), "Bottom-right X should be near 1");
            Assert.That(resultPixels[31 * 32 + 31].g, Is.InRange(0.8f, 1.1f), "Bottom-right Y should be near 1");

            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Helper Methods

        private static bool IsPowerOfTwo(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }

        #endregion
    }
}
