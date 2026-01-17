using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.editor.texture.ui;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureFormatUtilsTests
    {
        #region GetDisplayName Tests

        [Test]
        public void GetDisplayName_DXT1_ReturnsDXT1()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.DXT1);

            Assert.That(result, Is.EqualTo("DXT1"));
        }

        [Test]
        public void GetDisplayName_DXT5_ReturnsDXT5()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.DXT5);

            Assert.That(result, Is.EqualTo("DXT5"));
        }

        [Test]
        public void GetDisplayName_BC5_ReturnsBC5()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.BC5);

            Assert.That(result, Is.EqualTo("BC5"));
        }

        [Test]
        public void GetDisplayName_BC7_ReturnsBC7()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.BC7);

            Assert.That(result, Is.EqualTo("BC7"));
        }

        [Test]
        public void GetDisplayName_ASTC4x4_ReturnsFormattedName()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.ASTC_4x4);

            Assert.That(result, Is.EqualTo("ASTC 4x4"));
        }

        [Test]
        public void GetDisplayName_ASTC6x6_ReturnsFormattedName()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.ASTC_6x6);

            Assert.That(result, Is.EqualTo("ASTC 6x6"));
        }

        [Test]
        public void GetDisplayName_ASTC8x8_ReturnsFormattedName()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.ASTC_8x8);

            Assert.That(result, Is.EqualTo("ASTC 8x8"));
        }

        [Test]
        public void GetDisplayName_UnknownFormat_ReturnsToString()
        {
            var result = TextureFormatUtils.GetDisplayName(TextureFormat.RGBA32);

            Assert.That(result, Is.EqualTo(TextureFormat.RGBA32.ToString()));
        }

        #endregion

        #region GetInfo Tests

        [Test]
        public void GetInfo_DXT1_ReturnsCorrectInfo()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.DXT1);

            Assert.That(result, Does.Contain("4 bpp"));
            Assert.That(result, Does.Contain("RGB"));
        }

        [Test]
        public void GetInfo_DXT5_ReturnsCorrectInfo()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.DXT5);

            Assert.That(result, Does.Contain("8 bpp"));
            Assert.That(result, Does.Contain("RGBA"));
        }

        [Test]
        public void GetInfo_BC5_MentionsNormalMaps()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.BC5);

            Assert.That(result, Does.Contain("normal"));
        }

        [Test]
        public void GetInfo_BC7_MentionsHighQuality()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.BC7);

            Assert.That(result, Does.Contain("highest quality"));
        }

        [Test]
        public void GetInfo_ASTC4x4_ReturnsCorrectInfo()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.ASTC_4x4);

            Assert.That(result, Does.Contain("8 bpp"));
        }

        [Test]
        public void GetInfo_ASTC6x6_MentionsBalanced()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.ASTC_6x6);

            Assert.That(result, Does.Contain("balanced"));
        }

        [Test]
        public void GetInfo_ASTC8x8_MentionsEfficient()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.ASTC_8x8);

            Assert.That(result, Does.Contain("efficient"));
        }

        [Test]
        public void GetInfo_UnknownFormat_ReturnsEmptyString()
        {
            var result = TextureFormatUtils.GetInfo(TextureFormat.RGBA32);

            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GetColor Tests

        [Test]
        public void GetColor_BC7_ReturnsGreenish()
        {
            var result = TextureFormatUtils.GetColor(TextureFormat.BC7);

            // Green indicates high quality
            Assert.That(result.g, Is.GreaterThan(result.r));
        }

        [Test]
        public void GetColor_ASTC4x4_ReturnsGreenish()
        {
            var result = TextureFormatUtils.GetColor(TextureFormat.ASTC_4x4);

            Assert.That(result.g, Is.GreaterThan(result.r));
        }

        [Test]
        public void GetColor_BC5_ReturnsCyanish()
        {
            var result = TextureFormatUtils.GetColor(TextureFormat.BC5);

            // Cyan for normal maps
            Assert.That(result.b, Is.GreaterThan(result.r));
            Assert.That(result.g, Is.GreaterThan(result.r));
        }

        [Test]
        public void GetColor_DXT5_ReturnsYellowish()
        {
            var result = TextureFormatUtils.GetColor(TextureFormat.DXT5);

            // Yellow for balanced
            Assert.That(result.r, Is.GreaterThan(result.b));
            Assert.That(result.g, Is.GreaterThan(result.b));
        }

        [Test]
        public void GetColor_DXT1_ReturnsOrangeish()
        {
            var result = TextureFormatUtils.GetColor(TextureFormat.DXT1);

            // Orange for efficient
            Assert.That(result.r, Is.GreaterThan(result.b));
            Assert.That(result.r, Is.GreaterThan(result.g));
        }

        [Test]
        public void GetColor_UnknownFormat_ReturnsWhite()
        {
            var result = TextureFormatUtils.GetColor(TextureFormat.RGBA32);

            Assert.That(result, Is.EqualTo(Color.white));
        }

        [Test]
        public void GetColor_AllFormats_ReturnValidColors()
        {
            var formats = new[]
            {
                TextureFormat.DXT1,
                TextureFormat.DXT5,
                TextureFormat.BC5,
                TextureFormat.BC7,
                TextureFormat.ASTC_4x4,
                TextureFormat.ASTC_6x6,
                TextureFormat.ASTC_8x8
            };

            foreach (var format in formats)
            {
                var color = TextureFormatUtils.GetColor(format);

                Assert.That(color.r, Is.InRange(0f, 1f), $"Red channel out of range for {format}");
                Assert.That(color.g, Is.InRange(0f, 1f), $"Green channel out of range for {format}");
                Assert.That(color.b, Is.InRange(0f, 1f), $"Blue channel out of range for {format}");
            }
        }

        #endregion
    }
}
