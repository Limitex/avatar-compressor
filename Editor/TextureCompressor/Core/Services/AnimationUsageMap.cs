using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Records which material properties are touched by <em>any</em> animation in the avatar's
    /// merged animator hierarchy, plus every texture referenced by an animation object (PPtr)
    /// curve. The <see cref="AnimatedProperties"/> list is handed to an
    /// <see cref="IUnusedSlotOptimizer"/> so a slot whose feature toggle is driven by animation is
    /// never cleared; the animated-texture set protects textures that ship with the avatar through
    /// an animation curve regardless of slot state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The map is deliberately coarse: it collects the <em>set of animated material property
    /// names</em> across the whole avatar, ignoring which renderer or material they belong to.
    /// Animation curves bind to a renderer path + a <c>material.&lt;Property&gt;</c> name rather
    /// than to a specific Material asset, so a per-material association cannot be recovered
    /// reliably. Treating a property as "animated everywhere" if it is animated anywhere is the
    /// safe (over-inclusive) choice, matching the safe-side default of the unused-slot feature.
    /// </para>
    /// <para>
    /// Building requires NDMF's <see cref="AnimatorServicesContext"/>, which is only available
    /// during an NDMF build. Outside that context (e.g. inspector preview), unused-slot detection
    /// is disabled rather than guessing from static state.
    /// </para>
    /// </remarks>
    public sealed class AnimationUsageMap
    {
        // Animation curves expose material property bindings as "material.<PropertyName>".
        private const string MaterialBindingPrefix = "material.";

        private readonly HashSet<string> _animatedMaterialProperties;
        private readonly HashSet<Texture2D> _animatedTextures;

        /// <summary>
        /// Constructs a map from an explicit set of animated material property names
        /// (already stripped of the <c>material.</c> prefix) and optionally the textures
        /// referenced by animation object curves. Primarily intended for tests.
        /// </summary>
        public AnimationUsageMap(
            IEnumerable<string> animatedMaterialProperties,
            IEnumerable<Texture2D> animatedTextures = null
        )
        {
            _animatedMaterialProperties = new HashSet<string>();
            if (animatedMaterialProperties != null)
            {
                foreach (var prop in animatedMaterialProperties)
                {
                    if (!string.IsNullOrEmpty(prop))
                        _animatedMaterialProperties.Add(prop);
                }
            }

            _animatedTextures = new HashSet<Texture2D>();
            if (animatedTextures != null)
            {
                foreach (var texture in animatedTextures)
                {
                    if (texture != null)
                        _animatedTextures.Add(texture);
                }
            }
        }

        /// <summary>
        /// An empty map: nothing is animated. Detection still runs but never vetoes on animation.
        /// </summary>
        public static AnimationUsageMap Empty { get; } =
            new AnimationUsageMap(Array.Empty<string>());

        /// <summary>
        /// Number of distinct animated material properties recorded.
        /// </summary>
        public int Count => _animatedMaterialProperties.Count;

        /// <summary>
        /// The distinct animated material property names (without the <c>material.</c> prefix).
        /// Passed to external optimizers (e.g. lilToon) that take an animated-property list.
        /// </summary>
        public IReadOnlyCollection<string> AnimatedProperties => _animatedMaterialProperties;

        /// <summary>
        /// Returns true if the given material property is animated anywhere in the avatar.
        /// </summary>
        public bool IsMaterialPropertyAnimated(string propertyName)
        {
            return !string.IsNullOrEmpty(propertyName)
                && _animatedMaterialProperties.Contains(propertyName);
        }

        /// <summary>
        /// Returns true if the given texture is referenced by an animation object (PPtr) curve
        /// anywhere in the avatar. Such a texture ships with the upload regardless of material
        /// slot state, so clearing its slots would not remove it — it would only stop it from
        /// being collected and compressed.
        /// </summary>
        public bool IsTextureAnimated(Texture2D texture)
        {
            return texture != null && _animatedTextures.Contains(texture);
        }

        /// <summary>
        /// Builds the map from the avatar's merged animator hierarchy.
        /// Returns <c>null</c> if the animator services are unavailable or scanning fails — callers
        /// should treat a <c>null</c> map as "cannot prove anything unused" and skip detection.
        /// </summary>
        public static AnimationUsageMap Build(BuildContext ctx)
        {
            if (ctx == null)
                return null;

            var properties = new HashSet<string>();
            var textures = new HashSet<Texture2D>();

            try
            {
                ctx.ActivateExtensionContextRecursive<AnimatorServicesContext>();
                var animatorServices = ctx.Extension<AnimatorServicesContext>();

                if (animatorServices?.ControllerContext == null)
                    return null;

                foreach (var controller in animatorServices.ControllerContext.GetAllControllers())
                {
                    if (controller == null)
                        continue;

                    foreach (var node in controller.AllReachableNodes())
                    {
                        if (node is VirtualClip clip)
                        {
                            CollectMaterialProperties(clip.GetFloatCurveBindings(), properties);
                            CollectFromObjectCurves(clip, properties, textures);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[LAC Texture Compressor] Failed to scan animations for unused-slot detection: {ex.Message}. "
                        + "Unused-slot detection will be skipped (all slots treated as used)."
                );
                return null;
            }

            return new AnimationUsageMap(properties, textures);
        }

        private static void CollectFromObjectCurves(
            VirtualClip clip,
            HashSet<string> properties,
            HashSet<Texture2D> textures
        )
        {
            foreach (var binding in clip.GetObjectCurveBindings())
            {
                CollectMaterialProperty(binding, properties);

                var keyframes = clip.GetObjectCurve(binding);
                if (keyframes == null)
                    continue;

                foreach (var keyframe in keyframes)
                {
                    if (keyframe.value is Texture2D texture)
                        textures.Add(texture);
                }
            }
        }

        private static void CollectMaterialProperties(
            IEnumerable<EditorCurveBinding> bindings,
            HashSet<string> properties
        )
        {
            foreach (var binding in bindings)
                CollectMaterialProperty(binding, properties);
        }

        private static void CollectMaterialProperty(
            EditorCurveBinding binding,
            HashSet<string> properties
        )
        {
            string name = binding.propertyName;
            if (string.IsNullOrEmpty(name) || !name.StartsWith(MaterialBindingPrefix))
                return;

            string prop = name.Substring(MaterialBindingPrefix.Length);

            // Strip vector/color component suffixes (e.g. "_Color.r" -> "_Color") so that
            // a single animated channel marks the whole property as animated.
            int dot = prop.IndexOf('.');
            if (dot > 0)
                prop = prop.Substring(0, dot);

            if (prop.Length > 0)
                properties.Add(prop);
        }
    }
}
