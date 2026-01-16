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
        public Texture2D Texture;
        public string Guid;
        public float Complexity;
        public int RecommendedDivisor;
        public Vector2Int OriginalSize;
        public Vector2Int RecommendedSize;
        public string TextureType;
        public bool IsProcessed;
        public SkipReason SkipReason;
        public long OriginalMemory;
        public long EstimatedMemory;
        public bool IsNormalMap;
        public TextureFormat? PredictedFormat;
        public bool HasAlpha;
        public bool IsFrozen;
        public FrozenTextureSettings FrozenSettings;
    }
}
