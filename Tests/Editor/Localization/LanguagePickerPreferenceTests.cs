using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal sealed class LanguagePickerPreferenceTests : LocalizationTestBase
    {
        [Test]
        public void NewUser_ShowsLanguagePickerByDefault()
        {
            EditorPrefs.DeleteKey(
                AvatarCompressorPreferences.PrefsPrefix + "showLanguagePickerInInspector"
            );
            Assert.IsTrue(AvatarCompressorPreferences.ShowLanguagePickerInInspector);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Inspectors_RespectLanguagePickerPreference(bool visible)
        {
            InspectorUiProbe.RequireGraphicsDevice();
            AvatarCompressorPreferences.ShowLanguagePickerInInspector = visible;
            var gameObject = new GameObject("Language picker test");
            var preset = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            try
            {
                using var componentView = InspectorUiProbe.OpenInspector(
                    gameObject.AddComponent<TextureCompressor>()
                );
                using var presetView = InspectorUiProbe.OpenInspector(preset);
                foreach (var view in new[] { componentView, presetView })
                    Assert.AreEqual(
                        visible,
                        view.HasControl(AvatarCompressorLocalization.LanguagePickerControlName)
                    );
            }
            finally
            {
                Object.DestroyImmediate(preset);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Preferences_OffersLanguagePickerWhenInspectorsHideIt()
        {
            InspectorUiProbe.RequireGraphicsDevice();
            AvatarCompressorPreferences.ShowLanguagePickerInInspector = false;
            using var view = InspectorUiProbe.OpenPreferences();
            Assert.IsTrue(view.HasControl(AvatarCompressorLocalization.LanguagePickerControlName));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Preferences_TogglingInspectorPicker_PersistsAfterClosing(bool initiallyVisible)
        {
            InspectorUiProbe.RequireGraphicsDevice();
            AvatarCompressorPreferences.ShowLanguagePickerInInspector = initiallyVisible;
            using (var view = InspectorUiProbe.OpenPreferences())
                view.Toggle(nameof(AvatarCompressorPreferences.ShowLanguagePickerInInspector));

            Assert.AreEqual(
                !initiallyVisible,
                AvatarCompressorPreferences.ShowLanguagePickerInInspector
            );
            using var reopened = InspectorUiProbe.OpenPreferences();
            reopened.Draw();
            Assert.AreEqual(
                !initiallyVisible,
                AvatarCompressorPreferences.ShowLanguagePickerInInspector
            );
        }
    }
}
