using System.Collections.Generic;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Caches GUID-to-asset-path lookups to avoid repeated <see cref="AssetDatabase.GUIDToAssetPath"/> calls.
    /// Automatically clears on Unity project changes and can also be cleared manually when backing data is replaced.
    /// </summary>
    [InitializeOnLoad]
    public class GuidPathCache
    {
        private static int s_projectVersion;

        private readonly Dictionary<string, string> _cache = new();
        private int _cacheVersion = s_projectVersion;

        static GuidPathCache()
        {
            EditorApplication.projectChanged += OnProjectChanged;
        }

        /// <summary>
        /// Returns the asset path for the given GUID, using the cache to avoid repeated lookups.
        /// </summary>
        public string GetPath(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return "";

            SyncWithProjectState();

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
            _cacheVersion = s_projectVersion;
        }

        private void SyncWithProjectState()
        {
            if (_cacheVersion == s_projectVersion)
                return;

            _cache.Clear();
            _cacheVersion = s_projectVersion;
        }

        private static void OnProjectChanged()
        {
            s_projectVersion++;
        }
    }
}
