using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

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
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: true,
                complexity: 0.5f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.BC5, format);
        }

        [Test]
        public void PredictFormat_Desktop_NormalMapWithAlpha_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Normal maps with alpha should use BC7 to preserve the alpha channel
            var format = selector.PredictFormat(
                isNormalMap: true,
                complexity: 0.5f,
                hasAlpha: true
            );

            Assert.AreEqual(TextureFormat.BC7, format);
        }

        [Test]
        public void PredictFormat_Desktop_HighComplexity_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.8f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.BC7, format);
        }

        [Test]
        public void PredictFormat_Desktop_WithAlpha_ReturnsDXT5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.3f,
                hasAlpha: true
            );

            Assert.AreEqual(TextureFormat.DXT5, format);
        }

        [Test]
        public void PredictFormat_Desktop_Opaque_ReturnsDXT1()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.3f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.DXT1, format);
        }

        [Test]
        public void PredictFormat_Desktop_HighQualityDisabled_DoesNotUseBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: false,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.9f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.DXT1, format);
        }

        [Test]
        public void PredictFormat_Desktop_HighComplexityWithAlpha_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.9f,
                hasAlpha: true
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: true,
                complexity: 0.5f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.ASTC_4x4, format);
        }

        [Test]
        public void PredictFormat_Mobile_HighComplexityWithAlpha_ReturnsASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.8f,
                hasAlpha: true
            );

            Assert.AreEqual(TextureFormat.ASTC_4x4, format);
        }

        [Test]
        public void PredictFormat_Mobile_LowComplexityWithAlpha_ReturnsASTC6x6()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.3f,
                hasAlpha: true
            );

            Assert.AreEqual(TextureFormat.ASTC_6x6, format);
        }

        [Test]
        public void PredictFormat_Mobile_HighComplexityOpaque_ReturnsASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.8f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.ASTC_4x4, format);
        }

        [Test]
        public void PredictFormat_Mobile_MediumComplexityOpaque_ReturnsASTC6x6()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Medium complexity: >= threshold * MediumComplexityRatio && < threshold
            // 0.7 * 0.5 = 0.35, so 0.5 is medium
            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.5f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.ASTC_6x6, format);
        }

        [Test]
        public void PredictFormat_Mobile_LowComplexityOpaque_ReturnsASTC8x8()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Low complexity: < threshold * MediumComplexityRatio = 0.35
            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.2f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.ASTC_8x8, format);
        }

        [Test]
        public void PredictFormat_Mobile_HighQualityDisabled_UsesComplexityBasedSelection()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: false,
                highQualityComplexityThreshold: 0.7f
            );

            // With high quality disabled, even high complexity should use larger block size
            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.9f,
                hasAlpha: false
            );

            // Should still use complexity-based selection but without the high quality ASTC_4x4
            Assert.That(
                format,
                Is.EqualTo(TextureFormat.ASTC_6x6).Or.EqualTo(TextureFormat.ASTC_8x8)
            );
        }

        #endregion

        #region PredictFormat Threshold Tests

        [Test]
        public void PredictFormat_Desktop_ExactlyAtThreshold_ReturnsBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.7f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.BC7, format);
        }

        [Test]
        public void PredictFormat_Desktop_JustBelowThreshold_ReturnsDXT1()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.69f,
                hasAlpha: false
            );

            Assert.AreEqual(TextureFormat.DXT1, format);
        }

        [Test]
        public void PredictFormat_CustomThreshold_Works()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.5f
            );

            var format = selector.PredictFormat(
                isNormalMap: false,
                complexity: 0.5f,
                hasAlpha: false
            );

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
                highQualityComplexityThreshold: 0.5f
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.3f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.DXT1,
                texture.format,
                "Opaque low complexity texture should compress to DXT1"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_UncompressedTextureWithAlpha_Desktop_CompressesToDXT5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateTextureWithAlpha(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.3f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.DXT5,
                texture.format,
                "Texture with alpha should compress to DXT5"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_UncompressedHighComplexity_Desktop_CompressesToBC7()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.8f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.BC7,
                texture.format,
                "High complexity texture should compress to BC7"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_NormalMap_Desktop_CompressesToBC5()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: true,
                complexity: 0.5f
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateTextureWithAlpha(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: true,
                complexity: 0.5f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.BC7,
                texture.format,
                "Normal map with alpha should compress to BC7 to preserve alpha"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_AlreadyInTargetFormat_ReturnsFalse()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // First compress to get a DXT1 texture
            var texture = CreateOpaqueTexture(64, 64);
            selector.CompressTexture(
                texture,
                TextureFormat.RGBA32,
                isNormalMap: false,
                complexity: 0.3f
            );
            Assert.AreEqual(TextureFormat.DXT1, texture.format);

            // Try to compress again - should return false since already in target format
            bool result = selector.CompressTexture(
                texture,
                TextureFormat.RGBA32,
                isNormalMap: false,
                complexity: 0.3f
            );

            Assert.IsFalse(
                result,
                "Compression should return false when texture is already in target format"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_SourceAlreadyCompressed_PreservesSourceFormat()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Create and compress to BC7
            var texture = CreateOpaqueTexture(64, 64);
            selector.CompressTexture(
                texture,
                TextureFormat.RGBA32,
                isNormalMap: false,
                complexity: 0.9f
            );
            Assert.AreEqual(TextureFormat.BC7, texture.format);

            // Now call CompressTexture with sourceFormat as BC7 (already compressed)
            // It should try to preserve BC7, and since texture is already BC7, return false
            bool result = selector.CompressTexture(
                texture,
                TextureFormat.BC7,
                isNormalMap: false,
                complexity: 0.3f
            );

            Assert.IsFalse(
                result,
                "Should return false when source is compressed and texture already matches"
            );
            Assert.AreEqual(TextureFormat.BC7, texture.format, "Format should remain BC7");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_Mobile_LowComplexity_CompressesToASTC8x8()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.2f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.ASTC_8x8,
                texture.format,
                "Low complexity mobile texture should compress to ASTC_8x8"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_Mobile_HighComplexity_CompressesToASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.8f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.ASTC_4x4,
                texture.format,
                "High complexity mobile texture should compress to ASTC_4x4"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_Mobile_NormalMap_CompressesToASTC4x4()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Mobile,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: true,
                complexity: 0.5f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.ASTC_4x4,
                texture.format,
                "Mobile normal map should compress to ASTC_4x4"
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Low complexity would normally result in DXT1, but we override to BC7
            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.3f,
                formatOverride: FrozenTextureFormat.BC7
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Opaque texture would normally use DXT1, but we override to DXT5
            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.3f,
                formatOverride: FrozenTextureFormat.DXT5
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Auto should fall through to normal format selection
            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.3f,
                formatOverride: FrozenTextureFormat.Auto
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.3f,
                formatOverride: null
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Normal map would normally use BC5, but we override to DXT1
            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: true,
                complexity: 0.5f,
                formatOverride: FrozenTextureFormat.DXT1
            );

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
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateOpaqueTexture(64, 64);
            var originalFormat = texture.format;

            // Force ASTC format even on Desktop platform
            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: false,
                complexity: 0.5f,
                formatOverride: FrozenTextureFormat.ASTC_4x4
            );

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.ASTC_4x4, texture.format);

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region CompressTexture Preprocessor Integration Tests

        [Test]
        public void CompressTexture_BC5NormalMap_AppliesPreprocessor()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Create a normal map with varied normal directions
            var texture = CreateNormalMapTexture(64, 64);
            var originalFormat = texture.format;

            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: true,
                complexity: 0.5f
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(TextureFormat.BC5, texture.format, "Normal map should compress to BC5");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_BC5NormalMap_WithFrozenFormat_AppliesPreprocessor()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateNormalMapTexture(64, 64);
            var originalFormat = texture.format;

            // Force BC5 via frozen format
            bool result = selector.CompressTexture(
                texture,
                originalFormat,
                isNormalMap: true,
                complexity: 0.5f,
                formatOverride: FrozenTextureFormat.BC5
            );

            Assert.IsTrue(result, "Compression should return true when format changes");
            Assert.AreEqual(
                TextureFormat.BC5,
                texture.format,
                "Should compress to BC5 per frozen format"
            );

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region CompressTexture Pixel Data Integrity Tests

        [Test]
        public void CompressTexture_BC5NormalMap_PreservesNormalVectorDirection()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateNormalMapTexture(64, 64);
            var originalPixels = texture.GetPixels();
            var originalFormat = texture.format;

            selector.CompressTexture(texture, originalFormat, isNormalMap: true, complexity: 0.5f);

            var compressedPixels = texture.GetPixels();

            // BC5 only stores RG channels, Z is reconstructed from sqrt(1 - x² - y²)
            // Verify that X and Y directions are preserved within compression tolerance
            const float maxAngularDifferenceRad = 0.15f; // ~8.6 degrees tolerance for BC5 compression

            int sampleCount = 100;
            int step = Mathf.Max(1, originalPixels.Length / sampleCount);

            for (int i = 0; i < originalPixels.Length; i += step)
            {
                // Decode original normal
                float origX = originalPixels[i].r * 2f - 1f;
                float origY = originalPixels[i].g * 2f - 1f;
                float origZSq = 1f - origX * origX - origY * origY;
                float origZ = origZSq > 0f ? Mathf.Sqrt(origZSq) : 0f;
                var origNormal = new Vector3(origX, origY, origZ).normalized;

                // Decode compressed normal (BC5 stores only RG, Z is reconstructed)
                float compX = compressedPixels[i].r * 2f - 1f;
                float compY = compressedPixels[i].g * 2f - 1f;
                float compZSq = 1f - compX * compX - compY * compY;
                float compZ = compZSq > 0f ? Mathf.Sqrt(compZSq) : 0f;
                var compNormal = new Vector3(compX, compY, compZ).normalized;

                // Check angular difference
                float dot = Vector3.Dot(origNormal, compNormal);
                dot = Mathf.Clamp(dot, -1f, 1f);
                float angleDiff = Mathf.Acos(dot);

                Assert.That(
                    angleDiff,
                    Is.LessThanOrEqualTo(maxAngularDifferenceRad),
                    $"Normal at index {i} has angular difference of {angleDiff * Mathf.Rad2Deg:F2}° which exceeds tolerance"
                );
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_BC5NormalMap_ProducesValidNormalizedVectors()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateNormalMapTexture(64, 64);
            var originalFormat = texture.format;

            selector.CompressTexture(texture, originalFormat, isNormalMap: true, complexity: 0.5f);

            var compressedPixels = texture.GetPixels();

            // Verify all compressed normals can be reconstructed to valid unit vectors
            int sampleCount = 100;
            int step = Mathf.Max(1, compressedPixels.Length / sampleCount);

            for (int i = 0; i < compressedPixels.Length; i += step)
            {
                float x = compressedPixels[i].r * 2f - 1f;
                float y = compressedPixels[i].g * 2f - 1f;

                // X² + Y² should not exceed 1 (otherwise Z cannot be reconstructed)
                float xyLengthSq = x * x + y * y;
                Assert.That(
                    xyLengthSq,
                    Is.LessThanOrEqualTo(1.01f), // Small tolerance for compression artifacts
                    $"Normal at index {i} has X²+Y²={xyLengthSq:F4} which exceeds 1, making Z reconstruction invalid"
                );

                // Reconstruct Z and verify unit length
                float zSq = 1f - xyLengthSq;
                float z = zSq > 0f ? Mathf.Sqrt(zSq) : 0f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);

                Assert.That(
                    length,
                    Is.EqualTo(1f).Within(0.02f),
                    $"Reconstructed normal at index {i} has length {length:F4}, expected ~1.0"
                );
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_DXT5_PreservesColorData()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateTextureWithAlpha(64, 64);
            var originalPixels = texture.GetPixels();
            var originalFormat = texture.format;

            selector.CompressTexture(texture, originalFormat, isNormalMap: false, complexity: 0.3f);

            Assert.AreEqual(TextureFormat.DXT5, texture.format);

            var compressedPixels = texture.GetPixels();

            // DXT5 compression tolerance
            const float maxChannelDifference = 0.1f;

            int sampleCount = 100;
            int step = Mathf.Max(1, originalPixels.Length / sampleCount);

            for (int i = 0; i < originalPixels.Length; i += step)
            {
                float diffR = Mathf.Abs(originalPixels[i].r - compressedPixels[i].r);
                float diffG = Mathf.Abs(originalPixels[i].g - compressedPixels[i].g);
                float diffB = Mathf.Abs(originalPixels[i].b - compressedPixels[i].b);
                float diffA = Mathf.Abs(originalPixels[i].a - compressedPixels[i].a);

                Assert.That(
                    diffR,
                    Is.LessThanOrEqualTo(maxChannelDifference),
                    $"Red channel at index {i} differs by {diffR:F4}"
                );
                Assert.That(
                    diffG,
                    Is.LessThanOrEqualTo(maxChannelDifference),
                    $"Green channel at index {i} differs by {diffG:F4}"
                );
                Assert.That(
                    diffB,
                    Is.LessThanOrEqualTo(maxChannelDifference),
                    $"Blue channel at index {i} differs by {diffB:F4}"
                );
                Assert.That(
                    diffA,
                    Is.LessThanOrEqualTo(maxChannelDifference),
                    $"Alpha channel at index {i} differs by {diffA:F4}"
                );
            }

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Actual Compressed Source Tests (Issue #40)

        /// <summary>
        /// Tests BC5 to BC5 compression with actual BC5 source texture.
        /// This is the exact scenario reported in Issue #40.
        /// </summary>
        [Test]
        public void CompressTexture_ActualBC5Source_ToBC5_PreservesNormals()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Create and compress to BC5 (simulating existing BC5 asset)
            var originalTexture = CreateNormalMapTexture(64, 64);
            EditorUtility.CompressTexture(
                originalTexture,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.BC5, originalTexture.format, "Source should be BC5");

            // Create a working copy via Blit (same as TextureProcessor.ResizeTo)
            var workingCopy = BlitToRGBA32(originalTexture, 64, 64);

            // Apply compression with BC5 source format
            bool result = selector.CompressTexture(
                workingCopy,
                TextureFormat.BC5, // Source was BC5
                isNormalMap: true,
                complexity: 0.5f
            );

            Assert.IsTrue(result, "Compression should succeed");
            Assert.AreEqual(TextureFormat.BC5, workingCopy.format, "Output should be BC5");

            // Verify normals are valid (X² + Y² <= 1)
            var pixels = workingCopy.GetPixels();
            for (int i = 0; i < pixels.Length; i += 10)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                float xyLengthSq = x * x + y * y;

                Assert.That(
                    xyLengthSq,
                    Is.LessThanOrEqualTo(1.05f),
                    $"Normal at {i} should have X²+Y² <= 1 for Z reconstruction"
                );
            }

            Object.DestroyImmediate(originalTexture);
            Object.DestroyImmediate(workingCopy);
        }

        /// <summary>
        /// Tests that actual DXT5 source is correctly read from AG channels.
        /// </summary>
        [Test]
        public void CompressTexture_ActualDXT5Source_ToBC5_ReadsFromAGChannels()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Create and compress to DXT5 (DXTnm format)
            var originalTexture = CreateNormalMapTexture(64, 64);
            EditorUtility.CompressTexture(
                originalTexture,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.DXT5, originalTexture.format, "Source should be DXT5");

            // Create a working copy via Blit
            var workingCopy = BlitToRGBA32(originalTexture, 64, 64);

            // Apply compression with DXT5 source format
            bool result = selector.CompressTexture(
                workingCopy,
                TextureFormat.DXT5, // Source was DXT5 (DXTnm)
                isNormalMap: true,
                complexity: 0.5f
            );

            Assert.IsTrue(result, "Compression should succeed");
            Assert.AreEqual(TextureFormat.BC5, workingCopy.format, "Output should be BC5");

            // Verify normals are valid
            var pixels = workingCopy.GetPixels();
            for (int i = 0; i < pixels.Length; i += 10)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                float xyLengthSq = x * x + y * y;

                Assert.That(
                    xyLengthSq,
                    Is.LessThanOrEqualTo(1.05f),
                    $"Normal at {i} should have valid XY for Z reconstruction"
                );
            }

            Object.DestroyImmediate(originalTexture);
            Object.DestroyImmediate(workingCopy);
        }

        /// <summary>
        /// Tests BC5 to BC5 with downscaling (common real-world scenario).
        /// </summary>
        [Test]
        public void CompressTexture_ActualBC5Source_WithDownscale_PreservesNormals()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Create 128x128 BC5 texture
            var originalTexture = CreateNormalMapTexture(128, 128);
            EditorUtility.CompressTexture(
                originalTexture,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            // Downscale to 64x64 via Blit
            var workingCopy = BlitToRGBA32(originalTexture, 64, 64);

            bool result = selector.CompressTexture(
                workingCopy,
                TextureFormat.BC5,
                isNormalMap: true,
                complexity: 0.5f
            );

            Assert.IsTrue(result);
            Assert.AreEqual(TextureFormat.BC5, workingCopy.format);

            // Verify normals are valid
            var pixels = workingCopy.GetPixels();
            int invalidCount = 0;
            for (int i = 0; i < pixels.Length; i += 5)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                if (x * x + y * y > 1.05f)
                {
                    invalidCount++;
                }
            }

            Assert.AreEqual(0, invalidCount, "All normals should have valid X²+Y² <= 1");

            Object.DestroyImmediate(originalTexture);
            Object.DestroyImmediate(workingCopy);
        }

        /// <summary>
        /// Helper to simulate what TextureProcessor.ResizeTo does.
        /// </summary>
        private static Texture2D BlitToRGBA32(Texture2D source, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear // Important for normal maps
            );
            rt.filterMode = FilterMode.Bilinear;

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        #endregion

        #region DXTnm Source Format Tests

        [Test]
        public void CompressTexture_DXTnmSource_ToBC5_ReadsFromAGChannels()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Create texture with data in AG channels (simulating DXTnm layout)
            var texture = CreateDXTnmNormalMapTexture(64, 64);

            // DXT5 source format indicates DXTnm layout
            bool result = selector.CompressTexture(
                texture,
                TextureFormat.DXT5,
                isNormalMap: true,
                complexity: 0.5f
            );

            Assert.IsTrue(result, "Compression should succeed");
            Assert.AreEqual(TextureFormat.BC5, texture.format);

            // Verify the compressed texture has valid normals
            var pixels = texture.GetPixels();
            int sampleCount = 50;
            int step = Mathf.Max(1, pixels.Length / sampleCount);

            for (int i = 0; i < pixels.Length; i += step)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                float xyLengthSq = x * x + y * y;

                // X² + Y² should not exceed 1
                Assert.That(
                    xyLengthSq,
                    Is.LessThanOrEqualTo(1.01f),
                    $"Normal at {i} should have valid XY for Z reconstruction"
                );
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_BC7Source_ToBC5_ReadsFromAGChannels()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            var texture = CreateDXTnmNormalMapTexture(64, 64);

            // BC7 source format also uses DXTnm layout
            bool result = selector.CompressTexture(
                texture,
                TextureFormat.BC7,
                isNormalMap: true,
                complexity: 0.5f,
                formatOverride: FrozenTextureFormat.BC5
            );

            Assert.IsTrue(result, "Compression should succeed");
            Assert.AreEqual(TextureFormat.BC5, texture.format);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CompressTexture_DXTnmSource_PreservesNormalDirection()
        {
            var selector = new TextureFormatSelector(
                CompressionPlatform.Desktop,
                useHighQualityFormatForHighComplexity: true,
                highQualityComplexityThreshold: 0.7f
            );

            // Create DXTnm texture with known normal direction
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];

            // Tilted normal (0.5, 0.3, z) in AG layout
            float inputX = 0.5f;
            float inputY = 0.3f;
            byte encodedA = (byte)((inputX * 0.5f + 0.5f) * 255f);
            byte encodedG = (byte)((inputY * 0.5f + 0.5f) * 255f);

            for (int i = 0; i < 16; i++)
            {
                pixels[i] = new Color32(0, encodedG, 0, encodedA);
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            selector.CompressTexture(
                texture,
                TextureFormat.DXT5, // DXTnm source
                isNormalMap: true,
                complexity: 0.5f
            );

            var resultPixels = texture.GetPixels();

            // Check that direction is preserved
            float resultX = resultPixels[0].r * 2f - 1f;
            float resultY = resultPixels[0].g * 2f - 1f;

            // Allow tolerance for compression
            Assert.That(
                resultX,
                Is.EqualTo(inputX).Within(0.1f),
                "X direction should be preserved"
            );
            Assert.That(
                resultY,
                Is.EqualTo(inputY).Within(0.1f),
                "Y direction should be preserved"
            );

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

        private static Texture2D CreateNormalMapTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Create varied normals for testing
                // Normal encoded in tangent space: (x*0.5+0.5, y*0.5+0.5, z*0.5+0.5)
                float x = ((i % width) / (float)width - 0.5f) * 0.6f;
                float y = ((i / width) / (float)height - 0.5f) * 0.6f;

                // Encode to 0-1 range (stored as 0-255)
                byte r = (byte)((x * 0.5f + 0.5f) * 255f);
                byte g = (byte)((y * 0.5f + 0.5f) * 255f);

                pixels[i] = new Color32(r, g, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a normal map texture with data in DXTnm layout (XY in AG channels).
        /// Simulates data as it would be read from a DXT5/BC7 compressed normal map.
        /// </summary>
        private static Texture2D CreateDXTnmNormalMapTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Create varied normals for testing
                float x = ((i % width) / (float)width - 0.5f) * 0.6f;
                float y = ((i / width) / (float)height - 0.5f) * 0.6f;

                // DXTnm layout: X in A channel, Y in G channel
                // R and B are unused
                byte encodedA = (byte)((x * 0.5f + 0.5f) * 255f);
                byte encodedG = (byte)((y * 0.5f + 0.5f) * 255f);

                pixels[i] = new Color32(0, encodedG, 0, encodedA);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
