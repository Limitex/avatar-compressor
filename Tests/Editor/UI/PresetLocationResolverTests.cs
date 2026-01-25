using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PresetLocationResolverTests
    {
        private CustomTextureCompressorPreset _preset;

        [SetUp]
        public void SetUp()
        {
            _preset = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_preset != null)
            {
                Object.DestroyImmediate(_preset);
            }
        }

        #region GetRestriction Tests

        [Test]
        public void GetRestriction_WithNullPreset_ReturnsNone()
        {
            var restriction = PresetLocationResolver.GetRestriction(null);

            Assert.That(restriction, Is.EqualTo(PresetRestriction.None));
        }

        [Test]
        public void GetRestriction_WithUnlockedPreset_ReturnsNone()
        {
            // In-memory preset (no asset path) with Lock=false
            _preset.Lock = false;

            var restriction = PresetLocationResolver.GetRestriction(_preset);

            Assert.That(restriction, Is.EqualTo(PresetRestriction.None));
        }

        [Test]
        public void GetRestriction_WithLockedPreset_ReturnsLocked()
        {
            // In-memory preset (no asset path) with Lock=true
            _preset.Lock = true;

            var restriction = PresetLocationResolver.GetRestriction(_preset);

            Assert.That(restriction, Is.EqualTo(PresetRestriction.Locked));
        }

        #endregion

        #region PresetRestriction Extension Tests

        [Test]
        public void CanDirectEdit_None_ReturnsTrue()
        {
            Assert.That(PresetRestriction.None.CanDirectEdit(), Is.True);
        }

        [Test]
        public void CanDirectEdit_NonNone_ReturnsFalse()
        {
            Assert.That(PresetRestriction.Locked.CanDirectEdit(), Is.False);
            Assert.That(PresetRestriction.ExternalPackage.CanDirectEdit(), Is.False);
            Assert.That(PresetRestriction.BuiltIn.CanDirectEdit(), Is.False);
        }

        [Test]
        public void RequiresUnlink_None_ReturnsFalse()
        {
            Assert.That(PresetRestriction.None.RequiresUnlink(), Is.False);
        }

        [Test]
        public void RequiresUnlink_NonNone_ReturnsTrue()
        {
            Assert.That(PresetRestriction.Locked.RequiresUnlink(), Is.True);
            Assert.That(PresetRestriction.ExternalPackage.RequiresUnlink(), Is.True);
            Assert.That(PresetRestriction.BuiltIn.RequiresUnlink(), Is.True);
        }

        #endregion
    }
}
