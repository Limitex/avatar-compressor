using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Drives an <see cref="IUnusedSlotOptimizer"/> over the avatar's cloned materials, then drops
    /// any collected texture that is left unreferenced so it is excluded from the build.
    /// </summary>
    /// <remarks>
    /// The optimizer owns the "which slots are unused" decision (e.g. lilToon's own logic). This
    /// class owns only the build-pipeline side: applying it once per material and reconciling the
    /// texture → slot references afterward. That split keeps the orchestration testable with a fake
    /// optimizer, independent of any external shader package.
    /// </remarks>
    public static class UnusedSlotPruner
    {
        public readonly struct PruneResult
        {
            public PruneResult(int clearedSlots, int droppedTextures)
            {
                ClearedSlots = clearedSlots;
                DroppedTextures = droppedTextures;
            }

            /// <summary>
            /// Number of slot references the optimizer cleared among collected textures. Stale
            /// references (destroyed material) and slots cleared on textures that were filtered
            /// out of collection are not counted here.
            /// </summary>
            public int ClearedSlots { get; }

            /// <summary>Number of textures dropped because no slot references them anymore.</summary>
            public int DroppedTextures { get; }
        }

        /// <summary>
        /// Clears unused slots on <paramref name="materials"/> and removes now-unreferenced textures
        /// from <paramref name="textures"/> (mutated in place). No-op when the optimizer is
        /// unavailable.
        /// </summary>
        /// <param name="materials">
        /// All cloned materials to optimize. The full list is used (rather than only materials
        /// reachable from <paramref name="textures"/>) so that a material whose textures were all
        /// filtered out of collection still has its unused slots cleared.
        /// </param>
        public static PruneResult Prune(
            Dictionary<Texture2D, TextureInfo> textures,
            IUnusedSlotOptimizer optimizer,
            IReadOnlyCollection<string> animatedProperties,
            IEnumerable<Material> materials
        )
        {
            if (textures == null || optimizer == null || !optimizer.IsAvailable)
                return default;

            // 1. Let the optimizer clear unused slots on each distinct cloned material exactly once.
            if (materials != null)
            {
                var processed = new HashSet<Material>();
                foreach (var material in materials)
                {
                    if (material != null && processed.Add(material))
                        optimizer.ClearUnusedSlots(material, animatedProperties);
                }
            }

            // 2. Drop references the optimizer cleared, then textures with no remaining reference.
            int clearedSlots = 0;
            var droppedTextures = new List<Texture2D>();
            foreach (var kvp in textures)
            {
                var texture = kvp.Key;
                var info = kvp.Value;

                // Stale references (destroyed material) are dropped but not counted as cleared,
                // so the logged count reflects only what the optimizer actually did.
                info.References.RemoveAll(reference => reference?.Material == null);
                clearedSlots += info.References.RemoveAll(reference =>
                    reference.Material.GetTexture(reference.PropertyName) != texture
                );

                if (info.References.Count == 0)
                    droppedTextures.Add(texture);
            }

            foreach (var texture in droppedTextures)
                textures.Remove(texture);

            return new PruneResult(clearedSlots, droppedTextures.Count);
        }
    }
}
