using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PresetEditorStateTests
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
            {
                Object.DestroyImmediate(_gameObject);
            }
            if (_preset != null)
            {
                Object.DestroyImmediate(_preset);
            }
        }

        #region IsInEditMode Tests

        [Test]
        public void IsInEditMode_DefaultIsFalse()
        {
            Assert.That(PresetEditorState.IsInEditMode(_config), Is.False);
        }

        [Test]
        public void IsInEditMode_WithNullConfig_ReturnsFalse()
        {
            Assert.That(PresetEditorState.IsInEditMode(null), Is.False);
        }

        [Test]
        public void SetEditMode_True_SetsEditMode()
        {
            PresetEditorState.SetEditMode(_config, true);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.True);
        }

        [Test]
        public void SetEditMode_False_ClearsEditMode()
        {
            PresetEditorState.SetEditMode(_config, true);
            PresetEditorState.SetEditMode(_config, false);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.False);
        }

        #endregion

        #region SwitchToEditMode Tests

        [Test]
        public void SwitchToEditMode_SetsEditModeTrue()
        {
            PresetEditorState.SetEditMode(_config, false);

            PresetEditorState.SwitchToEditMode(_config);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.True);
        }

        [Test]
        public void SwitchToEditMode_PreservesCustomPresetAsset()
        {
            _config.CustomPresetAsset = _preset;
            PresetEditorState.SetEditMode(_config, false);

            PresetEditorState.SwitchToEditMode(_config);

            Assert.That(_config.CustomPresetAsset, Is.EqualTo(_preset));
        }

        [Test]
        public void SwitchToEditMode_WithNullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => PresetEditorState.SwitchToEditMode(null));
        }

        [Test]
        public void SwitchToEditMode_WithLockedPreset_DoesNotSwitchToEditMode()
        {
            _preset.Lock = true;
            _config.CustomPresetAsset = _preset;
            PresetEditorState.SetEditMode(_config, false);

            PresetEditorState.SwitchToEditMode(_config);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.False);
        }

        #endregion

        #region ApplyPresetAndSwitchToUseOnly Tests

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_SetsPresetToCustom()
        {
            _config.Preset = CompressorPreset.Balanced;

            PresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Custom));
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_AssignsPresetAsset()
        {
            _config.CustomPresetAsset = null;

            PresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(_config.CustomPresetAsset, Is.EqualTo(_preset));
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_ExitsEditMode()
        {
            PresetEditorState.SetEditMode(_config, true);

            PresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.False);
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_AppliesPresetSettings()
        {
            _preset.MaxDivisor = 12;
            _preset.Strategy = AnalysisStrategyType.Fast;

            PresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(_config.MaxDivisor, Is.EqualTo(12));
            Assert.That(_config.Strategy, Is.EqualTo(AnalysisStrategyType.Fast));
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_WithNullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                PresetEditorState.ApplyPresetAndSwitchToUseOnly(null, _preset)
            );
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_WithNullPreset_DoesNotChangeState()
        {
            _config.Preset = CompressorPreset.Balanced;
            PresetEditorState.SetEditMode(_config, true);

            PresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, null);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Balanced));
            Assert.That(PresetEditorState.IsInEditMode(_config), Is.True);
        }

        #endregion

        #region IsCustomEditable Tests

        [Test]
        public void IsCustomEditable_WithNullConfig_ReturnsFalse()
        {
            Assert.That(PresetEditorState.IsCustomEditable(null), Is.False);
        }

        [Test]
        public void IsCustomEditable_WithNonCustomPreset_ReturnsFalse()
        {
            _config.Preset = CompressorPreset.Balanced;

            Assert.That(PresetEditorState.IsCustomEditable(_config), Is.False);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithNoAsset_ReturnsTrue()
        {
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = null;

            Assert.That(PresetEditorState.IsCustomEditable(_config), Is.True);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithAssetInEditMode_ReturnsTrue()
        {
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = _preset;
            PresetEditorState.SetEditMode(_config, true);

            Assert.That(PresetEditorState.IsCustomEditable(_config), Is.True);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithAssetInUseOnlyMode_ReturnsFalse()
        {
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = _preset;
            PresetEditorState.SetEditMode(_config, false);

            Assert.That(PresetEditorState.IsCustomEditable(_config), Is.False);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithAssetDefaultState_ReturnsFalse()
        {
            // When no explicit edit mode is set, defaults to use-only mode
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = _preset;

            Assert.That(PresetEditorState.IsCustomEditable(_config), Is.False);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithLockedAssetInEditMode_ReturnsFalse()
        {
            // Even in edit mode, locked presets should not be editable
            _preset.Lock = true;
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = _preset;
            PresetEditorState.SetEditMode(_config, true);

            Assert.That(PresetEditorState.IsCustomEditable(_config), Is.False);
        }

        #endregion

        #region UnlinkPresetAndSwitchToEditMode Tests

        [Test]
        public void UnlinkPresetAndSwitchToEditMode_WithNullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => PresetEditorState.UnlinkPresetAndSwitchToEditMode(null));
        }

        [Test]
        public void UnlinkPresetAndSwitchToEditMode_UnlinksPreset()
        {
            _config.CustomPresetAsset = _preset;

            PresetEditorState.UnlinkPresetAndSwitchToEditMode(_config);

            Assert.That(_config.CustomPresetAsset, Is.Null);
        }

        [Test]
        public void UnlinkPresetAndSwitchToEditMode_SwitchesToEditMode()
        {
            _config.CustomPresetAsset = _preset;
            PresetEditorState.SetEditMode(_config, false);

            PresetEditorState.UnlinkPresetAndSwitchToEditMode(_config);

            Assert.That(PresetEditorState.IsInEditMode(_config), Is.True);
        }

        [Test]
        public void UnlinkPresetAndSwitchToEditMode_WithLockedPreset_UnlinksAndSwitches()
        {
            _preset.Lock = true;
            _config.CustomPresetAsset = _preset;
            PresetEditorState.SetEditMode(_config, false);

            PresetEditorState.UnlinkPresetAndSwitchToEditMode(_config);

            Assert.That(_config.CustomPresetAsset, Is.Null);
            Assert.That(PresetEditorState.IsInEditMode(_config), Is.True);
        }

        #endregion
    }
}
