using dev.limitex.avatar.compressor.texture;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.ui
{
    /// <summary>
    /// Data class for texture preview display.
    /// Uses class instead of struct because it contains reference types (FrozenTextureSettings).
    /// </summary>
    public class TexturePreviewData
    {
        public Texture2D Texture { get; set; }
        public string Guid { get; set; }
        public float Complexity { get; set; }
        public int RecommendedDivisor { get; set; }
        public Vector2Int OriginalSize { get; set; }
        public Vector2Int RecommendedSize { get; set; }
        public string TextureType { get; set; }
        public bool IsProcessed { get; set; }
        public SkipReason SkipReason { get; set; }
        public long OriginalMemory { get; set; }
        public long EstimatedMemory { get; set; }
        public bool IsNormalMap { get; set; }
        public TextureFormat? PredictedFormat { get; set; }
        public bool HasAlpha { get; set; }
        public bool IsFrozen { get; set; }
        public FrozenTextureSettings FrozenSettings { get; set; }
    }
}
