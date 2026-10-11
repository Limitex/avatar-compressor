using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using dev.limitex.avatar.compressor.editor;
using nadena.dev.ndmf.localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using static dev.limitex.avatar.compressor.tests.LocalizationTestCatalog;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal sealed class AvatarCompressorLocalizationTests : LocalizationTestBase
    {
        [Test]
        public void LoadLocalizations_TakesLanguageFromFileNameAndIncludesBundledLocales()
        {
            string[] languages = AvatarCompressorLocalization
                .LoadLocalizations()
                .Select(entry => entry.Language)
                .ToArray();
            string[] fileNames = PoPaths().Select(Path.GetFileNameWithoutExtension).ToArray();

            CollectionAssert.AreEquivalent(fileNames, languages);
            CollectionAssert.IsSubsetOf(BundledLocales, languages);
            CollectionAssert.AllItemsAreUnique(
                languages.Select(language => language.ToLowerInvariant()).ToArray()
            );
            foreach (string language in languages)
            {
                Assert.DoesNotThrow(
                    () => CultureInfo.GetCultureInfo(language),
                    $"'{language}' is not a culture name."
                );
            }
        }

        [Test]
        public void LoadLocalizations_SkipsFilesWithoutUniqueCultureNames()
        {
            string valid = CreateTempPo("fr-FR.po", "msgid \"k\"\nmsgstr \"v\"\n");
            string invalid = CreateTempPo("not a culture.po", "msgid \"k\"\nmsgstr \"v\"\n");

            try
            {
                LogAssert.Expect(LogType.Warning, new Regex("not a culture name"));
                LogAssert.Expect(LogType.Warning, new Regex("already provides this locale"));

                var loaded = AvatarCompressorLocalization.LoadLocalizations(
                    new[] { invalid, valid, valid }
                );

                Assert.AreEqual(1, loaded.Count);
                Assert.AreEqual("fr-FR", loaded[0].Language);
                Assert.AreEqual("v", loaded[0].Lookup("k"));
            }
            finally
            {
                DeleteTempAsset(valid);
                DeleteTempAsset(invalid);
            }
        }

        [Test]
        public void LoadLocalizations_ImportsCrowdinExportedHeader()
        {
            string path = CreateTempPo(
                "pt-BR.po",
                "msgid \"k\"\nmsgstr \"v\"\n",
                "\"Language: pt_BR\\n\"\n"
                    + "\"Plural-Forms: nplurals=2; plural=(n > 1);\\n\"\n"
                    + "\"X-Crowdin-Project: avatar-compressor\\n\"\n"
                    + "\"X-Crowdin-Language: pt-BR\\n\"\n"
                    + "\"PO-Revision-Date: 2026-09-23 00:00\\n\"\n"
            );

            try
            {
                var loaded = AvatarCompressorLocalization.LoadLocalizations(new[] { path });

                Assert.AreEqual(1, loaded.Count);
                Assert.AreEqual("pt-BR", loaded[0].Language);
                Assert.AreEqual("v", loaded[0].Lookup("k"));
            }
            finally
            {
                DeleteTempAsset(path);
            }
        }

        [Test]
        public void CreateLookup_EmptyOrMissingTranslation_ReturnsNull()
        {
            string path = CreateTempPo(
                "lac_localization_lookup.po",
                "msgid \"translated\"\nmsgstr \"value\"\n\nmsgid \"untranslated\"\nmsgstr \"\"\n"
            );

            try
            {
                var lookup = AvatarCompressorLocalization.CreateLookup(
                    AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path)
                );

                Assert.IsNotNull(lookup);
                Assert.AreEqual("value", lookup("translated"));
                Assert.IsNull(lookup("untranslated"));
                Assert.IsNull(lookup("missing"));
            }
            finally
            {
                DeleteTempAsset(path);
            }
        }

        [Test]
        public void CreateLookup_DeletedAsset_ReturnsNull()
        {
            string path = CreateTempPo(
                "lac_localization_deleted.po",
                "msgid \"k\"\nmsgstr \"v\"\n"
            );
            var lookup = AvatarCompressorLocalization.CreateLookup(
                AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path)
            );
            Assert.AreEqual("v", lookup("k"));

            DeleteTempAsset(path);

            Assert.IsNull(lookup("k"));
        }

        [UnityTest]
        public IEnumerator Translations_ReturnsTextFromEveryLocaleThatTranslatesKey()
        {
            var english = Track(new TemporaryCatalog("en-US"));
            var korean = Track(new TemporaryCatalog("ko-KR"));
            var japanese = Track(new TemporaryCatalog("ja-JP"));
            english.Import("English test text");
            korean.Import("Korean test text");
            japanese.Import("");
            yield return WaitForEditorCallbacks();

            LanguagePrefs.Language = "ja-JP";
            CollectionAssert.AreEquivalent(
                new[] { "English test text", "Korean test text" },
                AvatarCompressorLocalization.Translations(TemporaryCatalog.Key).ToArray()
            );
        }

        [UnityTest]
        public IEnumerator LanguagePrefs_SelectsTranslationFromMatchingFile()
        {
            foreach (string language in BundledLocales)
            {
                var catalog = Track(new TemporaryCatalog(language));
                catalog.Import("Test translation for " + language);
            }
            yield return WaitForEditorCallbacks();

            // Every locale has a distinct controlled value; always using English
            // cannot pass, even when the shipped translations are incomplete.
            foreach (string language in BundledLocales)
            {
                LanguagePrefs.Language = language;
                Assert.AreEqual(
                    "Test translation for " + language,
                    AvatarCompressorLocalization.Tr(TemporaryCatalog.Key)
                );
            }
        }

        [UnityTest]
        public IEnumerator Tr_FormatsArguments()
        {
            var catalog = Track(new TemporaryCatalog());
            catalog.Import("Test items: {0}");
            LanguagePrefs.Language = catalog.Language;
            yield return WaitForTranslation(TemporaryCatalog.Key, "Test items: {0}");

            Assert.AreEqual(
                "Test items: 3",
                AvatarCompressorLocalization.Tr(TemporaryCatalog.Key, 3)
            );
        }

        [UnityTest]
        public IEnumerator Tr_TooFewFormatArguments_ReturnsMissingKeyPlaceholder()
        {
            var catalog = Track(new TemporaryCatalog());
            catalog.Import("Test range: {0} to {1}");
            LanguagePrefs.Language = catalog.Language;
            yield return WaitForTranslation(TemporaryCatalog.Key, "Test range: {0} to {1}");

            Assert.AreEqual(
                $"<{TemporaryCatalog.Key}>",
                AvatarCompressorLocalization.Tr(TemporaryCatalog.Key, 1)
            );
        }
    }
}
