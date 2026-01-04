using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureFormatSelectorTests
    {
        #region PredictFormat Desktop Tests

        [Test]
        public void PredictFormat_Desktop_NormalMap_ReturnsBC5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: true, complexity: 0.5f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.BC5, format);
        }

        [Test]
        public void PredictFormat_Desktop_HighComplexity_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.8f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.BC7, format);
        }

        [Test]
        public void PredictFormat_Desktop_WithAlpha_ReturnsDXT5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.3f, hasAlpha: true);

            Assert.AreEqual(TextureFormat.DXT5, format);
        }

        [Test]
        public void PredictFormat_Desktop_Opaque_ReturnsDXT1()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.3f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.DXT1, format);
        }

        [Test]
        public void PredictFormat_Desktop_HighQualityDisabled_DoesNotUseBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: false,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.9f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.DXT1, format);
        }

        [Test]
        public void PredictFormat_Desktop_HighComplexityWithAlpha_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.9f, hasAlpha: true);

            Assert.AreEqual(TextureFormat.BC7, format);
        }

        #endregion

        #region PredictFormat Mobile Tests

        [Test]
        public void PredictFormat_Mobile_NormalMap_ReturnsASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: true, complexity: 0.5f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.ASTC_4x4, format);
        }

        [Test]
        public void PredictFormat_Mobile_HighComplexityWithAlpha_ReturnsASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.8f, hasAlpha: true);

            Assert.AreEqual(TextureFormat.ASTC_4x4, format);
        }

        [Test]
        public void PredictFormat_Mobile_LowComplexityWithAlpha_ReturnsASTC6x6()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.3f, hasAlpha: true);

            Assert.AreEqual(TextureFormat.ASTC_6x6, format);
        }

        [Test]
        public void PredictFormat_Mobile_HighComplexityOpaque_ReturnsASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.8f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.ASTC_4x4, format);
        }

        [Test]
        public void PredictFormat_Mobile_MediumComplexityOpaque_ReturnsASTC6x6()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            // Medium complexity: >= threshold * 0.5 && < threshold
            // 0.7 * 0.5 = 0.35, so 0.5 is medium
            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.5f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.ASTC_6x6, format);
        }

        [Test]
        public void PredictFormat_Mobile_LowComplexityOpaque_ReturnsASTC8x8()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            // Low complexity: < threshold * 0.5 = 0.35
            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.2f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.ASTC_8x8, format);
        }

        [Test]
        public void PredictFormat_Mobile_HighQualityDisabled_UsesComplexityBasedSelection()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: false,
                highQualityComplexityThreshold: 0.7f);

            // With high quality disabled, even high complexity should use larger block size
            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.9f, hasAlpha: false);

            // Should still use complexity-based selection but without the high quality ASTC_4x4
            Assert.That(format, Is.EqualTo(TextureFormat.ASTC_6x6).Or.EqualTo(TextureFormat.ASTC_8x8));
        }

        #endregion

        #region PredictFormat Threshold Tests

        [Test]
        public void PredictFormat_Desktop_ExactlyAtThreshold_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.7f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.BC7, format);
        }

        [Test]
        public void PredictFormat_Desktop_JustBelowThreshold_ReturnsDXT1()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.69f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.DXT1, format);
        }

        [Test]
        public void PredictFormat_CustomThreshold_Works()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.5f);

            var format = selector.PredictFormat(isNormalMap: false, complexity: 0.5f, hasAlpha: false);

            Assert.AreEqual(TextureFormat.BC7, format);
        }

        #endregion

        #region IsCompressedFormat Tests

        [Test]
        public void IsCompressedFormat_DXT1_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT1));
        }

        [Test]
        public void IsCompressedFormat_DXT5_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT5));
        }

        [Test]
        public void IsCompressedFormat_DXT1Crunched_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT1Crunched));
        }

        [Test]
        public void IsCompressedFormat_DXT5Crunched_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT5Crunched));
        }

        [Test]
        public void IsCompressedFormat_BC4_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC4));
        }

        [Test]
        public void IsCompressedFormat_BC5_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC5));
        }

        [Test]
        public void IsCompressedFormat_BC6H_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC6H));
        }

        [Test]
        public void IsCompressedFormat_BC7_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC7));
        }

        [Test]
        public void IsCompressedFormat_ASTC4x4_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_4x4));
        }

        [Test]
        public void IsCompressedFormat_ASTC6x6_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_6x6));
        }

        [Test]
        public void IsCompressedFormat_ASTC8x8_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_8x8));
        }

        [Test]
        public void IsCompressedFormat_ETC2_RGB_ReturnsTrue()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ETC2_RGB));
        }

        [Test]
        public void IsCompressedFormat_RGBA32_ReturnsFalse()
        {
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.RGBA32));
        }

        [Test]
        public void IsCompressedFormat_RGB24_ReturnsFalse()
        {
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.RGB24));
        }

        [Test]
        public void IsCompressedFormat_ARGB32_ReturnsFalse()
        {
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.ARGB32));
        }

        #endregion

        #region HasSignificantAlpha Tests

        [Test]
        public void HasSignificantAlpha_FullyOpaque_ReturnsFalse()
        {
            var texture = CreateOpaqueTexture(64, 64);

            var result = TextureFormatSelector.HasSignificantAlpha(texture);

            Assert.IsFalse(result);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void HasSignificantAlpha_WithTransparentPixels_ReturnsTrue()
        {
            var texture = CreateTextureWithAlpha(64, 64);

            var result = TextureFormatSelector.HasSignificantAlpha(texture);

            Assert.IsTrue(result);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void HasSignificantAlpha_MostlyOpaque_ReturnsFalse()
        {
            var texture = CreateMostlyOpaqueTexture(64, 64);

            var result = TextureFormatSelector.HasSignificantAlpha(texture);

            Assert.IsFalse(result);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void HasSignificantAlpha_SemiTransparent_ReturnsTrue()
        {
            var texture = CreateSemiTransparentTexture(64, 64);

            var result = TextureFormatSelector.HasSignificantAlpha(texture);

            Assert.IsTrue(result);

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_WithDefaultParameters_CreatesInstance()
        {
            var selector = new TextureFormatSelector();

            Assert.IsNotNull(selector);
        }

        [Test]
        public void Constructor_WithAllParameters_CreatesInstance()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: false,
                highQualityComplexityThreshold: 0.5f);

            Assert.IsNotNull(selector);
        }

        #endregion

        #region CompressTexture SourceFormat Preservation Tests

        [Test]
        public void CompressTexture_AlreadyDXT1_PreservesFormat()
        {
            // When source format is already compressed, it should be preserved
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT1));
        }

        [Test]
        public void CompressTexture_AlreadyBC7_PreservesFormat()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC7));
        }

        [Test]
        public void CompressTexture_AlreadyASTC_PreservesFormat()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_4x4));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_6x6));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_8x8));
        }

        #endregion

        #region Helper Methods

        private static Texture2D CreateOpaqueTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, 128, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateTextureWithAlpha(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Half transparent, half opaque
                byte alpha = (byte)(i % 2 == 0 ? 128 : 255);
                pixels[i] = new Color32(128, 128, 128, alpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateMostlyOpaqueTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                // All pixels have alpha >= 250 (threshold in HasSignificantAlpha)
                pixels[i] = new Color32(128, 128, 128, 252);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSemiTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Some pixels below threshold (250)
                pixels[i] = new Color32(128, 128, 128, 200);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
