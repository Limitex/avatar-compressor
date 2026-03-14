using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
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
    }
}
