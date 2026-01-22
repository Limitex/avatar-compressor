using dev.limitex.avatar.compressor;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class CustomCompressorPresetTests
    {
        private GameObject _testObject;
        private TextureCompressor _config;
        private CustomCompressorPreset _preset;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestAvatar");
            _config = _testObject.AddComponent<TextureCompressor>();
            _preset = ScriptableObject.CreateInstance<CustomCompressorPreset>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
            if (_preset != null)
            {
                Object.DestroyImmediate(_preset);
            }
        }

        #region ApplyTo Tests

        [Test]
        public void ApplyTo_CopiesAllSettings()
        {
            _preset.Strategy = AnalysisStrategyType.Fast;
            _preset.FastWeight = 0.5f;
            _preset.HighAccuracyWeight = 0.3f;
            _preset.PerceptualWeight = 0.2f;
            _preset.HighComplexityThreshold = 0.8f;
            _preset.LowComplexityThreshold = 0.3f;
            _preset.MinDivisor = 2;
            _preset.MaxDivisor = 16;
            _preset.MaxResolution = 1024;
            _preset.MinResolution = 64;
            _preset.ForcePowerOfTwo = false;
            _preset.ProcessMainTextures = false;
            _preset.ProcessNormalMaps = false;
            _preset.ProcessEmissionMaps = false;
            _preset.ProcessOtherTextures = false;
            _preset.MinSourceSize = 512;
            _preset.SkipIfSmallerThan = 256;
            _preset.TargetPlatform = CompressionPlatform.Mobile;
            _preset.UseHighQualityFormatForHighComplexity = false;

            _preset.ApplyTo(_config);

            Assert.That(_config.Strategy, Is.EqualTo(AnalysisStrategyType.Fast));
            Assert.That(_config.FastWeight, Is.EqualTo(0.5f));
            Assert.That(_config.HighAccuracyWeight, Is.EqualTo(0.3f));
            Assert.That(_config.PerceptualWeight, Is.EqualTo(0.2f));
            Assert.That(_config.HighComplexityThreshold, Is.EqualTo(0.8f));
            Assert.That(_config.LowComplexityThreshold, Is.EqualTo(0.3f));
            Assert.That(_config.MinDivisor, Is.EqualTo(2));
            Assert.That(_config.MaxDivisor, Is.EqualTo(16));
            Assert.That(_config.MaxResolution, Is.EqualTo(1024));
            Assert.That(_config.MinResolution, Is.EqualTo(64));
            Assert.That(_config.ForcePowerOfTwo, Is.False);
            Assert.That(_config.ProcessMainTextures, Is.False);
            Assert.That(_config.ProcessNormalMaps, Is.False);
            Assert.That(_config.ProcessEmissionMaps, Is.False);
            Assert.That(_config.ProcessOtherTextures, Is.False);
            Assert.That(_config.MinSourceSize, Is.EqualTo(512));
            Assert.That(_config.SkipIfSmallerThan, Is.EqualTo(256));
            Assert.That(_config.TargetPlatform, Is.EqualTo(CompressionPlatform.Mobile));
            Assert.That(_config.UseHighQualityFormatForHighComplexity, Is.False);
        }

        [Test]
        public void ApplyTo_DoesNotModifyPreset()
        {
            _preset.MaxDivisor = 4;
            _config.MaxDivisor = 16;

            _preset.ApplyTo(_config);

            Assert.That(_preset.MaxDivisor, Is.EqualTo(4));
        }

        #endregion

        #region CopyFrom Tests

        [Test]
        public void CopyFrom_CopiesAllSettings()
        {
            _config.Strategy = AnalysisStrategyType.HighAccuracy;
            _config.FastWeight = 0.1f;
            _config.HighAccuracyWeight = 0.7f;
            _config.PerceptualWeight = 0.2f;
            _config.HighComplexityThreshold = 0.9f;
            _config.LowComplexityThreshold = 0.1f;
            _config.MinDivisor = 1;
            _config.MaxDivisor = 4;
            _config.MaxResolution = 4096;
            _config.MinResolution = 128;
            _config.ForcePowerOfTwo = true;
            _config.ProcessMainTextures = true;
            _config.ProcessNormalMaps = false;
            _config.ProcessEmissionMaps = true;
            _config.ProcessOtherTextures = false;
            _config.MinSourceSize = 1024;
            _config.SkipIfSmallerThan = 512;
            _config.TargetPlatform = CompressionPlatform.Desktop;
            _config.UseHighQualityFormatForHighComplexity = true;

            _preset.CopyFrom(_config);

            Assert.That(_preset.Strategy, Is.EqualTo(AnalysisStrategyType.HighAccuracy));
            Assert.That(_preset.FastWeight, Is.EqualTo(0.1f));
            Assert.That(_preset.HighAccuracyWeight, Is.EqualTo(0.7f));
            Assert.That(_preset.PerceptualWeight, Is.EqualTo(0.2f));
            Assert.That(_preset.HighComplexityThreshold, Is.EqualTo(0.9f));
            Assert.That(_preset.LowComplexityThreshold, Is.EqualTo(0.1f));
            Assert.That(_preset.MinDivisor, Is.EqualTo(1));
            Assert.That(_preset.MaxDivisor, Is.EqualTo(4));
            Assert.That(_preset.MaxResolution, Is.EqualTo(4096));
            Assert.That(_preset.MinResolution, Is.EqualTo(128));
            Assert.That(_preset.ForcePowerOfTwo, Is.True);
            Assert.That(_preset.ProcessMainTextures, Is.True);
            Assert.That(_preset.ProcessNormalMaps, Is.False);
            Assert.That(_preset.ProcessEmissionMaps, Is.True);
            Assert.That(_preset.ProcessOtherTextures, Is.False);
            Assert.That(_preset.MinSourceSize, Is.EqualTo(1024));
            Assert.That(_preset.SkipIfSmallerThan, Is.EqualTo(512));
            Assert.That(_preset.TargetPlatform, Is.EqualTo(CompressionPlatform.Desktop));
            Assert.That(_preset.UseHighQualityFormatForHighComplexity, Is.True);
        }

        [Test]
        public void CopyFrom_DoesNotModifyComponent()
        {
            _config.MaxDivisor = 16;
            _preset.MaxDivisor = 4;

            _preset.CopyFrom(_config);

            Assert.That(_config.MaxDivisor, Is.EqualTo(16));
        }

        #endregion

        #region MatchesSettings Tests

        [Test]
        public void MatchesSettings_IdenticalSettings_ReturnsTrue()
        {
            _preset.CopyFrom(_config);

            Assert.That(_preset.MatchesSettings(_config), Is.True);
        }

        [Test]
        public void MatchesSettings_DifferentStrategy_ReturnsFalse()
        {
            _preset.CopyFrom(_config);
            _config.Strategy = AnalysisStrategyType.Perceptual;

            Assert.That(_preset.MatchesSettings(_config), Is.False);
        }

        [Test]
        public void MatchesSettings_DifferentFloatWeight_ReturnsFalse()
        {
            _preset.CopyFrom(_config);
            _config.FastWeight = _preset.FastWeight + 0.1f;

            Assert.That(_preset.MatchesSettings(_config), Is.False);
        }

        [Test]
        public void MatchesSettings_DifferentThreshold_ReturnsFalse()
        {
            _preset.CopyFrom(_config);
            _config.HighComplexityThreshold = _preset.HighComplexityThreshold + 0.1f;

            Assert.That(_preset.MatchesSettings(_config), Is.False);
        }

        [Test]
        public void MatchesSettings_DifferentDivisor_ReturnsFalse()
        {
            _preset.CopyFrom(_config);
            _config.MaxDivisor = _preset.MaxDivisor + 2;

            Assert.That(_preset.MatchesSettings(_config), Is.False);
        }

        [Test]
        public void MatchesSettings_DifferentResolution_ReturnsFalse()
        {
            _preset.CopyFrom(_config);
            _config.MaxResolution = _preset.MaxResolution / 2;

            Assert.That(_preset.MatchesSettings(_config), Is.False);
        }

        [Test]
        public void MatchesSettings_DifferentBoolSetting_ReturnsFalse()
        {
            _preset.CopyFrom(_config);
            _config.ProcessNormalMaps = !_preset.ProcessNormalMaps;

            Assert.That(_preset.MatchesSettings(_config), Is.False);
        }

        [Test]
        public void MatchesSettings_DifferentPlatform_ReturnsFalse()
        {
            _preset.CopyFrom(_config);
            _config.TargetPlatform = CompressionPlatform.Mobile;
            _preset.TargetPlatform = CompressionPlatform.Desktop;

            Assert.That(_preset.MatchesSettings(_config), Is.False);
        }

        [Test]
        public void MatchesSettings_SmallFloatDifference_UsesApproximateComparison()
        {
            _preset.CopyFrom(_config);
            // Very small difference should still match due to Mathf.Approximately
            _config.FastWeight = _preset.FastWeight + 0.000001f;

            Assert.That(_preset.MatchesSettings(_config), Is.True);
        }

        #endregion

        #region Round-Trip Tests

        [Test]
        public void RoundTrip_ApplyThenCopy_PreservesAllSettings()
        {
            _preset.Strategy = AnalysisStrategyType.Perceptual;
            _preset.MaxDivisor = 12;
            _preset.HighComplexityThreshold = 0.65f;

            _preset.ApplyTo(_config);

            var secondPreset = ScriptableObject.CreateInstance<CustomCompressorPreset>();
            secondPreset.CopyFrom(_config);

            Assert.That(secondPreset.Strategy, Is.EqualTo(_preset.Strategy));
            Assert.That(secondPreset.MaxDivisor, Is.EqualTo(_preset.MaxDivisor));
            Assert.That(
                secondPreset.HighComplexityThreshold,
                Is.EqualTo(_preset.HighComplexityThreshold)
            );

            Object.DestroyImmediate(secondPreset);
        }

        [Test]
        public void RoundTrip_CopyThenApply_MatchesOriginal()
        {
            _config.Strategy = AnalysisStrategyType.Fast;
            _config.MaxDivisor = 8;
            _config.LowComplexityThreshold = 0.35f;

            _preset.CopyFrom(_config);

            // Modify config
            _config.Strategy = AnalysisStrategyType.Combined;
            _config.MaxDivisor = 2;
            _config.LowComplexityThreshold = 0.1f;

            // Apply preset back
            _preset.ApplyTo(_config);

            Assert.That(_config.Strategy, Is.EqualTo(AnalysisStrategyType.Fast));
            Assert.That(_config.MaxDivisor, Is.EqualTo(8));
            Assert.That(_config.LowComplexityThreshold, Is.EqualTo(0.35f));
        }

        #endregion

        #region Component Integration Tests

        [Test]
        public void CustomPresetAsset_DefaultIsNull()
        {
            Assert.That(_config.CustomPresetAsset, Is.Null);
        }

        [Test]
        public void CustomPresetAsset_CanBeAssigned()
        {
            _config.CustomPresetAsset = _preset;

            Assert.That(_config.CustomPresetAsset, Is.EqualTo(_preset));
        }

        [Test]
        public void CustomPresetAsset_CanBeCleared()
        {
            _config.CustomPresetAsset = _preset;
            _config.CustomPresetAsset = null;

            Assert.That(_config.CustomPresetAsset, Is.Null);
        }

        #endregion

        #region ApplyPreset Custom Tests

        [Test]
        public void ApplyPreset_Custom_WithAsset_RestoresPresetSettings()
        {
            // Setup preset with specific values
            _preset.MaxDivisor = 12;
            _preset.Strategy = AnalysisStrategyType.Fast;
            _preset.HighComplexityThreshold = 0.85f;

            // Assign preset to config
            _config.CustomPresetAsset = _preset;

            // Change to different preset
            _config.ApplyPreset(CompressorPreset.Balanced);

            // Verify settings changed
            Assert.That(_config.MaxDivisor, Is.Not.EqualTo(12));

            // Switch back to Custom
            _config.ApplyPreset(CompressorPreset.Custom);

            // Verify preset settings are restored
            Assert.That(_config.MaxDivisor, Is.EqualTo(12));
            Assert.That(_config.Strategy, Is.EqualTo(AnalysisStrategyType.Fast));
            Assert.That(_config.HighComplexityThreshold, Is.EqualTo(0.85f));
        }

        [Test]
        public void ApplyPreset_Custom_WithoutAsset_KeepsCurrentValues()
        {
            // Set custom values
            _config.MaxDivisor = 7;
            _config.Strategy = AnalysisStrategyType.Perceptual;

            // Ensure no preset asset
            _config.CustomPresetAsset = null;

            // Apply Custom preset
            _config.ApplyPreset(CompressorPreset.Custom);

            // Values should be unchanged
            Assert.That(_config.MaxDivisor, Is.EqualTo(7));
            Assert.That(_config.Strategy, Is.EqualTo(AnalysisStrategyType.Perceptual));
        }

        #endregion
    }
}
