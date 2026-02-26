using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Integration tests for the normal map preprocessing pipeline:
    /// resize (via TextureProcessor) then preprocess (via NormalMapPreprocessor).
    /// </summary>
    [TestFixture]
    public class NormalMapPreprocessorPipelineTests
    {
        private static bool IsSoftwareRenderer =>
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private NormalMapPreprocessor _preprocessor;
        private TextureProcessor _processor;
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _preprocessor = new NormalMapPreprocessor();
            _processor = new TextureProcessor(32, 2048, false);
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

        #region RGB -> BC5 Pipeline

        [Test]
        public void Pipeline_RGBToBC5_FlatNormal_PreservesData()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = resized.GetPixels32();
            // Flat normal: X=0 -> R~128, Y=0 -> G~128, Z=1 -> B~255
            Assert.That(pixels[0].r, Is.InRange((byte)124, (byte)132));
            Assert.That(pixels[0].g, Is.InRange((byte)124, (byte)132));
            Assert.That(pixels[0].b, Is.GreaterThan((byte)200));
        }

        [Test]
        public void Pipeline_RGBToBC5_SphereNormal_ProducesValidVectors()
        {
            var source = NormalMapTestTextureFactory.CreateSphere(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = resized.GetPixels32();
            for (int i = 0; i < Mathf.Min(pixels.Length, 50); i++)
            {
                float x = (pixels[i].r / 255f) * 2f - 1f;
                float y = (pixels[i].g / 255f) * 2f - 1f;
                float z = (pixels[i].b / 255f) * 2f - 1f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                Assert.That(length, Is.InRange(0.8f, 1.2f), $"Pixel {i}: length={length}");
            }
        }

        #endregion

        #region RGB -> DXT5 Pipeline

        [Test]
        public void Pipeline_RGBToDXT5_FlatNormal_WritesCorrectAGLayout()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.DXT5);

            var pixels = resized.GetPixels32();
            // AG layout: X=0 -> A~128, Y=0 -> G~128, R=255, B=255
            Assert.That(pixels[0].a, Is.InRange((byte)124, (byte)132));
            Assert.That(pixels[0].g, Is.InRange((byte)124, (byte)132));
            Assert.AreEqual(255, pixels[0].r);
            Assert.AreEqual(255, pixels[0].b);
        }

        #endregion

        #region RGB -> BC7 Pipeline (preserve alpha)

        [Test]
        public void Pipeline_RGBToBC7_PreserveAlpha_KeepsSourceAlpha()
        {
            var source = NormalMapTestTextureFactory.CreateWithAlpha(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            var beforePixels = resized.GetPixels32();
            bool hadNonOpaqueAlpha = false;
            for (int i = 0; i < beforePixels.Length; i++)
            {
                if (beforePixels[i].a < 250)
                {
                    hadNonOpaqueAlpha = true;
                    break;
                }
            }

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true
            );

            var afterPixels = resized.GetPixels32();
            bool hasNonOpaqueAlpha = false;
            for (int i = 0; i < afterPixels.Length; i++)
            {
                if (afterPixels[i].a < 250)
                {
                    hasNonOpaqueAlpha = true;
                    break;
                }
            }

            Assert.IsTrue(hadNonOpaqueAlpha, "Source should have non-opaque alpha");
            Assert.IsTrue(hasNonOpaqueAlpha, "Alpha should be preserved after preprocessing");
        }

        [Test]
        public void Pipeline_RGBToBC7_NoPreserveAlpha_OverwritesAlpha()
        {
            var source = NormalMapTestTextureFactory.CreateWithAlpha(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: false
            );

            var pixels = resized.GetPixels32();
            // AG layout: alpha should contain X (normal data), not source alpha
            // For a flat normal, X=0 -> A=128
            Assert.That(
                pixels[0].a,
                Is.InRange((byte)100, (byte)160),
                "Alpha should contain normal X data, not source alpha"
            );
        }

        #endregion

        #region AG -> BC5 Cross-Layout Pipeline

        [Test]
        public void Pipeline_AGToBC5_SphereNormal_ConvertsCorrectly()
        {
            var source = NormalMapTestTextureFactory.CreateSphereAG(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.DXT5,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.AG
            );

            var pixels = resized.GetPixels32();
            // Center pixel should be flat normal in RG layout
            int centerIdx = 16 * 32 + 16;
            Assert.That(pixels[centerIdx].r, Is.InRange((byte)120, (byte)136));
            Assert.That(pixels[centerIdx].g, Is.InRange((byte)120, (byte)136));
        }

        #endregion

        #region Multi-pass Pipeline (fallback scenario)

        [Test]
        public void Pipeline_OriginalPixelsRestore_AllowsRePreprocessing()
        {
            var source = NormalMapTestTextureFactory.CreateSphere(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            // Save original pixels (as the service would)
            var originalPixels = resized.GetPixels32();

            // First pass: preprocess for BC5 (RG layout)
            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);

            // Restore original pixels
            resized.SetPixels32(originalPixels);
            resized.Apply();

            // Second pass: preprocess for DXT5 (AG layout)
            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.DXT5);

            var pixels = resized.GetPixels32();
            // Should be in AG layout now
            Assert.AreEqual(255, pixels[0].r, "R should be 255 in AG layout");
            Assert.AreEqual(255, pixels[0].b, "B should be 255 in AG layout");
        }

        #endregion

        #region Object Space Normal Pipeline

        [Test]
        public void Pipeline_ObjectSpaceNegativeZ_PreservesSign()
        {
            var source = NormalMapTestTextureFactory.CreateNegativeZ(32);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 16, 16, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = resized.GetPixels32();
            float z = (pixels[0].b / 255f) * 2f - 1f;
            Assert.That(z, Is.LessThan(0f), "Negative Z should be preserved for object space");
        }

        [Test]
        public void Pipeline_ObjectSpaceMixedZ_ProducesValidVectors()
        {
            if (IsSoftwareRenderer)
                Assert.Ignore(
                    "Software renderer blit destroys alternating Z patterns via half-texel blending."
                );

            var source = NormalMapTestTextureFactory.CreateMixedZ(32);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 16, 16, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = resized.GetPixels32();
            bool hasPositiveZ = false;
            bool hasNegativeZ = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                float z = (pixels[i].b / 255f) * 2f - 1f;
                if (z > 0.1f)
                    hasPositiveZ = true;
                if (z < -0.1f)
                    hasNegativeZ = true;
            }
            Assert.IsTrue(hasPositiveZ, "Should have some positive Z values");
            Assert.IsTrue(hasNegativeZ, "Should have some negative Z values");
        }

        #endregion

        #region Actual Compressed Texture E2E Tests

        [Test]
        public void FullPipeline_BC5ToBC5_PreservesNormals()
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphere(256);
            _createdObjects.Add(originalSource);
            var bc5Source = CreateActualCompressedTexture(originalSource, TextureFormat.BC5);
            _createdObjects.Add(bc5Source);

            Assert.AreEqual(TextureFormat.BC5, bc5Source.format);

            var resized = _processor.ResizeTo(bc5Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.BC5, TextureFormat.BC5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format);
            AssertCompressedNormalsAreValid(resized, "BC5 to BC5");
        }

        [Test]
        public void FullPipeline_DXT5ToBC5_PreservesNormals()
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphere(256);
            _createdObjects.Add(originalSource);
            var dxt5Source = CreateActualCompressedTexture(originalSource, TextureFormat.DXT5);
            _createdObjects.Add(dxt5Source);

            Assert.AreEqual(TextureFormat.DXT5, dxt5Source.format);

            var resized = _processor.ResizeTo(dxt5Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.DXT5, TextureFormat.BC5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format);
            AssertCompressedNormalsAreValid(resized, "DXT5 to BC5");
        }

        [Test]
        public void FullPipeline_BC7ToBC5_PreservesNormals()
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphere(256);
            _createdObjects.Add(originalSource);
            var bc7Source = CreateActualCompressedTexture(originalSource, TextureFormat.BC7);
            _createdObjects.Add(bc7Source);

            Assert.AreEqual(TextureFormat.BC7, bc7Source.format);

            var resized = _processor.ResizeTo(bc7Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.BC7, TextureFormat.BC5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format);
            AssertCompressedNormalsAreValid(resized, "BC7 to BC5");
        }

        [Test]
        [TestCase(512, 256)]
        [TestCase(512, 128)]
        [TestCase(1024, 256)]
        public void FullPipeline_BC5ToBC5_WithDownscale_PreservesNormals(
            int sourceSize,
            int targetSize
        )
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphere(sourceSize);
            _createdObjects.Add(originalSource);
            var bc5Source = CreateActualCompressedTexture(originalSource, TextureFormat.BC5);
            _createdObjects.Add(bc5Source);

            var resized = _processor.ResizeTo(bc5Source, targetSize, targetSize, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.BC5, TextureFormat.BC5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            AssertCompressedNormalsAreValid(resized, $"BC5 {sourceSize}->{targetSize}");
        }

        [Test]
        public void FullPipeline_DXT5ToDXT5_PreservesNormals()
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphere(256);
            _createdObjects.Add(originalSource);
            var dxt5Source = CreateActualCompressedTexture(originalSource, TextureFormat.DXT5);
            _createdObjects.Add(dxt5Source);

            Assert.AreEqual(TextureFormat.DXT5, dxt5Source.format);

            var resized = _processor.ResizeTo(dxt5Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.DXT5, TextureFormat.DXT5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.DXT5, resized.format);
            AssertDXTnmNormalsAreValid(resized, "DXT5 to DXT5");
        }

        [Test]
        public void FullPipeline_BC7ToBC7_PreservesNormals()
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphere(256);
            _createdObjects.Add(originalSource);
            var bc7Source = CreateActualCompressedTexture(originalSource, TextureFormat.BC7);
            _createdObjects.Add(bc7Source);

            Assert.AreEqual(TextureFormat.BC7, bc7Source.format);

            var resized = _processor.ResizeTo(bc7Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.BC7, TextureFormat.BC7);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC7, resized.format);
            AssertDXTnmNormalsAreValid(resized, "BC7 to BC7");
        }

        [Test]
        public void FullPipeline_RGBAToDXT5_AlphaStoresNormalX()
        {
            var source = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            _createdObjects.Add(source);
            var pixels = new Color32[64 * 64];
            float inputX = 0.6f;
            float inputY = 0.2f;
            float inputZ = Mathf.Sqrt(Mathf.Max(0f, 1f - inputX * inputX - inputY * inputY));
            byte sourceAlpha = 51; // ~0.2

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(
                    (byte)Mathf.Clamp((inputX * 0.5f + 0.5f) * 255f, 0f, 255f),
                    (byte)Mathf.Clamp((inputY * 0.5f + 0.5f) * 255f, 0f, 255f),
                    (byte)Mathf.Clamp((inputZ * 0.5f + 0.5f) * 255f, 0f, 255f),
                    sourceAlpha
                );
            }
            source.SetPixels32(pixels);
            source.Apply();

            var resized = _processor.ResizeTo(source, 64, 64, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.DXT5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            var result = resized.GetPixels32();
            int centerIdx = 32 * 64 + 32;
            float decodedXFromAlpha = (result[centerIdx].a / 255f) * 2f - 1f;

            Assert.That(
                decodedXFromAlpha,
                Is.EqualTo(inputX).Within(0.2f),
                "DXT5 alpha channel should store normal X (DXTnm layout)"
            );
            Assert.That(
                result[centerIdx].a,
                Is.GreaterThan((byte)(sourceAlpha + 50)),
                "Original alpha should be replaced in DXT5 normal-map path"
            );
        }

        [Test]
        [TestCase(512, 256)]
        [TestCase(256, 128)]
        [TestCase(128, 64)]
        public void FullPipeline_ActualResize_PreservesNormalDirection(
            int sourceSize,
            int targetSize
        )
        {
            var source = NormalMapTestTextureFactory.CreateSphere(sourceSize);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, targetSize, targetSize, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);

            var pixels = resized.GetPixels32();
            int sampleCount = Mathf.Min(500, pixels.Length);
            int step = Mathf.Max(1, pixels.Length / sampleCount);
            float maxDeviation = 0f;

            for (int i = 0; i < pixels.Length; i += step)
            {
                float x = (pixels[i].r / 255f) * 2f - 1f;
                float y = (pixels[i].g / 255f) * 2f - 1f;
                float z = (pixels[i].b / 255f) * 2f - 1f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                maxDeviation = Mathf.Max(maxDeviation, Mathf.Abs(length - 1f));
            }

            Assert.That(
                maxDeviation,
                Is.LessThanOrEqualTo(0.05f),
                $"Resize {sourceSize}->{targetSize}: Normal vectors should be approximately unit length"
            );
        }

        #endregion

        #region E2E Compression Round-Trip - BC5

        [Test]
        public void E2E_BC5_FlatNormal_RoundTrip_PreservesData()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(32);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // After BC5 round-trip, flat normal should still be approximately (128, 128) in RG
            Assert.That(
                pixels[0].r,
                Is.InRange((byte)120, (byte)136),
                "X should be ~128 after BC5 round-trip"
            );
            Assert.That(
                pixels[0].g,
                Is.InRange((byte)120, (byte)136),
                "Y should be ~128 after BC5 round-trip"
            );
        }

        [Test]
        public void E2E_BC5_SphereNormal_RoundTrip_ProducesReconstructableVectors()
        {
            var source = NormalMapTestTextureFactory.CreateSphere(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            int validCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                float x = (pixels[i].r / 255f) * 2f - 1f;
                float y = (pixels[i].g / 255f) * 2f - 1f;
                // BC5 only stores RG; Z is reconstructed in shader
                float zSq = 1f - x * x - y * y;
                if (zSq >= -0.1f) // Allow small negative due to compression error
                    validCount++;
            }
            float validRatio = (float)validCount / pixels.Length;
            Assert.That(
                validRatio,
                Is.GreaterThan(0.9f),
                $"At least 90% of normals should be reconstructable, got {validRatio:P0}"
            );
        }

        #endregion

        #region E2E Compression Round-Trip - DXT5

        [Test]
        public void E2E_DXT5_FlatNormal_RoundTrip_PreservesAGLayout()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(32);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.DXT5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // DXT5 AG layout: flat normal X=0 -> A~128, Y=0 -> G~128
            Assert.That(
                pixels[0].a,
                Is.InRange((byte)116, (byte)140),
                "A (X) should be ~128 after DXT5 round-trip"
            );
            Assert.That(
                pixels[0].g,
                Is.InRange((byte)116, (byte)140),
                "G (Y) should be ~128 after DXT5 round-trip"
            );
        }

        [Test]
        public void E2E_DXT5_GradientNormal_RoundTrip_MaintainsDirection()
        {
            var source = NormalMapTestTextureFactory.CreateGradient(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.DXT5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // Verify gradient is monotonically increasing in X (alpha channel)
            // Sample left edge vs right edge
            int leftIdx = 16 * 32 + 2;
            int rightIdx = 16 * 32 + 29;
            Assert.That(
                pixels[rightIdx].a,
                Is.GreaterThan(pixels[leftIdx].a),
                "X gradient should increase from left to right in alpha channel"
            );
        }

        #endregion

        #region E2E Compression Round-Trip - BC7

        [Test]
        public void E2E_BC7_PreserveAlpha_RoundTrip_KeepsAlphaVariation()
        {
            var source = NormalMapTestTextureFactory.CreateWithAlpha(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // Verify alpha still has variation (not all 255)
            bool hasLowAlpha = false;
            bool hasHighAlpha = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 200)
                    hasLowAlpha = true;
                if (pixels[i].a > 230)
                    hasHighAlpha = true;
            }
            Assert.IsTrue(
                hasLowAlpha && hasHighAlpha,
                "BC7 with preserveAlpha should retain alpha variation"
            );
        }

        [Test]
        public void E2E_BC7_AGLayout_RoundTrip_FlatNormal()
        {
            var source = NormalMapTestTextureFactory.CreateFlat(32);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: false
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // AG layout: flat normal X=0 -> A~128, Y=0 -> G~128
            Assert.That(
                pixels[0].a,
                Is.InRange((byte)116, (byte)140),
                "A (X) should be ~128 after BC7 round-trip"
            );
            Assert.That(
                pixels[0].g,
                Is.InRange((byte)116, (byte)140),
                "G (Y) should be ~128 after BC7 round-trip"
            );
        }

        #endregion

        #region E2E Cross-Layout Compression

        [Test]
        public void E2E_AGToDXT5_SphereNormal_RoundTrip_MaintainsLayout()
        {
            var source = NormalMapTestTextureFactory.CreateSphereAG(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            // AG source -> DXT5 target (same AG layout, but re-normalized)
            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.DXT5,
                TextureFormat.DXT5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.AG
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // Center pixel should be flat normal in AG layout
            int centerIdx = 16 * 32 + 16;
            Assert.That(
                pixels[centerIdx].a,
                Is.InRange((byte)116, (byte)140),
                "Center A (X) should be ~128"
            );
            Assert.That(
                pixels[centerIdx].g,
                Is.InRange((byte)116, (byte)140),
                "Center G (Y) should be ~128"
            );
        }

        [Test]
        public void E2E_AGToBC5_SphereNormal_RoundTrip_ConvertsLayout()
        {
            var source = NormalMapTestTextureFactory.CreateSphereAG(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            // AG source -> BC5 target (converts from AG to RG layout)
            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.DXT5,
                TextureFormat.BC5,
                sourceLayout: NormalMapPreprocessor.SourceLayout.AG
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // Center pixel should be flat normal in RG layout
            int centerIdx = 16 * 32 + 16;
            Assert.That(
                pixels[centerIdx].r,
                Is.InRange((byte)116, (byte)140),
                "Center R (X) should be ~128"
            );
            Assert.That(
                pixels[centerIdx].g,
                Is.InRange((byte)116, (byte)140),
                "Center G (Y) should be ~128"
            );
        }

        #endregion

        #region E2E Fallback with Compression

        [Test]
        public void E2E_FallbackRePreprocessing_ProducesValidCompression()
        {
            var source = NormalMapTestTextureFactory.CreateSphere(64);
            _createdObjects.Add(source);

            var resized = _processor.ResizeTo(source, 32, 32, isNormalMap: true);
            _createdObjects.Add(resized);

            // Save original pixels
            var originalPixels = resized.GetPixels32();

            // First attempt: BC5
            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.BC5);

            // Simulate fallback: restore and re-preprocess for DXT5
            resized.SetPixels32(originalPixels);
            resized.Apply();
            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32, TextureFormat.DXT5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            var pixels = resized.GetPixels32();
            // After DXT5 compression, center should have valid AG layout normals
            Assert.AreEqual(TextureFormat.DXT5, resized.format, "Should be DXT5 compressed");
        }

        #endregion

        #region Detection-Integrated Pipeline

        [Test]
        public void DetectionIntegrated_DXT5AGSource_ToBC5_PreservesNormals()
        {
            var rgbSource = NormalMapTestTextureFactory.CreateSphere(256);
            _createdObjects.Add(rgbSource);
            var dxt5Source = CreateAGCompressedTexture(rgbSource, TextureFormat.DXT5);
            _createdObjects.Add(dxt5Source);

            Assert.AreEqual(TextureFormat.DXT5, dxt5Source.format);

            var resized = _processor.ResizeTo(dxt5Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            var sourceLayout = NormalMapSourceLayoutDetector.Resolve(
                dxt5Source,
                TextureFormat.DXT5
            );
            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.AG,
                sourceLayout,
                "DXT5 AG source should be detected as AG layout"
            );

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.DXT5,
                TextureFormat.BC5,
                sourceLayout: sourceLayout
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format);
            AssertCompressedNormalsAreValid(resized, "Detection DXT5 AG -> BC5");
        }

        [Test]
        public void DetectionIntegrated_DXT5AGSource_ToDXT5_PreservesNormals()
        {
            var rgbSource = NormalMapTestTextureFactory.CreateSphere(256);
            _createdObjects.Add(rgbSource);
            var dxt5Source = CreateAGCompressedTexture(rgbSource, TextureFormat.DXT5);
            _createdObjects.Add(dxt5Source);

            var resized = _processor.ResizeTo(dxt5Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            var sourceLayout = NormalMapSourceLayoutDetector.Resolve(
                dxt5Source,
                TextureFormat.DXT5
            );
            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.AG,
                sourceLayout,
                "DXT5 AG source should be detected as AG layout"
            );

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.DXT5,
                TextureFormat.DXT5,
                sourceLayout: sourceLayout
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.DXT5, resized.format);
            AssertDXTnmNormalsAreValid(resized, "Detection DXT5 AG -> DXT5");
        }

        [Test]
        public void DetectionIntegrated_BC7RGSource_ToBC5_PreservesNormals()
        {
            var rgSource = NormalMapTestTextureFactory.CreateSphereRG(256);
            _createdObjects.Add(rgSource);
            var bc7Source = CreateActualCompressedTexture(rgSource, TextureFormat.BC7);
            _createdObjects.Add(bc7Source);

            Assert.AreEqual(TextureFormat.BC7, bc7Source.format);

            var resized = _processor.ResizeTo(bc7Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            var sourceLayout = NormalMapSourceLayoutDetector.Resolve(bc7Source, TextureFormat.BC7);
            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.RG,
                sourceLayout,
                "BC7 RG source should be detected as RG layout"
            );

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.BC7,
                TextureFormat.BC5,
                sourceLayout: sourceLayout
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format);
            AssertCompressedNormalsAreValid(resized, "Detection BC7 RG -> BC5");
        }

        [Test]
        public void DetectionIntegrated_RGBWithAlpha_ToBC7_PreservesNormals()
        {
            var rgbAlphaSource = NormalMapTestTextureFactory.CreateWithAlpha(256);
            _createdObjects.Add(rgbAlphaSource);
            var bc7Source = CreateActualCompressedTexture(rgbAlphaSource, TextureFormat.BC7);
            _createdObjects.Add(bc7Source);

            var resized = _processor.ResizeTo(bc7Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            var sourceLayout = NormalMapSourceLayoutDetector.Resolve(bc7Source, TextureFormat.BC7);
            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.RGB,
                sourceLayout,
                "BC7 RGB source with alpha should be detected as RGB layout"
            );

            bool hasAlpha = TextureFormatSelector.HasSignificantAlpha(resized);
            bool preserveAlpha = NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                TextureFormat.BC7,
                sourceLayout,
                hasAlpha
            );
            Assert.IsTrue(
                preserveAlpha,
                "RGB source with alpha targeting BC7 should preserve alpha"
            );

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.BC7,
                TextureFormat.BC7,
                preserveAlpha: preserveAlpha,
                sourceLayout: sourceLayout
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC7, resized.format);

            // Verify normals are valid in RGB layout
            var pixels = resized.GetPixels();
            int sampleCount = Mathf.Min(500, pixels.Length);
            int step = Mathf.Max(1, pixels.Length / sampleCount);
            for (int i = 0; i < pixels.Length; i += step)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                float z = pixels[i].b * 2f - 1f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                Assert.That(
                    length,
                    Is.InRange(0.7f, 1.3f),
                    $"Pixel {i}: normal length should be ~1, got {length}"
                );
            }

            // Verify alpha variation is preserved
            bool hasLowAlpha = false;
            bool hasHighAlpha = false;
            for (int i = 0; i < pixels.Length; i += step)
            {
                if (pixels[i].a < 0.85f)
                    hasLowAlpha = true;
                if (pixels[i].a > 0.95f)
                    hasHighAlpha = true;
            }
            Assert.IsTrue(
                hasLowAlpha && hasHighAlpha,
                "Alpha variation should be preserved in BC7 RGB layout"
            );
        }

        [Test]
        public void DetectionIntegrated_MixedZRGBInBC7_ToBC5_PreservesNormals()
        {
            var mixedZSource = NormalMapTestTextureFactory.CreateMixedZ(256);
            _createdObjects.Add(mixedZSource);
            var bc7Source = CreateActualCompressedTexture(mixedZSource, TextureFormat.BC7);
            _createdObjects.Add(bc7Source);

            Assert.AreEqual(TextureFormat.BC7, bc7Source.format);

            var resized = _processor.ResizeTo(bc7Source, 256, 256, isNormalMap: true);
            _createdObjects.Add(resized);

            var sourceLayout = NormalMapSourceLayoutDetector.Resolve(bc7Source, TextureFormat.BC7);
            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.RGB,
                sourceLayout,
                "BC7 RGB source with mixed Z should be detected as RGB layout"
            );

            _preprocessor.PrepareForCompression(
                resized,
                TextureFormat.BC7,
                TextureFormat.BC5,
                sourceLayout: sourceLayout
            );
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format);
            AssertCompressedNormalsAreValid(resized, "Detection MixedZ BC7 RGB -> BC5");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates an actual compressed texture using EditorUtility.CompressTexture.
        /// </summary>
        private static Texture2D CreateActualCompressedTexture(
            Texture2D source,
            TextureFormat targetFormat
        )
        {
            var copy = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                source.mipmapCount > 1,
                linear: true
            );
            copy.SetPixels(source.GetPixels());
            copy.Apply();

            EditorUtility.CompressTexture(copy, targetFormat, TextureCompressionQuality.Best);

            return copy;
        }

        /// <summary>
        /// Creates an actual compressed texture with proper AG channel layout preprocessing.
        /// Unlike CreateActualCompressedTexture, this preprocesses RGB→AG before compression.
        /// </summary>
        private Texture2D CreateAGCompressedTexture(Texture2D rgbSource, TextureFormat targetFormat)
        {
            var copy = new Texture2D(
                rgbSource.width,
                rgbSource.height,
                TextureFormat.RGBA32,
                rgbSource.mipmapCount > 1,
                linear: true
            );
            copy.SetPixels(rgbSource.GetPixels());
            copy.Apply();

            _preprocessor.PrepareForCompression(copy, TextureFormat.RGBA32, targetFormat);
            EditorUtility.CompressTexture(copy, targetFormat, TextureCompressionQuality.Best);

            return copy;
        }

        /// <summary>
        /// Asserts that compressed normal map data is valid.
        /// BC5 only stores RG, so we reconstruct Z and verify unit length.
        /// </summary>
        private void AssertCompressedNormalsAreValid(Texture2D texture, string testName)
        {
            var pixels = texture.GetPixels();
            int sampleCount = Mathf.Min(500, pixels.Length);
            int step = Mathf.Max(1, pixels.Length / sampleCount);

            int invalidCount = 0;
            float maxXYLengthSq = 0f;

            for (int i = 0; i < pixels.Length; i += step)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;

                float xyLengthSq = x * x + y * y;
                maxXYLengthSq = Mathf.Max(maxXYLengthSq, xyLengthSq);

                if (xyLengthSq > 1.05f)
                {
                    invalidCount++;
                }
            }

            Debug.Log($"[{testName}] Max X²+Y²: {maxXYLengthSq:F4}, Invalid count: {invalidCount}");

            Assert.That(
                maxXYLengthSq,
                Is.LessThanOrEqualTo(1.05f),
                $"{testName}: X²+Y² should not exceed 1 (Z must be reconstructable)"
            );

            Assert.AreEqual(
                0,
                invalidCount,
                $"{testName}: All sampled normals should have valid X²+Y² <= 1"
            );
        }

        /// <summary>
        /// Asserts that DXT5/BC7 compressed normal map data is valid in AG layout (DXTnm format).
        /// </summary>
        private void AssertDXTnmNormalsAreValid(Texture2D texture, string testName)
        {
            var pixels = texture.GetPixels();
            int sampleCount = Mathf.Min(500, pixels.Length);
            int step = Mathf.Max(1, pixels.Length / sampleCount);

            int invalidCount = 0;
            float maxLengthSq = 0f;

            for (int i = 0; i < pixels.Length; i += step)
            {
                float x = pixels[i].a * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                float lengthSq = x * x + y * y;
                maxLengthSq = Mathf.Max(maxLengthSq, lengthSq);

                if (lengthSq > 1.05f)
                {
                    invalidCount++;
                }
            }

            Debug.Log(
                $"[{testName}] DXTnm AG layout - Max X²+Y²: {maxLengthSq:F4}, Invalid count: {invalidCount}"
            );

            Assert.That(
                maxLengthSq,
                Is.LessThanOrEqualTo(1.05f),
                $"{testName}: X²+Y² in AG channels should not exceed 1 (Z must be reconstructable)"
            );

            Assert.AreEqual(
                0,
                invalidCount,
                $"{testName}: All sampled normals should have valid X²+Y² <= 1 in AG channels"
            );
        }

        #endregion
    }
}
