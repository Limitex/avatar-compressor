using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class NormalMapPreprocessorTests
    {
        private NormalMapPreprocessor _preprocessor;

        [SetUp]
        public void SetUp()
        {
            _preprocessor = new NormalMapPreprocessor();
        }

        #region PrepareForCompression Normal Map Tests

        [Test]
        public void PrepareForCompression_FromBC5_NormalizesVectors()
        {
            var texture = CreateNormalMapTexture(4, 4);

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            // Verify vectors are normalized by checking that decoded normals have unit length
            for (int i = 0; i < newPixels.Length; i++)
            {
                float x = newPixels[i].r * 2f - 1f;
                float y = newPixels[i].g * 2f - 1f;
                float zSquared = 1f - x * x - y * y;
                float z = zSquared > 0f ? Mathf.Sqrt(zSquared) : 0f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);

                Assert.That(
                    length,
                    Is.EqualTo(1f).Within(0.01f),
                    $"Normal vector at index {i} should be normalized"
                );
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FlatNormal_HandlesCorrectly()
        {
            // Flat normal pointing straight up: (0, 0, 1) encoded as (0.5, 0.5, 1)
            var texture = CreateFlatNormalTexture(4, 4);

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                // Flat normal should remain (0.5, 0.5) after normalization
                Assert.That(
                    pixels[i].r,
                    Is.EqualTo(0.5f).Within(0.01f),
                    $"Red channel at {i} should be 0.5 for flat normal"
                );
                Assert.That(
                    pixels[i].g,
                    Is.EqualTo(0.5f).Within(0.01f),
                    $"Green channel at {i} should be 0.5 for flat normal"
                );
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_TiltedNormal_PreservesDirection()
        {
            // Create a texture with a tilted normal (pointing somewhat to the right)
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // Normal pointing +X direction: (0.707, 0, 0.707) -> encoded (0.8535, 0.5, *)
            float x = 0.707f;
            float y = 0f;
            float encodedR = x * 0.5f + 0.5f;
            float encodedG = y * 0.5f + 0.5f;

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(encodedR, encodedG, 1f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            // X component should be preserved (approximately)
            float decodedX = newPixels[0].r * 2f - 1f;
            Assert.That(decodedX, Is.EqualTo(x).Within(0.01f), "X component should be preserved");

            // Y component should remain 0
            float decodedY = newPixels[0].g * 2f - 1f;
            Assert.That(decodedY, Is.EqualTo(0f).Within(0.01f), "Y component should remain 0");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_NullTexture_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _preprocessor.PrepareForCompression(null, TextureFormat.RGBA32, TextureFormat.BC5);
            });
        }

        [Test]
        public void PrepareForCompression_UnnormalizedInput_BecomesNormalized()
        {
            // Create a texture with unnormalized normal data (simulating post-blit corruption)
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
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

            // Verify the result is normalized
            float x = newPixels[0].r * 2f - 1f;
            float y = newPixels[0].g * 2f - 1f;
            float zSquared = 1f - x * x - y * y;
            float z = zSquared > 0f ? Mathf.Sqrt(zSquared) : 0f;
            float length = Mathf.Sqrt(x * x + y * y + z * z);

            Assert.That(length, Is.EqualTo(1f).Within(0.01f), "Output normal should be normalized");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_DegenerateVector_ResetsToFlatNormal()
        {
            // Create a texture with degenerate (near-zero) normal data
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // Degenerate vector: x=0, y=0 encoded as (0.5, 0.5) but with z=0
            // After decode: x=0, y=0, z=0 -> length=0 -> should reset to flat normal
            for (int i = 0; i < 4; i++)
            {
                // Encode values that will result in near-zero length after processing
                pixels[i] = new Color(0.5f, 0.5f, 0f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            // Should be reset to flat normal (0.5, 0.5)
            // Use 0.01f tolerance to account for byte precision (128/255 ≈ 0.502)
            for (int i = 0; i < newPixels.Length; i++)
            {
                Assert.That(
                    newPixels[i].r,
                    Is.EqualTo(0.5f).Within(0.01f),
                    $"Red channel at {i} should be 0.5 for flat normal"
                );
                Assert.That(
                    newPixels[i].g,
                    Is.EqualTo(0.5f).Within(0.01f),
                    $"Green channel at {i} should be 0.5 for flat normal"
                );
            }

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Source Format Tests

        [Test]
        public void PrepareForCompression_FromRGBSource_PreservesNegativeZ()
        {
            // Object Space Normal Map with negative Z (pointing backwards)
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // Normal pointing -Z direction: (0, 0, -1) encoded as (0.5, 0.5, 0) in RGB
            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 0f, 1f); // Z = -1 encoded
            }
            texture.SetPixels(pixels);
            texture.Apply();

            // Source is RGB format (has Z), should preserve negative Z
            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var newPixels = texture.GetPixels();
            float z = newPixels[0].b * 2f - 1f;

            // Z should be negative (preserved from original)
            Assert.That(
                z,
                Is.LessThan(0f),
                "Negative Z should be preserved for Object Space Normal Map"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FromRGBSource_PreservesPositiveZ()
        {
            // Standard Tangent Space Normal Map with positive Z
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // Normal pointing +Z direction: (0, 0, 1) encoded as (0.5, 0.5, 1) in RGB
            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 1f, 1f); // Z = +1 encoded
            }
            texture.SetPixels(pixels);
            texture.Apply();

            // Source is RGB format (has Z)
            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.BC5);

            var newPixels = texture.GetPixels();
            float z = newPixels[0].b * 2f - 1f;

            // Z should be positive (preserved from original)
            Assert.That(z, Is.GreaterThan(0f), "Positive Z should be preserved");

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region DXTnm Format Tests (DXT5, BC7 - AG channel layout)

        [Test]
        public void PrepareForCompression_FromDXT5_ReadsFromAGChannels()
        {
            // DXT5 (DXTnm) stores XY in AG channels
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // Tilted normal: (0.5, 0.3, z) encoded in AG as A=X, G=Y
            float x = 0.5f;
            float y = 0.3f;
            float encodedA = x * 0.5f + 0.5f; // X in A channel
            float encodedG = y * 0.5f + 0.5f; // Y in G channel

            for (int i = 0; i < 4; i++)
            {
                // R and B are ignored for DXTnm, only A and G matter
                pixels[i] = new Color(0f, encodedG, 0f, encodedA);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.DXT5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            // After processing, output is in RGB format
            float outputX = newPixels[0].r * 2f - 1f;
            float outputY = newPixels[0].g * 2f - 1f;

            Assert.That(outputX, Is.EqualTo(x).Within(0.02f), "X should be read from A channel");
            Assert.That(outputY, Is.EqualTo(y).Within(0.02f), "Y should be read from G channel");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FromBC7_ReadsFromAGChannels()
        {
            // BC7 also uses DXTnm layout (XY in AG)
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            float x = -0.4f;
            float y = 0.2f;
            float encodedA = x * 0.5f + 0.5f;
            float encodedG = y * 0.5f + 0.5f;

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0f, encodedG, 0f, encodedA);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC7, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            float outputX = newPixels[0].r * 2f - 1f;
            float outputY = newPixels[0].g * 2f - 1f;

            Assert.That(outputX, Is.EqualTo(x).Within(0.02f), "X should be read from A channel");
            Assert.That(outputY, Is.EqualTo(y).Within(0.02f), "Y should be read from G channel");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FromDXT5Crunched_ReadsFromAGChannels()
        {
            // DXT5Crunched also uses DXTnm layout
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            float x = 0.6f;
            float y = -0.1f;
            float encodedA = x * 0.5f + 0.5f;
            float encodedG = y * 0.5f + 0.5f;

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0f, encodedG, 0f, encodedA);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.DXT5Crunched,
                TextureFormat.BC5
            );

            var newPixels = texture.GetPixels();

            float outputX = newPixels[0].r * 2f - 1f;
            float outputY = newPixels[0].g * 2f - 1f;

            Assert.That(outputX, Is.EqualTo(x).Within(0.02f), "X should be read from A channel");
            Assert.That(outputY, Is.EqualTo(y).Within(0.02f), "Y should be read from G channel");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FromDXTnm_NormalizesAndRecalculatesZ()
        {
            // DXTnm format: XY in AG, Z must be recalculated
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // Tilted normal in AG channels
            float x = 0.5f;
            float y = 0.3f;
            float expectedZ = Mathf.Sqrt(1f - x * x - y * y);

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0f, y * 0.5f + 0.5f, 0f, x * 0.5f + 0.5f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.DXT5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            float outputZ = newPixels[0].b * 2f - 1f;

            // Z should be positive and correctly calculated
            Assert.That(
                outputZ,
                Is.EqualTo(expectedZ).Within(0.02f),
                "Z should be recalculated from XY"
            );
            Assert.That(outputZ, Is.GreaterThan(0f), "Z should be positive for DXTnm source");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FromDXTnm_FlatNormal_ProducesCorrectOutput()
        {
            // Flat normal (0, 0, 1) in DXTnm format
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // X=0 -> A=0.5, Y=0 -> G=0.5
            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0f, 0.5f, 0f, 0.5f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.DXT5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            // Should output flat normal in RGB
            Assert.That(newPixels[0].r, Is.EqualTo(0.5f).Within(0.01f), "X should be 0");
            Assert.That(newPixels[0].g, Is.EqualTo(0.5f).Within(0.01f), "Y should be 0");
            Assert.That(newPixels[0].b, Is.EqualTo(1f).Within(0.01f), "Z should be 1");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_ToDXT5_SetsConstantRBChannels()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            float x = 0.4f;
            float y = -0.2f;
            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(x * 0.5f + 0.5f, y * 0.5f + 0.5f, 1f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.RGBA32, TextureFormat.DXT5);

            var newPixels = texture.GetPixels();
            Assert.That(newPixels[0].r, Is.EqualTo(1f).Within(0.01f), "R should be constant 1.0");
            Assert.That(newPixels[0].b, Is.EqualTo(1f).Within(0.01f), "B should be constant 1.0");
            Assert.That(
                newPixels[0].g * 2f - 1f,
                Is.EqualTo(y).Within(0.02f),
                "Y should remain encoded in G"
            );
            Assert.That(
                newPixels[0].a * 2f - 1f,
                Is.EqualTo(x).Within(0.02f),
                "X should remain encoded in A"
            );

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_ToBC7_WithPreserveAlpha_KeepsSourceAlpha()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color32[4];
            byte sourceAlpha = 64;

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color32(128, 128, 255, sourceAlpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(
                texture,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true
            );

            var newPixels = texture.GetPixels32();
            Assert.AreEqual(sourceAlpha, newPixels[0].a, "Alpha should be preserved for BC7");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FromBC5Source_AssumesPositiveZ()
        {
            // BC5 source doesn't have Z, so should assume positive Z
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            // Flat normal: (0, 0, ?) - Z unknown from BC5
            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 0f, 1f); // B channel doesn't matter for BC5
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();
            // Should be flat normal (0, 0, 1) -> XY = (0.5, 0.5)
            Assert.That(newPixels[0].r, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(newPixels[0].g, Is.EqualTo(0.5f).Within(0.01f));
            // Z should be positive
            float z = newPixels[0].b * 2f - 1f;
            Assert.That(z, Is.GreaterThan(0f), "Z should be positive for BC5 source");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FromBC5_RecalculatesZChannel()
        {
            var texture = CreateTextureWithMissingZ(4, 4);

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC5, TextureFormat.BC5);

            var pixels = texture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                float z = pixels[i].b * 2f - 1f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);

                Assert.That(
                    length,
                    Is.EqualTo(1f).Within(0.01f),
                    $"Normal vector at index {i} should be normalized"
                );

                Assert.That(
                    z,
                    Is.GreaterThan(0f),
                    $"Z component at index {i} should be positive (BC5 source assumes positive Z)"
                );
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_FlatNormal_SetsCorrectZ()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 0f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();

            for (int i = 0; i < newPixels.Length; i++)
            {
                Assert.That(newPixels[i].r, Is.EqualTo(0.5f).Within(0.01f), "X should remain 0");
                Assert.That(newPixels[i].g, Is.EqualTo(0.5f).Within(0.01f), "Y should remain 0");
                Assert.That(
                    newPixels[i].b,
                    Is.EqualTo(1f).Within(0.01f),
                    "Z should be 1 for flat normal"
                );
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrepareForCompression_TiltedNormal_CalculatesCorrectZ()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color[4];

            float x = 0.5f;
            float y = 0f;
            float expectedZ = Mathf.Sqrt(1f - x * x - y * y);

            for (int i = 0; i < 4; i++)
            {
                pixels[i] = new Color(x * 0.5f + 0.5f, y * 0.5f + 0.5f, 0f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            _preprocessor.PrepareForCompression(texture, TextureFormat.BC5, TextureFormat.BC5);

            var newPixels = texture.GetPixels();
            float actualZ = newPixels[0].b * 2f - 1f;

            Assert.That(
                actualZ,
                Is.EqualTo(expectedZ).Within(0.01f),
                "Z should be correctly calculated from X and Y"
            );

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Helper Methods

        private static Texture2D CreateNormalMapTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Create varied normals for testing
                float x = (i % 3 - 1) * 0.3f;
                float y = ((i / 3) % 3 - 1) * 0.3f;

                // Encode to 0-1 range
                pixels[i] = new Color(x * 0.5f + 0.5f, y * 0.5f + 0.5f, 1f, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFlatNormalTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            // Flat normal: (0, 0, 1) encoded as (0.5, 0.5, 1)
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 1f, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateTextureWithMissingZ(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                float x = (i % 3 - 1) * 0.3f;
                float y = ((i / 3) % 3 - 1) * 0.3f;
                pixels[i] = new Color(x * 0.5f + 0.5f, y * 0.5f + 0.5f, 0f, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
