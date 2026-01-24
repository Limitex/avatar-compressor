using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class CustomPresetEditorStateTests
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
            // Clear all cached states to ensure clean state for next test
            CustomPresetEditorState.ClearAllStates();

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
            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.False);
        }

        [Test]
        public void IsInEditMode_WithNullConfig_ReturnsFalse()
        {
            Assert.That(CustomPresetEditorState.IsInEditMode(null), Is.False);
        }

        [Test]
        public void SetEditMode_True_SetsEditMode()
        {
            CustomPresetEditorState.SetEditMode(_config, true);

            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.True);
        }

        [Test]
        public void SetEditMode_False_ClearsEditMode()
        {
            CustomPresetEditorState.SetEditMode(_config, true);
            CustomPresetEditorState.SetEditMode(_config, false);

            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.False);
        }

        #endregion

        #region IsInUseOnlyMode Tests

        [Test]
        public void IsInUseOnlyMode_TrueWhenPresetAssignedAndNotEditing()
        {
            _config.CustomPresetAsset = _preset;
            CustomPresetEditorState.SetEditMode(_config, false);

            Assert.That(CustomPresetEditorState.IsInUseOnlyMode(_config), Is.True);
        }

        [Test]
        public void IsInUseOnlyMode_FalseWhenNoPresetAssigned()
        {
            _config.CustomPresetAsset = null;
            CustomPresetEditorState.SetEditMode(_config, false);

            Assert.That(CustomPresetEditorState.IsInUseOnlyMode(_config), Is.False);
        }

        [Test]
        public void IsInUseOnlyMode_FalseWhenInEditMode()
        {
            _config.CustomPresetAsset = _preset;
            CustomPresetEditorState.SetEditMode(_config, true);

            Assert.That(CustomPresetEditorState.IsInUseOnlyMode(_config), Is.False);
        }

        [Test]
        public void IsInUseOnlyMode_WithNullConfig_ReturnsFalse()
        {
            Assert.That(CustomPresetEditorState.IsInUseOnlyMode(null), Is.False);
        }

        #endregion

        #region SwitchToEditMode Tests

        [Test]
        public void SwitchToEditMode_SetsEditModeTrue()
        {
            CustomPresetEditorState.SetEditMode(_config, false);

            CustomPresetEditorState.SwitchToEditMode(_config);

            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.True);
        }

        [Test]
        public void SwitchToEditMode_PreservesCustomPresetAsset()
        {
            _config.CustomPresetAsset = _preset;
            CustomPresetEditorState.SetEditMode(_config, false);

            CustomPresetEditorState.SwitchToEditMode(_config);

            Assert.That(_config.CustomPresetAsset, Is.EqualTo(_preset));
        }

        [Test]
        public void SwitchToEditMode_WithNullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CustomPresetEditorState.SwitchToEditMode(null));
        }

        #endregion

        #region ApplyPresetAndSwitchToUseOnly Tests

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_SetsPresetToCustom()
        {
            _config.Preset = CompressorPreset.Balanced;

            CustomPresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Custom));
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_AssignsPresetAsset()
        {
            _config.CustomPresetAsset = null;

            CustomPresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(_config.CustomPresetAsset, Is.EqualTo(_preset));
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_ExitsEditMode()
        {
            CustomPresetEditorState.SetEditMode(_config, true);

            CustomPresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.False);
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_AppliesPresetSettings()
        {
            _preset.MaxDivisor = 12;
            _preset.Strategy = AnalysisStrategyType.Fast;

            CustomPresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, _preset);

            Assert.That(_config.MaxDivisor, Is.EqualTo(12));
            Assert.That(_config.Strategy, Is.EqualTo(AnalysisStrategyType.Fast));
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_WithNullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                CustomPresetEditorState.ApplyPresetAndSwitchToUseOnly(null, _preset)
            );
        }

        [Test]
        public void ApplyPresetAndSwitchToUseOnly_WithNullPreset_DoesNotChangeState()
        {
            _config.Preset = CompressorPreset.Balanced;
            CustomPresetEditorState.SetEditMode(_config, true);

            CustomPresetEditorState.ApplyPresetAndSwitchToUseOnly(_config, null);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Balanced));
            Assert.That(CustomPresetEditorState.IsInEditMode(_config), Is.True);
        }

        #endregion

        #region IsCustomEditable Tests

        [Test]
        public void IsCustomEditable_WithNullConfig_ReturnsFalse()
        {
            Assert.That(CustomPresetEditorState.IsCustomEditable(null), Is.False);
        }

        [Test]
        public void IsCustomEditable_WithNonCustomPreset_ReturnsFalse()
        {
            _config.Preset = CompressorPreset.Balanced;

            Assert.That(CustomPresetEditorState.IsCustomEditable(_config), Is.False);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithNoAsset_ReturnsTrue()
        {
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = null;

            Assert.That(CustomPresetEditorState.IsCustomEditable(_config), Is.True);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithAssetInEditMode_ReturnsTrue()
        {
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = _preset;
            CustomPresetEditorState.SetEditMode(_config, true);

            Assert.That(CustomPresetEditorState.IsCustomEditable(_config), Is.True);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithAssetInUseOnlyMode_ReturnsFalse()
        {
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = _preset;
            CustomPresetEditorState.SetEditMode(_config, false);

            Assert.That(CustomPresetEditorState.IsCustomEditable(_config), Is.False);
        }

        [Test]
        public void IsCustomEditable_CustomPresetWithAssetDefaultState_ReturnsFalse()
        {
            // When no explicit edit mode is set, defaults to use-only mode
            _config.Preset = CompressorPreset.Custom;
            _config.CustomPresetAsset = _preset;

            Assert.That(CustomPresetEditorState.IsCustomEditable(_config), Is.False);
        }

        #endregion

        #region Cache State Tests

        [Test]
        public void CachedStateCount_InitiallyZero()
        {
            CustomPresetEditorState.ClearAllStates();

            Assert.That(CustomPresetEditorState.CachedStateCount, Is.EqualTo(0));
        }

        [Test]
        public void CachedStateCount_IncreasesWhenSettingState()
        {
            CustomPresetEditorState.ClearAllStates();

            CustomPresetEditorState.SetEditMode(_config, true);

            Assert.That(CustomPresetEditorState.CachedStateCount, Is.EqualTo(1));
        }

        [Test]
        public void HasStateFor_ReturnsTrueAfterSetting()
        {
            CustomPresetEditorState.ClearAllStates();

            CustomPresetEditorState.SetEditMode(_config, true);

            Assert.That(CustomPresetEditorState.HasStateFor(_config), Is.True);
        }

        [Test]
        public void HasStateFor_ReturnsFalseBeforeSetting()
        {
            CustomPresetEditorState.ClearAllStates();

            Assert.That(CustomPresetEditorState.HasStateFor(_config), Is.False);
        }

        [Test]
        public void HasStateFor_WithNullConfig_ReturnsFalse()
        {
            Assert.That(CustomPresetEditorState.HasStateFor(null), Is.False);
        }

        [Test]
        public void ClearAllStates_RemovesAllEntries()
        {
            CustomPresetEditorState.SetEditMode(_config, true);

            CustomPresetEditorState.ClearAllStates();

            Assert.That(CustomPresetEditorState.CachedStateCount, Is.EqualTo(0));
            Assert.That(CustomPresetEditorState.HasStateFor(_config), Is.False);
        }

        #endregion
    }
}
