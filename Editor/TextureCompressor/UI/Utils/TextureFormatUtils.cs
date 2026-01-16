using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.ui
{
    /// <summary>
    /// Provides texture format display utilities.
    /// </summary>
    public static class TextureFormatUtils
    {
        /// <summary>
        /// Gets a user-friendly display name for a texture format.
        /// </summary>
        public static string GetDisplayName(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.DXT1 => "DXT1",
                TextureFormat.DXT5 => "DXT5",
                TextureFormat.BC5 => "BC5",
                TextureFormat.BC7 => "BC7",
                TextureFormat.ASTC_4x4 => "ASTC 4x4",
                TextureFormat.ASTC_6x6 => "ASTC 6x6",
                TextureFormat.ASTC_8x8 => "ASTC 8x8",
                _ => format.ToString()
            };
        }

        /// <summary>
        /// Gets additional info about a texture format (bpp, quality, use case).
        /// </summary>
        public static string GetInfo(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.DXT1 => "4 bpp, RGB only, fastest",
                TextureFormat.DXT5 => "8 bpp, RGBA, good quality",
                TextureFormat.BC5 => "8 bpp, normal maps",
                TextureFormat.BC7 => "8 bpp, highest quality",
                TextureFormat.ASTC_4x4 => "8 bpp, highest quality",
                TextureFormat.ASTC_6x6 => "3.56 bpp, balanced",
                TextureFormat.ASTC_8x8 => "2 bpp, most efficient",
                _ => ""
            };
        }

        /// <summary>
        /// Gets a color to represent the format quality/efficiency.
        /// </summary>
        public static Color GetColor(TextureFormat format)
        {
            return format switch
            {
                // High quality formats - green
                TextureFormat.BC7 => new Color(0.2f, 0.8f, 0.4f),
                TextureFormat.ASTC_4x4 => new Color(0.2f, 0.8f, 0.4f),

                // Normal map formats - cyan
                TextureFormat.BC5 => new Color(0.2f, 0.7f, 0.9f),

                // Balanced formats - yellow
                TextureFormat.DXT5 => new Color(0.9f, 0.8f, 0.2f),
                TextureFormat.ASTC_6x6 => new Color(0.9f, 0.8f, 0.2f),

                // Efficient formats - orange
                TextureFormat.DXT1 => new Color(0.9f, 0.6f, 0.2f),
                TextureFormat.ASTC_8x8 => new Color(0.9f, 0.6f, 0.2f),

                _ => Color.white
            };
        }
    }
}
