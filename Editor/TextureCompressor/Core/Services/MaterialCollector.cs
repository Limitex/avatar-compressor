using System;
using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor.common;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Service for collecting materials from various sources in an avatar hierarchy.
    /// </summary>
    public static class MaterialCollector
    {
        /// <summary>
        /// Collects all materials from Renderer components in the hierarchy.
        /// </summary>
        /// <remarks>
        /// This method is kept separate from <see cref="CollectFromComponents"/> for performance reasons.
        /// Direct access to <see cref="Renderer.sharedMaterials"/> is significantly faster than
        /// iterating through all properties via <see cref="UnityEditor.SerializedObject"/>.
        /// </remarks>
        /// <param name="root">Root GameObject of the hierarchy</param>
        /// <returns>List of material references from Renderers</returns>
        public static List<MaterialReference> CollectFromRenderers(GameObject root)
        {
            var references = new List<MaterialReference>();
            var renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                if (ComponentUtils.IsEditorOnly(renderer.gameObject)) continue;

                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;

                    references.Add(MaterialReference.FromRenderer(material, renderer));
                }
            }

            return references;
        }

        /// <summary>
        /// Collects materials referenced by animations from an Animator component.
        /// This is used for Editor preview (outside NDMF build context).
        /// </summary>
        /// <param name="root">Root GameObject with an Animator component</param>
        /// <returns>List of material references from animations</returns>
        public static List<MaterialReference> CollectFromAnimator(GameObject root)
        {
            var references = new List<MaterialReference>();

            var animator = root.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return references;
            }

            var clips = GetAllAnimationClips(animator.runtimeAnimatorController);

            foreach (var clip in clips)
            {
                if (clip == null) continue;

                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                foreach (var binding in bindings)
                {
                    var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    foreach (var keyframe in keyframes)
                    {
                        if (keyframe.value is Material material && material != null)
                        {
                            references.Add(MaterialReference.FromAnimation(material, clip));
                        }
                    }
                }
            }

            return references;
        }

        /// <summary>
        /// Collects materials referenced by animations from NDMF's AnimatorServicesContext.
        /// This is used during NDMF build.
        /// </summary>
        /// <param name="ctx">NDMF BuildContext</param>
        /// <returns>List of material references from animations</returns>
        public static List<MaterialReference> CollectFromAnimator(BuildContext ctx)
        {
            var references = new List<MaterialReference>();

            try
            {
                ctx.ActivateExtensionContextRecursive<AnimatorServicesContext>();
                var animatorServices = ctx.Extension<AnimatorServicesContext>();

                if (animatorServices?.AnimationIndex == null)
                {
                    return references;
                }

                var materials = animatorServices.AnimationIndex.GetPPtrReferencedObjects
                    .OfType<Material>()
                    .Distinct();

                foreach (var material in materials)
                {
                    if (material != null)
                    {
                        // In NDMF context, we don't have direct access to the source clip
                        // Use the AnimationIndex as the source object
                        references.Add(MaterialReference.FromAnimation(material, null));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[MaterialCollector] Failed to collect materials from animations: {ex.Message}");
            }

            return references;
        }

        /// <summary>
        /// Collects materials referenced by components (e.g., MA MaterialSetter) in the hierarchy.
        /// </summary>
        /// <remarks>
        /// Renderer components are skipped because they are handled by <see cref="CollectFromRenderers"/>,
        /// which provides faster direct access to materials. This avoids duplicate collection and
        /// leverages the performance benefit of direct property access over SerializedObject iteration.
        /// </remarks>
        /// <param name="root">Root GameObject of the hierarchy</param>
        /// <returns>List of material references from components</returns>
        public static List<MaterialReference> CollectFromComponents(GameObject root)
        {
            var references = new List<MaterialReference>();
            var allComponents = root.GetComponentsInChildren<Component>(true);

            foreach (var component in allComponents)
            {
                if (component == null) continue;
                if (ComponentUtils.IsEditorOnly(component.gameObject)) continue;

                // Skip Renderer components (handled separately)
                if (component is Renderer) continue;

                try
                {
                    var serializedObject = new SerializedObject(component);
                    var iterator = serializedObject.GetIterator();

                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            var obj = iterator.objectReferenceValue;
                            if (obj is Material material && material != null)
                            {
                                references.Add(MaterialReference.FromComponent(material, component));
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors from components that can't be serialized
                }
            }

            return references;
        }

        /// <summary>
        /// Collects all materials from all sources (Renderers, Animations, Components).
        /// This is used for Editor preview (outside NDMF build context).
        /// </summary>
        /// <param name="root">Root GameObject of the hierarchy</param>
        /// <returns>List of all material references</returns>
        public static List<MaterialReference> CollectAll(GameObject root)
        {
            var references = new List<MaterialReference>();

            references.AddRange(CollectFromRenderers(root));
            references.AddRange(CollectFromAnimator(root));
            references.AddRange(CollectFromComponents(root));

            return references;
        }

        /// <summary>
        /// Collects all materials from all sources (Renderers, Animations, Components).
        /// This is used during NDMF build.
        /// </summary>
        /// <param name="ctx">NDMF BuildContext</param>
        /// <returns>List of all material references</returns>
        public static List<MaterialReference> CollectAll(BuildContext ctx)
        {
            var references = new List<MaterialReference>();

            references.AddRange(CollectFromRenderers(ctx.AvatarRootObject));
            references.AddRange(CollectFromAnimator(ctx));
            references.AddRange(CollectFromComponents(ctx.AvatarRootObject));

            return references;
        }

        /// <summary>
        /// Gets distinct materials from a list of references.
        /// </summary>
        /// <param name="references">List of material references</param>
        /// <returns>Distinct materials</returns>
        public static IEnumerable<Material> GetDistinctMaterials(IEnumerable<MaterialReference> references)
        {
            return references
                .Where(r => r?.Material != null)
                .Select(r => r.Material)
                .Distinct();
        }

        #region Animation Clip Helpers

        private static List<AnimationClip> GetAllAnimationClips(RuntimeAnimatorController controller)
        {
            var clips = new HashSet<AnimationClip>();

            if (controller is AnimatorOverrideController overrideController)
            {
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);

                foreach (var pair in overrides)
                {
                    if (pair.Value != null)
                        clips.Add(pair.Value);
                    else if (pair.Key != null)
                        clips.Add(pair.Key);
                }

                if (overrideController.runtimeAnimatorController != null)
                {
                    foreach (var clip in GetAllAnimationClips(overrideController.runtimeAnimatorController))
                    {
                        clips.Add(clip);
                    }
                }
            }
            else if (controller is AnimatorController animatorController)
            {
                foreach (var layer in animatorController.layers)
                {
                    CollectClipsFromStateMachine(layer.stateMachine, clips);
                }
            }
            else
            {
                foreach (var clip in controller.animationClips)
                {
                    if (clip != null)
                        clips.Add(clip);
                }
            }

            return clips.ToList();
        }

        private static void CollectClipsFromStateMachine(AnimatorStateMachine stateMachine, HashSet<AnimationClip> clips)
        {
            if (stateMachine == null) return;

            foreach (var state in stateMachine.states)
            {
                if (state.state?.motion is AnimationClip clip)
                {
                    clips.Add(clip);
                }
                else if (state.state?.motion is BlendTree blendTree)
                {
                    CollectClipsFromBlendTree(blendTree, clips);
                }
            }

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                CollectClipsFromStateMachine(subStateMachine.stateMachine, clips);
            }
        }

        private static void CollectClipsFromBlendTree(BlendTree blendTree, HashSet<AnimationClip> clips)
        {
            if (blendTree == null) return;

            foreach (var child in blendTree.children)
            {
                if (child.motion is AnimationClip clip)
                {
                    clips.Add(clip);
                }
                else if (child.motion is BlendTree childBlendTree)
                {
                    CollectClipsFromBlendTree(childBlendTree, clips);
                }
            }
        }

        #endregion
    }
}
