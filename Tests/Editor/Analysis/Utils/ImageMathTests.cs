using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ImageMathTests
    {
        #region Sobel Gradient Tests

        [Test]
        public void CalculateSobelGradient_UniformImage_ReturnsZero()
        {
            // Uniform gray image should have zero gradient
            int width = 16;
            int height = 16;
            float[] grayscale = CreateUniformGrayscale(width, height, 0.5f);

            float gradient = ImageMath.CalculateSobelGradient(grayscale, width, height, width * height);

            Assert.That(gradient, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateSobelGradient_VerticalEdge_ReturnsPositiveValue()
        {
            // Image with vertical edge should have positive gradient
            int width = 16;
            int height = 16;
            float[] grayscale = CreateVerticalEdgeGrayscale(width, height);

            float gradient = ImageMath.CalculateSobelGradient(grayscale, width, height, width * height);

            Assert.That(gradient, Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateSobelGradient_EmptyImage_ReturnsZero()
        {
            float gradient = ImageMath.CalculateSobelGradient(new float[0], 0, 0, 0);
            Assert.AreEqual(0f, gradient);
        }

        #endregion

        #region Spatial Frequency Tests

        [Test]
        public void CalculateSpatialFrequency_UniformImage_ReturnsZero()
        {
            int width = 16;
            int height = 16;
            float[] grayscale = CreateUniformGrayscale(width, height, 0.5f);

            float frequency = ImageMath.CalculateSpatialFrequency(grayscale, width, height, width * height);

            Assert.That(frequency, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateSpatialFrequency_CheckerboardPattern_ReturnsHighValue()
        {
            int width = 16;
            int height = 16;
            float[] grayscale = CreateCheckerboardGrayscale(width, height);

            float frequency = ImageMath.CalculateSpatialFrequency(grayscale, width, height, width * height);

            Assert.That(frequency, Is.GreaterThan(0f));
        }

        #endregion

        #region Color Variance Tests

        [Test]
        public void CalculateColorVariance_UniformColor_ReturnsZero()
        {
            Color[] pixels = CreateUniformColorPixels(16, 16, new Color(0.5f, 0.5f, 0.5f, 1f));

            float variance = ImageMath.CalculateColorVariance(pixels, 256);

            Assert.That(variance, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateColorVariance_TwoColors_ReturnsPositiveValue()
        {
            Color[] pixels = new Color[100];
            for (int i = 0; i < 50; i++)
                pixels[i] = new Color(0f, 0f, 0f, 1f);
            for (int i = 50; i < 100; i++)
                pixels[i] = new Color(1f, 1f, 1f, 1f);

            float variance = ImageMath.CalculateColorVariance(pixels, 100);

            Assert.That(variance, Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateColorVariance_EmptyArray_ReturnsZero()
        {
            float variance = ImageMath.CalculateColorVariance(new Color[0], 0);
            Assert.AreEqual(0f, variance);
        }

        [Test]
        public void CalculateColorVariance_TransparentPixels_AreIgnored()
        {
            Color[] pixels = new Color[100];
            // 50 opaque black pixels
            for (int i = 0; i < 50; i++)
                pixels[i] = new Color(0f, 0f, 0f, 1f);
            // 50 transparent white pixels (should be ignored)
            for (int i = 50; i < 100; i++)
                pixels[i] = new Color(1f, 1f, 1f, 0.05f);

            float variance = ImageMath.CalculateColorVariance(pixels, 50);

            // Should be zero since all opaque pixels are the same color
            Assert.That(variance, Is.EqualTo(0f).Within(0.001f));
        }

        #endregion

        #region DCT Tests

        [Test]
        public void CalculateDctHighFrequencyRatio_UniformImage_ReturnsLowValue()
        {
            int width = 32;
            int height = 32;
            float[] grayscale = CreateUniformGrayscale(width, height, 0.5f);

            float ratio = ImageMath.CalculateDctHighFrequencyRatio(grayscale, width, height, width * height);

            // Uniform image should have low high-frequency content
            Assert.That(ratio, Is.LessThan(0.2f));
        }

        [Test]
        public void CalculateDctHighFrequencyRatio_NoiseImage_ReturnsHighValue()
        {
            int width = 32;
            int height = 32;
            float[] grayscale = CreateNoiseGrayscale(width, height, 42);

            float ratio = ImageMath.CalculateDctHighFrequencyRatio(grayscale, width, height, width * height);

            // Noise image should have high high-frequency content
            Assert.That(ratio, Is.GreaterThan(0.2f));
        }

        [Test]
        public void CalculateDctHighFrequencyRatio_TooSmallImage_ReturnsZero()
        {
            // Image smaller than DCT block size (8x8)
            float[] grayscale = new float[16]; // 4x4
            float ratio = ImageMath.CalculateDctHighFrequencyRatio(grayscale, 4, 4, 16);
            Assert.AreEqual(0f, ratio);
        }

        #endregion

        #region GLCM Tests

        [Test]
        public void CalculateGlcmFeatures_UniformImage_ReturnsExpectedValues()
        {
            int width = 16;
            int height = 16;
            float[] grayscale = CreateUniformGrayscale(width, height, 0.5f);

            var (contrast, homogeneity, energy) = ImageMath.CalculateGlcmFeatures(
                grayscale, width, height, width * height);

            // Uniform image: low contrast, high homogeneity, high energy
            Assert.That(contrast, Is.LessThan(1f));
            Assert.That(homogeneity, Is.GreaterThan(0.9f));
            Assert.That(energy, Is.GreaterThan(0.5f));
        }

        [Test]
        public void CalculateGlcmFeatures_EmptyImage_ReturnsDefaults()
        {
            var (contrast, homogeneity, energy) = ImageMath.CalculateGlcmFeatures(
                new float[0], 0, 0, 0);

            Assert.AreEqual(0f, contrast);
            Assert.AreEqual(1f, homogeneity);
            Assert.AreEqual(1f, energy);
        }

        #endregion

        #region Entropy Tests

        [Test]
        public void CalculateEntropy_UniformImage_ReturnsZero()
        {
            int width = 16;
            int height = 16;
            float[] grayscale = CreateUniformGrayscale(width, height, 0.5f);

            float entropy = ImageMath.CalculateEntropy(grayscale, width * height);

            // Uniform image has zero entropy (all same value)
            Assert.That(entropy, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateEntropy_TwoEqualGroups_ReturnsOne()
        {
            float[] grayscale = new float[256];
            for (int i = 0; i < 128; i++)
                grayscale[i] = 0f;
            for (int i = 128; i < 256; i++)
                grayscale[i] = 1f;

            float entropy = ImageMath.CalculateEntropy(grayscale, 256);

            // Two equally likely outcomes: entropy = 1 bit
            Assert.That(entropy, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void CalculateEntropy_EmptyArray_ReturnsZero()
        {
            float entropy = ImageMath.CalculateEntropy(new float[0], 0);
            Assert.AreEqual(0f, entropy);
        }

        #endregion

        #region Block Variance Tests

        [Test]
        public void CalculateBlockVariance_UniformImage_ReturnsZero()
        {
            int width = 32;
            int height = 32;
            float[] grayscale = CreateUniformGrayscale(width, height, 0.5f);

            float variance = ImageMath.CalculateBlockVariance(grayscale, width, height, width * height, 8);

            Assert.That(variance, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateBlockVariance_NoiseImage_ReturnsPositiveValue()
        {
            int width = 32;
            int height = 32;
            float[] grayscale = CreateNoiseGrayscale(width, height, 42);

            float variance = ImageMath.CalculateBlockVariance(grayscale, width, height, width * height, 8);

            Assert.That(variance, Is.GreaterThan(0f));
        }

        #endregion

        #region Helper Methods

        private static float[] CreateUniformGrayscale(int width, int height, float value)
        {
            float[] result = new float[width * height];
            for (int i = 0; i < result.Length; i++)
                result[i] = value;
            return result;
        }

        private static float[] CreateVerticalEdgeGrayscale(int width, int height)
        {
            float[] result = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y * width + x] = x < width / 2 ? 0f : 1f;
                }
            }
            return result;
        }

        private static float[] CreateCheckerboardGrayscale(int width, int height)
        {
            float[] result = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[y * width + x] = ((x + y) % 2 == 0) ? 0f : 1f;
                }
            }
            return result;
        }

        private static float[] CreateNoiseGrayscale(int width, int height, int seed)
        {
            float[] result = new float[width * height];
            System.Random random = new System.Random(seed);
            for (int i = 0; i < result.Length; i++)
                result[i] = (float)random.NextDouble();
            return result;
        }

        private static Color[] CreateUniformColorPixels(int width, int height, Color color)
        {
            Color[] result = new Color[width * height];
            for (int i = 0; i < result.Length; i++)
                result[i] = color;
            return result;
        }

        #endregion
    }
}
