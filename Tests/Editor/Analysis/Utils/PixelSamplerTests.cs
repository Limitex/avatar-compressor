using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PixelSamplerTests
    {
        #region SampleIfNeeded Tests - No Sampling Required

        [Test]
        public void SampleIfNeeded_SmallTexture_ReturnsOriginal()
        {
            int width = 64;
            int height = 64;
            Color[] pixels = CreateUniformPixels(width, height, Color.red);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreSame(pixels, sampledPixels);
            Assert.AreEqual(width, sampledWidth);
            Assert.AreEqual(height, sampledHeight);
        }

        [Test]
        public void SampleIfNeeded_ExactlyAtLimit_ReturnsOriginal()
        {
            // MaxSampledPixels = 262144 (512x512)
            int width = 512;
            int height = 512;
            Color[] pixels = CreateUniformPixels(width, height, Color.blue);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreSame(pixels, sampledPixels);
            Assert.AreEqual(width, sampledWidth);
            Assert.AreEqual(height, sampledHeight);
        }

        [Test]
        public void SampleIfNeeded_BelowLimit_ReturnsOriginal()
        {
            int width = 256;
            int height = 256;
            Color[] pixels = CreateUniformPixels(width, height, Color.green);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreSame(pixels, sampledPixels);
            Assert.AreEqual(width, sampledWidth);
            Assert.AreEqual(height, sampledHeight);
        }

        #endregion

        #region SampleIfNeeded Tests - Sampling Required

        [Test]
        public void SampleIfNeeded_LargeTexture_ReducesDimensions()
        {
            int width = 1024;
            int height = 1024;
            Color[] pixels = CreateUniformPixels(width, height, Color.white);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreNotSame(pixels, sampledPixels);
            Assert.Less(sampledWidth, width);
            Assert.Less(sampledHeight, height);
            Assert.LessOrEqual(sampledWidth * sampledHeight, AnalysisConstants.MaxSampledPixels);
        }

        [Test]
        public void SampleIfNeeded_LargeTexture_MaintainsAspectRatio()
        {
            int width = 2048;
            int height = 1024;
            Color[] pixels = CreateUniformPixels(width, height, Color.white);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            float originalRatio = (float)width / height;
            float sampledRatio = (float)sampledWidth / sampledHeight;

            // Allow some tolerance due to integer rounding
            Assert.That(sampledRatio, Is.EqualTo(originalRatio).Within(0.2f));
        }

        [Test]
        public void SampleIfNeeded_VeryLargeTexture_RespectsMinDimension()
        {
            // Very large texture that would shrink below minimum
            int width = 4096;
            int height = 4096;
            Color[] pixels = CreateUniformPixels(width, height, Color.white);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.GreaterOrEqual(sampledWidth, AnalysisConstants.MinSampledDimension);
            Assert.GreaterOrEqual(sampledHeight, AnalysisConstants.MinSampledDimension);
        }

        [Test]
        public void SampleIfNeeded_LargeTexture_SamplesCorrectPixelCount()
        {
            int width = 1024;
            int height = 1024;
            Color[] pixels = CreateUniformPixels(width, height, Color.red);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreEqual(sampledWidth * sampledHeight, sampledPixels.Length);
        }

        [Test]
        public void SampleIfNeeded_JustAboveLimit_PerformsSampling()
        {
            // Just above limit (513x512 = 262656 > 262144)
            int width = 513;
            int height = 512;
            Color[] pixels = CreateUniformPixels(width, height, Color.cyan);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreNotSame(pixels, sampledPixels);
        }

        #endregion

        #region SampleIfNeeded Tests - Pixel Value Preservation

        [Test]
        public void SampleIfNeeded_SamplesFromCorrectPositions()
        {
            int width = 1024;
            int height = 1024;
            Color[] pixels = new Color[width * height];

            // Create gradient pattern - top-left is red, bottom-right is blue
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = (float)x / width;
                    float b = (float)y / height;
                    pixels[y * width + x] = new Color(r, 0f, b, 1f);
                }
            }

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            // Top-left corner should be close to (0, 0, 0)
            Color topLeft = sampledPixels[0];
            Assert.That(topLeft.r, Is.LessThan(0.1f));
            Assert.That(topLeft.b, Is.LessThan(0.1f));

            // Bottom-right corner should be close to (1, 0, 1)
            Color bottomRight = sampledPixels[sampledHeight * sampledWidth - 1];
            Assert.That(bottomRight.r, Is.GreaterThan(0.9f));
            Assert.That(bottomRight.b, Is.GreaterThan(0.9f));
        }

        [Test]
        public void SampleIfNeeded_UniformColor_PreservesColor()
        {
            int width = 1024;
            int height = 1024;
            Color testColor = new Color(0.5f, 0.3f, 0.7f, 1f);
            Color[] pixels = CreateUniformPixels(width, height, testColor);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            // All sampled pixels should be the same color
            foreach (Color pixel in sampledPixels)
            {
                Assert.AreEqual(testColor, pixel);
            }
        }

        #endregion

        #region SampleIfNeeded Tests - Edge Cases

        [Test]
        public void SampleIfNeeded_NonSquareTexture_HandlesCorrectly()
        {
            int width = 2048;
            int height = 512;
            Color[] pixels = CreateUniformPixels(width, height, Color.magenta);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreNotSame(pixels, sampledPixels);
            Assert.LessOrEqual(sampledWidth * sampledHeight, AnalysisConstants.MaxSampledPixels);
        }

        [Test]
        public void SampleIfNeeded_TallTexture_HandlesCorrectly()
        {
            int width = 256;
            int height = 2048;
            Color[] pixels = CreateUniformPixels(width, height, Color.yellow);

            PixelSampler.SampleIfNeeded(pixels, width, height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

            Assert.AreNotSame(pixels, sampledPixels);
            Assert.LessOrEqual(sampledWidth * sampledHeight, AnalysisConstants.MaxSampledPixels);
        }

        #endregion

        #region Helper Methods

        private static Color[] CreateUniformPixels(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            return pixels;
        }

        #endregion
    }
}
