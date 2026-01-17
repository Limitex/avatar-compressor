using NUnit.Framework;
using dev.limitex.avatar.compressor;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ExcludedPathPresetsTests
    {
        #region GetDefaultPaths Tests

        [Test]
        public void GetDefaultPaths_ReturnsNonEmptyArray()
        {
            var paths = ExcludedPathPresets.GetDefaultPaths();

            Assert.That(paths, Is.Not.Null);
            Assert.That(paths.Length, Is.GreaterThan(0));
        }

        [Test]
        public void GetDefaultPaths_ReturnsCorrectCount()
        {
            var paths = ExcludedPathPresets.GetDefaultPaths();

            Assert.That(paths.Length, Is.EqualTo(ExcludedPathPresets.Presets.Length));
        }

        [Test]
        public void GetDefaultPaths_AllPathsAreNotNullOrEmpty()
        {
            var paths = ExcludedPathPresets.GetDefaultPaths();

            foreach (var path in paths)
            {
                Assert.That(path, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void GetDefaultPaths_ReturnsNewArrayEachTime()
        {
            var paths1 = ExcludedPathPresets.GetDefaultPaths();
            var paths2 = ExcludedPathPresets.GetDefaultPaths();

            Assert.That(paths1, Is.Not.SameAs(paths2));
        }

        [Test]
        public void GetDefaultPaths_ContainsVRCFuryTempPath()
        {
            var paths = ExcludedPathPresets.GetDefaultPaths();

            Assert.That(paths, Does.Contain("Packages/com.vrcfury.temp/"));
        }

        #endregion

        #region Presets Static Field Tests

        [Test]
        public void Presets_IsNotNull()
        {
            Assert.That(ExcludedPathPresets.Presets, Is.Not.Null);
        }

        [Test]
        public void Presets_ContainsValidEntries()
        {
            foreach (var preset in ExcludedPathPresets.Presets)
            {
                Assert.That(preset.Label, Is.Not.Null.And.Not.Empty, "Preset label should not be null or empty");
                Assert.That(preset.Path, Is.Not.Null.And.Not.Empty, "Preset path should not be null or empty");
            }
        }

        [Test]
        public void Presets_PathsEndWithSlash()
        {
            foreach (var preset in ExcludedPathPresets.Presets)
            {
                Assert.That(preset.Path, Does.EndWith("/"), $"Path '{preset.Path}' should end with '/'");
            }
        }

        #endregion

        #region ExcludedPathPreset Struct Tests

        [Test]
        public void ExcludedPathPreset_Constructor_SetsProperties()
        {
            var preset = new ExcludedPathPreset("Test Label", "test/path/");

            Assert.That(preset.Label, Is.EqualTo("Test Label"));
            Assert.That(preset.Path, Is.EqualTo("test/path/"));
        }

        [Test]
        public void ExcludedPathPreset_Constructor_AcceptsEmptyStrings()
        {
            var preset = new ExcludedPathPreset("", "");

            Assert.That(preset.Label, Is.EqualTo(""));
            Assert.That(preset.Path, Is.EqualTo(""));
        }

        [Test]
        public void ExcludedPathPreset_Constructor_AcceptsNull()
        {
            var preset = new ExcludedPathPreset(null, null);

            Assert.That(preset.Label, Is.Null);
            Assert.That(preset.Path, Is.Null);
        }

        #endregion
    }
}
