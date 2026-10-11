using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.texture;
using dev.limitex.avatar.compressor.editor.texture.ui;
using dev.limitex.avatar.compressor.editor.ui;
using nadena.dev.ndmf.localization;
using NUnit.Framework;
using UnityEngine.TestTools;
using static dev.limitex.avatar.compressor.tests.LocalizationTestCatalog;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal sealed class LocalizationReloadTests : LocalizationTestBase
    {
        private const string MainCategoryKey = "TextureCompressor:enum:texturePropertyCategoryMain";

        [UnityTest]
        public IEnumerator PreviewSearch_PoReimportInSameLanguage_RefreshesResultsAndCount()
        {
            var catalog = Track(new TemporaryCatalog());
            LanguagePrefs.Language = catalog.Language;
            catalog.Import("before", MainCategoryKey);
            yield return WaitForTranslation(MainCategoryKey, "before");

            var search = new PreviewSearch(new SearchBoxControl("updated"));
            var items = new[]
            {
                new TexturePreviewData { TextureType = TexturePropertyCategory.Main },
            };
            Assert.IsFalse(search.Matches(items[0]));
            Assert.AreEqual(0, search.CountMatches(items));

            catalog.Import("updated", MainCategoryKey);
            yield return WaitForTranslation(MainCategoryKey, "updated");

            Assert.IsTrue(search.Matches(items[0]));
            Assert.AreEqual(1, search.CountMatches(items));
        }

        [UnityTest]
        public IEnumerator CatalogChanges_AddedDeletedAndRecreatedLocale_RefreshesTranslation()
        {
            var catalog = Track(new TemporaryCatalog());
            LanguagePrefs.Language = catalog.Language;
            yield return WaitForTranslation(TemporaryCatalog.Key, $"<{TemporaryCatalog.Key}>");

            catalog.Import();
            yield return WaitForTranslation(TemporaryCatalog.Key, "translated");

            catalog.Delete();
            yield return WaitForTranslation(TemporaryCatalog.Key, $"<{TemporaryCatalog.Key}>");

            catalog.Import("recreated");
            yield return WaitForTranslation(TemporaryCatalog.Key, "recreated");
        }

        [UnityTest]
        public IEnumerator UntranslatedEntry_UsesEnglishFallback()
        {
            const string key = "Common:label:general";
            string english = ReadMessages(LocalizationFolder + "/en-US.po")[key];
            var catalog = Track(new TemporaryCatalog());
            LanguagePrefs.Language = catalog.Language;

            catalog.Import("translated", key);
            yield return WaitForTranslation(key, "translated");

            catalog.Import("", key);
            yield return WaitForTranslation(key, english);

            catalog.Import();
            yield return WaitForTranslation(key, english);
            Assert.AreEqual("<missing:key>", AvatarCompressorLocalization.Tr("missing:key"));
        }

        [UnityTest]
        public IEnumerator CatalogChanges_MovedLocale_UsesOnlyFilesInsideCatalog()
        {
            var catalog = Track(new TemporaryCatalog());
            LanguagePrefs.Language = catalog.Language;
            _ = AvatarCompressorLocalization.Tr(TemporaryCatalog.Key);

            catalog.Import();
            yield return WaitForTranslation(TemporaryCatalog.Key, "translated");

            // Preserve the asset GUID so a stale lookup would still return its translation.
            catalog.MoveOutside();
            yield return WaitForTranslation(TemporaryCatalog.Key, $"<{TemporaryCatalog.Key}>");

            catalog.MoveBack();
            yield return WaitForTranslation(TemporaryCatalog.Key, "translated");
        }

        [UnityTest]
        public IEnumerator CatalogReimport_NotifiesWithUpdatedTranslationInSameLanguage()
        {
            var catalog = Track(new TemporaryCatalog());
            LanguagePrefs.Language = catalog.Language;
            catalog.Import();
            yield return WaitForTranslation(TemporaryCatalog.Key, "translated");
            yield return WaitForEditorCallbacks();

            var observed = new List<string>();
            void OnChanged() => observed.Add(AvatarCompressorLocalization.Tr(TemporaryCatalog.Key));
            AvatarCompressorLocalization.Changed += OnChanged;
            try
            {
                catalog.Import("updated");
                yield return WaitForEditorCallbacks();
                CollectionAssert.AreEqual(new[] { "updated" }, observed);
                Assert.AreEqual(
                    catalog.Language.ToLowerInvariant(),
                    AvatarCompressorLocalization.CurrentLanguage
                );
            }
            finally
            {
                AvatarCompressorLocalization.Changed -= OnChanged;
            }
        }

        [UnityTest]
        public IEnumerator TemporaryCatalog_Aborted_RestoresOriginalFileAndGuid()
        {
            string path = PhysicalPath(LocalizationFolder + "/ko-KR.po");
            byte[] original = File.ReadAllBytes(path);
            byte[] meta = File.ReadAllBytes(path + ".meta");
            Assert.Throws<InvalidOperationException>(() =>
            {
                using var catalog = new TemporaryCatalog();
                catalog.Import();
                catalog.MoveOutside();
                throw new InvalidOperationException("Simulated test failure");
            });
            yield return WaitForEditorCallbacks();
            CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(meta, File.ReadAllBytes(path + ".meta"));
        }

        [UnityTest]
        public IEnumerator PreviewSearch_LanguageChange_RefreshesResultsAndCount()
        {
            var catalog = Track(new TemporaryCatalog());
            catalog.Import("test-main-category", MainCategoryKey);
            LanguagePrefs.Language = catalog.Language;
            yield return WaitForTranslation(MainCategoryKey, "test-main-category");

            LanguagePrefs.Language = "en-US";
            var search = new PreviewSearch(new SearchBoxControl("test-main-category"));
            var items = new[]
            {
                new TexturePreviewData { TextureType = TexturePropertyCategory.Main },
                new TexturePreviewData { TextureType = TexturePropertyCategory.Normal },
            };
            Assert.AreEqual(0, search.CountMatches(items));
            Assert.IsFalse(search.Matches(items[0]));

            LanguagePrefs.Language = catalog.Language;

            Assert.AreEqual(1, search.CountMatches(items));
            Assert.IsTrue(search.Matches(items[0]));
            Assert.IsFalse(search.Matches(items[1]));
        }
    }
}
