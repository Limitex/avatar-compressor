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
        private CustomCompressorPreset _preset;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("TestCompressor");
            _config = _gameObject.AddComponent<TextureCompressor>();
            _preset = ScriptableObject.CreateInstance<CustomCompressorPreset>();
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

        #region LRU Cache Eviction Tests

        [Test]
        public void LRU_CachedStateCount_InitiallyZero()
        {
            // Clear any existing state from other tests
            CustomPresetEditorState.ClearAllStates();

            Assert.That(CustomPresetEditorState.CachedStateCount, Is.EqualTo(0));
        }

        [Test]
        public void LRU_CachedStateCount_IncreasesWhenSettingState()
        {
            CustomPresetEditorState.ClearAllStates();

            CustomPresetEditorState.SetEditMode(_config, true);

            Assert.That(CustomPresetEditorState.CachedStateCount, Is.EqualTo(1));
        }

        [Test]
        public void LRU_HasStateFor_ReturnsTrueAfterSetting()
        {
            CustomPresetEditorState.ClearAllStates();

            CustomPresetEditorState.SetEditMode(_config, true);

            Assert.That(CustomPresetEditorState.HasStateFor(_config), Is.True);
        }

        [Test]
        public void LRU_HasStateFor_ReturnsFalseBeforeSetting()
        {
            CustomPresetEditorState.ClearAllStates();

            Assert.That(CustomPresetEditorState.HasStateFor(_config), Is.False);
        }

        [Test]
        public void LRU_ClearAllStates_RemovesAllEntries()
        {
            CustomPresetEditorState.SetEditMode(_config, true);

            CustomPresetEditorState.ClearAllStates();

            Assert.That(CustomPresetEditorState.CachedStateCount, Is.EqualTo(0));
            Assert.That(CustomPresetEditorState.HasStateFor(_config), Is.False);
        }

        [Test]
        public void LRU_EvictsOldestEntry_WhenCacheFull()
        {
            CustomPresetEditorState.ClearAllStates();

            // Create MaxCachedStates configs and set their state
            var gameObjects = new System.Collections.Generic.List<GameObject>();
            var configs = new System.Collections.Generic.List<TextureCompressor>();

            try
            {
                for (int i = 0; i < CustomPresetEditorState.MaxCachedStates; i++)
                {
                    var go = new GameObject($"TestCompressor_{i}");
                    gameObjects.Add(go);
                    var config = go.AddComponent<TextureCompressor>();
                    configs.Add(config);
                    CustomPresetEditorState.SetEditMode(config, true);
                }

                // Verify cache is at capacity
                Assert.That(
                    CustomPresetEditorState.CachedStateCount,
                    Is.EqualTo(CustomPresetEditorState.MaxCachedStates)
                );

                // First config should still have state
                Assert.That(CustomPresetEditorState.HasStateFor(configs[0]), Is.True);

                // Add one more config (beyond capacity)
                var extraGo = new GameObject("TestCompressor_Extra");
                gameObjects.Add(extraGo);
                var extraConfig = extraGo.AddComponent<TextureCompressor>();
                configs.Add(extraConfig);
                CustomPresetEditorState.SetEditMode(extraConfig, true);

                // Cache count should still be at max (one was evicted)
                Assert.That(
                    CustomPresetEditorState.CachedStateCount,
                    Is.EqualTo(CustomPresetEditorState.MaxCachedStates)
                );

                // The oldest entry (first config) should have been evicted
                Assert.That(CustomPresetEditorState.HasStateFor(configs[0]), Is.False);

                // The new entry should exist
                Assert.That(CustomPresetEditorState.HasStateFor(extraConfig), Is.True);
            }
            finally
            {
                // Cleanup
                CustomPresetEditorState.ClearAllStates();
                foreach (var go in gameObjects)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void LRU_UpdateExistingEntry_DoesNotEvict()
        {
            CustomPresetEditorState.ClearAllStates();

            var gameObjects = new System.Collections.Generic.List<GameObject>();
            var configs = new System.Collections.Generic.List<TextureCompressor>();

            try
            {
                // Fill cache to capacity
                for (int i = 0; i < CustomPresetEditorState.MaxCachedStates; i++)
                {
                    var go = new GameObject($"TestCompressor_{i}");
                    gameObjects.Add(go);
                    var config = go.AddComponent<TextureCompressor>();
                    configs.Add(config);
                    CustomPresetEditorState.SetEditMode(config, true);
                }

                // Update an existing entry (should not trigger eviction)
                CustomPresetEditorState.SetEditMode(configs[0], false);

                // All entries should still exist
                Assert.That(
                    CustomPresetEditorState.CachedStateCount,
                    Is.EqualTo(CustomPresetEditorState.MaxCachedStates)
                );
                foreach (var config in configs)
                {
                    Assert.That(
                        CustomPresetEditorState.HasStateFor(config),
                        Is.True,
                        $"Config {config.gameObject.name} should still have state"
                    );
                }
            }
            finally
            {
                CustomPresetEditorState.ClearAllStates();
                foreach (var go in gameObjects)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void LRU_AccessUpdatesTime_PreventingEviction()
        {
            CustomPresetEditorState.ClearAllStates();

            var gameObjects = new System.Collections.Generic.List<GameObject>();
            var configs = new System.Collections.Generic.List<TextureCompressor>();

            try
            {
                // Create first config (will be oldest)
                var firstGo = new GameObject("TestCompressor_First");
                gameObjects.Add(firstGo);
                var firstConfig = firstGo.AddComponent<TextureCompressor>();
                configs.Add(firstConfig);
                CustomPresetEditorState.SetEditMode(firstConfig, true);

                // Fill remaining cache
                for (int i = 1; i < CustomPresetEditorState.MaxCachedStates; i++)
                {
                    var go = new GameObject($"TestCompressor_{i}");
                    gameObjects.Add(go);
                    var config = go.AddComponent<TextureCompressor>();
                    configs.Add(config);
                    CustomPresetEditorState.SetEditMode(config, true);
                }

                // Access the first config to update its access time (makes it "recently used")
                CustomPresetEditorState.IsInEditMode(firstConfig);

                // Add a new config (should evict the second oldest, not the first)
                var extraGo = new GameObject("TestCompressor_Extra");
                gameObjects.Add(extraGo);
                var extraConfig = extraGo.AddComponent<TextureCompressor>();
                CustomPresetEditorState.SetEditMode(extraConfig, true);

                // First config should still exist (was accessed recently)
                Assert.That(
                    CustomPresetEditorState.HasStateFor(firstConfig),
                    Is.True,
                    "First config should not be evicted because it was accessed recently"
                );

                // Second config (index 1) should have been evicted (now the oldest)
                Assert.That(
                    CustomPresetEditorState.HasStateFor(configs[1]),
                    Is.False,
                    "Second config should be evicted as it became the oldest"
                );

                // New config should exist
                Assert.That(CustomPresetEditorState.HasStateFor(extraConfig), Is.True);
            }
            finally
            {
                CustomPresetEditorState.ClearAllStates();
                foreach (var go in gameObjects)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void LRU_MultipleEvictions_WorkCorrectly()
        {
            CustomPresetEditorState.ClearAllStates();

            var gameObjects = new System.Collections.Generic.List<GameObject>();
            var configs = new System.Collections.Generic.List<TextureCompressor>();

            try
            {
                // Fill cache to capacity
                for (int i = 0; i < CustomPresetEditorState.MaxCachedStates; i++)
                {
                    var go = new GameObject($"TestCompressor_{i}");
                    gameObjects.Add(go);
                    var config = go.AddComponent<TextureCompressor>();
                    configs.Add(config);
                    CustomPresetEditorState.SetEditMode(config, true);
                }

                // Add 3 more configs, causing 3 evictions
                for (int i = 0; i < 3; i++)
                {
                    var go = new GameObject($"TestCompressor_Extra_{i}");
                    gameObjects.Add(go);
                    var config = go.AddComponent<TextureCompressor>();
                    configs.Add(config);
                    CustomPresetEditorState.SetEditMode(config, true);
                }

                // Cache should still be at max
                Assert.That(
                    CustomPresetEditorState.CachedStateCount,
                    Is.EqualTo(CustomPresetEditorState.MaxCachedStates)
                );

                // First 3 configs should have been evicted
                Assert.That(CustomPresetEditorState.HasStateFor(configs[0]), Is.False);
                Assert.That(CustomPresetEditorState.HasStateFor(configs[1]), Is.False);
                Assert.That(CustomPresetEditorState.HasStateFor(configs[2]), Is.False);

                // Fourth config (index 3) should still exist
                Assert.That(CustomPresetEditorState.HasStateFor(configs[3]), Is.True);

                // All new configs should exist
                int totalConfigs = configs.Count;
                Assert.That(CustomPresetEditorState.HasStateFor(configs[totalConfigs - 1]), Is.True);
                Assert.That(CustomPresetEditorState.HasStateFor(configs[totalConfigs - 2]), Is.True);
                Assert.That(CustomPresetEditorState.HasStateFor(configs[totalConfigs - 3]), Is.True);
            }
            finally
            {
                CustomPresetEditorState.ClearAllStates();
                foreach (var go in gameObjects)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        #endregion
    }
}
