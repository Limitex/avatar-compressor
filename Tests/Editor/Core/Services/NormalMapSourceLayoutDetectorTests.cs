using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class NormalMapSourceLayoutDetectorTests
    {
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
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

        #region Resolve - Deterministic Formats

        [Test]
        public void Resolve_BC5Format_ReturnsRG()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(16);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.Resolve(texture, texture, TextureFormat.BC5);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RG, result);
        }

        [Test]
        public void Resolve_RGBA32Format_ReturnsRGB()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(16);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.Resolve(
                texture,
                texture,
                TextureFormat.RGBA32
            );

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RGB, result);
        }

        [Test]
        public void Resolve_RGB24Format_ReturnsRGB()
        {
            var texture = NormalMapTestTextureFactory.CreateFlat(16);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.Resolve(
                texture,
                texture,
                TextureFormat.RGB24
            );

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RGB, result);
        }

        #endregion

        #region Resolve - Ambiguous Formats (DXT5/BC7) Delegating to DetectDXTnmLike

        [Test]
        public void Resolve_DXT5Format_DelegatesDetection()
        {
            // DXT5 with AG layout texture should detect AG
            var texture = NormalMapTestTextureFactory.CreateSphereAG(32);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.Resolve(
                texture,
                texture,
                TextureFormat.DXT5
            );

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        [Test]
        public void Resolve_BC7Format_DelegatesDetection()
        {
            // BC7 with AG layout texture should detect AG
            var texture = NormalMapTestTextureFactory.CreateSphereAG(32);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.Resolve(texture, texture, TextureFormat.BC7);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        #endregion

        #region DetectDXTnmLike - AG Layout Detection

        [Test]
        public void DetectDXTnmLike_AGLayoutTexture_ReturnsAG()
        {
            // AG layout: R=255, B=255, normal data in A and G
            var texture = NormalMapTestTextureFactory.CreateSphereAG(32);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        [Test]
        public void DetectDXTnmLike_FlatAGLayout_ReturnsAG()
        {
            var texture = NormalMapTestTextureFactory.CreateFlatAG(32);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        #endregion

        #region DetectDXTnmLike - RG Layout Detection

        [Test]
        public void DetectDXTnmLike_RGLayoutTexture_ReturnsRG()
        {
            // RG layout with varying normals and B=128 (neutral Z).
            // B=128 decodes to zFromB~0, which stays in the neutral zone (-0.2 to 0.2),
            // producing zero Z-sign evidence and low RGB consistency.
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
            _createdObjects.Add(texture);
            var pixels = new Color32[32 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float nx = (x / 31f - 0.5f) * 1.2f;
                    float ny = (y / 31f - 0.5f) * 1.2f;
                    float lenSq = nx * nx + ny * ny;
                    if (lenSq > 1f)
                    {
                        float len = Mathf.Sqrt(lenSq);
                        nx /= len;
                        ny /= len;
                    }
                    byte encodedX = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte encodedY = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                    pixels[y * 32 + x] = new Color32(encodedX, encodedY, 128, 255);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RG, result);
        }

        #endregion

        #region DetectDXTnmLike - RGB Layout Detection

        [Test]
        public void DetectDXTnmLike_MixedZTexture_ReturnsRGB()
        {
            // Object-space with mixed Z signs
            var texture = NormalMapTestTextureFactory.CreateMixedZ(32);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RGB, result);
        }

        [Test]
        public void DetectDXTnmLike_NegativeZTexture_ReturnsRGB()
        {
            // Object-space with varying negative Z normals.
            // Varying normals ensure rgbAbsConsistency is meaningful (B matches
            // reconstructed Z magnitude), distinguishing from RG layout with constant B.
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
            _createdObjects.Add(texture);
            var pixels = new Color32[32 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float nx = (x / 31f - 0.5f) * 1.4f;
                    float ny = (y / 31f - 0.5f) * 1.4f;
                    float nz = -Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                    float len = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                    nx /= len;
                    ny /= len;
                    nz /= len;
                    pixels[y * 32 + x] = NormalMapTestTextureFactory.EncodeNormal(nx, ny, nz);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RGB, result);
        }

        [Test]
        public void DetectDXTnmLike_RGBWithAlpha_ReturnsRGB()
        {
            // RGB normals with significant alpha (cutout mask)
            var texture = NormalMapTestTextureFactory.CreateWithAlpha(32);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RGB, result);
        }

        #endregion

        #region DetectDXTnmLike - Branch 2: AG Without R/B Constants

        [Test]
        public void DetectDXTnmLike_AGLayoutWithoutRBConstants_ReturnsAG()
        {
            // AG layout with non-standard R/B values (not near 255).
            // This exercises Branch 2 (validAgRatio >= 0.9 && rgAdvantage <= -0.1)
            // because Branch 1 (rbNearOneRatio >= 0.9) is not satisfied.
            var texture = NormalMapTestTextureFactory.CreateSphereAGWithCustomRB(32, 100, 100);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        #endregion

        #region DetectDXTnmLike - Branch 6: RG Clear Advantage

        [Test]
        public void DetectDXTnmLike_RGBSphereWithOpaqueAlpha_ReturnsRG()
        {
            // Standard RGB tangent-space sphere with opaque alpha (A=255).
            // Branch 5 is blocked because strongRgbEvidence is true (B matches
            // reconstructed Z from RG). Branch 6 fires because RG validity is high
            // and rgAdvantage is large (A=255 makes xFromA=1.0, invalidating most
            // AG unit vector checks).
            var texture = NormalMapTestTextureFactory.CreateSphere(32);
            _createdObjects.Add(texture);

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.RG, result);
        }

        #endregion

        #region DetectDXTnmLike - Edge Cases

        [Test]
        public void DetectDXTnmLike_NullTexture_ReturnsAG()
        {
            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(null);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        [Test]
        public void DetectDXTnmLike_EmptyTexture_ReturnsAG()
        {
            var texture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            _createdObjects.Add(texture);

            LogAssert.Expect(LogType.Error, "Texture '' is degenerate (dimensions 0x0)");

            var result = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        #endregion

        #region Resolve - Fallback to Resized Texture

        [Test]
        public void Resolve_NullOriginal_UsesResizedTexture()
        {
            var resized = NormalMapTestTextureFactory.CreateSphereAG(32);
            _createdObjects.Add(resized);

            var result = NormalMapSourceLayoutDetector.Resolve(null, resized, TextureFormat.DXT5);

            Assert.AreEqual(NormalMapPreprocessor.SourceLayout.AG, result);
        }

        #endregion
    }
}
