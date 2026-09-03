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

        [TestCase("5239a248c3cecc8438149fd847e07082", "High Quality+")]
        [TestCase("1de212fc9c0e5db45889f9b52723b1d9", "Quality+")]
        [TestCase("dc3ad49e6d7ef4f429bb5967cf64b644", "Balanced+")]
        [TestCase("738122bf69ebfef46a329e3c46e09e60", "Aggressive+")]
        [TestCase("7881623902e2305439c564a23f41f40c", "Maximum+")]
        public void BuiltInPreset_KeepsDisplayNameEnglish(string presetGuid, string expectedName)
        {
            string presetPath = AssetDatabase.GUIDToAssetPath(presetGuid);
            var preset = AssetDatabase.LoadAssetAtPath<CustomTextureCompressorPreset>(presetPath);
            Assert.IsNotNull(preset);

            LanguagePrefs.Language = "zh-Hans";

            Assert.AreEqual(expectedName, BuiltInPresetLocalization.GetDisplayName(preset));
            Assert.AreEqual("内置/" + expectedName, BuiltInPresetLocalization.GetMenuPath(preset));
        }

        [Test]
        public void BuiltInPreset_LocalizesDescriptionWithoutChangingSerializedText()
        {
            const string presetGuid = "5239a248c3cecc8438149fd847e07082";
            string presetPath = AssetDatabase.GUIDToAssetPath(presetGuid);
            var preset = AssetDatabase.LoadAssetAtPath<CustomTextureCompressorPreset>(presetPath);
            Assert.IsNotNull(preset);

            string serializedDescription = preset.Description;
            LanguagePrefs.Language = "zh-Hans";

            Assert.AreNotEqual(
                serializedDescription,
                BuiltInPresetLocalization.GetDescription(preset)
            );
            Assert.AreEqual(serializedDescription, preset.Description);
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
                string localized = AvatarCompressorLocalization.EnumValue(value);
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
