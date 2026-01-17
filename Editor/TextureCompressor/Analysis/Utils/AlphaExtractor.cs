using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Utility for extracting opaque pixels and converting to grayscale.
    /// Preserves 2D structure for spatial analysis algorithms.
    /// </summary>
    public static class AlphaExtractor
    {
        private const float AlphaThreshold = 0.1f;
        private const float TransparentMarker = -1f;

        /// <summary>
        /// Extracts opaque pixels while preserving 2D structure.
        /// Transparent pixels are marked with TransparentMarker (-1) in grayscale array.
        /// </summary>
        public static void ExtractOpaquePixels(Color[] pixels, int width, int height,
            out Color[] opaquePixels, out float[] grayscale, out int opaqueCount)
        {
            opaquePixels = new Color[pixels.Length];
            grayscale = new float[pixels.Length];
            opaqueCount = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a >= AlphaThreshold)
                {
                    opaquePixels[i] = pixels[i];
                    grayscale[i] = pixels[i].r * 0.2126f +
                                   pixels[i].g * 0.7152f +
                                   pixels[i].b * 0.0722f;
                    opaqueCount++;
                }
                else
                {
                    opaquePixels[i] = Color.clear;
                    grayscale[i] = TransparentMarker;
                }
            }
        }

        /// <summary>
        /// Checks if a grayscale value represents a transparent pixel.
        /// </summary>
        public static bool IsTransparent(float grayscaleValue)
        {
            return grayscaleValue < 0f;
        }

        /// <summary>
        /// Converts pixel array to grayscale.
        /// </summary>
        public static float[] ConvertToGrayscale(Color[] pixels)
        {
            float[] grayscale = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                grayscale[i] = pixels[i].r * 0.2126f +
                               pixels[i].g * 0.7152f +
                               pixels[i].b * 0.0722f;
            }
            return grayscale;
        }
    }
}
