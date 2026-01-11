using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor.common;
using UnityEngine;
using UnityEditor;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Service for collecting textures from avatar hierarchy.
    /// </summary>
    public class TextureCollector
    {
        private static readonly HashSet<string> MainTextureProperties = new HashSet<string>
        {
            "_MainTex", "_BaseMap", "_BaseColorMap", "_Albedo", "_AlbedoMap",
            "_Diffuse", "_DiffuseMap", "_Color", "_ColorMap"
        };

        private static readonly HashSet<string> NormalMapProperties = new HashSet<string>
        {
            "_BumpMap", "_NormalMap", "_Normal", "_DetailNormalMap"
        };

        private static readonly HashSet<string> EmissionProperties = new HashSet<string>
        {
            "_EmissionMap", "_EmissiveMap", "_Emission", "_EmissiveColor"
        };

        private readonly int _minSourceSize;
        private readonly int _skipIfSmallerThan;
        private readonly bool _processMainTextures;
        private readonly bool _processNormalMaps;
        private readonly bool _processEmissionMaps;
        private readonly bool _processOtherTextures;
        private readonly List<string> _excludedPathPrefixes;
        private readonly HashSet<string> _frozenSkipGuids;

        public TextureCollector(
            int minSourceSize,
            int skipIfSmallerThan,
            bool processMainTextures,
            bool processNormalMaps,
            bool processEmissionMaps,
            bool processOtherTextures,
            IEnumerable<string> excludedPathPrefixes = null,
            IEnumerable<string> frozenSkipGuids = null)
        {
            _minSourceSize = minSourceSize;
            _skipIfSmallerThan = skipIfSmallerThan;
            _processMainTextures = processMainTextures;
            _processNormalMaps = processNormalMaps;
            _processEmissionMaps = processEmissionMaps;
            _processOtherTextures = processOtherTextures;
            _excludedPathPrefixes = excludedPathPrefixes != null
                ? new List<string>(excludedPathPrefixes.Where(p => !string.IsNullOrWhiteSpace(p)))
                : new List<string>();
            _frozenSkipGuids = frozenSkipGuids != null
                ? new HashSet<string>(frozenSkipGuids)
                : new HashSet<string>();
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
                if (ComponentUtils.IsEditorOnly(renderer.gameObject)) continue;

                var materials = renderer.sharedMaterials;
                foreach (var material in materials)
                {
                    if (material == null) continue;
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
            bool collectAll = false)
        {
            if (materials == null || textures == null) return;

            foreach (var material in materials.Distinct())
            {
                if (material == null) continue;
                CollectFromMaterial(material, null, textures, collectAll);
            }
        }

        private void CollectFromMaterial(
            Material material,
            Renderer renderer,
            Dictionary<Texture2D, TextureInfo> textures,
            bool collectAll = false)
        {
            var shader = material.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                var texture = material.GetTexture(propertyName) as Texture2D;

                if (texture == null) continue;

                string textureType = GetTextureType(propertyName);
                bool isNormalMap = NormalMapProperties.Contains(propertyName);
                bool isEmission = EmissionProperties.Contains(propertyName);

                var processResult = GetProcessResult(texture, propertyName);

                if (!collectAll && !processResult.shouldProcess) continue;

                if (!textures.TryGetValue(texture, out var info))
                {
                    info = new TextureInfo
                    {
                        TextureType = textureType,
                        PropertyName = propertyName,
                        IsNormalMap = isNormalMap,
                        IsEmission = isEmission,
                        IsProcessed = processResult.shouldProcess,
                        SkipReason = processResult.skipReason
                    };
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
                    // Update process status if this reference would be processed.
                    // If the same texture is referenced by multiple properties and any of them
                    // should be processed, the texture is marked as processed (order-independent).
                    if (processResult.shouldProcess && !info.IsProcessed)
                    {
                        info.IsProcessed = true;
                        info.SkipReason = SkipReason.None;
                    }
                }

                info.References.Add(new MaterialTextureReference
                {
                    Material = material,
                    PropertyName = propertyName,
                    Renderer = renderer
                });
            }
        }

        private (bool shouldProcess, SkipReason skipReason) GetProcessResult(Texture2D texture, string propertyName)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);

            // Skip runtime-generated textures (no asset path).
            // These are dynamically created during build and may use RGB values for non-visual data
            // (e.g., depth, deformation vectors), which compression would corrupt.
            if (string.IsNullOrEmpty(assetPath))
                return (false, SkipReason.RuntimeGenerated);

            // Skip textures in excluded paths
            foreach (var prefix in _excludedPathPrefixes)
            {
                if (assetPath.StartsWith(prefix))
                    return (false, SkipReason.ExcludedPath);
            }

            // Check frozen skip using GUID for reliable comparison
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(guid) && _frozenSkipGuids.Contains(guid))
                return (false, SkipReason.FrozenSkip);

            int maxDim = Mathf.Max(texture.width, texture.height);
            if (maxDim < _minSourceSize) return (false, SkipReason.TooSmall);
            if (maxDim <= _skipIfSmallerThan) return (false, SkipReason.TooSmall);

            bool shouldProcess;
            if (MainTextureProperties.Contains(propertyName))
                shouldProcess = _processMainTextures;
            else if (NormalMapProperties.Contains(propertyName))
                shouldProcess = _processNormalMaps;
            else if (EmissionProperties.Contains(propertyName))
                shouldProcess = _processEmissionMaps;
            else
                shouldProcess = _processOtherTextures;

            return shouldProcess ? (true, SkipReason.None) : (false, SkipReason.FilteredByType);
        }

        private string GetTextureType(string propertyName)
        {
            if (MainTextureProperties.Contains(propertyName)) return "Main";
            if (NormalMapProperties.Contains(propertyName)) return "Normal";
            if (EmissionProperties.Contains(propertyName)) return "Emission";
            return "Other";
        }
    }
}
