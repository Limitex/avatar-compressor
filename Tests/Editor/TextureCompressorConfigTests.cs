using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureCompressorConfigTests
    {
        private GameObject _configObject;
        private TextureCompressor _config;

        [SetUp]
        public void SetUp()
        {
            _configObject = new GameObject("ConfigObject");
            _config = _configObject.AddComponent<TextureCompressor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_configObject != null)
            {
                Object.DestroyImmediate(_configObject);
            }
        }

        #region ApplyPreset Tests

        [Test]
        public void ApplyPreset_HighQuality_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.HighQuality);

            Assert.AreEqual(CompressorPreset.HighQuality, _config.Preset);
            Assert.AreEqual(AnalysisStrategyType.Combined, _config.Strategy);
            Assert.AreEqual(0.1f, _config.FastWeight, 0.001f);
            Assert.AreEqual(0.5f, _config.HighAccuracyWeight, 0.001f);
            Assert.AreEqual(0.4f, _config.PerceptualWeight, 0.001f);
            Assert.AreEqual(0.3f, _config.HighComplexityThreshold, 0.001f);
            Assert.AreEqual(0.1f, _config.LowComplexityThreshold, 0.001f);
            Assert.AreEqual(1, _config.MinDivisor);
            Assert.AreEqual(2, _config.MaxDivisor);
            Assert.AreEqual(2048, _config.MaxResolution);
            Assert.AreEqual(256, _config.MinResolution);
            Assert.IsTrue(_config.ForcePowerOfTwo);
            Assert.IsTrue(_config.ProcessMainTextures);
            Assert.IsTrue(_config.ProcessNormalMaps);
            Assert.IsTrue(_config.ProcessEmissionMaps);
            Assert.IsTrue(_config.ProcessOtherTextures);
            Assert.AreEqual(1024, _config.MinSourceSize);
            Assert.AreEqual(512, _config.SkipIfSmallerThan);
            Assert.AreEqual(CompressionPlatform.Auto, _config.TargetPlatform);
            Assert.IsTrue(_config.UseHighQualityFormatForHighComplexity);
        }

        [Test]
        public void ApplyPreset_Quality_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Quality);

            Assert.AreEqual(CompressorPreset.Quality, _config.Preset);
            Assert.AreEqual(AnalysisStrategyType.Combined, _config.Strategy);
            Assert.AreEqual(0.2f, _config.FastWeight, 0.001f);
            Assert.AreEqual(0.5f, _config.HighAccuracyWeight, 0.001f);
            Assert.AreEqual(0.3f, _config.PerceptualWeight, 0.001f);
            Assert.AreEqual(0.5f, _config.HighComplexityThreshold, 0.001f);
            Assert.AreEqual(0.15f, _config.LowComplexityThreshold, 0.001f);
            Assert.AreEqual(1, _config.MinDivisor);
            Assert.AreEqual(4, _config.MaxDivisor);
            Assert.AreEqual(2048, _config.MaxResolution);
            Assert.AreEqual(128, _config.MinResolution);
            Assert.IsTrue(_config.ForcePowerOfTwo);
            Assert.AreEqual(512, _config.MinSourceSize);
            Assert.AreEqual(256, _config.SkipIfSmallerThan);
            Assert.IsTrue(_config.UseHighQualityFormatForHighComplexity);
        }

        [Test]
        public void ApplyPreset_Balanced_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Balanced);

            Assert.AreEqual(CompressorPreset.Balanced, _config.Preset);
            Assert.AreEqual(AnalysisStrategyType.Combined, _config.Strategy);
            Assert.AreEqual(0.3f, _config.FastWeight, 0.001f);
            Assert.AreEqual(0.5f, _config.HighAccuracyWeight, 0.001f);
            Assert.AreEqual(0.2f, _config.PerceptualWeight, 0.001f);
            Assert.AreEqual(0.7f, _config.HighComplexityThreshold, 0.001f);
            Assert.AreEqual(0.2f, _config.LowComplexityThreshold, 0.001f);
            Assert.AreEqual(1, _config.MinDivisor);
            Assert.AreEqual(8, _config.MaxDivisor);
            Assert.AreEqual(2048, _config.MaxResolution);
            Assert.AreEqual(64, _config.MinResolution);
            Assert.IsTrue(_config.ForcePowerOfTwo);
            Assert.AreEqual(256, _config.MinSourceSize);
            Assert.AreEqual(128, _config.SkipIfSmallerThan);
            Assert.IsTrue(_config.UseHighQualityFormatForHighComplexity);
        }

        [Test]
        public void ApplyPreset_Aggressive_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Aggressive);

            Assert.AreEqual(CompressorPreset.Aggressive, _config.Preset);
            Assert.AreEqual(AnalysisStrategyType.Fast, _config.Strategy);
            Assert.AreEqual(0.5f, _config.FastWeight, 0.001f);
            Assert.AreEqual(0.3f, _config.HighAccuracyWeight, 0.001f);
            Assert.AreEqual(0.2f, _config.PerceptualWeight, 0.001f);
            Assert.AreEqual(0.8f, _config.HighComplexityThreshold, 0.001f);
            Assert.AreEqual(0.3f, _config.LowComplexityThreshold, 0.001f);
            Assert.AreEqual(2, _config.MinDivisor);
            Assert.AreEqual(8, _config.MaxDivisor);
            Assert.AreEqual(2048, _config.MaxResolution);
            Assert.AreEqual(32, _config.MinResolution);
            Assert.IsTrue(_config.ForcePowerOfTwo);
            Assert.AreEqual(128, _config.MinSourceSize);
            Assert.AreEqual(64, _config.SkipIfSmallerThan);
            Assert.IsFalse(_config.UseHighQualityFormatForHighComplexity);
        }

        [Test]
        public void ApplyPreset_Maximum_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Maximum);

            Assert.AreEqual(CompressorPreset.Maximum, _config.Preset);
            Assert.AreEqual(AnalysisStrategyType.Fast, _config.Strategy);
            Assert.AreEqual(0.6f, _config.FastWeight, 0.001f);
            Assert.AreEqual(0.3f, _config.HighAccuracyWeight, 0.001f);
            Assert.AreEqual(0.1f, _config.PerceptualWeight, 0.001f);
            Assert.AreEqual(0.9f, _config.HighComplexityThreshold, 0.001f);
            Assert.AreEqual(0.4f, _config.LowComplexityThreshold, 0.001f);
            Assert.AreEqual(2, _config.MinDivisor);
            Assert.AreEqual(16, _config.MaxDivisor);
            Assert.AreEqual(2048, _config.MaxResolution);
            Assert.AreEqual(32, _config.MinResolution);
            Assert.IsTrue(_config.ForcePowerOfTwo);
            Assert.AreEqual(64, _config.MinSourceSize);
            Assert.AreEqual(32, _config.SkipIfSmallerThan);
            Assert.IsFalse(_config.UseHighQualityFormatForHighComplexity);
        }

        [Test]
        public void ApplyPreset_Custom_DoesNotChangeValues()
        {
            // Set custom values first
            _config.MinDivisor = 3;
            _config.MaxDivisor = 5;
            _config.MinResolution = 100;

            _config.ApplyPreset(CompressorPreset.Custom);

            // Custom preset should not change values
            Assert.AreEqual(CompressorPreset.Custom, _config.Preset);
            Assert.AreEqual(3, _config.MinDivisor);
            Assert.AreEqual(5, _config.MaxDivisor);
            Assert.AreEqual(100, _config.MinResolution);
        }

        #endregion

        #region Preset Ordering Tests

        [Test]
        public void Presets_QualityProgression_MaxDivisorIncreases()
        {
            _config.ApplyPreset(CompressorPreset.HighQuality);
            int highQualityMaxDivisor = _config.MaxDivisor;

            _config.ApplyPreset(CompressorPreset.Quality);
            int qualityMaxDivisor = _config.MaxDivisor;

            _config.ApplyPreset(CompressorPreset.Balanced);
            int balancedMaxDivisor = _config.MaxDivisor;

            _config.ApplyPreset(CompressorPreset.Maximum);
            int maximumMaxDivisor = _config.MaxDivisor;

            Assert.That(highQualityMaxDivisor, Is.LessThanOrEqualTo(qualityMaxDivisor));
            Assert.That(qualityMaxDivisor, Is.LessThanOrEqualTo(balancedMaxDivisor));
            Assert.That(balancedMaxDivisor, Is.LessThanOrEqualTo(maximumMaxDivisor));
        }

        [Test]
        public void Presets_QualityProgression_MinResolutionDecreases()
        {
            _config.ApplyPreset(CompressorPreset.HighQuality);
            int highQualityMinRes = _config.MinResolution;

            _config.ApplyPreset(CompressorPreset.Quality);
            int qualityMinRes = _config.MinResolution;

            _config.ApplyPreset(CompressorPreset.Balanced);
            int balancedMinRes = _config.MinResolution;

            _config.ApplyPreset(CompressorPreset.Maximum);
            int maximumMinRes = _config.MinResolution;

            Assert.That(highQualityMinRes, Is.GreaterThanOrEqualTo(qualityMinRes));
            Assert.That(qualityMinRes, Is.GreaterThanOrEqualTo(balancedMinRes));
            Assert.That(balancedMinRes, Is.GreaterThanOrEqualTo(maximumMinRes));
        }

        [Test]
        public void Presets_QualityProgression_HighComplexityThresholdIncreases()
        {
            _config.ApplyPreset(CompressorPreset.HighQuality);
            float highQualityThreshold = _config.HighComplexityThreshold;

            _config.ApplyPreset(CompressorPreset.Balanced);
            float balancedThreshold = _config.HighComplexityThreshold;

            _config.ApplyPreset(CompressorPreset.Maximum);
            float maximumThreshold = _config.HighComplexityThreshold;

            Assert.That(highQualityThreshold, Is.LessThanOrEqualTo(balancedThreshold));
            Assert.That(balancedThreshold, Is.LessThanOrEqualTo(maximumThreshold));
        }

        #endregion

        #region Default Values Tests

        [Test]
        public void DefaultValues_PresetIsBalanced()
        {
            Assert.AreEqual(CompressorPreset.Balanced, _config.Preset);
        }

        [Test]
        public void DefaultValues_StrategyIsCombined()
        {
            Assert.AreEqual(AnalysisStrategyType.Combined, _config.Strategy);
        }

        [Test]
        public void DefaultValues_TargetPlatformIsAuto()
        {
            Assert.AreEqual(CompressionPlatform.Auto, _config.TargetPlatform);
        }

        [Test]
        public void DefaultValues_AllProcessFlagsEnabled()
        {
            Assert.IsTrue(_config.ProcessMainTextures);
            Assert.IsTrue(_config.ProcessNormalMaps);
            Assert.IsTrue(_config.ProcessEmissionMaps);
            Assert.IsTrue(_config.ProcessOtherTextures);
        }

        [Test]
        public void DefaultValues_ForcePowerOfTwoEnabled()
        {
            Assert.IsTrue(_config.ForcePowerOfTwo);
        }

        [Test]
        public void DefaultValues_EnableLoggingEnabled()
        {
            Assert.IsTrue(_config.EnableLogging);
        }

        #endregion

        #region Enum Value Tests

        [Test]
        public void CompressorPreset_AllValuesAreDefined()
        {
            var values = System.Enum.GetValues(typeof(CompressorPreset));

            Assert.That(values, Contains.Item(CompressorPreset.HighQuality));
            Assert.That(values, Contains.Item(CompressorPreset.Quality));
            Assert.That(values, Contains.Item(CompressorPreset.Balanced));
            Assert.That(values, Contains.Item(CompressorPreset.Aggressive));
            Assert.That(values, Contains.Item(CompressorPreset.Maximum));
            Assert.That(values, Contains.Item(CompressorPreset.Custom));
        }

        [Test]
        public void AnalysisStrategyType_AllValuesAreDefined()
        {
            var values = System.Enum.GetValues(typeof(AnalysisStrategyType));

            Assert.That(values, Contains.Item(AnalysisStrategyType.Fast));
            Assert.That(values, Contains.Item(AnalysisStrategyType.HighAccuracy));
            Assert.That(values, Contains.Item(AnalysisStrategyType.Perceptual));
            Assert.That(values, Contains.Item(AnalysisStrategyType.Combined));
        }

        [Test]
        public void CompressionPlatform_AllValuesAreDefined()
        {
            var values = System.Enum.GetValues(typeof(CompressionPlatform));

            Assert.That(values, Contains.Item(CompressionPlatform.Auto));
            Assert.That(values, Contains.Item(CompressionPlatform.Desktop));
            Assert.That(values, Contains.Item(CompressionPlatform.Mobile));
        }

        #endregion

        #region Frozen Texture Management Tests

        [Test]
        public void IsFrozen_EmptyList_ReturnsFalse()
        {
            Assert.IsFalse(_config.IsFrozen("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4"));
        }

        [Test]
        public void IsFrozen_AfterAddingFrozen_ReturnsTrue()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            _config.SetFrozenSettings(guid, new FrozenTextureSettings(guid));

            Assert.IsTrue(_config.IsFrozen(guid));
        }

        [Test]
        public void IsFrozen_DifferentGuid_ReturnsFalse()
        {
            _config.SetFrozenSettings("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d1",
                new FrozenTextureSettings("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d1"));

            Assert.IsFalse(_config.IsFrozen("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d2"));
        }

        [Test]
        public void GetFrozenSettings_EmptyList_ReturnsNull()
        {
            var result = _config.GetFrozenSettings("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4");

            Assert.IsNull(result);
        }

        [Test]
        public void GetFrozenSettings_ExistingGuid_ReturnsSettings()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            var settings = new FrozenTextureSettings(guid, 4, FrozenTextureFormat.BC7, false);
            _config.SetFrozenSettings(guid, settings);

            var result = _config.GetFrozenSettings(guid);

            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Divisor);
            Assert.AreEqual(FrozenTextureFormat.BC7, result.Format);
        }

        [Test]
        public void SetFrozenSettings_NewGuid_AddToList()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            var settings = new FrozenTextureSettings(guid, 2, FrozenTextureFormat.DXT5, false);

            _config.SetFrozenSettings(guid, settings);

            Assert.AreEqual(1, _config.FrozenTextures.Count);
            Assert.AreEqual(guid, _config.FrozenTextures[0].TextureGuid);
        }

        [Test]
        public void SetFrozenSettings_ExistingGuid_UpdatesSettings()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            _config.SetFrozenSettings(guid, new FrozenTextureSettings(guid, 2, FrozenTextureFormat.DXT1, false));
            _config.SetFrozenSettings(guid, new FrozenTextureSettings(guid, 8, FrozenTextureFormat.BC7, true));

            Assert.AreEqual(1, _config.FrozenTextures.Count);
            Assert.AreEqual(8, _config.FrozenTextures[0].Divisor);
            Assert.AreEqual(FrozenTextureFormat.BC7, _config.FrozenTextures[0].Format);
            Assert.IsTrue(_config.FrozenTextures[0].Skip);
        }

        [Test]
        public void UnfreezeTexture_ExistingGuid_RemovesFromList()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            _config.SetFrozenSettings(guid, new FrozenTextureSettings(guid));

            _config.UnfreezeTexture(guid);

            Assert.AreEqual(0, _config.FrozenTextures.Count);
            Assert.IsFalse(_config.IsFrozen(guid));
        }

        [Test]
        public void UnfreezeTexture_NonExistingGuid_DoesNothing()
        {
            _config.SetFrozenSettings("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d1",
                new FrozenTextureSettings("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d1"));

            _config.UnfreezeTexture("a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d2");

            Assert.AreEqual(1, _config.FrozenTextures.Count);
        }

        [Test]
        public void FrozenTextures_DefaultIsEmptyList()
        {
            Assert.IsNotNull(_config.FrozenTextures);
            Assert.AreEqual(0, _config.FrozenTextures.Count);
        }

        #endregion

        #region Divisor Validation Tests

        [Test]
        public void IsValidDivisor_1_ReturnsTrue()
        {
            Assert.IsTrue(TextureCompressor.IsValidDivisor(1));
        }

        [Test]
        public void IsValidDivisor_2_ReturnsTrue()
        {
            Assert.IsTrue(TextureCompressor.IsValidDivisor(2));
        }

        [Test]
        public void IsValidDivisor_4_ReturnsTrue()
        {
            Assert.IsTrue(TextureCompressor.IsValidDivisor(4));
        }

        [Test]
        public void IsValidDivisor_8_ReturnsTrue()
        {
            Assert.IsTrue(TextureCompressor.IsValidDivisor(8));
        }

        [Test]
        public void IsValidDivisor_16_ReturnsTrue()
        {
            Assert.IsTrue(TextureCompressor.IsValidDivisor(16));
        }

        [Test]
        public void IsValidDivisor_0_ReturnsFalse()
        {
            Assert.IsFalse(TextureCompressor.IsValidDivisor(0));
        }

        [Test]
        public void IsValidDivisor_3_ReturnsFalse()
        {
            Assert.IsFalse(TextureCompressor.IsValidDivisor(3));
        }

        [Test]
        public void IsValidDivisor_5_ReturnsFalse()
        {
            Assert.IsFalse(TextureCompressor.IsValidDivisor(5));
        }

        [Test]
        public void IsValidDivisor_32_ReturnsFalse()
        {
            Assert.IsFalse(TextureCompressor.IsValidDivisor(32));
        }

        [Test]
        public void IsValidDivisor_Negative_ReturnsFalse()
        {
            Assert.IsFalse(TextureCompressor.IsValidDivisor(-1));
            Assert.IsFalse(TextureCompressor.IsValidDivisor(-4));
        }

        [Test]
        public void GetClosestValidDivisor_0_Returns1()
        {
            Assert.AreEqual(1, TextureCompressor.GetClosestValidDivisor(0));
        }

        [Test]
        public void GetClosestValidDivisor_1_Returns1()
        {
            Assert.AreEqual(1, TextureCompressor.GetClosestValidDivisor(1));
        }

        [Test]
        public void GetClosestValidDivisor_3_Returns2Or4()
        {
            // 3 is equidistant from 2 and 4, implementation returns 2
            var result = TextureCompressor.GetClosestValidDivisor(3);
            Assert.That(result, Is.EqualTo(2).Or.EqualTo(4));
        }

        [Test]
        public void GetClosestValidDivisor_5_Returns4()
        {
            Assert.AreEqual(4, TextureCompressor.GetClosestValidDivisor(5));
        }

        [Test]
        public void GetClosestValidDivisor_6_Returns4Or8()
        {
            // 6 is equidistant from 4 and 8
            var result = TextureCompressor.GetClosestValidDivisor(6);
            Assert.That(result, Is.EqualTo(4).Or.EqualTo(8));
        }

        [Test]
        public void GetClosestValidDivisor_7_Returns8()
        {
            Assert.AreEqual(8, TextureCompressor.GetClosestValidDivisor(7));
        }

        [Test]
        public void GetClosestValidDivisor_10_Returns8()
        {
            Assert.AreEqual(8, TextureCompressor.GetClosestValidDivisor(10));
        }

        [Test]
        public void GetClosestValidDivisor_15_Returns16()
        {
            Assert.AreEqual(16, TextureCompressor.GetClosestValidDivisor(15));
        }

        [Test]
        public void GetClosestValidDivisor_100_Returns16()
        {
            // Values far beyond 16 should clamp to 16
            Assert.AreEqual(16, TextureCompressor.GetClosestValidDivisor(100));
        }

        [Test]
        public void GetClosestValidDivisor_Negative_Returns1()
        {
            Assert.AreEqual(1, TextureCompressor.GetClosestValidDivisor(-5));
        }

        [Test]
        public void SetFrozenSettings_InvalidDivisor_AdjustsToClosestValid()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            var settings = new FrozenTextureSettings(guid, 3, FrozenTextureFormat.Auto, false);

            _config.SetFrozenSettings(guid, settings);

            var result = _config.GetFrozenSettings(guid);
            Assert.That(result.Divisor, Is.EqualTo(2).Or.EqualTo(4));
        }

        [Test]
        public void SetFrozenSettings_ValidDivisor_PreservesDivisor()
        {
            var guid = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4";
            var settings = new FrozenTextureSettings(guid, 8, FrozenTextureFormat.Auto, false);

            _config.SetFrozenSettings(guid, settings);

            var result = _config.GetFrozenSettings(guid);
            Assert.AreEqual(8, result.Divisor);
        }

        #endregion

        #region Preset Switching Tests

        [Test]
        public void ApplyPreset_SwitchingBetweenPresets_OverwritesPreviousValues()
        {
            _config.ApplyPreset(CompressorPreset.Maximum);
            Assert.AreEqual(16, _config.MaxDivisor);
            Assert.IsFalse(_config.UseHighQualityFormatForHighComplexity);

            _config.ApplyPreset(CompressorPreset.HighQuality);
            Assert.AreEqual(2, _config.MaxDivisor);
            Assert.IsTrue(_config.UseHighQualityFormatForHighComplexity);
        }

        [Test]
        public void ApplyPreset_AllPresets_AreApplicable()
        {
            var presets = new[]
            {
                CompressorPreset.HighQuality,
                CompressorPreset.Quality,
                CompressorPreset.Balanced,
                CompressorPreset.Aggressive,
                CompressorPreset.Maximum,
                CompressorPreset.Custom
            };

            foreach (var preset in presets)
            {
                Assert.DoesNotThrow(() => _config.ApplyPreset(preset),
                    $"Failed to apply preset {preset}");
                Assert.AreEqual(preset, _config.Preset,
                    $"Preset property not set correctly for {preset}");
            }
        }

        #endregion
    }
}
