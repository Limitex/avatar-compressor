using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class CustomPresetEditTransitionTests
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
            Assert.DoesNotThrow(() => CustomPresetEditTransition.TryEnterEditMode(null));
        }

        [Test]
        public void TryEnterEditMode_WithNoPreset_SwitchesToEditMode()
        {
            _config.CustomPresetAsset = null;

            CustomPresetEditTransition.TryEnterEditMode(_config);

            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.True);
        }

        [Test]
        public void TryEnterEditMode_WithUnlockedPreset_SwitchesToEditModeWithoutUnlink()
        {
            _preset.Lock = false;
            _config.CustomPresetAsset = _preset;

            CustomPresetEditTransition.TryEnterEditMode(_config);

            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.True);
            Assert.That(_config.CustomPresetAsset, Is.EqualTo(_preset));
        }

        #endregion

        #region EditRestrictionInfo Tests

        [Test]
        public void EditRestrictionInfo_Default_CanDirectEdit()
        {
            var info = new EditRestrictionInfo(false, false);

            Assert.That(info.CanDirectEdit, Is.True);
            Assert.That(info.RequiresUnlink, Is.False);
        }

        [Test]
        public void EditRestrictionInfo_Locked_RequiresUnlink()
        {
            var info = new EditRestrictionInfo(true, false);

            Assert.That(info.CanDirectEdit, Is.False);
            Assert.That(info.RequiresUnlink, Is.True);
            Assert.That(info.IsLocked, Is.True);
            Assert.That(info.IsBuiltIn, Is.False);
        }

        [Test]
        public void EditRestrictionInfo_BuiltIn_RequiresUnlink()
        {
            var info = new EditRestrictionInfo(false, true);

            Assert.That(info.CanDirectEdit, Is.False);
            Assert.That(info.RequiresUnlink, Is.True);
            Assert.That(info.IsLocked, Is.False);
            Assert.That(info.IsBuiltIn, Is.True);
        }

        [Test]
        public void EditRestrictionInfo_LockedAndBuiltIn_RequiresUnlink()
        {
            var info = new EditRestrictionInfo(true, true);

            Assert.That(info.CanDirectEdit, Is.False);
            Assert.That(info.RequiresUnlink, Is.True);
            Assert.That(info.IsLocked, Is.True);
            Assert.That(info.IsBuiltIn, Is.True);
        }

        #endregion

        #region GetEditRestriction Tests

        [Test]
        public void GetEditRestriction_WithNullConfig_ReturnsNoRestriction()
        {
            var restriction = CustomPresetEditorState.GetEditRestriction(null);

            Assert.That(restriction.CanDirectEdit, Is.True);
            Assert.That(restriction.RequiresUnlink, Is.False);
        }

        [Test]
        public void GetEditRestriction_WithNoPreset_ReturnsNoRestriction()
        {
            _config.CustomPresetAsset = null;

            var restriction = CustomPresetEditorState.GetEditRestriction(_config);

            Assert.That(restriction.CanDirectEdit, Is.True);
            Assert.That(restriction.RequiresUnlink, Is.False);
        }

        [Test]
        public void GetEditRestriction_WithUnlockedPreset_ReturnsNoRestriction()
        {
            _preset.Lock = false;
            _config.CustomPresetAsset = _preset;

            var restriction = CustomPresetEditorState.GetEditRestriction(_config);

            Assert.That(restriction.CanDirectEdit, Is.True);
            Assert.That(restriction.RequiresUnlink, Is.False);
        }

        [Test]
        public void GetEditRestriction_WithLockedPreset_ReturnsLocked()
        {
            _preset.Lock = true;
            _config.CustomPresetAsset = _preset;

            var restriction = CustomPresetEditorState.GetEditRestriction(_config);

            Assert.That(restriction.CanDirectEdit, Is.False);
            Assert.That(restriction.RequiresUnlink, Is.True);
            Assert.That(restriction.IsLocked, Is.True);
        }

        #endregion
    }
}
