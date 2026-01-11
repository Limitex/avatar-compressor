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
        public void CalculateNewDimensions_WithoutForcePowerOfTwo_ReturnsMultipleOf4()
        {
            // Without forcePowerOfTwo, dimensions are still rounded to multiples of 4
            // for DXT/BC compression compatibility. 300/2=150, rounded up to 152 (next multiple of 4)
            var resizerNoPow2 = new TextureProcessor(32, 2048, false);
            var result = resizerNoPow2.CalculateNewDimensions(300, 300, 2);
            Assert.AreEqual(152, result.x);
            Assert.AreEqual(152, result.y);
            Assert.AreEqual(0, result.x % 4, "Width should be multiple of 4");
            Assert.AreEqual(0, result.y % 4, "Height should be multiple of 4");
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

        [Test]
        public void CalculateNewDimensions_WithoutForcePowerOfTwo_ExactMultipleOf4_ReturnsExact()
        {
            // 400/2=200, which is already a multiple of 4, so no rounding needed
            var resizerNoPow2 = new TextureProcessor(32, 2048, false);
            var result = resizerNoPow2.CalculateNewDimensions(400, 400, 2);
            Assert.AreEqual(200, result.x);
            Assert.AreEqual(200, result.y);
        }

        [Test]
        public void CalculateNewDimensions_WithoutForcePowerOfTwo_AllResultsAreMultipleOf4()
        {
            var resizerNoPow2 = new TextureProcessor(32, 2048, false);
            int[] sizes = { 100, 150, 200, 300, 500, 600, 700, 800, 1000 };
            int[] divisors = { 1, 2, 4, 8 };

            foreach (int size in sizes)
            {
                foreach (int div in divisors)
                {
                    var result = resizerNoPow2.CalculateNewDimensions(size, size, div);
                    Assert.AreEqual(0, result.x % 4,
                        $"Size {size} with divisor {div}: Width {result.x} is not multiple of 4");
                    Assert.AreEqual(0, result.y % 4,
                        $"Size {size} with divisor {div}: Height {result.y} is not multiple of 4");
                }
            }
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
        public void Constructor_WithParameters_CreatesInstance()
        {
            var processor = new TextureProcessor(32, 2048, true);
            Assert.IsNotNull(processor);
        }

        [Test]
        public void Constructor_WithCustomMinMax_CreatesInstance()
        {
            var processor = new TextureProcessor(
                minResolution: 64,
                maxResolution: 1024,
                forcePowerOfTwo: false);

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

        #region Resize Tests

        [Test]
        public void Resize_WithDivisor1_ReturnsSameSize()
        {
            var source = CreateTexture(512, 512);
            var analysis = new TextureAnalysisResult(0.5f, 1, new Vector2Int(512, 512));

            var result = _processor.Resize(source, analysis);

            Assert.AreEqual(512, result.width);
            Assert.AreEqual(512, result.height);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_WithDivisor2_ReturnsHalfSize()
        {
            var source = CreateTexture(512, 512);
            var analysis = new TextureAnalysisResult(0.3f, 2, new Vector2Int(256, 256));

            var result = _processor.Resize(source, analysis);

            Assert.AreEqual(256, result.width);
            Assert.AreEqual(256, result.height);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_ReturnsUncompressedRGBA32Format()
        {
            var source = CreateTexture(256, 256);
            var analysis = new TextureAnalysisResult(0.5f, 2, new Vector2Int(128, 128));

            var result = _processor.Resize(source, analysis);

            Assert.AreEqual(TextureFormat.RGBA32, result.format,
                "Resize should return uncompressed RGBA32 format (compression is handled separately)");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_UsesRecommendedResolution()
        {
            var source = CreateTexture(1024, 512);
            var analysis = new TextureAnalysisResult(0.4f, 4, new Vector2Int(256, 128));

            var result = _processor.Resize(source, analysis);

            Assert.AreEqual(256, result.width);
            Assert.AreEqual(128, result.height);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_WhenNoDivisorNeeded_ReturnsCopy()
        {
            var source = CreateTexture(256, 256);
            var analysis = new TextureAnalysisResult(0.8f, 1, new Vector2Int(256, 256));

            var result = _processor.Resize(source, analysis);

            Assert.AreEqual(256, result.width);
            Assert.AreEqual(256, result.height);
            Assert.AreNotSame(source, result, "Should return a new texture instance");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_EnsuresDimensionsAreMultipleOf4()
        {
            // Using non-power-of-two processor
            var processorNoPow2 = new TextureProcessor(32, 2048, false);
            var source = CreateTexture(300, 300);
            var analysis = new TextureAnalysisResult(0.5f, 1, new Vector2Int(300, 300));

            var result = processorNoPow2.Resize(source, analysis);

            Assert.AreEqual(0, result.width % 4, "Width should be multiple of 4 for DXT/BC compression compatibility");
            Assert.AreEqual(0, result.height % 4, "Height should be multiple of 4 for DXT/BC compression compatibility");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_PreservesTextureSettings()
        {
            var source = CreateTexture(512, 512);
            source.wrapModeU = TextureWrapMode.Repeat;
            source.wrapModeV = TextureWrapMode.Clamp;
            source.filterMode = FilterMode.Trilinear;
            source.anisoLevel = 8;

            var analysis = new TextureAnalysisResult(0.5f, 2, new Vector2Int(256, 256));

            var result = _processor.Resize(source, analysis);

            Assert.AreEqual(TextureWrapMode.Repeat, result.wrapModeU);
            Assert.AreEqual(TextureWrapMode.Clamp, result.wrapModeV);
            Assert.AreEqual(FilterMode.Trilinear, result.filterMode);
            Assert.AreEqual(8, result.anisoLevel);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_PreservesMipmapSetting()
        {
            var sourceWithMips = new Texture2D(512, 512, TextureFormat.RGBA32, true);
            var sourceWithoutMips = new Texture2D(512, 512, TextureFormat.RGBA32, false);

            var analysis = new TextureAnalysisResult(0.5f, 2, new Vector2Int(256, 256));

            var resultWithMips = _processor.Resize(sourceWithMips, analysis);
            var resultWithoutMips = _processor.Resize(sourceWithoutMips, analysis);

            Assert.IsTrue(resultWithMips.mipmapCount > 1, "Should preserve mipmaps when source has mipmaps");
            Assert.AreEqual(1, resultWithoutMips.mipmapCount, "Should not add mipmaps when source has none");

            Object.DestroyImmediate(sourceWithMips);
            Object.DestroyImmediate(sourceWithoutMips);
            Object.DestroyImmediate(resultWithMips);
            Object.DestroyImmediate(resultWithoutMips);
        }

        [Test]
        public void Resize_NonSquareTexture_PreservesAspectRatio()
        {
            var source = CreateTexture(1024, 256);
            var analysis = new TextureAnalysisResult(0.5f, 2, new Vector2Int(512, 128));

            var result = _processor.Resize(source, analysis);

            Assert.AreEqual(512, result.width);
            Assert.AreEqual(128, result.height);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Helper Methods

        private static bool IsPowerOfTwo(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }

        private Texture2D CreateTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
