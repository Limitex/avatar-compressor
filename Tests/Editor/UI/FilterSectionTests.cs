using dev.limitex.avatar.compressor;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class FilterSectionTests
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

        #region Texture Filter Tests

        [Test]
        public void NewConfig_AllFiltersEnabled()
        {
            Assert.That(_config.ProcessMainTextures, Is.True);
            Assert.That(_config.ProcessNormalMaps, Is.True);
            Assert.That(_config.ProcessEmissionMaps, Is.True);
            Assert.That(_config.ProcessOtherTextures, Is.True);
        }

        [Test]
        public void TextureFilter_MainCanBeToggled()
        {
            _config.ProcessMainTextures = false;
            Assert.That(_config.ProcessMainTextures, Is.False);

            _config.ProcessMainTextures = true;
            Assert.That(_config.ProcessMainTextures, Is.True);
        }

        [Test]
        public void TextureFilter_NormalCanBeToggled()
        {
            _config.ProcessNormalMaps = false;
            Assert.That(_config.ProcessNormalMaps, Is.False);

            _config.ProcessNormalMaps = true;
            Assert.That(_config.ProcessNormalMaps, Is.True);
        }

        [Test]
        public void TextureFilter_EmissionCanBeToggled()
        {
            _config.ProcessEmissionMaps = false;
            Assert.That(_config.ProcessEmissionMaps, Is.False);

            _config.ProcessEmissionMaps = true;
            Assert.That(_config.ProcessEmissionMaps, Is.True);
        }

        [Test]
        public void TextureFilter_OtherCanBeToggled()
        {
            _config.ProcessOtherTextures = false;
            Assert.That(_config.ProcessOtherTextures, Is.False);

            _config.ProcessOtherTextures = true;
            Assert.That(_config.ProcessOtherTextures, Is.True);
        }

        [Test]
        public void TextureFilter_AllCanBeDisabled()
        {
            _config.ProcessMainTextures = false;
            _config.ProcessNormalMaps = false;
            _config.ProcessEmissionMaps = false;
            _config.ProcessOtherTextures = false;

            Assert.That(_config.ProcessMainTextures, Is.False);
            Assert.That(_config.ProcessNormalMaps, Is.False);
            Assert.That(_config.ProcessEmissionMaps, Is.False);
            Assert.That(_config.ProcessOtherTextures, Is.False);
        }

        #endregion

        #region Excluded Paths Tests

        [Test]
        public void NewConfig_ExcludedPathsHasDefaults()
        {
            // Default paths are set from ExcludedPathPresets.GetDefaultPaths()
            Assert.That(_config.ExcludedPaths, Is.Not.Null);
            Assert.That(
                _config.ExcludedPaths.Count,
                Is.EqualTo(ExcludedPathPresets.Presets.Length)
            );
        }

        [Test]
        public void ExcludedPaths_CanAddPath()
        {
            int initialCount = _config.ExcludedPaths.Count;
            _config.ExcludedPaths.Add("Assets/Test/");

            Assert.That(_config.ExcludedPaths.Count, Is.EqualTo(initialCount + 1));
            Assert.That(_config.ExcludedPaths, Does.Contain("Assets/Test/"));
        }

        [Test]
        public void ExcludedPaths_CanAddMultiplePaths()
        {
            int initialCount = _config.ExcludedPaths.Count;
            _config.ExcludedPaths.Add("Assets/Test1/");
            _config.ExcludedPaths.Add("Assets/Test2/");
            _config.ExcludedPaths.Add("Packages/com.test/");

            Assert.That(_config.ExcludedPaths.Count, Is.EqualTo(initialCount + 3));
        }

        [Test]
        public void ExcludedPaths_CanRemovePath()
        {
            _config.ExcludedPaths.Clear();
            _config.ExcludedPaths.Add("Assets/Test/");
            _config.ExcludedPaths.RemoveAt(0);

            Assert.That(_config.ExcludedPaths.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExcludedPaths_CanEditPath()
        {
            _config.ExcludedPaths.Clear();
            _config.ExcludedPaths.Add("Assets/Old/");
            _config.ExcludedPaths[0] = "Assets/New/";

            Assert.That(_config.ExcludedPaths[0], Is.EqualTo("Assets/New/"));
        }

        [Test]
        public void ExcludedPaths_CanClearAll()
        {
            _config.ExcludedPaths.Add("Assets/Test1/");
            _config.ExcludedPaths.Add("Assets/Test2/");
            _config.ExcludedPaths.Clear();

            Assert.That(_config.ExcludedPaths.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExcludedPaths_CanAddEmptyString()
        {
            // UI allows adding empty string for user to fill in
            _config.ExcludedPaths.Clear();
            _config.ExcludedPaths.Add("");

            Assert.That(_config.ExcludedPaths.Count, Is.EqualTo(1));
            Assert.That(_config.ExcludedPaths[0], Is.Empty);
        }

        [Test]
        public void ExcludedPaths_CanAddDuplicates()
        {
            // Duplicates are allowed (user's responsibility)
            _config.ExcludedPaths.Clear();
            _config.ExcludedPaths.Add("Assets/Test/");
            _config.ExcludedPaths.Add("Assets/Test/");

            Assert.That(_config.ExcludedPaths.Count, Is.EqualTo(2));
        }

        #endregion

        #region ExcludedPathPresets Tests

        [Test]
        public void ExcludedPathPresets_HasPresets()
        {
            Assert.That(ExcludedPathPresets.Presets, Is.Not.Null);
            Assert.That(ExcludedPathPresets.Presets.Length, Is.GreaterThan(0));
        }

        [Test]
        public void ExcludedPathPresets_AllPresetsHaveLabel()
        {
            foreach (var preset in ExcludedPathPresets.Presets)
            {
                Assert.That(
                    preset.Label,
                    Is.Not.Null.And.Not.Empty,
                    "All presets should have a label"
                );
            }
        }

        [Test]
        public void ExcludedPathPresets_AllPresetsHavePath()
        {
            foreach (var preset in ExcludedPathPresets.Presets)
            {
                Assert.That(
                    preset.Path,
                    Is.Not.Null.And.Not.Empty,
                    $"Preset '{preset.Label}' should have a path"
                );
            }
        }

        [Test]
        public void ExcludedPathPresets_PathsStartWithAssetsOrPackages()
        {
            foreach (var preset in ExcludedPathPresets.Presets)
            {
                Assert.That(
                    preset.Path.StartsWith("Assets/") || preset.Path.StartsWith("Packages/"),
                    Is.True,
                    $"Preset path '{preset.Path}' should start with Assets/ or Packages/"
                );
            }
        }

        #endregion

        #region Preset Behavior Tests

        [Test]
        public void ApplyPreset_ResetsTextureFiltersToTrue()
        {
            // Presets reset texture filters to enabled state
            _config.ProcessMainTextures = false;
            _config.ProcessNormalMaps = false;

            // Apply preset (non-Custom presets reset filters)
            _config.ApplyPreset(CompressorPreset.Balanced);

            // Filters are reset by preset
            Assert.That(_config.ProcessMainTextures, Is.True);
            Assert.That(_config.ProcessNormalMaps, Is.True);
        }

        [Test]
        public void ApplyPreset_Custom_DoesNotChangeTextureFilters()
        {
            // Custom preset preserves current settings
            _config.ProcessMainTextures = false;
            _config.ProcessNormalMaps = false;

            // Apply Custom preset
            _config.ApplyPreset(CompressorPreset.Custom);

            // Filters should remain unchanged
            Assert.That(_config.ProcessMainTextures, Is.False);
            Assert.That(_config.ProcessNormalMaps, Is.False);
        }

        [Test]
        public void ApplyPreset_DoesNotChangeExcludedPaths()
        {
            // Clear and add custom path
            _config.ExcludedPaths.Clear();
            _config.ExcludedPaths.Add("Assets/Test/");

            // Apply preset
            _config.ApplyPreset(CompressorPreset.Aggressive);

            // Excluded paths should remain (presets don't modify them)
            Assert.That(_config.ExcludedPaths.Count, Is.EqualTo(1));
            Assert.That(_config.ExcludedPaths[0], Is.EqualTo("Assets/Test/"));
        }

        #endregion
    }
}
