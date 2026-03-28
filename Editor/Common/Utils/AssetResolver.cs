using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Utility for resolving asset paths through NDMF's ObjectRegistry.
    /// When other NDMF plugins replace assets and register them via ObjectRegistry,
    /// the replacement objects lose their original asset path. This utility resolves
    /// the original asset path by following the ObjectRegistry replacement chain.
    /// </summary>
    public static class AssetResolver
    {
        /// <summary>
        /// Resolves the original asset path for an object by first checking ObjectRegistry
        /// replacement chains, then falling back to the object's own asset path.
        /// This ensures that textures replaced by earlier NDMF plugins in the build pipeline
        /// resolve to the original asset path even when the replacement has its own asset path.
        /// </summary>
        /// <returns>The asset path of the original asset, or the object's own path, or empty string.</returns>
        public static string ResolveAssetPath(Object obj)
        {
            if (obj == null)
                return string.Empty;

            // Check ObjectRegistry first: if another plugin replaced this object and registered
            // the replacement, resolve to the original asset path. This takes priority because
            // replacements may have their own asset path (e.g., plugins that generate and save
            // new textures as assets), but frozen/skip settings are keyed by the original's GUID.
            var registry = ObjectRegistry.ActiveRegistry;
            if (registry != null)
            {
                var reference = registry.GetReference(obj, create: false);
                if (reference?.Object != null && reference.Object != obj)
                {
                    string originalPath = AssetDatabase.GetAssetPath(reference.Object);
                    if (!string.IsNullOrEmpty(originalPath))
                        return originalPath;
                }
            }

            // Fallback: use the object's own asset path
            return AssetDatabase.GetAssetPath(obj) ?? string.Empty;
        }

        /// <summary>
        /// Resolves the asset GUID for an object, following ObjectRegistry replacement chains.
        /// </summary>
        /// <returns>The asset GUID, or empty string if unresolvable.</returns>
        public static string ResolveAssetGuid(Object obj)
        {
            string assetPath = ResolveAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            return AssetDatabase.AssetPathToGUID(assetPath);
        }
    }
}
