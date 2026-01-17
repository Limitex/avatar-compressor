using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AlphaExtractorTests
    {
        #region ExtractOpaquePixels Tests

        [Test]
        public void ExtractOpaquePixels_AllOpaque_ExtractsAllPixels()
        {
            Color[] pixels = new Color[4]
            {
                new Color(1f, 0f, 0f, 1f),
                new Color(0f, 1f, 0f, 1f),
                new Color(0f, 0f, 1f, 1f),
                new Color(1f, 1f, 0f, 1f)
            };

            AlphaExtractor.ExtractOpaquePixels(pixels, 2, 2,
                out Color[] opaquePixels, out float[] grayscale, out int opaqueCount);

            Assert.AreEqual(4, opaqueCount);
            Assert.AreEqual(4, opaquePixels.Length);
            Assert.AreEqual(4, grayscale.Length);
        }

        [Test]
        public void ExtractOpaquePixels_AllTransparent_ExtractsNoPixels()
        {
            Color[] pixels = new Color[4]
            {
                new Color(1f, 0f, 0f, 0f),
                new Color(0f, 1f, 0f, 0.05f),
                new Color(0f, 0f, 1f, 0f),
                new Color(1f, 1f, 0f, 0.09f)
            };

            AlphaExtractor.ExtractOpaquePixels(pixels, 2, 2,
                out Color[] opaquePixels, out float[] grayscale, out int opaqueCount);

            Assert.AreEqual(0, opaqueCount);
            // All grayscale values should be TransparentMarker (-1)
            foreach (float g in grayscale)
            {
                Assert.That(g, Is.LessThan(0f));
            }
        }

        [Test]
        public void ExtractOpaquePixels_MixedAlpha_ExtractsOnlyOpaque()
        {
            Color[] pixels = new Color[4]
            {
                new Color(1f, 0f, 0f, 1f),     // Opaque
                new Color(0f, 1f, 0f, 0.05f),  // Transparent
                new Color(0f, 0f, 1f, 0.5f),   // Opaque (above threshold)
                new Color(1f, 1f, 0f, 0f)      // Transparent
            };

            AlphaExtractor.ExtractOpaquePixels(pixels, 2, 2,
                out Color[] opaquePixels, out float[] grayscale, out int opaqueCount);

            Assert.AreEqual(2, opaqueCount);
        }

        [Test]
        public void ExtractOpaquePixels_AtAlphaThreshold_IsOpaque()
        {
            Color[] pixels = new Color[1]
            {
                new Color(0.5f, 0.5f, 0.5f, 0.1f)  // Exactly at threshold
            };

            AlphaExtractor.ExtractOpaquePixels(pixels, 1, 1,
                out Color[] opaquePixels, out float[] grayscale, out int opaqueCount);

            Assert.AreEqual(1, opaqueCount);
        }

        [Test]
        public void ExtractOpaquePixels_JustBelowThreshold_IsTransparent()
        {
            Color[] pixels = new Color[1]
            {
                new Color(0.5f, 0.5f, 0.5f, 0.099f)  // Just below threshold
            };

            AlphaExtractor.ExtractOpaquePixels(pixels, 1, 1,
                out Color[] opaquePixels, out float[] grayscale, out int opaqueCount);

            Assert.AreEqual(0, opaqueCount);
        }

        [Test]
        public void ExtractOpaquePixels_GrayscaleCalculation_UsesLuminanceWeights()
        {
            // Red pixel: grayscale = 1 * 0.2126 = 0.2126
            Color[] redPixel = new Color[1] { new Color(1f, 0f, 0f, 1f) };
            AlphaExtractor.ExtractOpaquePixels(redPixel, 1, 1,
                out _, out float[] redGrayscale, out _);
            Assert.That(redGrayscale[0], Is.EqualTo(0.2126f).Within(0.001f));

            // Green pixel: grayscale = 1 * 0.7152 = 0.7152
            Color[] greenPixel = new Color[1] { new Color(0f, 1f, 0f, 1f) };
            AlphaExtractor.ExtractOpaquePixels(greenPixel, 1, 1,
                out _, out float[] greenGrayscale, out _);
            Assert.That(greenGrayscale[0], Is.EqualTo(0.7152f).Within(0.001f));

            // Blue pixel: grayscale = 1 * 0.0722 = 0.0722
            Color[] bluePixel = new Color[1] { new Color(0f, 0f, 1f, 1f) };
            AlphaExtractor.ExtractOpaquePixels(bluePixel, 1, 1,
                out _, out float[] blueGrayscale, out _);
            Assert.That(blueGrayscale[0], Is.EqualTo(0.0722f).Within(0.001f));
        }

        [Test]
        public void ExtractOpaquePixels_TransparentPixelsMarkedAsClear()
        {
            Color[] pixels = new Color[1]
            {
                new Color(1f, 0f, 0f, 0f)  // Transparent red
            };

            AlphaExtractor.ExtractOpaquePixels(pixels, 1, 1,
                out Color[] opaquePixels, out _, out _);

            Assert.AreEqual(Color.clear, opaquePixels[0]);
        }

        [Test]
        public void ExtractOpaquePixels_EmptyArray_ReturnsEmptyResults()
        {
            Color[] pixels = new Color[0];

            AlphaExtractor.ExtractOpaquePixels(pixels, 0, 0,
                out Color[] opaquePixels, out float[] grayscale, out int opaqueCount);

            Assert.AreEqual(0, opaqueCount);
            Assert.AreEqual(0, opaquePixels.Length);
            Assert.AreEqual(0, grayscale.Length);
        }

        #endregion

        #region IsTransparent Tests

        [Test]
        public void IsTransparent_NegativeValue_ReturnsTrue()
        {
            Assert.IsTrue(AlphaExtractor.IsTransparent(-1f));
            Assert.IsTrue(AlphaExtractor.IsTransparent(-0.5f));
            Assert.IsTrue(AlphaExtractor.IsTransparent(-0.001f));
        }

        [Test]
        public void IsTransparent_ZeroValue_ReturnsFalse()
        {
            Assert.IsFalse(AlphaExtractor.IsTransparent(0f));
        }

        [Test]
        public void IsTransparent_PositiveValue_ReturnsFalse()
        {
            Assert.IsFalse(AlphaExtractor.IsTransparent(0.5f));
            Assert.IsFalse(AlphaExtractor.IsTransparent(1f));
            Assert.IsFalse(AlphaExtractor.IsTransparent(0.001f));
        }

        #endregion

        #region ConvertToGrayscale Tests

        [Test]
        public void ConvertToGrayscale_WhitePixel_ReturnsOne()
        {
            Color[] pixels = new Color[1] { Color.white };
            float[] grayscale = AlphaExtractor.ConvertToGrayscale(pixels);

            Assert.That(grayscale[0], Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ConvertToGrayscale_BlackPixel_ReturnsZero()
        {
            Color[] pixels = new Color[1] { Color.black };
            float[] grayscale = AlphaExtractor.ConvertToGrayscale(pixels);

            Assert.AreEqual(0f, grayscale[0]);
        }

        [Test]
        public void ConvertToGrayscale_GrayPixel_ReturnsHalf()
        {
            Color[] pixels = new Color[1] { new Color(0.5f, 0.5f, 0.5f, 1f) };
            float[] grayscale = AlphaExtractor.ConvertToGrayscale(pixels);

            Assert.That(grayscale[0], Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void ConvertToGrayscale_MultiplePixels_ConvertsAll()
        {
            Color[] pixels = new Color[3]
            {
                Color.red,
                Color.green,
                Color.blue
            };

            float[] grayscale = AlphaExtractor.ConvertToGrayscale(pixels);

            Assert.AreEqual(3, grayscale.Length);
            Assert.That(grayscale[0], Is.EqualTo(0.2126f).Within(0.001f));  // Red
            Assert.That(grayscale[1], Is.EqualTo(0.7152f).Within(0.001f));  // Green
            Assert.That(grayscale[2], Is.EqualTo(0.0722f).Within(0.001f));  // Blue
        }

        [Test]
        public void ConvertToGrayscale_EmptyArray_ReturnsEmptyArray()
        {
            Color[] pixels = new Color[0];
            float[] grayscale = AlphaExtractor.ConvertToGrayscale(pixels);

            Assert.AreEqual(0, grayscale.Length);
        }

        [Test]
        public void ConvertToGrayscale_IgnoresAlphaChannel()
        {
            Color[] pixels = new Color[2]
            {
                new Color(0.5f, 0.5f, 0.5f, 1f),   // Fully opaque
                new Color(0.5f, 0.5f, 0.5f, 0f)    // Fully transparent
            };

            float[] grayscale = AlphaExtractor.ConvertToGrayscale(pixels);

            // Both should have same grayscale value
            Assert.AreEqual(grayscale[0], grayscale[1]);
        }

        #endregion
    }
}
