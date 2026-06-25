using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Bakes lilToon's color adjustments into textures at build time so the adjusted texture
    /// ships instead of runtime shader parameters plus their input textures.
    /// </summary>
    /// <remarks>
    /// This is the boundary between the build pipeline and the optional, externally-owned lilToon
    /// package, mirroring <see cref="IUnusedSlotOptimizer"/>: the implementation wraps lilToon via
    /// reflection so the project compiles whether or not it is installed; when it is absent it
    /// reports <see cref="IsAvailable"/> = false and does nothing. See the <c>Integrations</c>
    /// folder for the concrete implementation.
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
        /// <returns>The baked textures created by this call (empty if nothing was baked).</returns>
        Texture2D[] Bake(Material material, IReadOnlyCollection<string> animatedProperties);
    }
}
