using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.ui;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class SearchBoxControlTests
    {
        private SearchBoxControl _searchBox;

        [SetUp]
        public void SetUp()
        {
            _searchBox = new SearchBoxControl();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_Default_IsEmptyAndNotSearching()
        {
            Assert.That(_searchBox.SearchText, Is.Empty);
            Assert.That(_searchBox.IsSearching, Is.False);
            Assert.That(_searchBox.UseFuzzySearch, Is.False);
        }

        [Test]
        public void Constructor_WithSearchText_SetsState()
        {
            var search = new SearchBoxControl("test");

            Assert.That(search.SearchText, Is.EqualTo("test"));
            Assert.That(search.IsSearching, Is.True);
            Assert.That(search.UseFuzzySearch, Is.False);
        }

        [Test]
        public void Constructor_WithFuzzySearch_SetsState()
        {
            var search = new SearchBoxControl("test", useFuzzySearch: true);

            Assert.That(search.SearchText, Is.EqualTo("test"));
            Assert.That(search.UseFuzzySearch, Is.True);
        }

        #endregion

        #region MatchesSearch Tests (Not Searching)

        [Test]
        public void MatchesSearch_NotSearching_ReturnsTrue()
        {
            Assert.That(_searchBox.MatchesSearch("any text"), Is.True);
        }

        [Test]
        public void MatchesSearch_NullText_NotSearching_ReturnsTrue()
        {
            Assert.That(_searchBox.MatchesSearch(null), Is.True);
        }

        [Test]
        public void MatchesSearch_EmptyText_NotSearching_ReturnsTrue()
        {
            Assert.That(_searchBox.MatchesSearch(""), Is.True);
        }

        #endregion

        #region MatchesSearch Tests (Active Search)

        [Test]
        public void MatchesSearch_SubstringMatch_ReturnsTrue()
        {
            var search = new SearchBoxControl("avatar");

            Assert.That(search.MatchesSearch("my_avatar_texture"), Is.True);
        }

        [Test]
        public void MatchesSearch_CaseInsensitive_ReturnsTrue()
        {
            var search = new SearchBoxControl("AVATAR");

            Assert.That(search.MatchesSearch("my_avatar_texture"), Is.True);
        }

        [Test]
        public void MatchesSearch_NoMatch_ReturnsFalse()
        {
            var search = new SearchBoxControl("xyz");

            Assert.That(search.MatchesSearch("avatar_texture"), Is.False);
        }

        [Test]
        public void MatchesSearch_ExactMatch_ReturnsTrue()
        {
            var search = new SearchBoxControl("texture");

            Assert.That(search.MatchesSearch("texture"), Is.True);
        }

        [Test]
        public void MatchesSearch_NullText_WhileSearching_ReturnsFalse()
        {
            var search = new SearchBoxControl("test");

            Assert.That(search.MatchesSearch(null), Is.False);
        }

        [Test]
        public void MatchesSearch_EmptyText_WhileSearching_ReturnsFalse()
        {
            var search = new SearchBoxControl("test");

            Assert.That(search.MatchesSearch(""), Is.False);
        }

        #endregion

        #region MatchesSearchAny Tests (Not Searching)

        [Test]
        public void MatchesSearchAny_NotSearching_ReturnsTrue()
        {
            Assert.That(_searchBox.MatchesSearchAny("text1", "text2"), Is.True);
        }

        #endregion

        #region MatchesSearchAny Tests (Two Args)

        [Test]
        public void MatchesSearchAny_TwoArgs_FirstMatches_ReturnsTrue()
        {
            var search = new SearchBoxControl("avatar");

            Assert.That(search.MatchesSearchAny("my_avatar", "no_match"), Is.True);
        }

        [Test]
        public void MatchesSearchAny_TwoArgs_SecondMatches_ReturnsTrue()
        {
            var search = new SearchBoxControl("avatar");

            Assert.That(search.MatchesSearchAny("no_match", "my_avatar"), Is.True);
        }

        [Test]
        public void MatchesSearchAny_TwoArgs_NoneMatch_ReturnsFalse()
        {
            var search = new SearchBoxControl("xyz");

            Assert.That(search.MatchesSearchAny("avatar", "texture"), Is.False);
        }

        #endregion

        #region MatchesSearchAny Tests (Three Args)

        [Test]
        public void MatchesSearchAny_ThreeArgs_LastMatches_ReturnsTrue()
        {
            var search = new SearchBoxControl("normal");

            Assert.That(search.MatchesSearchAny("diffuse", "specular", "normal_map"), Is.True);
        }

        [Test]
        public void MatchesSearchAny_ThreeArgs_NoneMatch_ReturnsFalse()
        {
            var search = new SearchBoxControl("xyz");

            Assert.That(search.MatchesSearchAny("avatar", "texture", "normal"), Is.False);
        }

        #endregion

        #region Fuzzy Search Tests

        [Test]
        public void MatchesSearch_FuzzyEnabled_MatchesNonContiguous()
        {
            var search = new SearchBoxControl("avt", useFuzzySearch: true);

            Assert.That(search.MatchesSearch("avatar_texture"), Is.True);
        }

        [Test]
        public void MatchesSearch_FuzzyDisabled_DoesNotMatchNonContiguous()
        {
            var search = new SearchBoxControl("avt");

            Assert.That(search.MatchesSearch("avatar_texture"), Is.False);
        }

        [Test]
        public void MatchesSearchAny_FuzzyEnabled_MatchesAny()
        {
            var search = new SearchBoxControl("avt", useFuzzySearch: true);

            Assert.That(search.MatchesSearchAny("no_match", "avatar_texture"), Is.True);
        }

        #endregion

        #region CountMatches Tests

        [Test]
        public void CountMatches_NotSearching_ReturnsItemCount()
        {
            var items = new List<string> { "a", "b", "c" };

            int result = _searchBox.CountMatches(items, _ => false);

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void CountMatches_Searching_CountsMatchingItems()
        {
            var search = new SearchBoxControl("test");
            var items = new List<string> { "test1", "test2", "other" };

            int result = search.CountMatches(items, s => search.MatchesSearch(s));

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void CountMatches_Searching_NoMatches_ReturnsZero()
        {
            var search = new SearchBoxControl("xyz");
            var items = new List<string> { "alpha", "beta", "gamma" };

            int result = search.CountMatches(items, s => search.MatchesSearch(s));

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void CountMatches_CachesResult_SkipsPredicateOnSecondCall()
        {
            var search = new SearchBoxControl("test");
            var items = new List<string> { "test1", "other" };
            int callCount = 0;
            System.Func<string, bool> predicate = s =>
            {
                callCount++;
                return s.Contains("test");
            };

            search.CountMatches(items, predicate);
            int firstCallCount = callCount;

            search.CountMatches(items, predicate);

            Assert.That(callCount, Is.EqualTo(firstCallCount));
        }

        [Test]
        public void CountMatches_InvalidateCountCache_ForcesRecount()
        {
            var search = new SearchBoxControl("test");
            var items = new List<string> { "test1", "other" };
            int callCount = 0;
            System.Func<string, bool> predicate = s =>
            {
                callCount++;
                return s.Contains("test");
            };

            search.CountMatches(items, predicate);
            int firstCallCount = callCount;

            search.InvalidateCountCache();
            search.CountMatches(items, predicate);

            Assert.That(callCount, Is.GreaterThan(firstCallCount));
        }

        [Test]
        public void CountMatches_CacheInvalidatedByCountChange()
        {
            var search = new SearchBoxControl("test");
            var items = new List<string> { "test1", "other" };
            int callCount = 0;
            System.Func<string, bool> predicate = s =>
            {
                callCount++;
                return s.Contains("test");
            };

            search.CountMatches(items, predicate);
            int firstCallCount = callCount;

            items.Add("test2");
            search.CountMatches(items, predicate);

            Assert.That(callCount, Is.GreaterThan(firstCallCount));
        }

        [Test]
        public void CountMatches_WorksWithArray()
        {
            var search = new SearchBoxControl("test");
            var items = new[] { "test1", "test2", "other" };

            int result = search.CountMatches(items, s => search.MatchesSearch(s));

            Assert.That(result, Is.EqualTo(2));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsSearchText()
        {
            var search = new SearchBoxControl("test", useFuzzySearch: true);

            search.Clear();

            Assert.That(search.SearchText, Is.Empty);
        }

        [Test]
        public void Clear_ResetsFuzzySearch()
        {
            var search = new SearchBoxControl("test", useFuzzySearch: true);

            search.Clear();

            Assert.That(search.UseFuzzySearch, Is.False);
        }

        [Test]
        public void Clear_IsSearchingBecomesFalse()
        {
            var search = new SearchBoxControl("test");

            search.Clear();

            Assert.That(search.IsSearching, Is.False);
        }

        #endregion
    }
}
