using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TexturePixelDataTests
    {
        #region Default Value Tests

        [Test]
        public void TexturePixelData_DefaultValues_TextureNull()
        {
            var data = default(TexturePixelData);

            Assert.That(data.Texture, Is.Null);
        }

        [Test]
        public void TexturePixelData_DefaultValues_PixelsNull()
        {
            var data = default(TexturePixelData);

            Assert.That(data.Pixels, Is.Null);
        }

        [Test]
        public void TexturePixelData_DefaultValues_DimensionsZero()
        {
            var data = default(TexturePixelData);

            Assert.That(data.Width, Is.EqualTo(0));
            Assert.That(data.Height, Is.EqualTo(0));
        }

        [Test]
        public void TexturePixelData_DefaultValues_FlagsAreFalse()
        {
            var data = default(TexturePixelData);

            Assert.That(data.IsNormalMap, Is.False);
            Assert.That(data.IsEmission, Is.False);
        }

        #endregion

        #region Field Assignment Tests

        [Test]
        public void TexturePixelData_SetWidth_Works()
        {
            var data = new TexturePixelData { Width = 512 };

            Assert.That(data.Width, Is.EqualTo(512));
        }

        [Test]
        public void TexturePixelData_SetHeight_Works()
        {
            var data = new TexturePixelData { Height = 256 };

            Assert.That(data.Height, Is.EqualTo(256));
        }

        [Test]
        public void TexturePixelData_SetPixels_Works()
        {
            var pixels = new Color[] { Color.red, Color.green, Color.blue };
            var data = new TexturePixelData { Pixels = pixels };

            Assert.That(data.Pixels, Is.EqualTo(pixels));
            Assert.That(data.Pixels.Length, Is.EqualTo(3));
        }

        [Test]
        public void TexturePixelData_SetIsNormalMap_Works()
        {
            var data = new TexturePixelData { IsNormalMap = true };

            Assert.That(data.IsNormalMap, Is.True);
        }

        [Test]
        public void TexturePixelData_SetIsEmission_Works()
        {
            var data = new TexturePixelData { IsEmission = true };

            Assert.That(data.IsEmission, Is.True);
        }

        #endregion

        #region Struct Behavior Tests

        [Test]
        public void TexturePixelData_Assignment_CopiesValues()
        {
            var pixels = new Color[] { Color.white };
            var original = new TexturePixelData
            {
                Pixels = pixels,
                Width = 64,
                Height = 64,
                IsNormalMap = true,
                IsEmission = false
            };
            var copy = original;

            Assert.That(copy.Width, Is.EqualTo(original.Width));
            Assert.That(copy.Height, Is.EqualTo(original.Height));
            Assert.That(copy.IsNormalMap, Is.EqualTo(original.IsNormalMap));
            Assert.That(copy.IsEmission, Is.EqualTo(original.IsEmission));
            // Note: Pixels array is a reference type, so it's the same reference
            Assert.That(copy.Pixels, Is.SameAs(original.Pixels));
        }

        #endregion
    }

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
            Assert.That(data.IsEmission, Is.False);
        }

        #endregion

        #region Field Assignment Tests

        [Test]
        public void ProcessedPixelData_SetOpaquePixels_Works()
        {
            var pixels = new Color[] { Color.red, Color.green };
            var data = new ProcessedPixelData { OpaquePixels = pixels };

            Assert.That(data.OpaquePixels, Is.EqualTo(pixels));
        }

        [Test]
        public void ProcessedPixelData_SetGrayscale_Works()
        {
            var grayscale = new float[] { 0.5f, 0.75f, 1.0f };
            var data = new ProcessedPixelData { Grayscale = grayscale };

            Assert.That(data.Grayscale, Is.EqualTo(grayscale));
        }

        [Test]
        public void ProcessedPixelData_SetDimensions_Works()
        {
            var data = new ProcessedPixelData { Width = 128, Height = 64 };

            Assert.That(data.Width, Is.EqualTo(128));
            Assert.That(data.Height, Is.EqualTo(64));
        }

        [Test]
        public void ProcessedPixelData_SetOpaqueCount_Works()
        {
            var data = new ProcessedPixelData { OpaqueCount = 1000 };

            Assert.That(data.OpaqueCount, Is.EqualTo(1000));
        }

        [Test]
        public void ProcessedPixelData_SetIsNormalMap_Works()
        {
            var data = new ProcessedPixelData { IsNormalMap = true };

            Assert.That(data.IsNormalMap, Is.True);
        }

        [Test]
        public void ProcessedPixelData_SetIsEmission_Works()
        {
            var data = new ProcessedPixelData { IsEmission = true };

            Assert.That(data.IsEmission, Is.True);
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
                IsEmission = true
            };

            Assert.That(data.OpaquePixels.Length, Is.EqualTo(64 * 64));
            Assert.That(data.Grayscale.Length, Is.EqualTo(64 * 64));
            Assert.That(data.Width, Is.EqualTo(64));
            Assert.That(data.Height, Is.EqualTo(64));
            Assert.That(data.OpaqueCount, Is.EqualTo(64 * 64));
            Assert.That(data.IsNormalMap, Is.False);
            Assert.That(data.IsEmission, Is.True);
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
                IsEmission = false
            };
            var copy = original;

            Assert.That(copy.Width, Is.EqualTo(original.Width));
            Assert.That(copy.Height, Is.EqualTo(original.Height));
            Assert.That(copy.OpaqueCount, Is.EqualTo(original.OpaqueCount));
            Assert.That(copy.IsNormalMap, Is.EqualTo(original.IsNormalMap));
            Assert.That(copy.IsEmission, Is.EqualTo(original.IsEmission));
            // Arrays are reference types
            Assert.That(copy.OpaquePixels, Is.SameAs(original.OpaquePixels));
            Assert.That(copy.Grayscale, Is.SameAs(original.Grayscale));
        }

        #endregion
    }
}
