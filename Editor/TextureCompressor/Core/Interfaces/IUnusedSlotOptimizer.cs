using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Clears texture slots that an external shader integration can prove are unused at runtime.
    /// </summary>
    /// <remarks>
    /// This is the boundary between the build pipeline and an optional, externally-owned shader
    /// package. Implementations wrap that package via reflection so the project compiles whether or
    /// not it is installed; when it is absent they report <see cref="IsAvailable"/> = false and do
    /// nothing. See the <c>Integrations</c> folder for concrete implementations.
    /// </remarks>
    internal interface IUnusedSlotOptimizer
    {
        /// <summary>
        /// True if the backing shader package was found and can be invoked.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Clears, in place, the texture slots that are provably unused on the given material.
        /// Implementations decide which slots qualify and are expected to no-op on materials they
        /// do not recognize. <paramref name="animatedProperties"/> lists material property names
        /// driven by animation, which must never be treated as unused.
        /// </summary>
        /// <remarks>
        /// Implementations may only <em>clear</em> slots (set them to <c>null</c>), never rebind
        /// them to a different texture: <see cref="UnusedSlotPruner"/> restores protected slots
        /// and counts dropped textures by comparing against a pre-call snapshot, and both assume
        /// a slot is either untouched or nulled.
        /// </remarks>
        void ClearUnusedSlots(Material material, IReadOnlyCollection<string> animatedProperties);
    }
}
