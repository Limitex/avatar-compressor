using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ProcessedPixelDataTests
    {
        #region Default Value Tests

        [Test]
        public void ProcessedPixelData_DefaultValues_OpaquePixelsNull()
        {
            var data = default(ProcessedPixelData);

            Assert.That(data.OpaquePixels, Is.Null);
        }

        [Test]
        public void ProcessedPixelData_DefaultValues_GrayscaleNull()
        {
            var data = default(ProcessedPixelData);

            Assert.That(data.Grayscale, Is.Null);
        }

        [Test]
        public void ProcessedPixelData_DefaultValues_DimensionsZero()
        {
            var data = default(ProcessedPixelData);

            Assert.That(data.Width, Is.EqualTo(0));
            Assert.That(data.Height, Is.EqualTo(0));
        }

        [Test]
        public void ProcessedPixelData_DefaultValues_OpaqueCountZero()
        {
            var data = default(ProcessedPixelData);

            Assert.That(data.OpaqueCount, Is.EqualTo(0));
        }

        [Test]
        public void ProcessedPixelData_DefaultValues_FlagsAreFalse()
        {
            var data = default(ProcessedPixelData);

            Assert.That(data.IsNormalMap, Is.False);
        }

        #endregion

        #region Complete Initialization Tests

        [Test]
        public void ProcessedPixelData_CompleteInitialization_AllFieldsSet()
        {
            var opaquePixels = new Color[64 * 64];
            var grayscale = new float[64 * 64];

            for (int i = 0; i < opaquePixels.Length; i++)
            {
                opaquePixels[i] = Color.gray;
                grayscale[i] = 0.5f;
            }

            var data = new ProcessedPixelData
            {
                OpaquePixels = opaquePixels,
                Grayscale = grayscale,
                Width = 64,
                Height = 64,
                OpaqueCount = 64 * 64,
                IsNormalMap = false,
            };

            Assert.That(data.OpaquePixels.Length, Is.EqualTo(64 * 64));
            Assert.That(data.Grayscale.Length, Is.EqualTo(64 * 64));
            Assert.That(data.Width, Is.EqualTo(64));
            Assert.That(data.Height, Is.EqualTo(64));
            Assert.That(data.OpaqueCount, Is.EqualTo(64 * 64));
            Assert.That(data.IsNormalMap, Is.False);
        }

        #endregion

        #region Struct Behavior Tests

        [Test]
        public void ProcessedPixelData_Assignment_CopiesValues()
        {
            var opaquePixels = new Color[] { Color.white };
            var grayscale = new float[] { 1.0f };

            var original = new ProcessedPixelData
            {
                OpaquePixels = opaquePixels,
                Grayscale = grayscale,
                Width = 1,
                Height = 1,
                OpaqueCount = 1,
                IsNormalMap = true,
            };
            var copy = original;

            Assert.That(copy.Width, Is.EqualTo(original.Width));
            Assert.That(copy.Height, Is.EqualTo(original.Height));
            Assert.That(copy.OpaqueCount, Is.EqualTo(original.OpaqueCount));
            Assert.That(copy.IsNormalMap, Is.EqualTo(original.IsNormalMap));
            // Arrays are reference types
            Assert.That(copy.OpaquePixels, Is.SameAs(original.OpaquePixels));
            Assert.That(copy.Grayscale, Is.SameAs(original.Grayscale));
        }

        #endregion
    }
}
