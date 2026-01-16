using NUnit.Framework;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class FrozenTextureSettingsTests
    {
        #region Constructor Tests

        [Test]
        public void DefaultConstructor_SetsDefaultValues()
        {
            var settings = new FrozenTextureSettings();

            Assert.IsNull(settings.TextureGuid);
            Assert.AreEqual(1, settings.Divisor);
            Assert.AreEqual(FrozenTextureFormat.Auto, settings.Format);
            Assert.IsFalse(settings.Skip);
        }

        [Test]
        public void GuidConstructor_SetsGuidAndDefaults()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            var settings = new FrozenTextureSettings(guid);

            Assert.AreEqual(guid, settings.TextureGuid);
            Assert.AreEqual(1, settings.Divisor);
            Assert.AreEqual(FrozenTextureFormat.Auto, settings.Format);
            Assert.IsFalse(settings.Skip);
        }

        [Test]
        public void FullConstructor_SetsAllValues()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            var settings = new FrozenTextureSettings(guid, 4, FrozenTextureFormat.BC7, true);

            Assert.AreEqual(guid, settings.TextureGuid);
            Assert.AreEqual(4, settings.Divisor);
            Assert.AreEqual(FrozenTextureFormat.BC7, settings.Format);
            Assert.IsTrue(settings.Skip);
        }

        #endregion

        #region Property Modification Tests

        [Test]
        public void Divisor_CanBeModified()
        {
            var settings = new FrozenTextureSettings();

            settings.Divisor = 8;

            Assert.AreEqual(8, settings.Divisor);
        }

        [Test]
        public void Format_CanBeModified()
        {
            var settings = new FrozenTextureSettings();

            settings.Format = FrozenTextureFormat.DXT5;

            Assert.AreEqual(FrozenTextureFormat.DXT5, settings.Format);
        }

        [Test]
        public void Skip_CanBeModified()
        {
            var settings = new FrozenTextureSettings();

            settings.Skip = true;

            Assert.IsTrue(settings.Skip);
        }

        [Test]
        public void TextureGuid_CanBeModified()
        {
            var settings = new FrozenTextureSettings();

            settings.TextureGuid = "0123456789abcdef0123456789abcdef";

            Assert.AreEqual("0123456789abcdef0123456789abcdef", settings.TextureGuid);
        }

        #endregion
    }

    [TestFixture]
    public class FrozenTextureFormatTests
    {
        #region Enum Value Tests

        [Test]
        public void FrozenTextureFormat_AllValuesAreDefined()
        {
            var values = System.Enum.GetValues(typeof(FrozenTextureFormat));

            Assert.That(values, Contains.Item(FrozenTextureFormat.Auto));
            Assert.That(values, Contains.Item(FrozenTextureFormat.DXT1));
            Assert.That(values, Contains.Item(FrozenTextureFormat.DXT5));
            Assert.That(values, Contains.Item(FrozenTextureFormat.BC5));
            Assert.That(values, Contains.Item(FrozenTextureFormat.BC7));
            Assert.That(values, Contains.Item(FrozenTextureFormat.ASTC_4x4));
            Assert.That(values, Contains.Item(FrozenTextureFormat.ASTC_6x6));
            Assert.That(values, Contains.Item(FrozenTextureFormat.ASTC_8x8));
        }

        [Test]
        public void FrozenTextureFormat_HasExpectedCount()
        {
            var values = System.Enum.GetValues(typeof(FrozenTextureFormat));

            // Auto + 4 desktop formats + 3 mobile formats = 8
            Assert.AreEqual(8, values.Length);
        }

        #endregion
    }
}
