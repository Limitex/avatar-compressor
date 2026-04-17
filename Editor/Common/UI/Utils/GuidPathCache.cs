using System.Collections.Generic;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Shared cache of GUID-to-asset-path lookups. Auto-clears on Unity project changes.
    /// </summary>
    [InitializeOnLoad]
    public static class GuidPathCache
    {
        private static readonly Dictionary<string, string> s_cache = new();

        static GuidPathCache()
        {
            EditorApplication.projectChanged += Clear;
        }

        /// <summary>
        /// Returns the asset path for the given GUID, using the cache to avoid repeated lookups.
        /// </summary>
        public static string GetPath(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return "";

            if (!s_cache.TryGetValue(guid, out var path))
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                s_cache[guid] = path;
            }
            return path;
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public static void Clear()
        {
            s_cache.Clear();
        }
    }
}
