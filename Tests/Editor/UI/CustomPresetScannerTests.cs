using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class CustomPresetScannerTests
    {
        private CustomCompressorPreset _presetWithMenuPath;
        private CustomCompressorPreset _presetWithoutMenuPath;

        [SetUp]
        public void SetUp()
        {
            CustomPresetScanner.ClearCache();

            _presetWithMenuPath = ScriptableObject.CreateInstance<CustomCompressorPreset>();
            _presetWithMenuPath.MenuPath = "Test/Preset";

            _presetWithoutMenuPath = ScriptableObject.CreateInstance<CustomCompressorPreset>();
            _presetWithoutMenuPath.MenuPath = "";
        }

        [TearDown]
        public void TearDown()
        {
            if (_presetWithMenuPath != null)
            {
                UnityEngine.Object.DestroyImmediate(_presetWithMenuPath);
            }
            if (_presetWithoutMenuPath != null)
            {
                UnityEngine.Object.DestroyImmediate(_presetWithoutMenuPath);
            }
        }

        #region GetMenuPresets Tests

        [Test]
        public void GetMenuPresets_ReturnsListType()
        {
            var result = CustomPresetScanner.GetMenuPresets();

            Assert.That(result, Is.Not.Null);
            Assert.That(
                result,
                Is.InstanceOf<System.Collections.Generic.List<CustomCompressorPreset>>()
            );
        }

        [Test]
        public void GetMenuPresets_ExcludesEmptyMenuPath()
        {
            // This test verifies the filtering logic by checking that presets without MenuPath are not included
            // Note: The actual asset database search depends on saved assets, so we test the concept
            var presets = CustomPresetScanner.GetMenuPresets();

            // All returned presets should have non-empty MenuPath
            foreach (var preset in presets)
            {
                Assert.That(
                    preset.MenuPath,
                    Is.Not.Empty,
                    $"Preset '{preset.name}' should have a non-empty MenuPath"
                );
            }
        }

        #endregion

        #region BuildPresetMenu Tests

        [Test]
        public void BuildPresetMenu_ReturnsGenericMenu()
        {
            var menu = CustomPresetScanner.BuildPresetMenu(onPresetSelected: _ => { });

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu, Is.InstanceOf<GenericMenu>());
        }

        [Test]
        public void BuildPresetMenu_WithNullCallback_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var menu = CustomPresetScanner.BuildPresetMenu(onPresetSelected: null);
            });
        }

        [Test]
        public void BuildPresetMenu_CalledMultipleTimes_ReturnsNewInstance()
        {
            var menu1 = CustomPresetScanner.BuildPresetMenu(onPresetSelected: _ => { });

            var menu2 = CustomPresetScanner.BuildPresetMenu(onPresetSelected: _ => { });

            Assert.That(menu1, Is.Not.SameAs(menu2));
        }

        #endregion

        #region Cache Tests

        [Test]
        public void ClearCache_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CustomPresetScanner.ClearCache());
        }

        [Test]
        public void GetMenuPresets_ReturnsCopyOfList()
        {
            var list1 = CustomPresetScanner.GetMenuPresets();
            var list2 = CustomPresetScanner.GetMenuPresets();

            Assert.That(list1, Is.Not.SameAs(list2));
        }

        [Test]
        public void GetMenuPresets_AfterClearCache_ReturnsValidList()
        {
            CustomPresetScanner.GetMenuPresets();
            CustomPresetScanner.ClearCache();

            var result = CustomPresetScanner.GetMenuPresets();

            Assert.That(result, Is.Not.Null);
        }

        #endregion
    }
}
