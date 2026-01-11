using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Data structure for texture preview information in the editor UI.
    /// </summary>
    public struct TexturePreviewData
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
