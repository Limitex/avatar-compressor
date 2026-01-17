using dev.limitex.avatar.compressor;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class FrozenTextureOperationsTests
    {
        private GameObject _testObject;
        private TextureCompressor _config;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestAvatar");
            _config = _testObject.AddComponent<TextureCompressor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region Initial State Tests

        [Test]
        public void NewConfig_FrozenTexturesIsEmpty()
        {
            Assert.That(_config.FrozenTextures, Is.Not.Null);
            Assert.That(_config.FrozenTextures.Count, Is.EqualTo(0));
        }

        [Test]
        public void NewConfig_IsFrozenReturnsFalse()
        {
            Assert.That(_config.IsFrozen("any-guid"), Is.False);
        }

        #endregion

        #region Freeze Texture Tests

        [Test]
        public void SetFrozenSettings_AddsNewEntry()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );

            _config.SetFrozenSettings("test-guid", settings);

            Assert.That(_config.FrozenTextures.Count, Is.EqualTo(1));
        }

        [Test]
        public void SetFrozenSettings_IsFrozenReturnsTrue()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );

            _config.SetFrozenSettings("test-guid", settings);

            Assert.That(_config.IsFrozen("test-guid"), Is.True);
        }

        [Test]
        public void SetFrozenSettings_UpdatesExistingEntry()
        {
            var settings1 = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );
            var settings2 = new FrozenTextureSettings(
                "test-guid",
                4,
                FrozenTextureFormat.BC7,
                false
            );

            _config.SetFrozenSettings("test-guid", settings1);
            _config.SetFrozenSettings("test-guid", settings2);

            Assert.That(_config.FrozenTextures.Count, Is.EqualTo(1));
            Assert.That(_config.GetFrozenSettings("test-guid").Divisor, Is.EqualTo(4));
        }

        [Test]
        public void SetFrozenSettings_MultipleDifferentTextures_WorksCorrectly()
        {
            _config.SetFrozenSettings(
                "guid-1",
                new FrozenTextureSettings("guid-1", 2, FrozenTextureFormat.Auto, false)
            );
            _config.SetFrozenSettings(
                "guid-2",
                new FrozenTextureSettings("guid-2", 4, FrozenTextureFormat.Auto, false)
            );
            _config.SetFrozenSettings(
                "guid-3",
                new FrozenTextureSettings("guid-3", 8, FrozenTextureFormat.Auto, false)
            );

            Assert.That(_config.FrozenTextures.Count, Is.EqualTo(3));
            Assert.That(_config.IsFrozen("guid-1"), Is.True);
            Assert.That(_config.IsFrozen("guid-2"), Is.True);
            Assert.That(_config.IsFrozen("guid-3"), Is.True);
        }

        #endregion

        #region Unfreeze Texture Tests

        [Test]
        public void UnfreezeTexture_RemovesEntry()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );
            _config.SetFrozenSettings("test-guid", settings);

            _config.UnfreezeTexture("test-guid");

            Assert.That(_config.FrozenTextures.Count, Is.EqualTo(0));
        }

        [Test]
        public void UnfreezeTexture_IsFrozenReturnsFalse()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );
            _config.SetFrozenSettings("test-guid", settings);

            _config.UnfreezeTexture("test-guid");

            Assert.That(_config.IsFrozen("test-guid"), Is.False);
        }

        [Test]
        public void UnfreezeTexture_NonExistentGuid_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _config.UnfreezeTexture("non-existent-guid"));
        }

        [Test]
        public void UnfreezeTexture_OnlyRemovesTargetEntry()
        {
            _config.SetFrozenSettings(
                "guid-1",
                new FrozenTextureSettings("guid-1", 2, FrozenTextureFormat.Auto, false)
            );
            _config.SetFrozenSettings(
                "guid-2",
                new FrozenTextureSettings("guid-2", 4, FrozenTextureFormat.Auto, false)
            );

            _config.UnfreezeTexture("guid-1");

            Assert.That(_config.FrozenTextures.Count, Is.EqualTo(1));
            Assert.That(_config.IsFrozen("guid-1"), Is.False);
            Assert.That(_config.IsFrozen("guid-2"), Is.True);
        }

        #endregion

        #region Frozen Settings Modification Tests

        [Test]
        public void FrozenSettings_DivisorCanBeModified()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );
            _config.SetFrozenSettings("test-guid", settings);

            var frozen = _config.GetFrozenSettings("test-guid");
            frozen.Divisor = 8;

            Assert.That(_config.GetFrozenSettings("test-guid").Divisor, Is.EqualTo(8));
        }

        [Test]
        public void FrozenSettings_FormatCanBeModified()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );
            _config.SetFrozenSettings("test-guid", settings);

            var frozen = _config.GetFrozenSettings("test-guid");
            frozen.Format = FrozenTextureFormat.BC7;

            Assert.That(
                _config.GetFrozenSettings("test-guid").Format,
                Is.EqualTo(FrozenTextureFormat.BC7)
            );
        }

        [Test]
        public void FrozenSettings_SkipCanBeModified()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                2,
                FrozenTextureFormat.Auto,
                false
            );
            _config.SetFrozenSettings("test-guid", settings);

            var frozen = _config.GetFrozenSettings("test-guid");
            frozen.Skip = true;

            Assert.That(_config.GetFrozenSettings("test-guid").Skip, Is.True);
        }

        #endregion

        #region GetFrozenSettings Tests

        [Test]
        public void GetFrozenSettings_ExistingGuid_ReturnsSettings()
        {
            var settings = new FrozenTextureSettings(
                "test-guid",
                4,
                FrozenTextureFormat.DXT5,
                true
            );
            _config.SetFrozenSettings("test-guid", settings);

            var result = _config.GetFrozenSettings("test-guid");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Divisor, Is.EqualTo(4));
            Assert.That(result.Format, Is.EqualTo(FrozenTextureFormat.DXT5));
            Assert.That(result.Skip, Is.True);
        }

        [Test]
        public void GetFrozenSettings_NonExistentGuid_ReturnsNull()
        {
            var result = _config.GetFrozenSettings("non-existent-guid");

            Assert.That(result, Is.Null);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void FrozenTextures_EmptyGuid_HandledGracefully()
        {
            Assert.DoesNotThrow(() => _config.IsFrozen(""));
            Assert.That(_config.IsFrozen(""), Is.False);
        }

        [Test]
        public void FrozenTextures_NullGuid_HandledGracefully()
        {
            Assert.DoesNotThrow(() => _config.IsFrozen(null));
            Assert.That(_config.IsFrozen(null), Is.False);
        }

        [Test]
        public void FrozenTextures_DivisorBoundaries_Valid()
        {
            int[] validDivisors = { 1, 2, 4, 8, 16 };

            foreach (int divisor in validDivisors)
            {
                var settings = new FrozenTextureSettings(
                    $"guid-{divisor}",
                    divisor,
                    FrozenTextureFormat.Auto,
                    false
                );
                Assert.DoesNotThrow(
                    () => _config.SetFrozenSettings($"guid-{divisor}", settings),
                    $"Divisor {divisor} should be valid"
                );
            }
        }

        [Test]
        public void FrozenTextures_AllFormats_Valid()
        {
            foreach (
                FrozenTextureFormat format in System.Enum.GetValues(typeof(FrozenTextureFormat))
            )
            {
                var settings = new FrozenTextureSettings($"guid-{format}", 2, format, false);
                Assert.DoesNotThrow(
                    () => _config.SetFrozenSettings($"guid-{format}", settings),
                    $"Format {format} should be valid"
                );
            }
        }

        #endregion
    }
}
