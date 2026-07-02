using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class CpuAreaAverageResizerTests
    {
        private CpuAreaAverageResizer _resizer;

        [SetUp]
        public void SetUp()
        {
            _resizer = new CpuAreaAverageResizer();
        }

        #region AxisPlan Tests

        [Test]
        public void BuildPlan_IntegerRatio2x_UniformWeights()
        {
            var plan = CpuAreaAverageResizer.BuildPlan(4, 2);

            Assert.AreEqual(2, plan.Start.Length);
            Assert.AreEqual(2, plan.Len[0]);
            Assert.AreEqual(2, plan.Len[1]);

            Assert.That(plan.Weights[plan.Offset[0]], Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(plan.Weights[plan.Offset[0] + 1], Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void BuildPlan_IntegerRatio4x_UniformWeights()
        {
            var plan = CpuAreaAverageResizer.BuildPlan(8, 2);

            Assert.AreEqual(2, plan.Start.Length);
            Assert.AreEqual(4, plan.Len[0]);
            Assert.AreEqual(4, plan.Len[1]);

            for (int t = 0; t < 4; t++)
            {
                Assert.That(
                    plan.Weights[plan.Offset[0] + t],
                    Is.EqualTo(0.25f).Within(1e-5f),
                    $"Weight at tap {t} should be 0.25"
                );
            }
        }

        [Test]
        public void BuildPlan_NonIntegerRatio_HasFractionalWeights()
        {
            // 3 -> 2: scale = 1.5, pixel 0 covers [0, 1.5), pixel 1 covers [1.5, 3)
            var plan = CpuAreaAverageResizer.BuildPlan(3, 2);

            Assert.AreEqual(2, plan.Start.Length);

            // Output pixel 0: covers src [0, 1.5) -> taps at src 0 (weight 1.0) and src 1 (weight 0.5)
            Assert.AreEqual(0, plan.Start[0]);
            Assert.AreEqual(2, plan.Len[0]);

            float w0 = plan.Weights[plan.Offset[0]];
            float w1 = plan.Weights[plan.Offset[0] + 1];
            Assert.That(w0, Is.GreaterThan(w1), "First tap should have more weight");
            Assert.That(w0 + w1, Is.EqualTo(1f).Within(1e-5f), "Weights should sum to 1");
        }

        [Test]
        public void BuildPlan_SameSize_IdentityWeights()
        {
            var plan = CpuAreaAverageResizer.BuildPlan(4, 4);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, plan.Start[i]);
                Assert.AreEqual(1, plan.Len[i]);
                Assert.That(plan.Weights[plan.Offset[i]], Is.EqualTo(1f).Within(1e-5f));
            }
        }

        [Test]
        public void BuildPlan_Upscale2x_UsesBilinearTaps()
        {
            var plan = CpuAreaAverageResizer.BuildPlan(4, 8);

            // dst 3 center maps to src 1.25 -> taps at src 1 (0.75) and src 2 (0.25)
            Assert.AreEqual(1, plan.Start[3]);
            Assert.AreEqual(2, plan.Len[3]);
            Assert.That(plan.Weights[plan.Offset[3]], Is.EqualTo(0.75f).Within(1e-5f));
            Assert.That(plan.Weights[plan.Offset[3] + 1], Is.EqualTo(0.25f).Within(1e-5f));

            // edge pixels clamp to a single source tap
            Assert.AreEqual(1, plan.Len[0]);
            Assert.That(plan.Weights[plan.Offset[0]], Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void BuildPlan_ToSinglePixel_AllSourcePixelsContribute()
        {
            var plan = CpuAreaAverageResizer.BuildPlan(8, 1);

            Assert.AreEqual(1, plan.Start.Length);
            Assert.AreEqual(0, plan.Start[0]);
            Assert.AreEqual(8, plan.Len[0]);

            float sum = 0f;
            for (int t = 0; t < 8; t++)
            {
                Assert.That(plan.Weights[plan.Offset[0] + t], Is.EqualTo(0.125f).Within(1e-5f));
                sum += plan.Weights[plan.Offset[0] + t];
            }
            Assert.That(sum, Is.EqualTo(1f).Within(1e-5f));
        }

        #endregion

        #region Resize Color Tests

        [Test]
        public void Resize_2x2To1x1_AveragesInLinearSpace()
        {
            // sRGB source: channels are decoded to linear, averaged, then
            // re-encoded. avg(1,0,0,1) = 0.5 linear -> 0.735 sRGB;
            // avg(0,0,1,0) = 0.25 linear -> 0.537 sRGB.
            var source = CreateTextureWithPixels(
                2,
                2,
                new[]
                {
                    new Color(1, 0, 0, 1),
                    new Color(0, 1, 0, 1),
                    new Color(0, 0, 1, 1),
                    new Color(1, 1, 0, 1),
                }
            );

            var result = _resizer.Resize(source, 1, 1, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.width);
            Assert.AreEqual(1, result.height);

            var pixel = result.GetPixel(0, 0);
            Assert.That(pixel.r, Is.EqualTo(0.735f).Within(0.02f));
            Assert.That(pixel.g, Is.EqualTo(0.735f).Within(0.02f));
            Assert.That(pixel.b, Is.EqualTo(0.537f).Within(0.02f));
            Assert.That(pixel.a, Is.EqualTo(1f).Within(0.02f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_4x4To2x2_BlockAverages()
        {
            var pixels = new Color[16];
            pixels[0] = pixels[1] = Color.red;
            pixels[2] = pixels[3] = Color.green;
            pixels[4] = pixels[5] = Color.red;
            pixels[6] = pixels[7] = Color.green;
            pixels[8] = pixels[9] = Color.blue;
            pixels[10] = pixels[11] = Color.white;
            pixels[12] = pixels[13] = Color.blue;
            pixels[14] = pixels[15] = Color.white;

            var source = CreateTextureWithPixels(4, 4, pixels);

            var result = _resizer.Resize(source, 2, 2, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var resultPixels = result.GetPixels();

            Assert.That(resultPixels[0].r, Is.EqualTo(1f).Within(0.02f));
            Assert.That(resultPixels[0].g, Is.EqualTo(0f).Within(0.02f));

            Assert.That(resultPixels[1].g, Is.EqualTo(1f).Within(0.02f));
            Assert.That(resultPixels[1].r, Is.EqualTo(0f).Within(0.02f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_SolidColor_PreservesColor()
        {
            var color = new Color(0.3f, 0.6f, 0.9f, 0.7f);
            var source = CreateSolidTexture(64, 64, color);

            var result = _resizer.Resize(source, 16, 16, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var pixel = result.GetPixel(8, 8);
            Assert.That(pixel.r, Is.EqualTo(color.r).Within(0.02f));
            Assert.That(pixel.g, Is.EqualTo(color.g).Within(0.02f));
            Assert.That(pixel.b, Is.EqualTo(color.b).Within(0.02f));
            Assert.That(pixel.a, Is.EqualTo(color.a).Within(0.02f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_Checkerboard_AveragesInLinearSpace()
        {
            // avg(decode(0), decode(1)) = 0.5 linear -> 0.735 sRGB
            int size = 64;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = ((x + y) % 2 == 0) ? Color.white : Color.black;
                }
            }
            var source = CreateTextureWithPixels(size, size, pixels);

            var result = _resizer.Resize(source, 1, 1, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var pixel = result.GetPixel(0, 0);
            Assert.That(pixel.r, Is.EqualTo(0.735f).Within(0.02f));
            Assert.That(pixel.g, Is.EqualTo(0.735f).Within(0.02f));
            Assert.That(pixel.b, Is.EqualTo(0.735f).Within(0.02f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_NonSquare_ReturnsCorrectDimensions()
        {
            var source = CreateSolidTexture(128, 64, Color.white);

            var result = _resizer.Resize(source, 32, 16, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(32, result.width);
            Assert.AreEqual(16, result.height);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_Upscale2x_InterpolatesBetweenPixels()
        {
            var pixels = new[] { new Color(0f, 0f, 0f, 1f), new Color(1f, 1f, 1f, 1f) };
            var source = CreateLinearTextureWithPixels(2, 1, pixels);

            var result = _resizer.Resize(source, 4, 1, forceLinearOutput: false);

            Assert.IsNotNull(result);
            // dst 1 maps to src 0.25, dst 2 to src 0.75 -> interpolated values,
            // not the nearest-neighbor replication a box plan would produce
            Assert.That(result.GetPixel(1, 0).r, Is.EqualTo(0.25f).Within(0.01f));
            Assert.That(result.GetPixel(2, 0).r, Is.EqualTo(0.75f).Within(0.01f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_MixedDownscaleAndUpscale_ReturnsCorrectDimensions()
        {
            var source = CreateLinearSolidTexture(128, 16, new Color(0.25f, 0.5f, 0.75f, 1f));

            var result = _resizer.Resize(source, 32, 32, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(32, result.width);
            Assert.AreEqual(32, result.height);
            var pixel = result.GetPixel(16, 16);
            Assert.That(pixel.r, Is.EqualTo(0.25f).Within(0.01f));
            Assert.That(pixel.g, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(pixel.b, Is.EqualTo(0.75f).Within(0.01f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Color Space Policy

        [Test]
        public void Resize_LinearTexture_PreservesValuesAndLinearFlag()
        {
            var source = CreateLinearSolidTexture(64, 64, new Color(0.5f, 0.5f, 0.5f, 1f));
            Assert.IsFalse(source.isDataSRGB, "sanity: linear source");

            var result = _resizer.Resize(source, 16, 16, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.isDataSRGB, "output must keep the source's linear flag");
            var pixel = result.GetPixel(8, 8);
            Assert.That(pixel.r, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(pixel.g, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(pixel.b, Is.EqualTo(0.5f).Within(0.01f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_SrgbTexture_KeepsSrgbFlag()
        {
            var source = CreateSolidTexture(64, 64, Color.gray);
            Assert.IsTrue(source.isDataSRGB, "sanity: default source is sRGB");

            var result = _resizer.Resize(source, 16, 16, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.isDataSRGB, "output must keep the source's sRGB flag");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_NonReadableSource_MatchesReadableSource()
        {
            var color = new Color(0.5f, 0.5f, 0.5f, 1f);
            var readable = CreateLinearSolidTexture(64, 64, color);
            var nonReadable = CreateLinearSolidTexture(64, 64, color);
            nonReadable.Apply(false, true);
            Assert.IsFalse(nonReadable.isReadable, "sanity: second texture is non-readable");

            var a = _resizer.Resize(readable, 16, 16, forceLinearOutput: false);
            var b = _resizer.Resize(nonReadable, 16, 16, forceLinearOutput: false);

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            var pa = a.GetPixel(8, 8);
            var pb = b.GetPixel(8, 8);
            Assert.That(pb.r, Is.EqualTo(pa.r).Within(0.01f));
            Assert.That(pb.g, Is.EqualTo(pa.g).Within(0.01f));
            Assert.That(pb.b, Is.EqualTo(pa.b).Within(0.01f));
            Assert.That(pb.a, Is.EqualTo(pa.a).Within(0.01f));

            Object.DestroyImmediate(readable);
            Object.DestroyImmediate(nonReadable);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void Resize_NonReadableSrgbSource_PreservesBytes_WhenSRGBWriteDisabled()
        {
            // The readback blit must not depend on ambient GL.sRGBWrite (editor
            // IMGUI leaves it false): without the write-side encode, the sRGB RT
            // stores linear bytes and the decode table double-decodes, darkening
            // mid-tones (128 -> ~55). Black/white are sRGB fixed points and
            // cannot catch this.
            var midGray = new Color(128f / 255f, 128f / 255f, 128f / 255f, 1f);
            var source = CreateSolidTexture(64, 64, midGray);
            source.Apply(false, true);
            Assert.IsFalse(source.isReadable, "sanity: source is non-readable");
            Assert.IsTrue(source.isDataSRGB, "sanity: source is sRGB");

            var previousSRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            Texture2D result;
            try
            {
                result = _resizer.Resize(source, 16, 16, forceLinearOutput: false);
            }
            finally
            {
                GL.sRGBWrite = previousSRGBWrite;
            }

            Assert.IsNotNull(result);
            var pixel = result.GetPixels32()[0];
            Assert.That(pixel.r, Is.InRange((byte)127, (byte)129));
            Assert.That(pixel.g, Is.InRange((byte)127, (byte)129));
            Assert.That(pixel.b, Is.InRange((byte)127, (byte)129));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_NonReadableSrgbSource_SameSize_PreservesBytes_WhenSRGBWriteDisabled()
        {
            // The same-size byte-copy path reads back through the same blit.
            var midGray = new Color(128f / 255f, 128f / 255f, 128f / 255f, 1f);
            var source = CreateSolidTexture(64, 64, midGray);
            source.Apply(false, true);

            var previousSRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            Texture2D result;
            try
            {
                result = _resizer.Resize(source, 64, 64, forceLinearOutput: false);
            }
            finally
            {
                GL.sRGBWrite = previousSRGBWrite;
            }

            Assert.IsNotNull(result);
            var pixel = result.GetPixels32()[0];
            Assert.That(pixel.r, Is.InRange((byte)127, (byte)129));
            Assert.That(pixel.g, Is.InRange((byte)127, (byte)129));
            Assert.That(pixel.b, Is.InRange((byte)127, (byte)129));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Same Size / Edge Cases

        [Test]
        public void Resize_SameSize_ReturnsCopy()
        {
            var source = CreateSolidTexture(64, 64, Color.red);

            var result = _resizer.Resize(source, 64, 64, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(64, result.width);
            Assert.AreEqual(64, result.height);
            Assert.AreNotSame(source, result);

            var pixel = result.GetPixel(0, 0);
            Assert.That(pixel.r, Is.EqualTo(1f).Within(0.02f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_NullSource_ReturnsNull()
        {
            var result = _resizer.Resize(null, 64, 64, forceLinearOutput: false);
            Assert.IsNull(result);
        }

        [Test]
        public void Resize_PreservesTextureSettings()
        {
            var source = CreateSolidTexture(64, 64, Color.white);
            source.wrapModeU = TextureWrapMode.Repeat;
            source.wrapModeV = TextureWrapMode.Clamp;
            source.filterMode = FilterMode.Trilinear;
            source.anisoLevel = 8;

            var result = _resizer.Resize(source, 32, 32, forceLinearOutput: false);

            Assert.AreEqual(TextureWrapMode.Repeat, result.wrapModeU);
            Assert.AreEqual(TextureWrapMode.Clamp, result.wrapModeV);
            Assert.AreEqual(FilterMode.Trilinear, result.filterMode);
            Assert.AreEqual(8, result.anisoLevel);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_PreservesMipmaps()
        {
            var source = new Texture2D(64, 64, TextureFormat.RGBA32, true);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            source.SetPixels(pixels);
            source.Apply(true);

            var result = _resizer.Resize(source, 32, 32, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.mipmapCount > 1);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Normal Map Tests

        [Test]
        public void Resize_NormalMap_FlatNormal_IsPreserved()
        {
            var flatNormal = new Color(0.5f, 0.5f, 1f, 1f); // (0,0,1) encoded
            var source = CreateLinearSolidTexture(64, 64, flatNormal);

            var result = _resizer.Resize(source, 16, 16, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var pixel = result.GetPixel(8, 8);
            Assert.That(pixel.r, Is.EqualTo(0.5f).Within(0.02f));
            Assert.That(pixel.g, Is.EqualTo(0.5f).Within(0.02f));
            Assert.That(pixel.b, Is.EqualTo(1f).Within(0.02f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_NormalMap_TiltedNormal_PreservesValues()
        {
            float nx = 0.5f;
            float ny = 0.5f;
            float nz = Mathf.Sqrt(1f - nx * nx - ny * ny);
            var encoded = new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f);
            var source = CreateLinearSolidTexture(64, 64, encoded);

            var result = _resizer.Resize(source, 16, 16, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var pixel = result.GetPixel(8, 8);
            Assert.That(pixel.r, Is.EqualTo(encoded.r).Within(0.02f));
            Assert.That(pixel.g, Is.EqualTo(encoded.g).Within(0.02f));
            Assert.That(pixel.b, Is.EqualTo(encoded.b).Within(0.02f));

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_NormalMap_NegativeZ_PreservesSign()
        {
            var encoded = new Color(0.5f, 0.5f, 0f, 1f); // (0, 0, -1)
            var source = CreateLinearSolidTexture(64, 64, encoded);

            var result = _resizer.Resize(source, 16, 16, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var pixel = result.GetPixel(8, 8);
            float zDecoded = pixel.b * 2f - 1f;
            Assert.That(zDecoded, Is.LessThan(0f), "Negative Z should be preserved");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_NormalMap_OpposingNormals_AveragesCorrectly()
        {
            var normalA = new Color(1f, 0.5f, 0.5f, 1f); // (1, 0, 0)
            var normalB = new Color(0f, 0.5f, 0.5f, 1f); // (-1, 0, 0)

            var pixels = new Color[4];
            pixels[0] = normalA;
            pixels[1] = normalB;
            pixels[2] = normalA;
            pixels[3] = normalB;
            var source = CreateLinearTextureWithPixels(2, 2, pixels);

            var result = _resizer.Resize(source, 1, 1, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var pixel = result.GetPixel(0, 0);
            Assert.That(pixel.r, Is.EqualTo(0.5f).Within(0.02f), "X should average to 0");
            Assert.That(pixel.g, Is.EqualTo(0.5f).Within(0.02f), "Y should be preserved");

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Point Filter

        [Test]
        public void Resize_PointFiltered_Downscale_PicksNearestTexels()
        {
            // 4x4 with a distinct color per 2x2 quadrant; nearest neighbor must
            // return exact palette colors, never blends.
            var colors = new Color[16];
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    colors[y * 4 + x] =
                        y < 2
                            ? (x < 2 ? Color.red : Color.green)
                            : (x < 2 ? Color.blue : Color.yellow);
                }
            }
            var source = CreateTextureWithPixels(4, 4, colors);
            source.filterMode = FilterMode.Point;

            var result = _resizer.Resize(source, 2, 2, forceLinearOutput: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(FilterMode.Point, result.filterMode);
            var pixels = result.GetPixels32();
            Assert.AreEqual(new Color32(255, 0, 0, 255), pixels[0]);
            Assert.AreEqual(new Color32(0, 255, 0, 255), pixels[1]);
            Assert.AreEqual(new Color32(0, 0, 255, 255), pixels[2]);
            Assert.AreEqual(new Color32(255, 255, 0, 255), pixels[3]);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Resize_PointFiltered_Upscale_ReplicatesTexels()
        {
            var colors = new[] { Color.red, Color.green, Color.blue, Color.yellow };
            var source = CreateTextureWithPixels(2, 2, colors);
            source.filterMode = FilterMode.Point;

            var result = _resizer.Resize(source, 4, 4, forceLinearOutput: false);

            Assert.IsNotNull(result);
            var pixels = result.GetPixels32();
            // Each source texel becomes an exact 2x2 block.
            Assert.AreEqual(new Color32(255, 0, 0, 255), pixels[0]);
            Assert.AreEqual(new Color32(255, 0, 0, 255), pixels[5]);
            Assert.AreEqual(new Color32(0, 255, 0, 255), pixels[2]);
            Assert.AreEqual(new Color32(0, 255, 0, 255), pixels[7]);
            Assert.AreEqual(new Color32(0, 0, 255, 255), pixels[8]);
            Assert.AreEqual(new Color32(0, 0, 255, 255), pixels[13]);
            Assert.AreEqual(new Color32(255, 255, 0, 255), pixels[10]);
            Assert.AreEqual(new Color32(255, 255, 0, 255), pixels[15]);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Integration with TextureProcessor

        [Test]
        public void TextureProcessor_WithCpuAreaAverageResizer_ReturnsCorrectDimensions()
        {
            var processor = new TextureProcessor(32, 2048, false, ResizeBackendPreference.CPU);
            var source = CreateSolidTexture(512, 512, Color.white);
            var analysis = new TextureAnalysisResult(0.5f, 2, new Vector2Int(256, 256));

            var result = processor.ResizeSingle(source, analysis, isNormalMap: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.width);
            Assert.AreEqual(256, result.height);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void TextureProcessor_AutoBackendPreference_ReturnsCorrectDimensions()
        {
            // Auto is exercised deliberately; assertions are dimension-only, so
            // the test stays safe on machines whose GPU results are untrusted.
            var processor = new TextureProcessor(32, 2048, false, ResizeBackendPreference.Auto);
            var source = CreateSolidTexture(256, 256, Color.white);
            var analysis = new TextureAnalysisResult(0.5f, 2, new Vector2Int(128, 128));

            var result = processor.ResizeSingle(source, analysis, isNormalMap: false);

            Assert.IsNotNull(result);
            Assert.AreEqual(128, result.width);
            Assert.AreEqual(128, result.height);

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(result);
        }

        #endregion

        #region Helpers

        private static Texture2D CreateTextureWithPixels(int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateLinearTextureWithPixels(
            int width,
            int height,
            Color[] pixels
        )
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear: true);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateLinearSolidTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear: true);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
