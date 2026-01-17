using NUnit.Framework;
using dev.limitex.avatar.compressor.editor;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class FuzzyMatcherTests
    {
        #region Basic Matching Tests

        [Test]
        public void Match_ExactMatch_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("hello", "hello"));
        }

        [Test]
        public void Match_SubstringMatch_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("hello world", "world"));
        }

        [Test]
        public void Match_CaseInsensitive_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("Hello World", "hello"));
            Assert.IsTrue(FuzzyMatcher.Match("HELLO", "hello"));
            Assert.IsTrue(FuzzyMatcher.Match("hello", "HELLO"));
        }

        [Test]
        public void Match_PatternAtStart_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("texture_diffuse", "texture"));
        }

        [Test]
        public void Match_PatternAtEnd_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("avatar_texture", "texture"));
        }

        [Test]
        public void Match_PatternInMiddle_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("my_texture_file", "texture"));
        }

        [Test]
        public void Match_NoMatch_ReturnsFalse()
        {
            Assert.IsFalse(FuzzyMatcher.Match("hello", "xyz", maxErrors: 0));
        }

        #endregion

        #region Empty and Null Input Tests

        [Test]
        public void Match_EmptyPattern_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("hello", ""));
        }

        [Test]
        public void Match_NullPattern_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("hello", null));
        }

        [Test]
        public void Match_EmptyText_ReturnsFalse()
        {
            Assert.IsFalse(FuzzyMatcher.Match("", "hello"));
        }

        [Test]
        public void Match_NullText_ReturnsFalse()
        {
            Assert.IsFalse(FuzzyMatcher.Match(null, "hello"));
        }

        [Test]
        public void Match_BothEmpty_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("", ""));
        }

        [Test]
        public void Match_BothNull_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match(null, null));
        }

        #endregion

        #region Fuzzy Matching - Substitution Tests

        [Test]
        public void Match_OneSubstitution_WithOneErrorAllowed_ReturnsTrue()
        {
            // "hallo" vs "hello" - one substitution (e -> a)
            Assert.IsTrue(FuzzyMatcher.Match("hallo", "hello", maxErrors: 1));
        }

        [Test]
        public void Match_OneSubstitution_WithZeroErrorsAllowed_ReturnsFalse()
        {
            Assert.IsFalse(FuzzyMatcher.Match("hallo", "hello", maxErrors: 0));
        }

        [Test]
        public void Match_TwoSubstitutions_WithOneErrorAllowed_ReturnsFalse()
        {
            // "hxllx" vs "hello" - two substitutions
            Assert.IsFalse(FuzzyMatcher.Match("hxllx", "hello", maxErrors: 1));
        }

        [Test]
        public void Match_TwoSubstitutions_WithTwoErrorsAllowed_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("hxllx", "hello", maxErrors: 2));
        }

        #endregion

        #region Fuzzy Matching - Insertion Tests

        [Test]
        public void Match_OneInsertion_WithOneErrorAllowed_ReturnsTrue()
        {
            // "helo" in "hello" - one insertion needed (l)
            Assert.IsTrue(FuzzyMatcher.Match("hello", "helo", maxErrors: 1));
        }

        [Test]
        public void Match_PatternWithExtraChar_WithOneErrorAllowed_ReturnsTrue()
        {
            // "helllo" vs "hello" - pattern has extra l
            Assert.IsTrue(FuzzyMatcher.Match("hello", "helllo", maxErrors: 1));
        }

        #endregion

        #region Fuzzy Matching - Deletion Tests

        [Test]
        public void Match_OneDeletion_WithOneErrorAllowed_ReturnsTrue()
        {
            // "helllo" contains "hello" with one extra l to skip
            Assert.IsTrue(FuzzyMatcher.Match("helllo", "hello", maxErrors: 1));
        }

        #endregion

        #region maxErrors Boundary Tests

        [Test]
        public void Match_NegativeMaxErrors_TreatedAsZero()
        {
            // With negative maxErrors (treated as 0), only exact match works
            Assert.IsTrue(FuzzyMatcher.Match("hello", "hello", maxErrors: -1));
            Assert.IsFalse(FuzzyMatcher.Match("hallo", "hello", maxErrors: -1));
        }

        [Test]
        public void Match_ZeroMaxErrors_RequiresExactSubstring()
        {
            Assert.IsTrue(FuzzyMatcher.Match("hello world", "hello", maxErrors: 0));
            Assert.IsFalse(FuzzyMatcher.Match("hallo world", "hello", maxErrors: 0));
        }

        [Test]
        public void Match_LargeMaxErrors_AllowsManyDifferences()
        {
            // "abcde" vs "12345" - completely different, needs 5 errors
            Assert.IsTrue(FuzzyMatcher.Match("abcde", "12345", maxErrors: 5));
        }

        #endregion

        #region Pattern Length Tests

        [Test]
        public void Match_PatternLongerThanText_ReturnsFalse()
        {
            Assert.IsFalse(FuzzyMatcher.Match("hi", "hello", maxErrors: 0));
        }

        [Test]
        public void Match_PatternLongerThanText_WithErrors_CanMatch()
        {
            // "hi" vs "hix" - pattern is longer but within error tolerance
            Assert.IsTrue(FuzzyMatcher.Match("hi", "hix", maxErrors: 1));
        }

        [Test]
        public void Match_PatternMuchLongerThanText_ReturnsFalse()
        {
            // Pattern too long even with maxErrors
            Assert.IsFalse(FuzzyMatcher.Match("ab", "abcdefgh", maxErrors: 2));
        }

        [Test]
        public void Match_SingleCharPattern_MatchesAnywhere()
        {
            Assert.IsTrue(FuzzyMatcher.Match("texture", "t", maxErrors: 0));
            Assert.IsTrue(FuzzyMatcher.Match("texture", "e", maxErrors: 0));
            Assert.IsTrue(FuzzyMatcher.Match("texture", "x", maxErrors: 0));
        }

        [Test]
        public void Match_SingleCharText_SingleCharPattern_Matches()
        {
            Assert.IsTrue(FuzzyMatcher.Match("a", "a", maxErrors: 0));
        }

        [Test]
        public void Match_SingleCharText_SingleCharPattern_NoMatch()
        {
            Assert.IsFalse(FuzzyMatcher.Match("a", "b", maxErrors: 0));
        }

        #endregion

        #region Long Pattern Tests (32+ characters - fuzzy matching not supported)

        [Test]
        public void Match_LongPatternExactMatch_ReturnsTrue()
        {
            // Pattern longer than 31 chars - exact substring match still works via fast path
            string longPattern = "abcdefghijklmnopqrstuvwxyz123456";
            string text = "prefix_" + longPattern + "_suffix";
            Assert.IsTrue(FuzzyMatcher.Match(text, longPattern, maxErrors: 0));
        }

        [Test]
        public void Match_LongPatternExactMatch_CaseInsensitive_ReturnsTrue()
        {
            // Long pattern exact match should still be case-insensitive
            string longPattern = "abcdefghijklmnopqrstuvwxyz123456";
            string text = "PREFIX_ABCDEFGHIJKLMNOPQRSTUVWXYZ123456_SUFFIX";
            Assert.IsTrue(FuzzyMatcher.Match(text, longPattern, maxErrors: 0));
        }

        [Test]
        public void Match_LongPatternFuzzy_NotSupported_ReturnsFalse()
        {
            // Fuzzy matching is NOT supported for patterns > 31 chars
            // This is by design - Bitap algorithm has a 31-char limit
            string longPattern = "abcdefghijklmnopqrstuvwxyz123456";
            // Text has one char difference - but fuzzy match not supported
            string textWithError = "prefix_abcdefghijklmnopqrstuvwxyz1X3456_suffix";
            Assert.IsFalse(FuzzyMatcher.Match(textWithError, longPattern, maxErrors: 1));
        }

        [Test]
        public void Match_LongPatternNoMatch_ReturnsFalse()
        {
            string longPattern = "abcdefghijklmnopqrstuvwxyz123456";
            string text = "completely_different_text_here";
            Assert.IsFalse(FuzzyMatcher.Match(text, longPattern, maxErrors: 1));
        }

        [Test]
        public void Match_PatternAt31Chars_FuzzyStillWorks()
        {
            // Pattern exactly at 31 chars should still support fuzzy matching
            string pattern31 = "abcdefghijklmnopqrstuvwxyz12345"; // 31 chars
            string textWithError = "prefix_abcdefghijklmnopqrstuvwxyz1X345_suffix";
            Assert.IsTrue(FuzzyMatcher.Match(textWithError, pattern31, maxErrors: 1));
        }

        [Test]
        public void Match_PatternAt32Chars_FuzzyNotSupported()
        {
            // Pattern at 32 chars exceeds limit - fuzzy not supported
            string pattern32 = "abcdefghijklmnopqrstuvwxyz123456"; // 32 chars
            string textWithError = "prefix_abcdefghijklmnopqrstuvwxyz1X3456_suffix";
            Assert.IsFalse(FuzzyMatcher.Match(textWithError, pattern32, maxErrors: 1));
        }

        #endregion

        #region Unicode Support Tests

        [Test]
        public void Match_JapaneseExactMatch_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("テクスチャ", "テクスチャ"));
        }

        [Test]
        public void Match_JapaneseSubstring_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("アバターテクスチャファイル", "テクスチャ"));
        }

        [Test]
        public void Match_JapaneseFuzzy_WithOneError_ReturnsTrue()
        {
            // "テクスチヤ" vs "テクスチャ" - one char difference
            Assert.IsTrue(FuzzyMatcher.Match("テクスチヤ", "テクスチャ", maxErrors: 1));
        }

        [Test]
        public void Match_MixedJapaneseEnglish_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("avatar_テクスチャ_diffuse", "テクスチャ"));
        }

        [Test]
        public void Match_ChineseCharacters_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("纹理文件", "纹理"));
        }

        [Test]
        public void Match_KoreanCharacters_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("텍스처파일", "텍스처"));
        }

        [Test]
        public void Match_EmojiCharacters_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("avatar_🎨_texture", "🎨"));
            Assert.IsTrue(FuzzyMatcher.Match("✨effect_map✨", "effect"));
        }

        #endregion

        #region Real-World Texture Name Tests

        [Test]
        public void Match_TextureFileName_TypoInName_ReturnsTrue()
        {
            // User types "difuse" instead of "diffuse"
            Assert.IsTrue(FuzzyMatcher.Match("avatar_diffuse_map.png", "difuse", maxErrors: 1));
        }

        [Test]
        public void Match_TextureFileName_PartialName_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("character_body_normal_map", "normal"));
        }

        [Test]
        public void Match_AssetPath_SearchByFolder_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("Assets/Textures/Avatar/body_diffuse.png", "Avatar"));
        }

        [Test]
        public void Match_AssetPath_SearchByExtension_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("Assets/Textures/body.png", ".png"));
        }

        [Test]
        public void Match_TextureType_CaseInsensitive_ReturnsTrue()
        {
            Assert.IsTrue(FuzzyMatcher.Match("Normal", "normal"));
            Assert.IsTrue(FuzzyMatcher.Match("EMISSION", "emission"));
        }

        #endregion

        #region Default maxErrors Tests

        [Test]
        public void Match_DefaultMaxErrors_IsOne()
        {
            // Default maxErrors should be 1, allowing one error
            Assert.IsTrue(FuzzyMatcher.Match("hallo", "hello")); // One substitution
            Assert.IsFalse(FuzzyMatcher.Match("hxllx", "hello")); // Two substitutions - should fail
        }

        #endregion
    }
}
