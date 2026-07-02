using System;
using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Result of running texture baking over one material: how many texture slots received a
    /// baked replacement, and how many bake operations were skipped because one of their input
    /// properties is driven by animation.
    /// </summary>
    public readonly struct LilToonBakeResult
    {
        public LilToonBakeResult(int bakedSlots, int skippedByAnimation)
        {
            BakedSlots = bakedSlots;
            SkippedByAnimation = skippedByAnimation;
        }

        /// <summary>Number of texture slots replaced with a baked texture.</summary>
        public int BakedSlots { get; }

        /// <summary>Number of bake operations skipped because an input property is animated.</summary>
        public int SkippedByAnimation { get; }
    }

    /// <summary>
    /// Bakes lilToon's color adjustments into textures at build time so the adjusted texture
    /// ships instead of runtime shader parameters plus their input textures.
    /// </summary>
    /// <remarks>
    /// This is the boundary between the build pipeline and the optional, externally-owned lilToon
    /// package, mirroring <see cref="IUnusedSlotOptimizer"/>: the implementation depends only on
    /// lilToon's baker shader and property names, so the project compiles whether or not lilToon
    /// is installed; when it is absent it reports <see cref="IsAvailable"/> = false and does
    /// nothing. See the <c>Integrations</c> folder for the concrete implementation.
    /// </remarks>
    public interface ILilToonBaker
    {
        /// <summary>
        /// True if the backing shader package was found and can be invoked.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Bakes, in place on the given (cloned) material, every supported adjustment whose input
        /// properties are not animated, replacing the affected texture slots with newly generated
        /// textures. Implementations no-op on materials they do not recognize.
        /// </summary>
        /// <param name="animatedProperties">
        /// Material property names driven by animation; a bake whose input appears here is
        /// skipped entirely so the animation keeps working.
        /// </param>
        /// <param name="canReplaceTexture">
        /// Consulted with (texture, propertyName) before a slot's texture is replaced; returning
        /// false skips that bake. Used to honor the user's exclusion and frozen-skip settings —
        /// baked output is uncompressed, so baking a texture the pipeline will then refuse to
        /// recompress would inflate it. Null means always allowed.
        /// </param>
        /// <param name="isProtectedTexture">
        /// Returns true for textures the bake must not repaint or drop: pinned by frozen
        /// settings ("ship exactly as configured"), or referenced by an animation curve — such
        /// a texture ships with the avatar regardless, so consuming its slot would only stop it
        /// from being collected and compressed. A protected texture is never used as a bake
        /// target and never consumed (slot-cleared) as a bake input; the affected bake or layer
        /// is skipped instead. Null means no textures are protected.
        /// </param>
        LilToonBakeResult Bake(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            Func<Texture2D, string, bool> canReplaceTexture,
            Func<Texture2D, bool> isProtectedTexture
        );
    }
}
