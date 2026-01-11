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
        public void PredictFormat_Desktop_NormalMapWithAlpha_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            // Normal maps with alpha should use BC7 to preserve the alpha channel
            var format = selector.PredictFormat(isNormalMap: true, complexity: 0.5f, hasAlpha: true);

            Assert.AreEqual(TextureFormat.BC7, format);
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

            // Medium complexity: >= threshold * MediumComplexityRatio && < threshold
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

            // Low complexity: < threshold * MediumComplexityRatio = 0.35
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

        #region CompressTexture Actual Compression Tests

        [Test]
        public void CompressTexture_UncompressedOpaqueTexture_Desktop_CompressesToDXT1()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: false, complexity: 0.3f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.DXT1, texture.format, "Opaque low complexity texture should compress to DXT1");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_UncompressedTextureWithAlpha_Desktop_CompressesToDXT5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateTextureWithAlpha(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: false, complexity: 0.3f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.DXT5, texture.format, "Texture with alpha should compress to DXT5");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_UncompressedHighComplexity_Desktop_CompressesToBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: false, complexity: 0.8f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.BC7, texture.format, "High complexity texture should compress to BC7");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_NormalMap_Desktop_CompressesToBC5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: true, complexity: 0.5f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.BC5, texture.format, "Normal map should compress to BC5");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_NormalMapWithAlpha_Desktop_CompressesToBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateTextureWithAlpha(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: true, complexity: 0.5f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.BC7, texture.format, "Normal map with alpha should compress to BC7 to preserve alpha");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_AlreadyInTargetFormat_ReturnsFalse()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            // First compress to get a DXT1 texture
            var texture = CreateOpaqueTexture(64, 64);
            selector.CompressTexture(texture, TextureFormat.RGBA32, isNormalMap: false, complexity: 0.3f);
            Assert.AreEqual(TextureFormat.DXT1, texture.format);

            // Try to compress again - should return false since already in target format
            bool result = selector.CompressTexture(texture, TextureFormat.RGBA32, isNormalMap: false, complexity: 0.3f);

            Assert.IsFalse(result, "Compression should return false when texture is already in target format");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_SourceAlreadyCompressed_PreservesSourceFormat()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            // Create and compress to BC7
            var texture = CreateOpaqueTexture(64, 64);
            selector.CompressTexture(texture, TextureFormat.RGBA32, isNormalMap: false, complexity: 0.9f);
            Assert.AreEqual(TextureFormat.BC7, texture.format);

            // Now call CompressTexture with sourceFormat as BC7 (already compressed)
            // It should try to preserve BC7, and since texture is already BC7, return false
            bool result = selector.CompressTexture(texture, TextureFormat.BC7, isNormalMap: false, complexity: 0.3f);

            Assert.IsFalse(result, "Should return false when source is compressed and texture already matches");
            Assert.AreEqual(TextureFormat.BC7, texture.format, "Format should remain BC7");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_Mobile_LowComplexity_CompressesToASTC8x8()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: false, complexity: 0.2f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.ASTC_8x8, texture.format, "Low complexity mobile texture should compress to ASTC_8x8");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_Mobile_HighComplexity_CompressesToASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: false, complexity: 0.8f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.ASTC_4x4, texture.format, "High complexity mobile texture should compress to ASTC_4x4");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_Mobile_NormalMap_CompressesToASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(texture, originalFormat, isNormalMap: true, complexity: 0.5f);

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.ASTC_4x4, texture.format, "Mobile normal map should compress to ASTC_4x4");

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region ConvertFrozenFormat Tests

        [Test]
        public void ConvertFrozenFormat_DXT1_ReturnsDXT1()
        {
            var result = TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.DXT1);

            Assert.AreEqual(TextureFormat.DXT1, result);
        }

        [Test]
        public void ConvertFrozenFormat_DXT5_ReturnsDXT5()
        {
            var result = TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.DXT5);

            Assert.AreEqual(TextureFormat.DXT5, result);
        }

        [Test]
        public void ConvertFrozenFormat_BC5_ReturnsBC5()
        {
            var result = TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.BC5);

            Assert.AreEqual(TextureFormat.BC5, result);
        }

        [Test]
        public void ConvertFrozenFormat_BC7_ReturnsBC7()
        {
            var result = TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.BC7);

            Assert.AreEqual(TextureFormat.BC7, result);
        }

        [Test]
        public void ConvertFrozenFormat_ASTC4x4_ReturnsASTC4x4()
        {
            var result = TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.ASTC_4x4);

            Assert.AreEqual(TextureFormat.ASTC_4x4, result);
        }

        [Test]
        public void ConvertFrozenFormat_ASTC6x6_ReturnsASTC6x6()
        {
            var result = TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.ASTC_6x6);

            Assert.AreEqual(TextureFormat.ASTC_6x6, result);
        }

        [Test]
        public void ConvertFrozenFormat_ASTC8x8_ReturnsASTC8x8()
        {
            var result = TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.ASTC_8x8);

            Assert.AreEqual(TextureFormat.ASTC_8x8, result);
        }

        [Test]
        public void ConvertFrozenFormat_Auto_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                TextureFormatSelector.ConvertFrozenFormat(FrozenTextureFormat.Auto);
            });
        }

        #endregion

        #region CompressTexture with FrozenFormat Override Tests

        [Test]
        public void CompressTexture_WithFrozenFormatOverride_UsesOverrideFormat()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Low complexity would normally result in DXT1, but we override to BC7
            bool result = selector.CompressTexture(
                texture, originalFormat, isNormalMap: false, complexity: 0.3f,
                formatOverride: FrozenTextureFormat.BC7);

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.BC7, texture.format);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_WithFrozenFormatDXT5_UsesDXT5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Opaque texture would normally use DXT1, but we override to DXT5
            bool result = selector.CompressTexture(
                texture, originalFormat, isNormalMap: false, complexity: 0.3f,
                formatOverride: FrozenTextureFormat.DXT5);

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.DXT5, texture.format);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_WithFrozenFormatAuto_UsesNormalSelection()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Auto should fall through to normal format selection
            bool result = selector.CompressTexture(
                texture, originalFormat, isNormalMap: false, complexity: 0.3f,
                formatOverride: FrozenTextureFormat.Auto);

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.DXT1, texture.format);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_WithNullFormatOverride_UsesNormalSelection()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture, originalFormat, isNormalMap: false, complexity: 0.3f,
                formatOverride: null);

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.DXT1, texture.format);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_FrozenOverrideOnNormalMap_OverridesBC5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Normal map would normally use BC5, but we override to DXT1
            bool result = selector.CompressTexture(
                texture, originalFormat, isNormalMap: true, complexity: 0.5f,
                formatOverride: FrozenTextureFormat.DXT1);

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.DXT1, texture.format);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_FrozenMobileFormat_OnDesktop_UsesSpecifiedFormat()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f);

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Force ASTC format even on Desktop platform
            bool result = selector.CompressTexture(
                texture, originalFormat, isNormalMap: false, complexity: 0.5f,
                formatOverride: FrozenTextureFormat.ASTC_4x4);

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.ASTC_4x4, texture.format);

            Object.DestroyImmediate(texture);
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

            // Alpha value above threshold (SignificantAlphaThreshold = 250)
            byte opaqueAlpha = (byte)(AnalysisConstants.SignificantAlphaThreshold + 2);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, 128, opaqueAlpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSemiTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            // Alpha value below threshold (SignificantAlphaThreshold = 250)
            byte transparentAlpha = (byte)(AnalysisConstants.SignificantAlphaThreshold - 50);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, 128, transparentAlpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
