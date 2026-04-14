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
        /// Analyzes a batch of textures and returns per-texture results.
        /// Implementations are responsible for their own pixel access strategy
        /// (CPU readback or direct GPU texture sampling).
        /// </summary>
        Dictionary<Texture2D, float> AnalyzeBatch(Dictionary<Texture2D, TextureInfo> textures);
    }
}
