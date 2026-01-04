using System.Collections.Generic;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Format options for frozen textures.
    /// </summary>
    public enum FrozenTextureFormat
    {
        Auto,       // Use automatic format selection
        DXT1,       // RGB only, 4 bpp (Desktop)
        DXT5,       // RGBA, 8 bpp (Desktop)
        BC5,        // Normal maps, 8 bpp (Desktop)
        BC7,        // Highest quality, 8 bpp (Desktop)
        ASTC_4x4,   // Mobile highest quality, 8 bpp
        ASTC_6x6,   // Mobile balanced, 3.56 bpp
        ASTC_8x8    // Mobile efficient, 2 bpp
    }

    /// <summary>
    /// Settings for a frozen texture with user-specified compression overrides.
    /// </summary>
    [System.Serializable]
    public class FrozenTextureSettings
    {
        /// <summary>
        /// Asset path of the texture (stable identifier).
        /// </summary>
        public string TexturePath;

        /// <summary>
        /// Resolution divisor (1 = no reduction, 2 = half, 4 = quarter, etc.)
        /// </summary>
        public int Divisor = 1;

        /// <summary>
        /// Compression format override.
        /// </summary>
        public FrozenTextureFormat Format = FrozenTextureFormat.Auto;

        /// <summary>
        /// If true, completely skip compression for this texture.
        /// </summary>
        public bool Skip = false;

        public FrozenTextureSettings()
        {
        }

        public FrozenTextureSettings(string texturePath)
        {
            TexturePath = texturePath;
        }

        public FrozenTextureSettings(string texturePath, int divisor, FrozenTextureFormat format, bool skip)
        {
            TexturePath = texturePath;
            Divisor = divisor;
            Format = format;
            Skip = skip;
        }
    }
}
