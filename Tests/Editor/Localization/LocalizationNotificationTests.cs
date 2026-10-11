using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.texture;
using nadena.dev.ndmf.localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static dev.limitex.avatar.compressor.tests.LocalizationTestCatalog;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal sealed class LocalizationNotificationTests : LocalizationTestBase
    {
        [UnityTest]
        public IEnumerator LanguageChanges_NotifyOnceWithFinalTranslation()
        {
            var catalog = Track(new TemporaryCatalog());
            catalog.Import("Final test translation");
            LanguagePrefs.Language = "en-US";
            _ = AvatarCompressorLocalization.Localizer;
            yield return WaitForEditorCallbacks();

            int calls = 0;
            string translation = null;
            void OnChanged()
            {
                calls++;
                translation = AvatarCompressorLocalization.Tr(TemporaryCatalog.Key);
            }

            AvatarCompressorLocalization.Changed += OnChanged;
            try
            {
                LanguagePrefs.Language = "ja-JP";
                LanguagePrefs.Language = catalog.Language;
                Assert.AreEqual(0, calls, "Notifications must not run inside the language picker.");

                yield return WaitForEditorCallbacks();
                Assert.AreEqual(1, calls);
                Assert.AreEqual("Final test translation", translation);

                LanguagePrefs.Language = catalog.Language;
                yield return WaitForEditorCallbacks();
                Assert.AreEqual(1, calls, "Selecting the same language must not repaint again.");
            }
            finally
            {
                AvatarCompressorLocalization.Changed -= OnChanged;
            }
        }

        [UnityTest]
        public IEnumerator CatalogReload_WithoutLanguageChange_NotifiesAfterReload()
        {
            _ = AvatarCompressorLocalization.Localizer;
            yield return WaitForEditorCallbacks();
            int calls = 0;
            string language = AvatarCompressorLocalization.CurrentLanguage;
            void OnChanged()
            {
                calls++;
            }

            AvatarCompressorLocalization.Changed += OnChanged;
            try
            {
                Localizer.ReloadLocalizations();
                Localizer.ReloadLocalizations();
                Assert.AreEqual(0, calls);
                yield return WaitForEditorCallbacks();

                Assert.AreEqual(1, calls);
                Assert.AreEqual(language, AvatarCompressorLocalization.CurrentLanguage);
            }
            finally
            {
                AvatarCompressorLocalization.Changed -= OnChanged;
            }
        }

        [UnityTest]
        public IEnumerator UnsubscribedListener_DoesNotReceivePendingNotification()
        {
            _ = AvatarCompressorLocalization.Localizer;
            yield return WaitForEditorCallbacks();
            int calls = 0;
            void OnChanged() => calls++;
            AvatarCompressorLocalization.Changed += OnChanged;
            Localizer.ReloadLocalizations();
            AvatarCompressorLocalization.Changed -= OnChanged;

            yield return WaitForEditorCallbacks();
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Inspectors_RegisterUnityRepaintUntilDestroyed()
        {
            var gameObject = new GameObject("Localization repaint test");
            var preset = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            Editor componentEditor = null;
            Editor presetEditor = null;
            try
            {
                componentEditor = Editor.CreateEditor(gameObject.AddComponent<TextureCompressor>());
                presetEditor = Editor.CreateEditor(preset);
                Assert.IsInstanceOf<TextureCompressorEditor>(componentEditor);
                Assert.IsInstanceOf<CustomTextureCompressorPresetEditor>(presetEditor);
                Action componentRepaint = componentEditor.Repaint;
                Action presetRepaint = presetEditor.Repaint;
                Assert.AreEqual(1, SubscriptionCount(componentRepaint));
                Assert.AreEqual(1, SubscriptionCount(presetRepaint));

                UnityEngine.Object.DestroyImmediate(componentEditor);
                UnityEngine.Object.DestroyImmediate(presetEditor);
                Assert.AreEqual(0, SubscriptionCount(componentRepaint));
                Assert.AreEqual(0, SubscriptionCount(presetRepaint));
            }
            finally
            {
                if (componentEditor != null)
                    UnityEngine.Object.DestroyImmediate(componentEditor);
                if (presetEditor != null)
                    UnityEngine.Object.DestroyImmediate(presetEditor);
                UnityEngine.Object.DestroyImmediate(preset);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Preferences_RegisterUnityRepaintOnlyWhileActive()
        {
            var provider = AvatarCompressorPreferences.CreateProvider();
            try
            {
                Assert.AreEqual(0, SubscriptionCount(provider.Repaint));
                for (int i = 0; i < 2; i++)
                {
                    provider.OnActivate("", new VisualElement());
                    Assert.AreEqual(1, SubscriptionCount(provider.Repaint));
                    provider.OnDeactivate();
                    Assert.AreEqual(0, SubscriptionCount(provider.Repaint));
                }
            }
            finally
            {
                provider.OnDeactivate();
            }
        }

        private static int SubscriptionCount(Action callback)
        {
            // Inspect our own event at the Unity boundary: counting a test wrapper
            // can pass when it never calls Repaint. No production test hooks needed.
            var field = typeof(AvatarCompressorLocalization).GetField(
                nameof(AvatarCompressorLocalization.Changed),
                BindingFlags.Static | BindingFlags.NonPublic
            );
            Assert.IsNotNull(field);
            var listeners = (Action)field.GetValue(null);
            return listeners?.GetInvocationList().Count(listener => listener.Equals(callback)) ?? 0;
        }
    }
}
