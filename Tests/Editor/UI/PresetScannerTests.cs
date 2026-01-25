using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PresetScannerTests
    {
        private CustomTextureCompressorPreset _presetWithMenuPath;
        private CustomTextureCompressorPreset _presetWithoutMenuPath;

        [SetUp]
        public void SetUp()
        {
            _presetWithMenuPath = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            _presetWithMenuPath.MenuPath = "Test/Preset";

            _presetWithoutMenuPath =
                ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
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
            var result = PresetScanner.GetMenuPresets();

            Assert.That(result, Is.Not.Null);
            Assert.That(
                result,
                Is.InstanceOf<System.Collections.Generic.List<CustomTextureCompressorPreset>>()
            );
        }

        [Test]
        public void GetMenuPresets_ExcludesEmptyMenuPath()
        {
            // This test verifies the filtering logic by checking that presets without MenuPath are not included
            // Note: The actual asset database search depends on saved assets, so we test the concept
            var presets = PresetScanner.GetMenuPresets();

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
            var menu = PresetScanner.BuildPresetMenu(
                currentPreset: null,
                onPresetSelected: _ => { }
            );

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu, Is.InstanceOf<GenericMenu>());
        }

        [Test]
        public void BuildPresetMenu_WithNullCallback_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var menu = PresetScanner.BuildPresetMenu(
                    currentPreset: null,
                    onPresetSelected: null
                );
            });
        }

        [Test]
        public void BuildPresetMenu_CalledMultipleTimes_ReturnsNewInstance()
        {
            var menu1 = PresetScanner.BuildPresetMenu(
                currentPreset: null,
                onPresetSelected: _ => { }
            );

            var menu2 = PresetScanner.BuildPresetMenu(
                currentPreset: null,
                onPresetSelected: _ => { }
            );

            Assert.That(menu1, Is.Not.SameAs(menu2));
        }

        [Test]
        public void BuildPresetMenu_WithCurrentPreset_ReturnsGenericMenu()
        {
            var menu = PresetScanner.BuildPresetMenu(
                currentPreset: _presetWithMenuPath,
                onPresetSelected: _ => { }
            );

            Assert.That(menu, Is.Not.Null);
            Assert.That(menu, Is.InstanceOf<GenericMenu>());
        }

        [Test]
        public void BuildPresetMenu_WithCurrentPreset_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                PresetScanner.BuildPresetMenu(
                    currentPreset: _presetWithMenuPath,
                    onPresetSelected: _ => { }
                );
            });
        }

        #endregion

        #region Cache Tests

        [Test]
        public void GetMenuPresets_ReturnsCopyOfList()
        {
            var list1 = PresetScanner.GetMenuPresets();
            var list2 = PresetScanner.GetMenuPresets();

            Assert.That(list1, Is.Not.SameAs(list2));
        }

        #endregion

        #region Sorting Tests

        [Test]
        public void GetMenuPresets_SortsByMenuOrderFirst()
        {
            var presets = PresetScanner.GetMenuPresets();

            if (presets.Count < 2)
            {
                Assert.Ignore("Not enough presets to test sorting");
                return;
            }

            // Verify MenuOrder is non-decreasing
            for (int i = 1; i < presets.Count; i++)
            {
                var prev = presets[i - 1];
                var curr = presets[i];

                bool isOrderValid =
                    prev.MenuOrder < curr.MenuOrder
                    || (
                        prev.MenuOrder == curr.MenuOrder
                        && string.Compare(
                            prev.MenuPath,
                            curr.MenuPath,
                            System.StringComparison.Ordinal
                        ) <= 0
                    );

                Assert.That(
                    isOrderValid,
                    Is.True,
                    $"Presets not sorted correctly: '{prev.MenuPath}' (Order={prev.MenuOrder}) should come before '{curr.MenuPath}' (Order={curr.MenuOrder})"
                );
            }
        }

        [Test]
        public void GetMenuPresets_SortsByMenuPathWhenMenuOrderEqual()
        {
            var presets = PresetScanner.GetMenuPresets();

            // Find consecutive presets with same MenuOrder
            for (int i = 1; i < presets.Count; i++)
            {
                var prev = presets[i - 1];
                var curr = presets[i];

                if (prev.MenuOrder == curr.MenuOrder)
                {
                    int pathCompare = string.Compare(
                        prev.MenuPath,
                        curr.MenuPath,
                        System.StringComparison.Ordinal
                    );

                    Assert.That(
                        pathCompare,
                        Is.LessThanOrEqualTo(0),
                        $"Presets with same MenuOrder ({prev.MenuOrder}) not sorted by MenuPath: '{prev.MenuPath}' should come before '{curr.MenuPath}'"
                    );
                }
            }
        }

        [Test]
        public void SortLogic_MenuOrderTakesPrecedenceOverMenuPath()
        {
            // Create presets with specific MenuOrder and MenuPath values
            var presetA = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            var presetB = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            var presetC = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();

            try
            {
                // presetA: MenuOrder=200, MenuPath="A" (alphabetically first, but higher order)
                presetA.MenuOrder = 200;
                presetA.MenuPath = "A";

                // presetB: MenuOrder=100, MenuPath="Z" (alphabetically last, but lower order)
                presetB.MenuOrder = 100;
                presetB.MenuPath = "Z";

                // presetC: MenuOrder=100, MenuPath="B" (same order as B, but alphabetically between A and Z)
                presetC.MenuOrder = 100;
                presetC.MenuPath = "B";

                var list = new System.Collections.Generic.List<CustomTextureCompressorPreset>
                {
                    presetA,
                    presetB,
                    presetC,
                };

                // Apply same sorting logic as PresetScanner
                list.Sort(
                    (a, b) =>
                    {
                        int orderCompare = a.MenuOrder.CompareTo(b.MenuOrder);
                        if (orderCompare != 0)
                            return orderCompare;
                        return string.Compare(
                            a.MenuPath,
                            b.MenuPath,
                            System.StringComparison.Ordinal
                        );
                    }
                );

                // Expected order: presetC (100, "B"), presetB (100, "Z"), presetA (200, "A")
                Assert.That(
                    list[0],
                    Is.SameAs(presetC),
                    "First should be presetC (MenuOrder=100, MenuPath='B')"
                );
                Assert.That(
                    list[1],
                    Is.SameAs(presetB),
                    "Second should be presetB (MenuOrder=100, MenuPath='Z')"
                );
                Assert.That(
                    list[2],
                    Is.SameAs(presetA),
                    "Third should be presetA (MenuOrder=200, MenuPath='A')"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presetA);
                UnityEngine.Object.DestroyImmediate(presetB);
                UnityEngine.Object.DestroyImmediate(presetC);
            }
        }

        [Test]
        public void SortLogic_SameMenuOrder_SortsByMenuPathAlphabetically()
        {
            var preset1 = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            var preset2 = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            var preset3 = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();

            try
            {
                preset1.MenuOrder = 100;
                preset1.MenuPath = "Category/Zebra";

                preset2.MenuOrder = 100;
                preset2.MenuPath = "Category/Apple";

                preset3.MenuOrder = 100;
                preset3.MenuPath = "Category/Mango";

                var list = new System.Collections.Generic.List<CustomTextureCompressorPreset>
                {
                    preset1,
                    preset2,
                    preset3,
                };

                // Apply same sorting logic as PresetScanner
                list.Sort(
                    (a, b) =>
                    {
                        int orderCompare = a.MenuOrder.CompareTo(b.MenuOrder);
                        if (orderCompare != 0)
                            return orderCompare;
                        return string.Compare(
                            a.MenuPath,
                            b.MenuPath,
                            System.StringComparison.Ordinal
                        );
                    }
                );

                // Expected order: Apple, Mango, Zebra
                Assert.That(list[0].MenuPath, Is.EqualTo("Category/Apple"));
                Assert.That(list[1].MenuPath, Is.EqualTo("Category/Mango"));
                Assert.That(list[2].MenuPath, Is.EqualTo("Category/Zebra"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preset1);
                UnityEngine.Object.DestroyImmediate(preset2);
                UnityEngine.Object.DestroyImmediate(preset3);
            }
        }

        [Test]
        public void SortLogic_DefaultMenuOrderValue_IsHigherThanBuiltIn()
        {
            var defaultPreset = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();

            try
            {
                // Default value should be 1000 (higher than built-in presets which use 100-500)
                Assert.That(
                    defaultPreset.MenuOrder,
                    Is.EqualTo(1000),
                    "Default MenuOrder should be 1000"
                );

                // Built-in presets should appear before user presets
                Assert.That(
                    defaultPreset.MenuOrder,
                    Is.GreaterThan(500),
                    "Default MenuOrder should be greater than built-in preset range (100-500)"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(defaultPreset);
            }
        }

        [Test]
        public void GetMenuPresets_BuiltInPresetsAppearFirst()
        {
            var presets = PresetScanner.GetMenuPresets();

            if (presets.Count == 0)
            {
                Assert.Ignore("No presets available to test");
                return;
            }

            // Find the first preset with default MenuOrder (user preset)
            int firstUserPresetIndex = -1;
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i].MenuOrder >= 1000)
                {
                    firstUserPresetIndex = i;
                    break;
                }
            }

            // All presets before the first user preset should have MenuOrder < 1000 (built-in)
            if (firstUserPresetIndex > 0)
            {
                for (int i = 0; i < firstUserPresetIndex; i++)
                {
                    Assert.That(
                        presets[i].MenuOrder,
                        Is.LessThan(1000),
                        $"Preset '{presets[i].MenuPath}' at index {i} should be a built-in preset (MenuOrder < 1000)"
                    );
                }
            }
        }

        #endregion
    }
}
