using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Utility for sampling pixels from large textures.
    /// </summary>
    public static class PixelSampler
    {
        private const int MaxSampledPixels = AnalysisConstants.MaxSampledPixels;

        /// <summary>
        /// Calculates the sampled dimensions for a texture.
        /// Used by both CPU (SampleIfNeeded) and GPU (GpuAnalysisBackend) paths
        /// to ensure identical sampling behavior.
        /// </summary>
        public static (int Width, int Height) CalculateSampledDimensions(int width, int height)
        {
            int totalPixels = width * height;

            if (totalPixels <= MaxSampledPixels)
            {
                return (width, height);
            }

            float ratio = Mathf.Sqrt((float)MaxSampledPixels / totalPixels);
            int sampledWidth = Mathf.Max(
                AnalysisConstants.MinSampledDimension,
                (int)(width * ratio)
            );
            int sampledHeight = Mathf.Max(
                AnalysisConstants.MinSampledDimension,
                (int)(height * ratio)
            );
            return (sampledWidth, sampledHeight);
        }

        /// <summary>
        /// Samples pixels if the texture exceeds the maximum sample size.
        /// </summary>
        public static void SampleIfNeeded(
            Color[] pixels,
            int width,
            int height,
            out Color[] sampledPixels,
            out int sampledWidth,
            out int sampledHeight
        )
        {
            var (sw, sh) = CalculateSampledDimensions(width, height);
            sampledWidth = sw;
            sampledHeight = sh;

            if (sampledWidth == width && sampledHeight == height)
            {
                sampledPixels = pixels;
                return;
            }

            sampledPixels = new Color[sampledWidth * sampledHeight];

            float xStep = (float)width / sampledWidth;
            float yStep = (float)height / sampledHeight;

            for (int y = 0; y < sampledHeight; y++)
            {
                for (int x = 0; x < sampledWidth; x++)
                {
                    int srcX = Mathf.Min((int)(x * xStep), width - 1);
                    int srcY = Mathf.Min((int)(y * yStep), height - 1);
                    sampledPixels[y * sampledWidth + x] = pixels[srcY * width + srcX];
                }
            }
        }
    }
}
