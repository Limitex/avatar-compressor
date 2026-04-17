using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Backend interface for texture analysis.
    /// Implementations may use CPU (thread pool) or GPU (compute shaders).
    /// </summary>
    public interface ITextureAnalysisBackend
    {
        /// <summary>
        /// Analyzes a batch of textures and returns per-texture raw complexity scores (0-1).
        /// Implementations are responsible for their own pixel access strategy
        /// (CPU readback or direct GPU texture sampling).
        /// </summary>
        /// <remarks>
        /// The returned dictionary's keys MUST be a subset of the input's keys.
        /// Implementations may drop entries they cannot analyze (e.g. Unity-destroyed
        /// references); for analysis failures on valid textures, return a default score
        /// rather than dropping the entry so callers can distinguish the two cases.
        /// </remarks>
        Dictionary<Texture2D, float> AnalyzeBatch(Dictionary<Texture2D, TextureInfo> textures);
    }
}
