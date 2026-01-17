using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Raw pixel data from a texture for analysis.
    /// </summary>
    public struct TexturePixelData
    {
        public Texture2D Texture;
        public Color[] Pixels;
        public int Width;
        public int Height;
        public bool IsNormalMap;
        public bool IsEmission;
    }

    /// <summary>
    /// Processed pixel data ready for complexity analysis.
    /// </summary>
    public struct ProcessedPixelData
    {
        public Color[] OpaquePixels;
        public float[] Grayscale;
        public int Width;
        public int Height;
        public int OpaqueCount;
        public bool IsNormalMap;
        public bool IsEmission;
    }
}
