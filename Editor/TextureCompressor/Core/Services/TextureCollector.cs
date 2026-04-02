using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Service for collecting textures from avatar hierarchy.
    /// </summary>
    public class TextureCollector
    {
        private readonly int _minSourceSize;
        private readonly int _skipIfSmallerThan;
        private readonly bool _processMainTextures;
        private readonly bool _processNormalMaps;
        private readonly bool _processEmissionMaps;
        private readonly bool _processOtherTextures;
        private readonly bool _skipUnknownUncompressedTextures;
        private readonly List<string> _excludedPathPrefixes;
        private readonly HashSet<string> _frozenSkipGuids;

        public TextureCollector(
            int minSourceSize,
            int skipIfSmallerThan,
            bool processMainTextures,
            bool processNormalMaps,
            bool processEmissionMaps,
            bool processOtherTextures,
            bool skipUnknownUncompressedTextures,
            IEnumerable<string> excludedPathPrefixes = null,
            IEnumerable<string> frozenSkipGuids = null
        )
        {
            _minSourceSize = minSourceSize;
            _skipIfSmallerThan = skipIfSmallerThan;
            _processMainTextures = processMainTextures;
            _processNormalMaps = processNormalMaps;
            _processEmissionMaps = processEmissionMaps;
            _processOtherTextures = processOtherTextures;
            _skipUnknownUncompressedTextures = skipUnknownUncompressedTextures;
            _excludedPathPrefixes =
                excludedPathPrefixes != null
                    ? new List<string>(
                        excludedPathPrefixes.Where(p => !string.IsNullOrWhiteSpace(p))
                    )
                    : new List<string>();
            _frozenSkipGuids =
                frozenSkipGuids != null
                    ? new HashSet<string>(frozenSkipGuids)
                    : new HashSet<string>();
        }

        /// <summary>
        /// Resolves the original asset for an object via ObjectRegistry replacement chain.
        /// If another NDMF plugin replaced the object, the registry maps it back to the original.
        /// </summary>
        private static Object ResolveOriginalObject(Object obj)
        {
            var registry = ObjectRegistry.ActiveRegistry;
            if (registry != null)
            {
                var reference = registry.GetReference(obj, create: false);
                if (reference?.Object != null && reference.Object != obj)
                    return reference.Object;
            }
            return obj;
        }

        /// <summary>
        /// Collects textures that should be processed from the avatar hierarchy.
        /// </summary>
        public Dictionary<Texture2D, TextureInfo> Collect(GameObject root)
        {
            return CollectInternal(root, collectAll: false);
        }

        /// <summary>
        /// Collects all textures from the avatar hierarchy, including skipped ones.
        /// Skipped textures will have IsProcessed=false and SkipReason set.
        /// </summary>
        public Dictionary<Texture2D, TextureInfo> CollectAll(GameObject root)
        {
            return CollectInternal(root, collectAll: true);
        }

        private Dictionary<Texture2D, TextureInfo> CollectInternal(GameObject root, bool collectAll)
        {
            var textures = new Dictionary<Texture2D, TextureInfo>();
            var renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                // Skip EditorOnly tagged objects (stripped from build)
                if (ComponentUtils.IsEditorOnly(renderer.gameObject))
                    continue;

                var materials = renderer.sharedMaterials;
                foreach (var material in materials)
                {
                    if (material == null)
                        continue;
                    CollectFromMaterial(material, renderer, textures, collectAll);
                }
            }

            return textures;
        }

        /// <summary>
        /// Collects textures from a list of materials (e.g., from animations).
        /// Call this after Collect() to add additional materials to the same dictionary.
        /// </summary>
        /// <param name="materials">The materials to collect textures from.</param>
        /// <param name="textures">The texture dictionary to add to (typically from Collect()).</param>
        /// <param name="collectAll">If true, collects all textures including skipped ones (for preview).</param>
        public void CollectFromMaterials(
            IEnumerable<Material> materials,
            Dictionary<Texture2D, TextureInfo> textures,
            bool collectAll = false
        )
        {
            if (materials == null || textures == null)
                return;

            foreach (var material in materials.Distinct())
            {
                if (material == null)
                    continue;
                CollectFromMaterial(material, null, textures, collectAll);
            }
        }

        private void CollectFromMaterial(
            Material material,
            Renderer renderer,
            Dictionary<Texture2D, TextureInfo> textures,
            bool collectAll = false
        )
        {
            var shader = material.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                var texture = material.GetTexture(propertyName) as Texture2D;

                if (texture == null)
                    continue;

                var category = TexturePropertyDefinitions.GetCategory(propertyName);
                bool isNormalMap = category == TexturePropertyCategory.Normal;
                bool isEmission = category == TexturePropertyCategory.Emission;

                if (!textures.TryGetValue(texture, out var info))
                {
                    info = new TextureInfo
                    {
                        TextureType = GetTextureType(propertyName),
                        IsNormalMap = isNormalMap,
                        IsEmission = isEmission,
                    };
                    EvaluateProcessability(info, texture, propertyName);

                    if (!collectAll && !info.IsProcessed)
                        continue;

                    textures[texture] = info;
                }
                else
                {
                    // If the same texture is used as both normal map and other type,
                    // prioritize normal map classification to ensure proper analysis
                    if (isNormalMap && !info.IsNormalMap)
                    {
                        info.IsNormalMap = true;
                        info.TextureType = "Normal";
                    }
                    // Similarly for emission
                    if (isEmission && !info.IsEmission)
                    {
                        info.IsEmission = true;
                    }
                    // Per-texture checks (path, size, frozen) don't change between properties,
                    // but property-dependent skip reasons can be upgraded when a subsequent
                    // property reference satisfies the required conditions.
                    if (!info.IsProcessed && CanUpgradeSkipReason(info.SkipReason, propertyName))
                    {
                        info.IsProcessed = true;
                        info.SkipReason = SkipReason.None;
                    }
                }

                info.References.Add(
                    new MaterialTextureReference
                    {
                        Material = material,
                        PropertyName = propertyName,
                        Renderer = renderer,
                    }
                );
            }
        }

        /// <summary>
        /// Evaluates whether a texture should be processed and populates the TextureInfo accordingly.
        /// Resolves the original asset via ObjectRegistry for textures replaced by upstream NDMF plugins.
        /// </summary>
        private void EvaluateProcessability(
            TextureInfo info,
            Texture2D texture,
            string propertyName
        )
        {
            // Resolve original asset via ObjectRegistry replacement chain.
            // If another NDMF plugin replaced the texture, the replacement has no asset path;
            // ObjectRegistry follows the chain back to the original asset.
            Object resolvedObj = ResolveOriginalObject(texture);

            string assetPath = AssetDatabase.GetAssetPath(resolvedObj);

            // Skip runtime-generated textures (no asset path).
            // These are dynamically created during build and may use RGB values for non-visual data
            // (e.g., depth, deformation vectors), which compression would corrupt.
            if (string.IsNullOrEmpty(assetPath))
            {
                info.IsProcessed = false;
                info.SkipReason = SkipReason.RuntimeGenerated;
                return;
            }

            // Skip textures in excluded paths
            foreach (var prefix in _excludedPathPrefixes)
            {
                if (assetPath.StartsWith(prefix))
                {
                    info.IsProcessed = false;
                    info.SkipReason = SkipReason.ExcludedPath;
                    return;
                }
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            info.AssetGuid = guid ?? string.Empty;

            if (!string.IsNullOrEmpty(guid) && _frozenSkipGuids.Contains(guid))
            {
                info.IsProcessed = false;
                info.SkipReason = SkipReason.FrozenSkip;
                return;
            }

            int maxDim = Mathf.Max(texture.width, texture.height);
            if (maxDim < _minSourceSize || maxDim <= _skipIfSmallerThan)
            {
                info.IsProcessed = false;
                info.SkipReason = SkipReason.TooSmall;
                return;
            }

            // Skip uncompressed textures on unknown shader properties to avoid corrupting
            // non-visual data (e.g., SPS bake data, masks, LUTs).
            // Already-compressed textures (DXT, BC, ASTC, etc.) are not skipped by this check —
            // they were intentionally compressed upstream and may still be processed by other filters.
            if (
                _skipUnknownUncompressedTextures
                && !TextureFormatInfo.IsCompressed(texture.format)
                && !TexturePropertyDefinitions.IsKnownTextureProperty(propertyName)
            )
            {
                info.IsProcessed = false;
                info.SkipReason = SkipReason.UnknownUncompressedProperty;
                return;
            }

            if (!IsTypeEnabled(propertyName))
            {
                info.IsProcessed = false;
                info.SkipReason = SkipReason.FilteredByType;
                return;
            }

            info.IsProcessed = true;
            info.SkipReason = SkipReason.None;
        }

        /// <summary>
        /// Determines whether a property-dependent skip reason can be upgraded to processed.
        /// Only skip reasons that depend on the property name are eligible; per-texture
        /// reasons (path, size, frozen, runtime-generated) are never upgradeable.
        /// </summary>
        private bool CanUpgradeSkipReason(SkipReason reason, string propertyName)
        {
            switch (reason)
            {
                case SkipReason.FilteredByType:
                    return IsTypeEnabled(propertyName);
                case SkipReason.UnknownUncompressedProperty:
                    return TexturePropertyDefinitions.IsKnownTextureProperty(propertyName)
                        && IsTypeEnabled(propertyName);
                default:
                    return false;
            }
        }

        private bool IsTypeEnabled(string propertyName)
        {
            switch (TexturePropertyDefinitions.GetCategory(propertyName))
            {
                case TexturePropertyCategory.Main:
                    return _processMainTextures;
                case TexturePropertyCategory.Normal:
                    return _processNormalMaps;
                case TexturePropertyCategory.Emission:
                    return _processEmissionMaps;
                default:
                    return _processOtherTextures;
            }
        }

        private static string GetTextureType(string propertyName)
        {
            return TexturePropertyDefinitions.GetCategory(propertyName).ToString();
        }
    }
}
