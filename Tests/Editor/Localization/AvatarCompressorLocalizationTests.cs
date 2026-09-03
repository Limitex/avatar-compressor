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
            "Packages/dev.limitex.avatar-compressor/Editor/Localization/";

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
        public void LocalizationAssets_AllFourLocalesLoad()
        {
            var assets = AvatarCompressorLocalization.LoadLocalizationAssets();

            Assert.AreEqual(4, assets.Count);
            CollectionAssert.AreEquivalent(
                AvatarCompressorLocalization.SupportedLanguages.Select(language =>
                    language.ToLowerInvariant()
                ),
                assets.Select(asset => asset.localeIsoCode.ToLowerInvariant())
            );
        }

        [Test]
        public void LocalizationFiles_HaveMatchingKeysAndFormatPlaceholders()
        {
            var english = ReadMessages("en-US");

            foreach (string language in AvatarCompressorLocalization.SupportedLanguages)
            {
                var localized = ReadMessages(language);
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

        [TestCase("en-US", "General")]
        [TestCase("zh-Hans", "常规")]
        [TestCase("ja-JP", "一般")]
        [TestCase("ko-KR", "일반")]
        public void LanguagePrefs_SelectsRequestedLocalization(string language, string expected)
        {
            LanguagePrefs.Language = language;

            Assert.AreEqual(expected, AvatarCompressorLocalization.Tr("preferences.general"));
        }

        [Test]
        public void EnumMappings_CoverEveryDisplayedValue()
        {
            LanguagePrefs.Language = "en-US";

            AssertEnumLocalized<CompressorPreset>();
            AssertEnumLocalized<AnalysisStrategyType>();
            AssertEnumLocalized<CompressionPlatform>();
            AssertEnumLocalized<FrozenTextureFormat>();
            AssertEnumLocalized<AnalysisBackendPreference>();
            AssertEnumLocalized<ResizeBackendPreference>();
            AssertEnumLocalized<TexturePropertyCategory>();
            AssertEnumLocalized<SkipReason>();
        }

        [Test]
        public void BuiltInPreset_LocalizesDisplayWithoutChangingSerializedText()
        {
            const string presetGuid = "5239a248c3cecc8438149fd847e07082";
            string presetPath = AssetDatabase.GUIDToAssetPath(presetGuid);
            var preset = AssetDatabase.LoadAssetAtPath<CustomTextureCompressorPreset>(presetPath);
            Assert.IsNotNull(preset);

            string serializedDescription = preset.Description;
            LanguagePrefs.Language = "zh-Hans";

            Assert.AreEqual("高质量+", BuiltInPresetLocalization.GetDisplayName(preset));
            Assert.AreEqual(serializedDescription, preset.Description);
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

        private static Dictionary<string, string> ReadMessages(string language)
        {
            string path = LocalizationFolder + language + ".po";
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
