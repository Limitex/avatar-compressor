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
        /// Samples pixels if the texture exceeds the maximum sample size.
        /// </summary>
        public static void SampleIfNeeded(Color[] pixels, int width, int height,
            out Color[] sampledPixels, out int sampledWidth, out int sampledHeight)
        {
            int totalPixels = width * height;

            if (totalPixels <= MaxSampledPixels)
            {
                sampledPixels = pixels;
                sampledWidth = width;
                sampledHeight = height;
                return;
            }

            float ratio = Mathf.Sqrt((float)MaxSampledPixels / totalPixels);
            sampledWidth = Mathf.Max(AnalysisConstants.MinSampledDimension, (int)(width * ratio));
            sampledHeight = Mathf.Max(AnalysisConstants.MinSampledDimension, (int)(height * ratio));

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
