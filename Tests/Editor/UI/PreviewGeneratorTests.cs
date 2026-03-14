using System.Collections.Generic;
using System.IO;
using System.Reflection;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PreviewGeneratorTests
    {
        private const string TestAssetFolder = "Assets/_LAC_TMP";
        private GameObject _testObject;
        private TextureCompressor _config;
        private List<Object> _createdObjects;
        private List<string> _createdAssetPaths;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<Object>();
            _createdAssetPaths = new List<string>();
            _testObject = new GameObject("TestAvatar");
            _config = _testObject.AddComponent<TextureCompressor>();

            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_LAC_TMP");
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }

            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();

            foreach (var path in _createdAssetPaths)
            {
                if (
                    !string.IsNullOrEmpty(path)
                    && AssetDatabase.LoadAssetAtPath<Object>(path) != null
                )
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssetPaths.Clear();

            if (AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                var remaining = AssetDatabase.FindAssets("", new[] { TestAssetFolder });
                if (remaining.Length == 0)
                {
                    AssetDatabase.DeleteAsset(TestAssetFolder);
                }
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

            _config.FrozenTextures.Add(
                new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.Auto, false)
            );
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentFrozenTextureDivisor_ReturnsDifferentHash()
        {
            _config.FrozenTextures.Add(
                new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.Auto, false)
            );
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.FrozenTextures.Clear();
            _config.FrozenTextures.Add(
                new FrozenTextureSettings("test-guid", 4, FrozenTextureFormat.Auto, false)
            );
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [TestCase("FastWeight", 0.3f, 0.5f)]
        [TestCase("HighAccuracyWeight", 0.3f, 0.7f)]
        [TestCase("PerceptualWeight", 0.2f, 0.8f)]
        [TestCase("LowComplexityThreshold", 0.2f, 0.4f)]
        public void ComputeSettingsHash_DifferentFloatSetting_ReturnsDifferentHash(
            string propertyName,
            float value1,
            float value2
        )
        {
            var field = typeof(TextureCompressor).GetField(propertyName);
            field.SetValue(_config, value1);
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            field.SetValue(_config, value2);
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(
                hash1,
                Is.Not.EqualTo(hash2),
                $"{propertyName} change should invalidate hash"
            );
        }

        [TestCase("ProcessEmissionMaps", true, false)]
        [TestCase("ProcessOtherTextures", true, false)]
        [TestCase("UseHighQualityFormatForHighComplexity", true, false)]
        public void ComputeSettingsHash_DifferentBoolSetting_ReturnsDifferentHash(
            string propertyName,
            bool value1,
            bool value2
        )
        {
            var field = typeof(TextureCompressor).GetField(propertyName);
            field.SetValue(_config, value1);
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            field.SetValue(_config, value2);
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(
                hash1,
                Is.Not.EqualTo(hash2),
                $"{propertyName} change should invalidate hash"
            );
        }

        [Test]
        public void ComputeSettingsHash_DifferentSkipIfSmallerThan_ReturnsDifferentHash()
        {
            _config.SkipIfSmallerThan = 128;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.SkipIfSmallerThan = 256;
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentFrozenTextureFormat_ReturnsDifferentHash()
        {
            _config.FrozenTextures.Add(
                new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.Auto, false)
            );
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.FrozenTextures.Clear();
            _config.FrozenTextures.Add(
                new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.DXT1, false)
            );
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentFrozenTextureSkip_ReturnsDifferentHash()
        {
            _config.FrozenTextures.Add(
                new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.Auto, false)
            );
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);

            _config.FrozenTextures.Clear();
            _config.FrozenTextures.Add(
                new FrozenTextureSettings("test-guid", 2, FrozenTextureFormat.Auto, true)
            );
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_SameConfig_ReturnsDeterministicHash()
        {
            _config.Strategy = AnalysisStrategyType.Combined;
            int hash1 = PreviewGenerator.ComputeSettingsHash(_config);
            int hash2 = PreviewGenerator.ComputeSettingsHash(_config);

            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void ComputeSettingsHash_DifferentBackendPreference_ReturnsDifferentHash()
        {
            int hash1 = PreviewGenerator.ComputeSettingsHash(
                _config,
                AnalysisBackendPreference.Auto
            );
            int hash2 = PreviewGenerator.ComputeSettingsHash(
                _config,
                AnalysisBackendPreference.CPU
            );

            Assert.That(
                hash1,
                Is.Not.EqualTo(hash2),
                "Backend preference change should invalidate hash"
            );
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

        [Test]
        public void Generate_FrozenOpaqueNonReadableTexture_HasNoAlpha()
        {
            _config.MinSourceSize = 64;
            _config.SkipIfSmallerThan = 0;
            _config.ForcePowerOfTwo = false;

            var renderer = _testObject.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateImportedTexture(
                128,
                128,
                hasTransparentPixel: false,
                readable: false
            );

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
            _config.FrozenTextures.Add(
                new FrozenTextureSettings(guid, 1, FrozenTextureFormat.Auto, false)
            );

            var generator = new PreviewGenerator();
            var result = generator.Generate(_config);

            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0].IsFrozen, Is.True);
            Assert.That(result[0].HasAlpha, Is.False);
        }

        #endregion

        #region Helpers

        private Material CreateMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            _createdObjects.Add(material);
            return material;
        }

        private Texture2D CreateImportedTexture(
            int width,
            int height,
            bool hasTransparentPixel,
            bool readable
        )
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, 128, 255);
            }

            if (hasTransparentPixel)
            {
                pixels[pixels.Length / 2] = new Color32(128, 128, 128, 0);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/PreviewTexture_{width}x{height}_{System.Guid.NewGuid():N}.png";
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            _createdAssetPaths.Add(assetPath);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.IsNotNull(importer, "TextureImporter should exist for PNG assets");

            importer.isReadable = readable;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        #endregion
    }
}
