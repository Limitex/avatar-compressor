using System;
using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor.common;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using nadena.dev.ndmf.runtime;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Pass that compresses avatar textures, including those referenced by animations.
    /// </summary>
    internal class TextureCompressorPass : Pass<TextureCompressorPass>
    {
        public override string DisplayName => "Avatar Compressor: Compress Avatar Textures";

        protected override void Execute(BuildContext ctx)
        {
            var components = ctx.AvatarRootObject.GetComponentsInChildren<TextureCompressor>(true);

            if (components.Length == 0) return;

            ValidateComponents(components);

            var config = components[0];

            // Get animation-referenced materials from AnimatorServicesContext
            var animationMaterials = GetAnimationReferencedMaterials(ctx);

            // Create service and compress textures
            var service = new TextureCompressorService(config);
            var (processedTextures, clonedMaterials) = service.CompressWithAnimationSupport(
                ctx.AvatarRootObject,
                config.EnableLogging,
                animationMaterials);

            // Update animation curves with replaced materials and textures
            UpdateAnimationReferences(ctx, processedTextures, clonedMaterials);

            CleanupComponents(components);
        }

        private static void ValidateComponents(TextureCompressor[] components)
        {
            if (components == null || components.Length == 0) return;

            // Warn about multiple components
            if (components.Length > 1)
            {
                Debug.LogWarning(
                    $"[LAC Texture Compressor] Multiple TextureCompressor components found ({components.Length}). " +
                    "Only the first component's settings will be used.",
                    components[0]);
            }

            // Warn about components not on avatar root
            foreach (var component in components)
            {
                if (component == null) continue;

                if (!RuntimeUtil.IsAvatarRoot(component.transform))
                {
                    Debug.LogWarning(
                        $"[LAC Texture Compressor] Component on '{component.gameObject.name}' is not on the avatar root. " +
                        "It is recommended to place the component on the avatar root GameObject.",
                        component);
                }
            }
        }

        /// <summary>
        /// Gets materials referenced by animations (MaterialSwap, etc.) from AnimatorServicesContext.
        /// </summary>
        private static List<Material> GetAnimationReferencedMaterials(BuildContext ctx)
        {
            try
            {
                // Activate the AnimatorServicesContext extension before accessing it
                ctx.ActivateExtensionContextRecursive<AnimatorServicesContext>();

                var animatorServices = ctx.Extension<AnimatorServicesContext>();
                if (animatorServices?.AnimationIndex == null)
                {
                    return new List<Material>();
                }

                return animatorServices.AnimationIndex.GetPPtrReferencedObjects
                    .OfType<Material>()
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[LAC Texture Compressor] Failed to get animation-referenced materials: {ex.Message}. " +
                    "Animation materials will not be processed.");
                return new List<Material>();
            }
        }

        /// <summary>
        /// Updates animation curves to reference cloned materials and compressed textures.
        /// </summary>
        private static void UpdateAnimationReferences(
            BuildContext ctx,
            Dictionary<Texture2D, Texture2D> processedTextures,
            Dictionary<Material, Material> clonedMaterials)
        {
            try
            {
                var animatorServices = ctx.Extension<AnimatorServicesContext>();
                if (animatorServices?.AnimationIndex == null) return;

                var animationIndex = animatorServices.AnimationIndex;

                // Rewrite object curves: update both materials and textures in a single pass
                animationIndex.RewriteObjectCurves(obj =>
                {
                    if (obj is Material m && clonedMaterials.TryGetValue(m, out var cloned))
                        return cloned;
                    if (obj is Texture2D t && processedTextures.TryGetValue(t, out var compressed))
                        return compressed;
                    return obj;
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[LAC Texture Compressor] Failed to update animation references: {ex.Message}. " +
                    "Some animation curves may reference original materials/textures.");
            }
        }

        private static void CleanupComponents(TextureCompressor[] components)
        {
            if (components == null) return;

            foreach (var component in components)
            {
                if (component != null)
                {
                    ComponentUtils.SafeDestroy(component);
                }
            }
        }
    }
}
