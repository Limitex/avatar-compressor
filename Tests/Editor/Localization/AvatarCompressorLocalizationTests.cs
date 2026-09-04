using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.texture;
using dev.limitex.avatar.compressor.editor.texture.ui;
using nadena.dev.ndmf.localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal sealed class AvatarCompressorLocalizationTests
    {
        private const string LocalizationFolder =
            "Packages/dev.limitex.avatar-compressor/Editor/Common/Localization";

        private static readonly string[] BundledLocales =
        {
            "en-US",
            "zh-Hans",
            "zh-Hant",
            "ja-JP",
            "ko-KR",
        };

        private string _originalLanguage;

        [SetUp]
        public void SetUp()
        {
            _originalLanguage = LanguagePrefs.Language;
        }

        [TearDown]
        public void TearDown()
        {
            LanguagePrefs.Language = _originalLanguage;
        }

        [Test]
        public void LocalizationAssets_DiscoversAllPoFilesAndIncludesBundledLocales()
        {
            var assets = AvatarCompressorLocalization.LoadLocalizationAssets();
            var poPaths = AssetDatabase
                .FindAssets(string.Empty, new[] { LocalizationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            CollectionAssert.AreEquivalent(poPaths, assets.Select(AssetDatabase.GetAssetPath));
            CollectionAssert.IsSubsetOf(
                BundledLocales.Select(language => language.ToLowerInvariant()),
                assets.Select(asset => asset.localeIsoCode.ToLowerInvariant())
            );
            Assert.AreEqual(
                assets.Count,
                assets
                    .Select(asset => asset.localeIsoCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()
            );
        }

        [Test]
        public void LocalizationFiles_HaveMatchingKeysAndFormatPlaceholders()
        {
            var english = ReadMessages(LocalizationFolder + "/en-US.po");

            foreach (var asset in AvatarCompressorLocalization.LoadLocalizationAssets())
            {
                string language = asset.localeIsoCode;
                var localized = ReadMessages(AssetDatabase.GetAssetPath(asset));
                CollectionAssert.AreEquivalent(
                    english.Keys,
                    localized.Keys,
                    $"Locale {language} has a different key set."
                );

                foreach (string key in english.Keys)
                {
                    Assert.IsNotEmpty(localized[key], $"Locale {language} has an empty '{key}'.");
                    CollectionAssert.AreEquivalent(
                        GetPlaceholders(english[key]),
                        GetPlaceholders(localized[key]),
                        $"Locale {language}, key '{key}' has different format placeholders."
                    );
                }
            }
        }

        [Test]
        public void LocalizationKeys_FollowScopeKindNameConvention()
        {
            foreach (string key in ReadMessages(LocalizationFolder + "/en-US.po").Keys)
            {
                StringAssert.IsMatch(
                    @"^(Common|TextureCompressor):[a-z][A-Za-z0-9]*:[a-z][A-Za-z0-9]*(?::tooltip)?$",
                    key
                );
            }
        }

        [Test]
        public void EnglishSource_UsesMsgstrForCrowdinSourceText()
        {
            string english = File.ReadAllText(LocalizationFolder + "/en-US.po");
            StringAssert.Contains("\"X-Crowdin-SourceKey: msgstr\\n\"", english);
        }

        [TestCase("en-US", "General")]
        [TestCase("zh-Hans", "常规")]
        [TestCase("zh-Hant", "一般")]
        [TestCase("ja-JP", "一般")]
        [TestCase("ko-KR", "일반")]
        public void LanguagePrefs_SelectsRequestedLocalization(string language, string expected)
        {
            LanguagePrefs.Language = language;

            Assert.AreEqual(expected, AvatarCompressorLocalization.Tr("Common:label:general"));
        }

        [TestCaseSource(nameof(BundledLocales))]
        public void NotAvailableMarker_RemainsLanguageIndependent(string language)
        {
            LanguagePrefs.Language = language;

            Assert.AreEqual("N/A", AvatarCompressorLocalization.Tr("Common:label:notAvailable"));
        }

        [Test]
        public void EnumMappings_CoverEveryDisplayedValue()
        {
            LanguagePrefs.Language = "en-US";

            AssertEnumLocalized<AnalysisStrategyType>();
            AssertEnumLocalized<CompressionPlatform>();
            AssertEnumLocalized<FrozenTextureFormat>();
            AssertEnumLocalized<AnalysisBackendPreference>();
            AssertEnumLocalized<ResizeBackendPreference>();
            AssertEnumLocalized<TexturePropertyCategory>();
            AssertEnumLocalized<SkipReason>();
        }

        [Test]
        public void CustomPresetInspector_PreservesMultiObjectEditingWithoutHeaderDecorators()
        {
            Assert.IsTrue(
                Attribute.IsDefined(
                    typeof(CustomTextureCompressorPresetEditor),
                    typeof(CanEditMultipleObjects)
                )
            );

            string[] fieldsWithHeaderDecorators = typeof(CustomTextureCompressorPreset)
                .GetFields()
                .Where(field => Attribute.IsDefined(field, typeof(HeaderAttribute)))
                .Select(field => field.Name)
                .ToArray();
            CollectionAssert.IsEmpty(fieldsWithHeaderDecorators);
        }

        [TestCase(CompressorPreset.HighQuality, "High Quality")]
        [TestCase(CompressorPreset.Quality, "Quality")]
        [TestCase(CompressorPreset.Balanced, "Balanced")]
        [TestCase(CompressorPreset.Aggressive, "Aggressive")]
        [TestCase(CompressorPreset.Maximum, "Maximum")]
        [TestCase(CompressorPreset.Custom, "Custom")]
        public void StandardPreset_KeepsDisplayNameEnglish(
            CompressorPreset preset,
            string expectedName
        )
        {
            LanguagePrefs.Language = "zh-Hans";

            Assert.AreEqual(expectedName, PresetSection.GetPresetDisplayName(preset));
        }

        private static void AssertEnumLocalized<T>()
            where T : struct, Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                string localized = AvatarCompressorLocalization.EnumValue(
                    "TextureCompressor",
                    value
                );
                Assert.IsFalse(
                    localized.StartsWith("<", StringComparison.Ordinal),
                    $"Missing localization for {typeof(T).Name}.{value}."
                );
            }
        }

        private static Dictionary<string, string> ReadMessages(string path)
        {
            var messages = new Dictionary<string, string>(StringComparer.Ordinal);
            string currentId = null;

            foreach (string line in File.ReadAllLines(path))
            {
                if (line.StartsWith("msgid \"", StringComparison.Ordinal))
                {
                    currentId = ReadQuotedValue(line);
                }
                else if (
                    currentId != null
                    && currentId.Length > 0
                    && line.StartsWith("msgstr \"", StringComparison.Ordinal)
                )
                {
                    Assert.IsFalse(
                        messages.ContainsKey(currentId),
                        $"Duplicate key '{currentId}'."
                    );
                    messages.Add(currentId, ReadQuotedValue(line));
                    currentId = null;
                }
            }

            return messages;
        }

        private static string ReadQuotedValue(string line)
        {
            int firstQuote = line.IndexOf('"');
            return line.Substring(firstQuote + 1, line.Length - firstQuote - 2);
        }

        private static string[] GetPlaceholders(string value)
        {
            return Regex
                .Matches(value, @"\{\d+(?::[^}]*)?\}")
                .Cast<Match>()
                .Select(match => match.Value)
                .OrderBy(placeholder => placeholder, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
