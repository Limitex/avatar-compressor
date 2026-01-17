using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Provides memory calculation utilities for textures.
    /// </summary>
    public static class MemoryCalculator
    {
        /// <summary>
        /// Calculates compressed memory size based on format and mipmap count.
        /// </summary>
        public static long CalculateCompressedMemory(
            int width,
            int height,
            TextureFormat format,
            int mipmapCount
        )
        {
            float bitsPerPixel = GetBitsPerPixel(format);
            long bytes = 0;
            for (int index = 0; index < mipmapCount; ++index)
            {
                // Each mipmap level is 1/4 the size of previous: (width * height) / 4^index
                bytes += (long)
                    Mathf.RoundToInt(((width * height) >> (2 * index)) * bitsPerPixel / 8f);
            }
            return bytes;
        }

        /// <summary>
        /// Formats bytes to human-readable string.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / 1024f / 1024f:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024f:F2} KB";
            return $"{bytes} B";
        }

        /// <summary>
        /// Returns bits per pixel for the given texture format.
        /// </summary>
        public static float GetBitsPerPixel(TextureFormat format)
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
    }
}
