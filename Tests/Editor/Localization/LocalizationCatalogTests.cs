using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using static dev.limitex.avatar.compressor.tests.LocalizationTestCatalog;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal sealed class LocalizationCatalogTests
    {
        [TestCase("{{0}}", new string[] { })]
        [TestCase("{{{0}}}", new[] { "{0}" })]
        [TestCase("{0} / {{1}} / {0}", new[] { "{0}", "{0}" })]
        [TestCase("{1,-8:P0} {0,4}", new[] { "{0}", "{1:P0}" })]
        [TestCase("{{{{0}}}} {2:N2}", new[] { "{2:N2}" })]
        public void FormatPlaceholders_RecognizesOnlySubstitutedArguments(
            string text,
            string[] expected
        )
        {
            CollectionAssert.AreEqual(expected, GetPlaceholders(text));
        }

        [Test]
        public void Translations_HaveNoFuzzyFlags()
        {
            // Unity may read fuzzy entries that gettext treats as untranslated.
            foreach (string path in PoPaths())
            {
                Assert.IsFalse(
                    Regex.IsMatch(
                        File.ReadAllText(PhysicalPath(path)),
                        @"^#,.*\bfuzzy\b",
                        RegexOptions.Multiline
                    ),
                    $"'{path}' contains a fuzzy flag."
                );
            }
        }

        [Test]
        public void EnglishSource_HasNoEmptyStrings()
        {
            foreach (var entry in ReadMessages(LocalizationFolder + "/en-US.po"))
            {
                Assert.IsNotEmpty(entry.Value, $"Source key '{entry.Key}' is empty.");
            }
        }

        [Test]
        public void Translations_OnlyContainSourceKeysAndKeepFormatPlaceholders()
        {
            var english = ReadMessages(LocalizationFolder + "/en-US.po");

            foreach (string path in PoPaths().Where(path => !path.EndsWith("/en-US.po")))
            {
                string language = Path.GetFileNameWithoutExtension(path);
                var localized = ReadMessages(path);

                CollectionAssert.IsSubsetOf(
                    localized.Keys,
                    english.Keys,
                    $"Locale {language} has keys that are not in en-US.po."
                );

                var translated = localized.Where(entry => entry.Value.Length > 0).ToArray();
                foreach (var entry in translated)
                {
                    CollectionAssert.AreEquivalent(
                        GetPlaceholders(english[entry.Key]),
                        GetPlaceholders(entry.Value),
                        $"Locale {language}, key '{entry.Key}' has different format placeholders."
                    );
                }

                TestContext.WriteLine(
                    $"{language}: {english.Count - translated.Length} untranslated of {english.Count}"
                );
            }
        }

        [Test]
        public void Translations_AreValidFormatStrings()
        {
            object[] args = Enumerable.Range(0, 10).Cast<object>().ToArray();

            foreach (string path in PoPaths())
            {
                string language = Path.GetFileNameWithoutExtension(path);
                var braced = ReadMessages(path)
                    .Where(entry => entry.Value.IndexOfAny(new[] { '{', '}' }) >= 0);
                foreach (var entry in braced)
                {
                    Assert.DoesNotThrow(
                        () => string.Format(CultureInfo.InvariantCulture, entry.Value, args),
                        $"Locale {language}, key '{entry.Key}' is not a valid format string."
                    );
                }
            }
        }

        [Test]
        public void EnglishSource_UsesMsgstrForCrowdinSourceText()
        {
            string english = File.ReadAllText(PhysicalPath(LocalizationFolder + "/en-US.po"));
            StringAssert.Contains("\"X-Crowdin-SourceKey: msgstr\\n\"", english);
        }
    }
}
