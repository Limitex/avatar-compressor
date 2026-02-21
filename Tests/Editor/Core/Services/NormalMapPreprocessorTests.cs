using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class NormalMapPreprocessorTests
    {
        private NormalMapPreprocessor _preprocessor;
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _preprocessor = new NormalMapPreprocessor();
            _createdObjects = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        #region PrepareForCompression - Null/Edge Cases

        [Test]
        public void PrepareForCompression_NullTexture_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _preprocessor.PrepareForCompression(null, TextureFormat.RGBA32, TextureFormat.BC5)
            );
        }

        [Test]
        public void PrepareForCompression_SameSourceAndTarget_StillProcesses()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.BC5,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.RG
            );

            var pixels = texture.GetPixels32();
            // Flat normal: X=0 -> R=128, Y=0 -> G=128
            Assert.That(pixels[0].r, Is.InRange((byte)126, (byte)130));
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130));
        }

        #endregion

        #region PrepareForCompression - RGB to BC5 (RG layout)

        [Test]
        public void PrepareForCompression_RGBToBC5_WritesXYToRG()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            // Flat normal (0,0,1): X=0 -> R=128, Y=0 -> G=128, A=255
            Assert.That(pixels[0].r, Is.InRange((byte)126, (byte)130));
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130));
            Assert.AreEqual(255, pixels[0].a);
        }

        [Test]
        public void PrepareForCompression_RGBToBC5_TiltedNormal_PreservesDirection()
        {
            var texture = CreateSinglePixelTexture(192, 192, 255, 255); // tilted normal
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            // X and Y should be positive (> 128)
            Assert.That(pixels[0].r, Is.GreaterThan((byte)128));
            Assert.That(pixels[0].g, Is.GreaterThan((byte)128));
        }

        #endregion

        #region PrepareForCompression - RGB to DXT5 (AG layout)

        [Test]
        public void PrepareForCompression_RGBToDXT5_WritesXToA_YToG()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.DXT5);

            var pixels = texture.GetPixels32();
            // Flat normal: X=0 -> A=128, Y=0 -> G=128, R=255, B=255
            Assert.That(pixels[0].a, Is.InRange((byte)126, (byte)130));
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130));
            Assert.AreEqual(255, pixels[0].r);
            Assert.AreEqual(255, pixels[0].b);
        }

        #endregion

        #region PrepareForCompression - RGB to BC7 (AG layout, no preserve alpha)

        [Test]
        public void PrepareForCompression_RGBToBC7_Default_WritesAGLayout()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: false
            );

            var pixels = texture.GetPixels32();
            // AG layout: X=0 -> A=128, Y=0 -> G=128, R=255, B=255
            Assert.That(pixels[0].a, Is.InRange((byte)126, (byte)130));
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130));
            Assert.AreEqual(255, pixels[0].r);
            Assert.AreEqual(255, pixels[0].b);
        }

        #endregion

        #region PrepareForCompression - RGB to BC7 (RGB layout, preserve alpha)

        [Test]
        public void PrepareForCompression_RGBToBC7_PreserveAlpha_WritesRGBLayout()
        {
            var texture = NormalMapTestTextureFactory.CreateWithAlpha(8);
            _createdObjects.Add(texture);

            var originalPixels = texture.GetPixels32();
            byte originalAlpha = originalPixels[0].a;

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true
            );

            var pixels = texture.GetPixels32();
            // RGB layout with alpha preservation: alpha should be preserved
            Assert.AreEqual(originalAlpha, pixels[0].a);
            // XY should be in RG
            Assert.That(pixels[0].r, Is.InRange((byte)126, (byte)130)); // X=0
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130)); // Y=0
        }

        #endregion

        #region PrepareForCompression - AG to BC5 (cross-layout conversion)

        [Test]
        public void PrepareForCompression_AGToBC5_ReadsFromAG_WritesToRG()
        {
            var texture = NormalMapTestTextureFactory.CreateFlatAG(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.DXT5,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.AG
            );

            var pixels = texture.GetPixels32();
            // After conversion: flat normal in RG layout
            Assert.That(pixels[0].r, Is.InRange((byte)126, (byte)130)); // X=0
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130)); // Y=0
        }

        #endregion

        #region PrepareForCompression - RG to DXT5 (cross-layout conversion)

        [Test]
        public void PrepareForCompression_RGToDXT5_ReadsFromRG_WritesToAG()
        {
            var texture = NormalMapTestTextureFactory.CreateFlatRG(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.BC5,
                TextureFormat.DXT5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.RG
            );

            var pixels = texture.GetPixels32();
            // After conversion: flat normal in AG layout
            Assert.That(pixels[0].a, Is.InRange((byte)126, (byte)130)); // X=0
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130)); // Y=0
            Assert.AreEqual(255, pixels[0].r);
            Assert.AreEqual(255, pixels[0].b);
        }

        #endregion

        #region PrepareForCompression - Normalization

        [Test]
        public void PrepareForCompression_NormalizesOutputVectors()
        {
            var texture = NormalMapTestTextureFactory.CreateSphere(16);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            for (int i = 0; i < Mathf.Min(pixels.Length, 100); i++)
            {
                float x = (pixels[i].r / 255f) * 2f - 1f;
                float y = (pixels[i].g / 255f) * 2f - 1f;
                float z = (pixels[i].b / 255f) * 2f - 1f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                // Allow tolerance for 8-bit quantization
                Assert.That(
                    length,
                    Is.InRange(0.85f, 1.15f),
                    $"Pixel {i}: length={length} (x={x}, y={y}, z={z})"
                );
            }
        }

        [Test]
        public void PrepareForCompression_DegenerateVector_ResetsToFlatNormal()
        {
            // Create texture with (128, 128, 128) = vector (0, 0, 0)
            var texture = NormalMapTestTextureFactory.CreateDegenerate(4);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            // Degenerate (0,0,0) should reset to flat normal (0, 0, 1) -> (128, 128, 255)
            Assert.That(pixels[0].r, Is.InRange((byte)126, (byte)130)); // X=0
            Assert.That(pixels[0].g, Is.InRange((byte)126, (byte)130)); // Y=0
            Assert.That(pixels[0].b, Is.GreaterThan((byte)200)); // Z close to 1
        }

        #endregion

        #region PrepareForCompression - Object Space Z Sign

        [Test]
        public void PrepareForCompression_NegativeZ_PreservesSign()
        {
            var texture = NormalMapTestTextureFactory.CreateNegativeZ(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            float z = (pixels[0].b / 255f) * 2f - 1f;
            // Z should be negative (from RGB source with negative Z)
            Assert.That(z, Is.LessThan(0f));
        }

        [Test]
        public void PrepareForCompression_AGSource_AssumesPositiveZ()
        {
            var texture = NormalMapTestTextureFactory.CreateFlatAG(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.DXT5,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.AG
            );

            var pixels = texture.GetPixels32();
            float z = (pixels[0].b / 255f) * 2f - 1f;
            // AG sources have no Z channel, should assume positive
            Assert.That(z, Is.GreaterThan(0f));
        }

        [Test]
        public void PrepareForCompression_RGBSource_PreservesPositiveZ()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            float z = (pixels[0].b / 255f) * 2f - 1f;
            Assert.That(z, Is.GreaterThan(0.9f), "Positive Z from flat normal should be preserved");
        }

        #endregion

        #region PrepareForCompression - Format-based Auto Detection

        [Test]
        public void PrepareForCompression_FromBC7_ReadsFromAGChannels()
        {
            // BC7 source auto-detects as AG layout (DXTnm)
            // Create texture with normal data in AG: X>0 in A=192, Y>0 in G=192
            var texture = CreateSinglePixelTexture(255, 192, 255, 192);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC7, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            // Should read X from A=192 (positive) and Y from G=192 (positive)
            Assert.That(
                pixels[0].r,
                Is.GreaterThan((byte)128),
                "X should be positive (read from A)"
            );
            Assert.That(
                pixels[0].g,
                Is.GreaterThan((byte)128),
                "Y should be positive (read from G)"
            );
        }

        [Test]
        public void PrepareForCompression_FromDXT5Crunched_ReadsFromAGChannels()
        {
            // DXT5Crunched source auto-detects as AG layout
            var texture = CreateSinglePixelTexture(255, 192, 255, 192);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.DXT5Crunched,
                TextureFormat.BC5
            );

            var pixels = texture.GetPixels32();
            Assert.That(
                pixels[0].r,
                Is.GreaterThan((byte)128),
                "X should be positive (read from A)"
            );
            Assert.That(
                pixels[0].g,
                Is.GreaterThan((byte)128),
                "Y should be positive (read from G)"
            );
        }

        [Test]
        public void PrepareForCompression_FromBC5_RecalculatesZChannel()
        {
            // BC5 stores only RG; Z should be recalculated from unit sphere constraint
            var texture = NormalMapTestTextureFactory.CreateFlatRG(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.BC5,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.RG
            );

            var pixels = texture.GetPixels32();
            float z = (pixels[0].b / 255f) * 2f - 1f;
            // Flat normal (X=0, Y=0) -> Z should be sqrt(1-0-0) = 1
            Assert.That(
                z,
                Is.GreaterThan(0.9f),
                "Z should be recalculated to ~1.0 for flat normal"
            );
        }

        [Test]
        public void PrepareForCompression_FromDXTnm_FlatNormal_ProducesCorrectOutput()
        {
            // DXT5 (DXTnm) flat normal in AG layout
            var texture = NormalMapTestTextureFactory.CreateFlatAG(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.DXT5,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.AG
            );

            var pixels = texture.GetPixels32();
            float x = (pixels[0].r / 255f) * 2f - 1f;
            float y = (pixels[0].g / 255f) * 2f - 1f;
            float z = (pixels[0].b / 255f) * 2f - 1f;
            // Flat normal should produce (0, 0, 1) in RG layout output
            Assert.That(Mathf.Abs(x), Is.LessThan(0.05f), "X should be ~0");
            Assert.That(Mathf.Abs(y), Is.LessThan(0.05f), "Y should be ~0");
            Assert.That(z, Is.GreaterThan(0.9f), "Z should be ~1");
        }

        [Test]
        public void PrepareForCompression_UnnormalizedInput_BecomesNormalized()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            _createdObjects.Add(texture);
            var pixels = new Color[4];

            // Intentionally unnormalized: x=0.8, y=0.8 would give length > 1
            float unnormalizedX = 0.8f;
            float unnormalizedY = 0.8f;
            float encodedR = unnormalizedX * 0.5f + 0.5f;
            float encodedG = unnormalizedY * 0.5f + 0.5f;

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(encodedR, encodedG, 0f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            float x = newPixels[0].r * 2f - 1f;
            float y = newPixels[0].g * 2f - 1f;
            float zSquared = 1f - x * x - y * y;
            float z = zSquared > 0f ? Mathf.Sqrt(zSquared) : 0f;
            float length = Mathf.Sqrt(x * x + y * y + z * z);

            Assert.That(length, Is.EqualTo(1f).Within(0.01f), "Output normal should be normalized");
        }

        #endregion

        #region PrepareForCompression - Z Reconstruction Accuracy

        [Test]
        public void PrepareForCompression_FlatNormal_SetsCorrectZ()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(8);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            // Flat normal (0,0,1): Z should be encoded as ~255
            Assert.That(pixels[0].b, Is.GreaterThan((byte)245), "Z=1 should encode to ~255");
        }

        [Test]
        public void PrepareForCompression_TiltedNormal_CalculatesCorrectZ()
        {
            // Create a tilted normal: X=0.5, Y=0 encoded as R=191, G=128, B=~219
            byte encodedX = (byte)Mathf.Clamp((0.5f * 0.5f + 0.5f) * 255f, 0f, 255f); // 191
            byte encodedY = (byte)Mathf.Clamp((0f * 0.5f + 0.5f) * 255f, 0f, 255f); // 128
            byte encodedZ = (byte)
                Mathf.Clamp((Mathf.Sqrt(1f - 0.25f) * 0.5f + 0.5f) * 255f, 0f, 255f); // ~219
            var texture = CreateSinglePixelTexture(encodedX, encodedY, encodedZ, 255);
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels32();
            float x = (pixels[0].r / 255f) * 2f - 1f;
            float y = (pixels[0].g / 255f) * 2f - 1f;
            float z = (pixels[0].b / 255f) * 2f - 1f;

            // Expected: X~0.5, Y~0, Z~sqrt(1-0.25)=0.866
            float expectedZ = Mathf.Sqrt(1f - x * x - y * y);
            Assert.That(
                Mathf.Abs(z - expectedZ),
                Is.LessThan(0.05f),
                $"Z={z} should match recalculated Z={expectedZ}"
            );
        }

        #endregion

        #region PrepareForCompression - Source Layout Override

        [Test]
        public void PrepareForCompression_SourceLayoutRG_ReadsFromRG()
        {
            // Create texture with normal data in RG channels
            var texture = CreateSinglePixelTexture(192, 192, 0, 255); // X>0, Y>0 in RG
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.BC7,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.RG
            );

            var pixels = texture.GetPixels32();
            // Should read from RG: X should be positive (> 128)
            Assert.That(pixels[0].r, Is.GreaterThan((byte)128));
            Assert.That(pixels[0].g, Is.GreaterThan((byte)128));
        }

        [Test]
        public void PrepareForCompression_SourceLayoutAG_ReadsFromAG()
        {
            // Create texture with normal data in AG channels: R=255, G=192, B=255, A=192
            var texture = CreateSinglePixelTexture(255, 192, 255, 192); // X>0 in A, Y>0 in G
            _createdObjects.Add(texture);

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.BC7,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.AG
            );

            var pixels = texture.GetPixels32();
            // Should read from AG: X from A=192 -> positive, Y from G=192 -> positive
            Assert.That(pixels[0].r, Is.GreaterThan((byte)128)); // X written to R
            Assert.That(pixels[0].g, Is.GreaterThan((byte)128)); // Y written to G
        }

        #endregion

        #region ShouldPreserveSemanticAlpha

        [Test]
        public void ShouldPreserveSemanticAlpha_BC7_RGB_WithAlpha_ReturnsTrue()
        {
            var result = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.BC7,
                NormalMapPreprocessor.SourceLayout.RGB,
                hasSignificantAlpha: true
            );
            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldPreserveSemanticAlpha_BC7_RGB_NoAlpha_ReturnsTrue()
        {
            // RGB layout stores explicit signed Z, so preserve even without alpha
            var result = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.BC7,
                NormalMapPreprocessor.SourceLayout.RGB,
                hasSignificantAlpha: false
            );
            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldPreserveSemanticAlpha_BC7_AG_WithAlpha_ReturnsFalse()
        {
            // AG layout: alpha is normal X data, not semantic
            var result = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.BC7,
                NormalMapPreprocessor.SourceLayout.AG,
                hasSignificantAlpha: true
            );
            Assert.IsFalse(result);
        }

        [Test]
        public void ShouldPreserveSemanticAlpha_BC7_RG_WithAlpha_ReturnsTrue()
        {
            var result = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.BC7,
                NormalMapPreprocessor.SourceLayout.RG,
                hasSignificantAlpha: true
            );
            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldPreserveSemanticAlpha_BC7_RG_NoAlpha_ReturnsFalse()
        {
            var result = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.BC7,
                NormalMapPreprocessor.SourceLayout.RG,
                hasSignificantAlpha: false
            );
            Assert.IsFalse(result);
        }

        [Test]
        public void ShouldPreserveSemanticAlpha_DXT5_Any_ReturnsFalse()
        {
            var result = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.DXT5,
                NormalMapPreprocessor.SourceLayout.RGB,
                hasSignificantAlpha: true
            );
            Assert.IsFalse(result);
        }

        [Test]
        public void ShouldPreserveSemanticAlpha_BC5_Any_ReturnsFalse()
        {
            var result = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.BC5,
                NormalMapPreprocessor.SourceLayout.RGB,
                hasSignificantAlpha: true
            );
            Assert.IsFalse(result);
        }

        #endregion

        #region Helper Methods

        private Texture2D CreateSinglePixelTexture(byte r, byte g, byte b, byte a)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(r, g, b, a);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
