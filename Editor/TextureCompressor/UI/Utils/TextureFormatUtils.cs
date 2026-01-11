using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Utility methods for texture format display and memory estimation.
    /// </summary>
    public static class TextureFormatUtils
    {
        /// <summary>
        /// Estimates compressed memory size based on target format.
        /// </summary>
        public static long EstimateCompressedMemory(int width, int height, TextureFormat format)
        {
            float bitsPerPixel = GetBitsPerPixel(format);
            return (long)(width * height * bitsPerPixel / 8f);
        }

        /// <summary>
        /// Returns bits per pixel for the given texture format.
        /// </summary>
        private static float GetBitsPerPixel(TextureFormat format)
        {
            switch (format)
            {
                // DXT/BC formats (Desktop)
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                    return 4f;
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC5:
                case TextureFormat.BC7:
                    return 8f;
                case TextureFormat.BC4:
                    return 4f;
                case TextureFormat.BC6H:
                    return 8f;

                // ASTC formats (Mobile)
                case TextureFormat.ASTC_4x4:
                    return 8f;
                case TextureFormat.ASTC_5x5:
                    return 5.12f;
                case TextureFormat.ASTC_6x6:
                    return 3.56f;
                case TextureFormat.ASTC_8x8:
                    return 2f;
                case TextureFormat.ASTC_10x10:
                    return 1.28f;
                case TextureFormat.ASTC_12x12:
                    return 0.89f;

                // Uncompressed formats
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    return 32f;
                case TextureFormat.RGB24:
                    return 24f;
                case TextureFormat.RGB565:
                case TextureFormat.RGBA4444:
                case TextureFormat.ARGB4444:
                    return 16f;

                default:
                    return 32f; // Assume uncompressed RGBA
            }
        }

        /// <summary>
        /// Gets a user-friendly display name for a texture format.
        /// </summary>
        public static string GetFormatDisplayName(TextureFormat format)
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
        public static string GetFormatInfo(TextureFormat format)
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
        public static Color GetFormatColor(TextureFormat format)
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
