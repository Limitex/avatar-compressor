using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Tests for verifying that normal map data is preserved through the full preprocessing pipeline.
    /// Tests the combination of resize + preprocessor for different source/target format scenarios.
    /// </summary>
    [TestFixture]
    public class NormalMapPreprocessorPipelineTests
    {
        private TextureProcessor _processor;
        private NormalMapPreprocessor _preprocessor;

        [SetUp]
        public void SetUp()
        {
            _processor = new TextureProcessor(32, 2048, false);
            _preprocessor = new NormalMapPreprocessor();
        }

        #region Source Format Pipeline Tests

        [Test]
        [TestCase(TextureFormat.BC5, "BC5")]
        [TestCase(TextureFormat.RGBA32, "RGBA32")]
        public void FullPipeline_RGChannelSource_PreservesNormals(
            TextureFormat sourceFormat,
            string formatName
        )
        {
            // BC5 and RGBA32 both store XY in RG channels
            var source = NormalMapTestTextureFactory.CreateSphereNormal(256);

            var resized = _processor.ResizeTo(source, 256, 256, isNormalMap: true);

            _preprocessor.PrepareForCompression(resized, sourceFormat);

            AssertNormalsAreValid(resized, $"{formatName} SphereNormal");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(resized);
        }

        [Test]
        [TestCase(TextureFormat.DXT5, "DXT5")]
        [TestCase(TextureFormat.BC7, "BC7")]
        public void FullPipeline_DXTnmSource_PreservesNormals(
            TextureFormat sourceFormat,
            string formatName
        )
        {
            // DXTnm formats (DXT5, BC7) store XY in AG channels
            // Create a texture with data in AG channels to simulate DXTnm layout
            var source = CreateDXTnmLayoutTexture(256);

            var resized = _processor.ResizeTo(source, 256, 256, isNormalMap: true);

            _preprocessor.PrepareForCompression(resized, sourceFormat);

            AssertNormalsAreValid(resized, $"{formatName} DXTnm SphereNormal");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(resized);
        }

        [Test]
        public void FullPipeline_BC5Source_RecalculatesPositiveZ()
        {
            // BC5 source doesn't have Z, so Z must be recalculated as positive (tangent space)
            var source = NormalMapTestTextureFactory.CreateSphereNormal(256);

            var resized = _processor.ResizeTo(source, 256, 256, isNormalMap: true);

            _preprocessor.PrepareForCompression(resized, TextureFormat.BC5);

            var resultPixels = resized.GetPixels();

            // Verify Z was recalculated correctly (should be positive for tangent space)
            int sampleCount = 100;
            int step = resultPixels.Length / sampleCount;
            for (int i = 0; i < resultPixels.Length; i += step)
            {
                var normal = NormalMapTestTextureFactory.DecodeNormal(resultPixels[i]);
                float length = normal.magnitude;

                Assert.That(
                    length,
                    Is.EqualTo(1f).Within(0.02f),
                    $"Normal at {i} should be normalized"
                );
                Assert.That(
                    normal.z,
                    Is.GreaterThanOrEqualTo(0f),
                    $"Z at {i} should be non-negative for tangent space"
                );
            }

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(resized);
        }

        [Test]
        public void FullPipeline_DXTnmSource_RecalculatesPositiveZ()
        {
            // DXTnm source doesn't have Z, so Z must be recalculated as positive
            var source = CreateDXTnmLayoutTexture(256);

            var resized = _processor.ResizeTo(source, 256, 256, isNormalMap: true);

            _preprocessor.PrepareForCompression(resized, TextureFormat.DXT5);

            var resultPixels = resized.GetPixels();

            int sampleCount = 100;
            int step = resultPixels.Length / sampleCount;
            for (int i = 0; i < resultPixels.Length; i += step)
            {
                var normal = NormalMapTestTextureFactory.DecodeNormal(resultPixels[i]);

                Assert.That(
                    normal.z,
                    Is.GreaterThanOrEqualTo(0f),
                    $"Z at {i} should be non-negative for DXTnm source"
                );
            }

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(resized);
        }

        [Test]
        public void FullPipeline_RGBSource_ObjectSpaceNormal_PreservesZSign()
        {
            // Object space normal with negative Z should preserve the sign when source is RGB
            var source = NormalMapTestTextureFactory.CreateMixedZNormal(256);
            var sourcePixels = source.GetPixels();

            var resized = _processor.ResizeTo(source, 256, 256, isNormalMap: true);

            // RGB source format (RGBA32) preserves Z sign for Object Space normal maps
            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32);

            var resultPixels = resized.GetPixels();

            // Check that Z sign gradient is preserved
            // Left side should have negative Z, right side positive Z
            for (int x = 0; x < 256; x += 32)
            {
                int index = 128 * 256 + x;
                var sourceNormal = NormalMapTestTextureFactory.DecodeNormal(sourcePixels[index]);
                var resultNormal = NormalMapTestTextureFactory.DecodeNormal(resultPixels[index]);

                bool sourceZNegative = sourceNormal.z < 0;
                bool resultZNegative = resultNormal.z < 0;

                Assert.AreEqual(
                    sourceZNegative,
                    resultZNegative,
                    $"Z sign should be preserved at x={x}"
                );
            }

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(resized);
        }

        #endregion

        #region Actual Compressed Texture E2E Tests

        /// <summary>
        /// Tests BC5 to BC5 compression pipeline with actual compressed textures.
        /// </summary>
        [Test]
        public void FullPipeline_BC5ToBC5_PreservesNormals()
        {
            // Create a normal map and compress it to BC5 (simulating existing BC5 asset)
            var originalSource = NormalMapTestTextureFactory.CreateSphereNormal(256);
            var bc5Source = CreateActualCompressedTexture(originalSource, TextureFormat.BC5);

            // Verify source is actually BC5
            Assert.AreEqual(TextureFormat.BC5, bc5Source.format, "Source should be BC5 format");

            // Pass through the full pipeline (same as TextureCompressorService)
            var resized = _processor.ResizeTo(bc5Source, 256, 256, isNormalMap: true);

            // Apply preprocessor with BC5 source format
            _preprocessor.PrepareForCompression(resized, TextureFormat.BC5);

            // Compress to BC5
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            // Verify the output is valid
            Assert.AreEqual(TextureFormat.BC5, resized.format, "Output should be BC5 format");
            AssertCompressedNormalsAreValid(resized, "BC5 to BC5");

            Object.DestroyImmediate(originalSource);
            Object.DestroyImmediate(bc5Source);
            Object.DestroyImmediate(resized);
        }

        /// <summary>
        /// Tests DXT5 (DXTnm) to BC5 compression pipeline with actual compressed textures.
        /// </summary>
        [Test]
        public void FullPipeline_DXT5ToBC5_PreservesNormals()
        {
            // Create a normal map and compress it to DXT5 (DXTnm format)
            var originalSource = NormalMapTestTextureFactory.CreateSphereNormal(256);
            var dxt5Source = CreateActualCompressedTexture(originalSource, TextureFormat.DXT5);

            Assert.AreEqual(TextureFormat.DXT5, dxt5Source.format, "Source should be DXT5 format");

            var resized = _processor.ResizeTo(dxt5Source, 256, 256, isNormalMap: true);

            // Apply preprocessor with DXT5 source format (reads from AG channels)
            _preprocessor.PrepareForCompression(resized, TextureFormat.DXT5);

            // Compress to BC5
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format, "Output should be BC5 format");
            AssertCompressedNormalsAreValid(resized, "DXT5 to BC5");

            Object.DestroyImmediate(originalSource);
            Object.DestroyImmediate(dxt5Source);
            Object.DestroyImmediate(resized);
        }

        /// <summary>
        /// Tests BC7 (DXTnm) to BC5 compression pipeline with actual compressed textures.
        /// </summary>
        [Test]
        public void FullPipeline_BC7ToBC5_PreservesNormals()
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphereNormal(256);
            var bc7Source = CreateActualCompressedTexture(originalSource, TextureFormat.BC7);

            Assert.AreEqual(TextureFormat.BC7, bc7Source.format, "Source should be BC7 format");

            var resized = _processor.ResizeTo(bc7Source, 256, 256, isNormalMap: true);

            // Apply preprocessor with BC7 source format (reads from AG channels)
            _preprocessor.PrepareForCompression(resized, TextureFormat.BC7);

            // Compress to BC5
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            Assert.AreEqual(TextureFormat.BC5, resized.format, "Output should be BC5 format");
            AssertCompressedNormalsAreValid(resized, "BC7 to BC5");

            Object.DestroyImmediate(originalSource);
            Object.DestroyImmediate(bc7Source);
            Object.DestroyImmediate(resized);
        }

        /// <summary>
        /// Tests BC5 to BC5 with actual downscaling (common real-world scenario).
        /// </summary>
        [Test]
        [TestCase(512, 256)]
        [TestCase(512, 128)]
        [TestCase(1024, 256)]
        public void FullPipeline_BC5ToBC5_WithDownscale_PreservesNormals(
            int sourceSize,
            int targetSize
        )
        {
            var originalSource = NormalMapTestTextureFactory.CreateSphereNormal(sourceSize);
            var bc5Source = CreateActualCompressedTexture(originalSource, TextureFormat.BC5);

            var resized = _processor.ResizeTo(bc5Source, targetSize, targetSize, isNormalMap: true);
            _preprocessor.PrepareForCompression(resized, TextureFormat.BC5);
            EditorUtility.CompressTexture(
                resized,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );

            AssertCompressedNormalsAreValid(resized, $"BC5 {sourceSize}->{targetSize}");

            Object.DestroyImmediate(originalSource);
            Object.DestroyImmediate(bc5Source);
            Object.DestroyImmediate(resized);
        }

        /// <summary>
        /// Verifies that Graphics.Blit correctly reads BC5 data into RG channels.
        /// This test documents the expected behavior of Unity's BC5 handling.
        /// </summary>
        [Test]
        public void GraphicsBlit_BC5Texture_ReadsIntoRGChannels()
        {
            // Create a known normal and compress to BC5
            var source = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            var pixels = new Color[64 * 64];

            // Fill with a tilted normal: (0.5, 0.3, z)
            float inputX = 0.5f;
            float inputY = 0.3f;
            float inputZ = Mathf.Sqrt(1f - inputX * inputX - inputY * inputY);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = NormalMapTestTextureFactory.EncodeNormal(inputX, inputY, inputZ);
            }
            source.SetPixels(pixels);
            source.Apply();

            // Compress to BC5
            var bc5Texture = CreateActualCompressedTexture(source, TextureFormat.BC5);

            // Blit through pipeline (this is what TextureProcessor.ResizeTo does)
            var blitResult = _processor.ResizeTo(bc5Texture, 64, 64, isNormalMap: true);

            var resultPixels = blitResult.GetPixels();

            // BC5 stores XY in RG channels, after blit these should be in RG of RGBA32
            float resultX = resultPixels[32 * 64 + 32].r * 2f - 1f;
            float resultY = resultPixels[32 * 64 + 32].g * 2f - 1f;

            // Allow tolerance for BC5 compression artifacts
            Assert.That(
                resultX,
                Is.EqualTo(inputX).Within(0.1f),
                "X should be preserved in R channel after blit"
            );
            Assert.That(
                resultY,
                Is.EqualTo(inputY).Within(0.1f),
                "Y should be preserved in G channel after blit"
            );

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(bc5Texture);
            Object.DestroyImmediate(blitResult);
        }

        /// <summary>
        /// Creates an actual compressed texture using EditorUtility.CompressTexture.
        /// </summary>
        private static Texture2D CreateActualCompressedTexture(
            Texture2D source,
            TextureFormat targetFormat
        )
        {
            // Create a copy to avoid modifying the original
            var copy = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                source.mipmapCount > 1,
                linear: true
            );
            copy.SetPixels(source.GetPixels());
            copy.Apply();

            // Actually compress the texture
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
                // BC5 stores XY in RG channels
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;

                float xyLengthSq = x * x + y * y;
                maxXYLengthSq = Mathf.Max(maxXYLengthSq, xyLengthSq);

                // X² + Y² must be <= 1 for Z to be reconstructable
                if (xyLengthSq > 1.05f) // Small tolerance for compression artifacts
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

        #endregion

        #region Resize + Preprocess Combined Tests

        [Test]
        [TestCase(512, 256)]
        [TestCase(256, 128)]
        [TestCase(128, 64)]
        public void FullPipeline_ActualResize_PreservesNormalDirection(
            int sourceSize,
            int targetSize
        )
        {
            var source = NormalMapTestTextureFactory.CreateSphereNormal(sourceSize);

            var resized = _processor.ResizeTo(source, targetSize, targetSize, isNormalMap: true);

            // Apply preprocessor
            _preprocessor.PrepareForCompression(resized, TextureFormat.RGBA32);

            AssertNormalsAreValid(resized, $"Resize {sourceSize}->{targetSize}");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(resized);
        }

        #endregion

        #region Assertion Helpers

        private void AssertNormalsAreValid(Texture2D texture, string testName)
        {
            var pixels = texture.GetPixels();
            int sampleCount = Mathf.Min(500, pixels.Length);
            int step = Mathf.Max(1, pixels.Length / sampleCount);

            int invalidCount = 0;
            float maxLengthDeviation = 0f;

            for (int i = 0; i < pixels.Length; i += step)
            {
                var normal = NormalMapTestTextureFactory.DecodeNormal(pixels[i]);
                float length = normal.magnitude;
                float deviation = Mathf.Abs(length - 1f);

                maxLengthDeviation = Mathf.Max(maxLengthDeviation, deviation);

                if (deviation > 0.05f)
                {
                    invalidCount++;
                }
            }

            Debug.Log(
                $"[{testName}] Max length deviation: {maxLengthDeviation:F4}, Invalid count: {invalidCount}"
            );

            Assert.That(
                maxLengthDeviation,
                Is.LessThanOrEqualTo(0.05f),
                $"{testName}: Normal vectors should be approximately unit length"
            );

            Assert.AreEqual(0, invalidCount, $"{testName}: All sampled normals should be valid");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a texture with sphere normal data in DXTnm layout (XY in AG channels).
        /// Simulates data as it would be read from a DXT5/BC7 normal map.
        /// </summary>
        private static Texture2D CreateDXTnmLayoutTexture(int size)
        {
            var texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: true
            );

            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Generate sphere normal
                    float cx = x / (float)size - 0.5f;
                    float cy = y / (float)size - 0.5f;
                    float dist = Mathf.Sqrt(cx * cx + cy * cy);

                    float nx,
                        ny;
                    if (dist > 0.45f)
                    {
                        nx = 0f;
                        ny = 0f;
                    }
                    else
                    {
                        nx = cx * 2f;
                        ny = cy * 2f;
                        float len = Mathf.Sqrt(nx * nx + ny * ny);
                        if (len > 0.0001f)
                        {
                            float nz = Mathf.Sqrt(Mathf.Max(0, 1f - nx * nx - ny * ny));
                            float totalLen = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                            nx /= totalLen;
                            ny /= totalLen;
                        }
                    }

                    // Encode in DXTnm layout: X in A channel, Y in G channel
                    // R and B are unused in DXTnm
                    float encodedA = nx * 0.5f + 0.5f;
                    float encodedG = ny * 0.5f + 0.5f;

                    pixels[y * size + x] = new Color(0f, encodedG, 0f, encodedA);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
