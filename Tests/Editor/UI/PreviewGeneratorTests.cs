using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;
using dev.limitex.avatar.compressor.texture.ui;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PreviewGeneratorTests
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

        #region ComputeSettingsHash Tests

        [Test]
        public void ComputeSettingsHash_SameConfig_ReturnsSameHash()
        {
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentPreset_ReturnsDifferentHash()
        {
            _config.Preset = CompressorPreset.Balanced;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.Preset = CompressorPreset.Aggressive;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentStrategy_ReturnsDifferentHash()
        {
            _config.Strategy = AnalysisStrategyType.Fast;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.Strategy = AnalysisStrategyType.HighAccuracy;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentComplexityThreshold_ReturnsDifferentHash()
        {
            _config.HighComplexityThreshold = 0.7f;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.HighComplexityThreshold = 0.8f;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentMinDivisor_ReturnsDifferentHash()
        {
            _config.MinDivisor = 1;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.MinDivisor = 2;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentMaxDivisor_ReturnsDifferentHash()
        {
            _config.MaxDivisor = 8;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.MaxDivisor = 16;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentMaxResolution_ReturnsDifferentHash()
        {
            _config.MaxResolution = 2048;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.MaxResolution = 4096;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentMinResolution_ReturnsDifferentHash()
        {
            _config.MinResolution = 32;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.MinResolution = 64;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentForcePowerOfTwo_ReturnsDifferentHash()
        {
            _config.ForcePowerOfTwo = true;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.ForcePowerOfTwo = false;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentProcessMainTextures_ReturnsDifferentHash()
        {
            _config.ProcessMainTextures = true;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.ProcessMainTextures = false;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentProcessNormalMaps_ReturnsDifferentHash()
        {
            _config.ProcessNormalMaps = true;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.ProcessNormalMaps = false;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentMinSourceSize_ReturnsDifferentHash()
        {
            _config.MinSourceSize = 256;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.MinSourceSize = 512;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentTargetPlatform_ReturnsDifferentHash()
        {
            _config.TargetPlatform = CompressionPlatform.Desktop;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.TargetPlatform = CompressionPlatform.Mobile;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_AddExcludedPath_ReturnsDifferentHash()
        {
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.ExcludedPaths.Add("test/path/");
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentExcludedPath_ReturnsDifferentHash()
        {
            _config.ExcludedPaths.Add("path/a/");
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.ExcludedPaths.Clear();
            _config.ExcludedPaths.Add("path/b/");
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_AddFrozenTexture_ReturnsDifferentHash()
        {
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.FrozenTextures.Add(new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.Auto, false));
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentFrozenTextureDivisor_ReturnsDifferentHash()
        {
            _config.FrozenTextures.Add(new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.Auto, false));
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.FrozenTextures.Clear();
            _config.FrozenTextures.Add(new FrozenTextureSettings("test-guid", 4, FrozenTextureFormat.Auto, false));
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentStrategyWeights_ReturnsDifferentHash()
        {
            _config.FastWeight = 0.3f;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.FastWeight = 0.5f;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        #endregion

        #region PreviewGenerator Instance Tests

        [Test]
        public void PreviewGenerator_NewInstance_CountsAreZero()
        {
            var generator = new PreviewGenerator();

            Assert.That(generator.ProcessedCount, Is.EqualTo(0));
            Assert.That(generator.FrozenCount, Is.EqualTo(0));
            Assert.That(generator.SkippedCount, Is.EqualTo(0));
        }

        [Test]
        public void Generate_EmptyHierarchy_ReturnsEmptyArray()
        {
            var generator = new PreviewGenerator();

            var result = generator.Generate(_config);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void Generate_EmptyHierarchy_SetsCountsToZero()
        {
            var generator = new PreviewGenerator();

            generator.Generate(_config);

            Assert.That(generator.ProcessedCount, Is.EqualTo(0));
            Assert.That(generator.FrozenCount, Is.EqualTo(0));
            Assert.That(generator.SkippedCount, Is.EqualTo(0));
        }

        #endregion
    }
}
