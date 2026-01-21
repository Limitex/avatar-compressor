using dev.limitex.avatar.compressor;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PresetSectionTests
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

        #region Preset Application Tests

        [Test]
        public void ApplyPreset_HighQuality_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.HighQuality);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.HighQuality));
            Assert.That(_config.MinDivisor, Is.EqualTo(1));
            Assert.That(_config.MaxDivisor, Is.EqualTo(2));
        }

        [Test]
        public void ApplyPreset_Quality_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Quality);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Quality));
            Assert.That(_config.MinDivisor, Is.EqualTo(1));
            Assert.That(_config.MaxDivisor, Is.EqualTo(4));
        }

        [Test]
        public void ApplyPreset_Standard_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Standard);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Standard));
            Assert.That(_config.MinDivisor, Is.EqualTo(1));
            Assert.That(_config.MaxDivisor, Is.EqualTo(4));
        }

        [Test]
        public void ApplyPreset_Balanced_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Balanced);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Balanced));
            Assert.That(_config.MinDivisor, Is.EqualTo(2));
            Assert.That(_config.MaxDivisor, Is.EqualTo(8));
        }

        [Test]
        public void ApplyPreset_Aggressive_SetsCorrectValues()
        {
            _config.ApplyPreset(CompressorPreset.Aggressive);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Aggressive));
            Assert.That(_config.MaxDivisor, Is.GreaterThanOrEqualTo(8));
        }

        [Test]
        public void ApplyPreset_Custom_KeepsCurrentValues()
        {
            // Set some custom values first
            _config.MinDivisor = 3;
            _config.MaxDivisor = 7;

            _config.ApplyPreset(CompressorPreset.Custom);

            Assert.That(_config.Preset, Is.EqualTo(CompressorPreset.Custom));
            // Custom preset should keep the values
            Assert.That(_config.MinDivisor, Is.EqualTo(3));
            Assert.That(_config.MaxDivisor, Is.EqualTo(7));
        }

        #endregion

        #region Preset Switching Tests

        [Test]
        public void SwitchPreset_FromBalancedToAggressive_UpdatesMinDivisor()
        {
            // Balanced has MinDivisor=1, Aggressive has MinDivisor=2
            _config.ApplyPreset(CompressorPreset.Balanced);
            int balancedMinDivisor = _config.MinDivisor;

            _config.ApplyPreset(CompressorPreset.Aggressive);

            Assert.That(_config.MinDivisor, Is.GreaterThan(balancedMinDivisor));
        }

        [Test]
        public void SwitchPreset_FromAggressiveToHighQuality_UpdatesSettings()
        {
            _config.ApplyPreset(CompressorPreset.Aggressive);
            int aggressiveMaxDivisor = _config.MaxDivisor;

            _config.ApplyPreset(CompressorPreset.HighQuality);

            Assert.That(_config.MaxDivisor, Is.LessThan(aggressiveMaxDivisor));
        }

        [Test]
        public void SwitchPreset_MultipleChanges_WorksCorrectly()
        {
            foreach (CompressorPreset preset in System.Enum.GetValues(typeof(CompressorPreset)))
            {
                Assert.DoesNotThrow(() => _config.ApplyPreset(preset));
                Assert.That(_config.Preset, Is.EqualTo(preset));
            }
        }

        #endregion

        #region Preset Constraint Tests

        [Test]
        public void AllPresets_MinDivisorIsAtLeastOne()
        {
            foreach (CompressorPreset preset in System.Enum.GetValues(typeof(CompressorPreset)))
            {
                if (preset == CompressorPreset.Custom)
                    continue;

                _config.ApplyPreset(preset);
                Assert.That(
                    _config.MinDivisor,
                    Is.GreaterThanOrEqualTo(1),
                    $"Preset {preset} should have MinDivisor >= 1"
                );
            }
        }

        [Test]
        public void AllPresets_MaxDivisorGreaterOrEqualMinDivisor()
        {
            foreach (CompressorPreset preset in System.Enum.GetValues(typeof(CompressorPreset)))
            {
                if (preset == CompressorPreset.Custom)
                    continue;

                _config.ApplyPreset(preset);
                Assert.That(
                    _config.MaxDivisor,
                    Is.GreaterThanOrEqualTo(_config.MinDivisor),
                    $"Preset {preset} should have MaxDivisor >= MinDivisor"
                );
            }
        }

        [Test]
        public void AllPresets_MinResolutionIsPositive()
        {
            foreach (CompressorPreset preset in System.Enum.GetValues(typeof(CompressorPreset)))
            {
                if (preset == CompressorPreset.Custom)
                    continue;

                _config.ApplyPreset(preset);
                Assert.That(
                    _config.MinResolution,
                    Is.GreaterThan(0),
                    $"Preset {preset} should have MinResolution > 0"
                );
            }
        }

        [Test]
        public void AllPresets_MaxResolutionGreaterOrEqualMinResolution()
        {
            foreach (CompressorPreset preset in System.Enum.GetValues(typeof(CompressorPreset)))
            {
                if (preset == CompressorPreset.Custom)
                    continue;

                _config.ApplyPreset(preset);
                Assert.That(
                    _config.MaxResolution,
                    Is.GreaterThanOrEqualTo(_config.MinResolution),
                    $"Preset {preset} should have MaxResolution >= MinResolution"
                );
            }
        }

        [Test]
        public void AllPresets_ComplexityThresholdsValid()
        {
            foreach (CompressorPreset preset in System.Enum.GetValues(typeof(CompressorPreset)))
            {
                if (preset == CompressorPreset.Custom)
                    continue;

                _config.ApplyPreset(preset);
                Assert.That(
                    _config.LowComplexityThreshold,
                    Is.GreaterThanOrEqualTo(0f),
                    $"Preset {preset} LowComplexityThreshold should be >= 0"
                );
                Assert.That(
                    _config.HighComplexityThreshold,
                    Is.LessThanOrEqualTo(1f),
                    $"Preset {preset} HighComplexityThreshold should be <= 1"
                );
                Assert.That(
                    _config.HighComplexityThreshold,
                    Is.GreaterThanOrEqualTo(_config.LowComplexityThreshold),
                    $"Preset {preset} HighComplexityThreshold should be >= LowComplexityThreshold"
                );
            }
        }

        #endregion
    }
}
