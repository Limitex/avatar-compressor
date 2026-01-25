using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Simple LRU (Least Recently Used) cache with configurable capacity.
    /// Evicts oldest entries when capacity is exceeded.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public class LruCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, (TValue value, double accessTime)> _cache = new();
        private readonly int _maxCapacity;
        private readonly Func<double> _timeProvider;

        /// <summary>
        /// Creates a new LRU cache with the specified capacity.
        /// </summary>
        /// <param name="maxCapacity">Maximum number of entries before eviction occurs.</param>
        /// <param name="timeProvider">
        /// Optional time provider for testability.
        /// Defaults to EditorApplication.timeSinceStartup.
        /// </param>
        public LruCache(int maxCapacity, Func<double> timeProvider = null)
        {
            _maxCapacity = maxCapacity;
            _timeProvider = timeProvider ?? (() => EditorApplication.timeSinceStartup);
        }

        /// <summary>
        /// Gets the current number of entries in the cache.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Attempts to get a value from the cache.
        /// Updates the access time if found (LRU touch).
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The value if found, default otherwise.</param>
        /// <returns>True if the key was found, false otherwise.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                // Update access time (LRU touch)
                _cache[key] = (entry.value, _timeProvider());
                value = entry.value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Sets a value in the cache. Evicts the oldest entry if at capacity.
        /// </summary>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value to store.</param>
        public void Set(TKey key, TValue value)
        {
            // Evict oldest entry if cache is full and this is a new key
            if (_cache.Count >= _maxCapacity && !_cache.ContainsKey(key))
            {
                EvictOldest();
            }
            _cache[key] = (value, _timeProvider());
        }

        private void EvictOldest()
        {
            if (_cache.Count == 0)
                return;

            var oldest = _cache.First();
            foreach (var kvp in _cache)
            {
                if (kvp.Value.accessTime < oldest.Value.accessTime)
                {
                    oldest = kvp;
                }
            }
            _cache.Remove(oldest.Key);
        }
    }
}
