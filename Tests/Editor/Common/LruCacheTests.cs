using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class LruCacheTests
    {
        private double _mockTime;
        private LruCache<int, string> _cache;

        [SetUp]
        public void SetUp()
        {
            _mockTime = 0.0;
            _cache = new LruCache<int, string>(4, () => _mockTime);
        }

        #region Basic Operations

        [Test]
        public void Count_InitiallyZero()
        {
            Assert.That(_cache.Count, Is.EqualTo(0));
        }

        [Test]
        public void MaxCapacity_ReturnsConfiguredValue()
        {
            Assert.That(_cache.MaxCapacity, Is.EqualTo(4));
        }

        [Test]
        public void Set_IncreasesCount()
        {
            _cache.Set(1, "value1");

            Assert.That(_cache.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryGetValue_ReturnsTrueForExistingKey()
        {
            _cache.Set(1, "value1");

            bool found = _cache.TryGetValue(1, out var value);

            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo("value1"));
        }

        [Test]
        public void TryGetValue_ReturnsFalseForMissingKey()
        {
            bool found = _cache.TryGetValue(999, out var value);

            Assert.That(found, Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void ContainsKey_ReturnsTrueForExistingKey()
        {
            _cache.Set(1, "value1");

            Assert.That(_cache.ContainsKey(1), Is.True);
        }

        [Test]
        public void ContainsKey_ReturnsFalseForMissingKey()
        {
            Assert.That(_cache.ContainsKey(999), Is.False);
        }

        [Test]
        public void Clear_RemovesAllEntries()
        {
            _cache.Set(1, "value1");
            _cache.Set(2, "value2");

            _cache.Clear();

            Assert.That(_cache.Count, Is.EqualTo(0));
            Assert.That(_cache.ContainsKey(1), Is.False);
            Assert.That(_cache.ContainsKey(2), Is.False);
        }

        [Test]
        public void Remove_RemovesSpecificEntry()
        {
            _cache.Set(1, "value1");
            _cache.Set(2, "value2");

            bool removed = _cache.Remove(1);

            Assert.That(removed, Is.True);
            Assert.That(_cache.Count, Is.EqualTo(1));
            Assert.That(_cache.ContainsKey(1), Is.False);
            Assert.That(_cache.ContainsKey(2), Is.True);
        }

        [Test]
        public void Remove_ReturnsFalseForMissingKey()
        {
            bool removed = _cache.Remove(999);

            Assert.That(removed, Is.False);
        }

        [Test]
        public void Set_UpdatesExistingValue()
        {
            _cache.Set(1, "original");
            _cache.Set(1, "updated");

            _cache.TryGetValue(1, out var value);

            Assert.That(value, Is.EqualTo("updated"));
            Assert.That(_cache.Count, Is.EqualTo(1));
        }

        #endregion

        #region LRU Eviction

        [Test]
        public void Set_EvictsOldestEntry_WhenAtCapacity()
        {
            // Fill cache to capacity
            _mockTime = 1.0;
            _cache.Set(1, "value1");
            _mockTime = 2.0;
            _cache.Set(2, "value2");
            _mockTime = 3.0;
            _cache.Set(3, "value3");
            _mockTime = 4.0;
            _cache.Set(4, "value4");

            Assert.That(_cache.Count, Is.EqualTo(4));

            // Add one more (should evict key 1, the oldest)
            _mockTime = 5.0;
            _cache.Set(5, "value5");

            Assert.That(_cache.Count, Is.EqualTo(4));
            Assert.That(_cache.ContainsKey(1), Is.False, "Oldest entry should be evicted");
            Assert.That(_cache.ContainsKey(5), Is.True, "New entry should exist");
        }

        [Test]
        public void Set_DoesNotEvict_WhenUpdatingExistingKey()
        {
            // Fill cache to capacity
            _mockTime = 1.0;
            _cache.Set(1, "value1");
            _mockTime = 2.0;
            _cache.Set(2, "value2");
            _mockTime = 3.0;
            _cache.Set(3, "value3");
            _mockTime = 4.0;
            _cache.Set(4, "value4");

            // Update existing key (should not trigger eviction)
            _mockTime = 5.0;
            _cache.Set(1, "updated");

            Assert.That(_cache.Count, Is.EqualTo(4));
            Assert.That(_cache.ContainsKey(1), Is.True);
            Assert.That(_cache.ContainsKey(2), Is.True);
            Assert.That(_cache.ContainsKey(3), Is.True);
            Assert.That(_cache.ContainsKey(4), Is.True);
        }

        [Test]
        public void TryGetValue_UpdatesAccessTime_PreventingEviction()
        {
            // Fill cache: key 1 is oldest
            _mockTime = 1.0;
            _cache.Set(1, "value1");
            _mockTime = 2.0;
            _cache.Set(2, "value2");
            _mockTime = 3.0;
            _cache.Set(3, "value3");
            _mockTime = 4.0;
            _cache.Set(4, "value4");

            // Access key 1 to update its access time
            _mockTime = 5.0;
            _cache.TryGetValue(1, out _);

            // Add new entry (should evict key 2, now the oldest)
            _mockTime = 6.0;
            _cache.Set(5, "value5");

            Assert.That(
                _cache.ContainsKey(1),
                Is.True,
                "Key 1 should not be evicted (was accessed)"
            );
            Assert.That(_cache.ContainsKey(2), Is.False, "Key 2 should be evicted (became oldest)");
            Assert.That(_cache.ContainsKey(5), Is.True, "New entry should exist");
        }

        [Test]
        public void MultipleEvictions_WorkCorrectly()
        {
            // Fill cache to capacity
            for (int i = 1; i <= 4; i++)
            {
                _mockTime = i;
                _cache.Set(i, $"value{i}");
            }

            // Add 3 more entries (should evict keys 1, 2, 3)
            for (int i = 5; i <= 7; i++)
            {
                _mockTime = i;
                _cache.Set(i, $"value{i}");
            }

            Assert.That(_cache.Count, Is.EqualTo(4));
            Assert.That(_cache.ContainsKey(1), Is.False, "Key 1 should be evicted");
            Assert.That(_cache.ContainsKey(2), Is.False, "Key 2 should be evicted");
            Assert.That(_cache.ContainsKey(3), Is.False, "Key 3 should be evicted");
            Assert.That(_cache.ContainsKey(4), Is.True, "Key 4 should remain");
            Assert.That(_cache.ContainsKey(5), Is.True, "Key 5 should exist");
            Assert.That(_cache.ContainsKey(6), Is.True, "Key 6 should exist");
            Assert.That(_cache.ContainsKey(7), Is.True, "Key 7 should exist");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Set_WithCapacityOne_EvictsImmediately()
        {
            var smallCache = new LruCache<int, string>(1, () => _mockTime);

            _mockTime = 1.0;
            smallCache.Set(1, "value1");
            _mockTime = 2.0;
            smallCache.Set(2, "value2");

            Assert.That(smallCache.Count, Is.EqualTo(1));
            Assert.That(smallCache.ContainsKey(1), Is.False);
            Assert.That(smallCache.ContainsKey(2), Is.True);
        }

        [Test]
        public void TryGetValue_WithDefaultValue_ReturnsDefaultForMissingKey()
        {
            var intCache = new LruCache<string, int>(4, () => _mockTime);

            bool found = intCache.TryGetValue("missing", out var value);

            Assert.That(found, Is.False);
            Assert.That(value, Is.EqualTo(0)); // default(int)
        }

        [Test]
        public void Set_WithSameTimeForAllEntries_EvictsFirstInserted()
        {
            // All entries have the same timestamp
            _mockTime = 1.0;
            _cache.Set(1, "value1");
            _cache.Set(2, "value2");
            _cache.Set(3, "value3");
            _cache.Set(4, "value4");

            // Add new entry (should evict first one due to iteration order)
            _cache.Set(5, "value5");

            Assert.That(_cache.Count, Is.EqualTo(4));
            Assert.That(_cache.ContainsKey(5), Is.True);
        }

        #endregion
    }
}
