using System;
using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Drives an <see cref="IUnusedSlotOptimizer"/> over the avatar's cloned materials, restoring
    /// slots whose texture must survive the build (frozen by the user, or referenced by an
    /// animation object curve).
    /// </summary>
    /// <remarks>
    /// The optimizer owns the "which slots are unused" decision (e.g. lilToon's own logic); this
    /// class owns only the build-pipeline side. It must run <em>before</em> texture collection so
    /// that <see cref="TextureCollector"/> sees the final slot state — classification (normal map /
    /// emission upgrades) and filter rules then apply to surviving bindings only, with no
    /// after-the-fact reconciliation that could drift from the collector's semantics.
    /// </remarks>
    internal static class UnusedSlotPruner
    {
        internal readonly struct PruneResult
        {
            public PruneResult(int clearedSlots, int droppedTextures)
            {
                ClearedSlots = clearedSlots;
                DroppedTextures = droppedTextures;
            }

            /// <summary>
            /// Number of texture slots left cleared by the optimizer (protected slots that were
            /// restored are not counted).
            /// </summary>
            public int ClearedSlots { get; }

            /// <summary>
            /// Number of distinct textures no longer bound to any slot of the given materials,
            /// i.e. excluded from the upload (unless an animation curve still references them).
            /// </summary>
            public int DroppedTextures { get; }
        }

        /// <summary>
        /// Clears unused slots on every distinct material in <paramref name="materials"/>. A slot
        /// is restored after the optimizer ran when its texture is referenced by an animation
        /// object curve (per <paramref name="usageMap"/>) or <paramref name="isProtectedTexture"/>
        /// returns true (e.g. frozen by the user): such a texture stays in the build anyway, so
        /// keeping the slot keeps it collectable and compressible instead of shipping it untouched
        /// (animation case) or silently overriding an explicit user pin (frozen case).
        /// </summary>
        public static PruneResult Prune(
            IUnusedSlotOptimizer optimizer,
            AnimationUsageMap usageMap,
            IEnumerable<Material> materials,
            Func<Texture2D, bool> isProtectedTexture = null
        )
        {
            if (
                optimizer == null
                || !optimizer.IsAvailable
                || usageMap == null
                || materials == null
            )
                return default;

            int clearedSlots = 0;
            var clearedTextures = new HashSet<Texture2D>();
            var survivingTextures = new HashSet<Texture2D>();
            var processed = new HashSet<Material>();
            var slotSnapshot = new List<(string Property, Texture2D Texture)>();

            foreach (var material in materials)
            {
                if (material == null || material.shader == null || !processed.Add(material))
                    continue;

                slotSnapshot.Clear();
                foreach (var property in material.GetTexturePropertyNames())
                {
                    if (material.GetTexture(property) is Texture2D texture)
                        slotSnapshot.Add((property, texture));
                }

                optimizer.ClearUnusedSlots(material, usageMap.AnimatedProperties);

                foreach (var (property, texture) in slotSnapshot)
                {
                    if (material.GetTexture(property) == texture)
                    {
                        survivingTextures.Add(texture);
                        continue;
                    }

                    if (
                        material.GetTexture(property) == null
                        && (
                            usageMap.IsTextureAnimated(texture)
                            || (isProtectedTexture?.Invoke(texture) ?? false)
                        )
                    )
                    {
                        material.SetTexture(property, texture);
                        survivingTextures.Add(texture);
                        continue;
                    }

                    clearedSlots++;
                    clearedTextures.Add(texture);
                }
            }

            int droppedTextures = 0;
            foreach (var texture in clearedTextures)
            {
                if (!survivingTextures.Contains(texture))
                    droppedTextures++;
            }

            return new PruneResult(clearedSlots, droppedTextures);
        }
    }
}
