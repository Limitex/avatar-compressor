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

        #region Initial State Tests

        [Test]
        public void NewInstance_SearchTextIsEmpty()
        {
            Assert.That(_searchBox.SearchText, Is.Empty);
        }

        [Test]
        public void NewInstance_IsSearchingIsFalse()
        {
            Assert.That(_searchBox.IsSearching, Is.False);
        }

        [Test]
        public void NewInstance_UseFuzzySearchIsFalse()
        {
            Assert.That(_searchBox.UseFuzzySearch, Is.False);
        }

        #endregion

        #region MatchesSearch Tests

        [Test]
        public void MatchesSearch_EmptySearchText_ReturnsTrue()
        {
            // When not searching, everything matches
            Assert.That(_searchBox.MatchesSearch("any text"), Is.True);
        }

        [Test]
        public void MatchesSearch_NullText_ReturnsFalse()
        {
            Assert.That(_searchBox.MatchesSearch(null), Is.False);
        }

        [Test]
        public void MatchesSearch_EmptyText_ReturnsFalse()
        {
            Assert.That(_searchBox.MatchesSearch(""), Is.False);
        }

        #endregion

        #region MatchesSearchAny Tests

        [Test]
        public void MatchesSearchAny_NotSearching_ReturnsTrue()
        {
            Assert.That(_searchBox.MatchesSearchAny("text1", "text2"), Is.True);
        }

        [Test]
        public void MatchesSearchAny_EmptyArray_ReturnsTrue_WhenNotSearching()
        {
            Assert.That(_searchBox.MatchesSearchAny(), Is.True);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsSearchText()
        {
            _searchBox.Clear();

            Assert.That(_searchBox.SearchText, Is.Empty);
        }

        [Test]
        public void Clear_ResetsFuzzySearch()
        {
            _searchBox.Clear();

            Assert.That(_searchBox.UseFuzzySearch, Is.False);
        }

        [Test]
        public void Clear_IsSearchingBecomesFalse()
        {
            _searchBox.Clear();

            Assert.That(_searchBox.IsSearching, Is.False);
        }

        #endregion

        #region InvalidateCache Tests

        [Test]
        public void InvalidateCache_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _searchBox.InvalidateCache());
        }

        [Test]
        public void InvalidateCache_CanBeCalledMultipleTimes()
        {
            Assert.DoesNotThrow(() =>
            {
                _searchBox.InvalidateCache();
                _searchBox.InvalidateCache();
                _searchBox.InvalidateCache();
            });
        }

        #endregion

        #region IsMatch Tests

        [Test]
        public void IsMatch_NotSearching_ReturnsTrue()
        {
            bool result = _searchBox.IsMatch(0, 10, i => false);

            Assert.That(result, Is.True);
        }

        #endregion

        #region CountMatches Tests

        [Test]
        public void CountMatches_NotSearching_ReturnsItemCount()
        {
            int result = _searchBox.CountMatches(10, i => false);

            Assert.That(result, Is.EqualTo(10));
        }

        #endregion

        #region OnSearchChanged Event Tests

        [Test]
        public void OnSearchChanged_CanSubscribe()
        {
            bool eventFired = false;
            _searchBox.OnSearchChanged += () => eventFired = true;

            // Event should be subscribed without throwing
            Assert.That(eventFired, Is.False);
        }

        #endregion
    }
}
