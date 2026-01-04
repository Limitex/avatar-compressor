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

            Assert.IsNull(settings.TexturePath);
            Assert.AreEqual(1, settings.Divisor);
            Assert.AreEqual(FrozenTextureFormat.Auto, settings.Format);
            Assert.IsFalse(settings.Skip);
        }

        [Test]
        public void PathConstructor_SetsPathAndDefaults()
        {
            var path = "Assets/Textures/test.png";
            var settings = new FrozenTextureSettings(path);

            Assert.AreEqual(path, settings.TexturePath);
            Assert.AreEqual(1, settings.Divisor);
            Assert.AreEqual(FrozenTextureFormat.Auto, settings.Format);
            Assert.IsFalse(settings.Skip);
        }

        [Test]
        public void FullConstructor_SetsAllValues()
        {
            var path = "Assets/Textures/test.png";
            var settings = new FrozenTextureSettings(path, 4, FrozenTextureFormat.BC7, true);

            Assert.AreEqual(path, settings.TexturePath);
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
        public void TexturePath_CanBeModified()
        {
            var settings = new FrozenTextureSettings();

            settings.TexturePath = "Assets/NewPath/texture.png";

            Assert.AreEqual("Assets/NewPath/texture.png", settings.TexturePath);
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
