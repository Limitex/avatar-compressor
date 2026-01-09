using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Represents the source type of a material reference.
    /// </summary>
    public enum MaterialSourceType
    {
        /// <summary>Material is referenced by a Renderer component.</summary>
        Renderer,
        /// <summary>Material is referenced by an animation (e.g., MaterialSwap).</summary>
        Animation,
        /// <summary>Material is referenced by a component (e.g., MA MaterialSetter).</summary>
        Component
    }

    /// <summary>
    /// Represents a material and its reference source.
    /// Used to track where materials are referenced from for proper cloning and reference updates.
    /// </summary>
    public class MaterialReference
    {
        /// <summary>
        /// The referenced material.
        /// </summary>
        public Material Material { get; }

        /// <summary>
        /// The type of source that references this material.
        /// </summary>
        public MaterialSourceType SourceType { get; }

        /// <summary>
        /// The object that references this material (Renderer, AnimationClip, Component, etc.).
        /// </summary>
        public Object SourceObject { get; }

        /// <summary>
        /// The index of the material slot (for Renderer sources).
        /// -1 if not applicable.
        /// </summary>
        public int SlotIndex { get; }

        /// <summary>
        /// Creates a new MaterialReference.
        /// </summary>
        /// <param name="material">The referenced material</param>
        /// <param name="sourceType">The type of source</param>
        /// <param name="sourceObject">The object that references this material</param>
        /// <param name="slotIndex">The material slot index (-1 if not applicable)</param>
        public MaterialReference(
            Material material,
            MaterialSourceType sourceType,
            Object sourceObject,
            int slotIndex = -1)
        {
            Material = material;
            SourceType = sourceType;
            SourceObject = sourceObject;
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// Creates a MaterialReference for a Renderer source.
        /// </summary>
        public static MaterialReference FromRenderer(Material material, Renderer renderer, int slotIndex)
        {
            return new MaterialReference(material, MaterialSourceType.Renderer, renderer, slotIndex);
        }

        /// <summary>
        /// Creates a MaterialReference for an Animation source.
        /// </summary>
        public static MaterialReference FromAnimation(Material material, Object animationSource)
        {
            return new MaterialReference(material, MaterialSourceType.Animation, animationSource);
        }

        /// <summary>
        /// Creates a MaterialReference for a Component source.
        /// </summary>
        public static MaterialReference FromComponent(Material material, Component component)
        {
            return new MaterialReference(material, MaterialSourceType.Component, component);
        }
    }
}
