using System.Collections.Generic;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Caches GUID-to-asset-path lookups to avoid repeated <see cref="AssetDatabase.GUIDToAssetPath"/> calls.
    /// Call <see cref="Clear"/> when the backing data is replaced or assets may have been renamed/moved.
    /// </summary>
    public class GuidPathCache
    {
        private readonly Dictionary<string, string> _cache = new();

        /// <summary>
        /// Returns the asset path for the given GUID, using the cache to avoid repeated lookups.
        /// </summary>
        public string GetPath(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return "";

            if (!_cache.TryGetValue(guid, out var path))
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                _cache[guid] = path;
            }
            return path;
        }

        /// <summary>
        /// Clears the cache. Useful when the backing data is fully replaced (e.g. preview regeneration).
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
    }
}
