using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.common
{
    /// <summary>
    /// Utility class for working with NDMF ObjectRegistry.
    /// Provides methods to track original objects through the build pipeline.
    /// </summary>
    public static class ObjectRegistryUtils
    {
        /// <summary>
        /// Gets the original asset path of a texture, tracking through ObjectRegistry
        /// if the texture was generated/cloned by another plugin.
        /// </summary>
        /// <param name="texture">The texture to get the original asset path for.</param>
        /// <returns>The asset path of the original texture, or empty string if not found.</returns>
        public static string GetOriginalAssetPath(Texture2D texture)
        {
            if (texture == null) return string.Empty;

            var originalTexture = GetOriginalObject(texture);
            return AssetDatabase.GetAssetPath(originalTexture);
        }

        /// <summary>
        /// Gets the original object from ObjectRegistry if the object was replaced/cloned.
        /// </summary>
        /// <typeparam name="T">The type of UnityEngine.Object.</typeparam>
        /// <param name="obj">The object to trace back to its original.</param>
        /// <returns>The original object, or the input object if no original was registered.</returns>
        public static T GetOriginalObject<T>(T obj) where T : Object
        {
            if (obj == null) return null;

            var reference = ObjectRegistry.GetReference(obj);
            if (reference == null) return obj;

            var originalObject = reference.Object as T;
            return originalObject != null ? originalObject : obj;
        }
    }
}
