using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PresetEditTransitionTests
    {
        private GameObject _gameObject;
        private TextureCompressor _config;
        private CustomTextureCompressorPreset _preset;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestCompressor");
            _config = _gameObject.AddComponent<TextureCompressor>();
            _preset = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);
            if (_preset != null)
                Object.DestroyImmediate(_preset);
        }

        #region TryEnterEditMode Tests - Direct Edit Cases

        [Test]
        public void TryEnterEditMode_WithNullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => PresetEditTransition.TryEnterEditMode(null));
        }

        [Test]
        public void TryEnterEditMode_WithNoPreset_SwitchesToEditMode()
        {
            _config.CustomPresetAsset = null;

            PresetEditTransition.TryEnterEditMode(_config);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.True);
        }

        [Test]
        public void TryEnterEditMode_WithUnlockedPreset_SwitchesToEditModeWithoutUnlink()
        {
            _preset.Lock = false;
            _config.CustomPresetAsset = _preset;

            PresetEditTransition.TryEnterEditMode(_config);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.True);
            Assert.That(_config.CustomPresetAsset, Is.EqualTo(_preset));
        }

        #endregion

        #region PresetRestriction Tests

        [Test]
        public void PresetRestriction_None_CanDirectEdit()
        {
            var restriction = PresetRestriction.None;

            Assert.That(restriction.CanDirectEdit(), Is.True);
            Assert.That(restriction.RequiresUnlink(), Is.False);
        }

        [Test]
        public void PresetRestriction_Locked_RequiresUnlink()
        {
            var restriction = PresetRestriction.Locked;

            Assert.That(restriction.CanDirectEdit(), Is.False);
            Assert.That(restriction.RequiresUnlink(), Is.True);
        }

        [Test]
        public void PresetRestriction_ExternalPackage_RequiresUnlink()
        {
            var restriction = PresetRestriction.ExternalPackage;

            Assert.That(restriction.CanDirectEdit(), Is.False);
            Assert.That(restriction.RequiresUnlink(), Is.True);
        }

        [Test]
        public void PresetRestriction_BuiltIn_RequiresUnlink()
        {
            var restriction = PresetRestriction.BuiltIn;

            Assert.That(restriction.CanDirectEdit(), Is.False);
            Assert.That(restriction.RequiresUnlink(), Is.True);
        }

        #endregion

        #region GetRestriction Tests

        [Test]
        public void GetRestriction_WithNullConfig_ReturnsNone()
        {
            var restriction = PresetEditorState.GetRestriction(null);

            Assert.That(restriction.CanDirectEdit(), Is.True);
            Assert.That(restriction.RequiresUnlink(), Is.False);
        }

        [Test]
        public void GetRestriction_WithNoPreset_ReturnsNone()
        {
            _config.CustomPresetAsset = null;

            var restriction = PresetEditorState.GetRestriction(_config);

            Assert.That(restriction.CanDirectEdit(), Is.True);
            Assert.That(restriction.RequiresUnlink(), Is.False);
        }

        [Test]
        public void GetRestriction_WithUnlockedPreset_ReturnsNone()
        {
            _preset.Lock = false;
            _config.CustomPresetAsset = _preset;

            var restriction = PresetEditorState.GetRestriction(_config);

            Assert.That(restriction.CanDirectEdit(), Is.True);
            Assert.That(restriction.RequiresUnlink(), Is.False);
        }

        [Test]
        public void GetRestriction_WithLockedPreset_ReturnsLocked()
        {
            _preset.Lock = true;
            _config.CustomPresetAsset = _preset;

            var restriction = PresetEditorState.GetRestriction(_config);

            Assert.That(restriction.CanDirectEdit(), Is.False);
            Assert.That(restriction.RequiresUnlink(), Is.True);
            Assert.That(restriction, Is.EqualTo(PresetRestriction.Locked));
        }

        #endregion
    }
}
