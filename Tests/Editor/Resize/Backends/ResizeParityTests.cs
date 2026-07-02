using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ResizeParityTests
    {
        private const float ChannelTolerance = 0.02f;

        private const string ShaderPath =
            "Packages/dev.limitex.avatar-compressor/"
            + "Editor/TextureCompressor/Resize/Shaders/AreaAverageResize.compute";

        private CpuAreaAverageResizer _cpuResizer;
        private GpuAreaAverageResizer _gpuResizer;
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<Object>();
            _cpuResizer = new CpuAreaAverageResizer();

            GpuTestGuard.RequireRealGpu();

            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ShaderPath);
            if (shader == null)
            {
                Assert.Ignore("Compute shader asset not found (not running inside Unity package)");
            }

            _gpuResizer = new GpuAreaAverageResizer(shader);
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

        #region Color Parity

        [Test]
        public void Parity_SolidColor_MatchesWithinTolerance()
        {
            var source = Track(CreateSolidTexture(64, 64, new Color(0.3f, 0.6f, 0.9f, 0.7f)));

            var cpuResult = Track(_cpuResizer.Resize(source, 16, 16));
            var gpuResult = Track(_gpuResizer.Resize(source, 16, 16));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_HorizontalGradient_MatchesWithinTolerance()
        {
            var source = Track(CreateGradientTexture(128, 128));

            var cpuResult = Track(_cpuResizer.Resize(source, 32, 32));
            var gpuResult = Track(_gpuResizer.Resize(source, 32, 32));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_Checkerboard_MatchesWithinTolerance()
        {
            var source = Track(CreateCheckerboardTexture(64, 64));

            var cpuResult = Track(_cpuResizer.Resize(source, 1, 1));
            var gpuResult = Track(_gpuResizer.Resize(source, 1, 1));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_NonSquare_MatchesWithinTolerance()
        {
            var source = Track(CreateSolidTexture(128, 64, Color.cyan));

            var cpuResult = Track(_cpuResizer.Resize(source, 32, 16));
            var gpuResult = Track(_gpuResizer.Resize(source, 32, 16));

            Assert.AreEqual(cpuResult.width, gpuResult.width);
            Assert.AreEqual(cpuResult.height, gpuResult.height);
            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_LargeRatio_MatchesWithinTolerance()
        {
            var source = Track(CreateGradientTexture(256, 256));

            var cpuResult = Track(_cpuResizer.Resize(source, 32, 32));
            var gpuResult = Track(_gpuResizer.Resize(source, 32, 32));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_LinearColorTexture_MatchesWithinTolerance()
        {
            var source = Track(CreateLinearTexture(64, 64, new Color(0.5f, 0.25f, 0.75f, 0.6f)));

            var cpuResult = Track(_cpuResizer.Resize(source, 16, 16));
            var gpuResult = Track(_gpuResizer.Resize(source, 16, 16));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_Upscale_MatchesWithinTolerance()
        {
            var source = Track(CreateGradientTexture(32, 32));

            var cpuResult = Track(_cpuResizer.Resize(source, 64, 64));
            var gpuResult = Track(_gpuResizer.Resize(source, 64, 64));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_MixedAxes_MatchesWithinTolerance()
        {
            var source = Track(CreateGradientTexture(128, 16));

            var cpuResult = Track(_cpuResizer.Resize(source, 32, 32));
            var gpuResult = Track(_gpuResizer.Resize(source, 32, 32));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_NonReadableSource_MatchesWithinTolerance()
        {
            var source = Track(CreateCheckerboardTexture(64, 64));
            source.Apply(false, true);
            Assert.IsFalse(source.isReadable, "sanity: source must be non-readable");

            var cpuResult = Track(_cpuResizer.Resize(source, 16, 16));
            var gpuResult = Track(_gpuResizer.Resize(source, 16, 16));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        #endregion

        #region Normal Map Parity

        [Test]
        public void Parity_NormalMap_Flat_MatchesWithinTolerance()
        {
            var flatNormal = new Color(0.5f, 0.5f, 1f, 1f);
            var source = Track(CreateLinearTexture(64, 64, flatNormal));

            var cpuResult = Track(_cpuResizer.Resize(source, 16, 16));
            var gpuResult = Track(_gpuResizer.Resize(source, 16, 16));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        [Test]
        public void Parity_NormalMap_Tilted_MatchesWithinTolerance()
        {
            float nx = 0.3f;
            float ny = 0.4f;
            float nz = Mathf.Sqrt(1f - nx * nx - ny * ny);
            var encoded = new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f);
            var source = Track(CreateLinearTexture(64, 64, encoded));

            var cpuResult = Track(_cpuResizer.Resize(source, 16, 16));
            var gpuResult = Track(_gpuResizer.Resize(source, 16, 16));

            AssertPixelsParity(cpuResult, gpuResult);
        }

        #endregion

        #region Helpers

        private void AssertPixelsParity(Texture2D cpu, Texture2D gpu)
        {
            Assert.IsNotNull(cpu, "CPU result should not be null");
            Assert.IsNotNull(gpu, "GPU result should not be null");
            Assert.AreEqual(cpu.width, gpu.width);
            Assert.AreEqual(cpu.height, gpu.height);

            var cpuPixels = cpu.GetPixels();
            var gpuPixels = gpu.GetPixels();

            int step = Mathf.Max(1, cpuPixels.Length / 256);
            for (int i = 0; i < cpuPixels.Length; i += step)
            {
                Assert.That(
                    gpuPixels[i].r,
                    Is.EqualTo(cpuPixels[i].r).Within(ChannelTolerance),
                    $"R mismatch at pixel {i}"
                );
                Assert.That(
                    gpuPixels[i].g,
                    Is.EqualTo(cpuPixels[i].g).Within(ChannelTolerance),
                    $"G mismatch at pixel {i}"
                );
                Assert.That(
                    gpuPixels[i].b,
                    Is.EqualTo(cpuPixels[i].b).Within(ChannelTolerance),
                    $"B mismatch at pixel {i}"
                );
                Assert.That(
                    gpuPixels[i].a,
                    Is.EqualTo(cpuPixels[i].a).Within(ChannelTolerance),
                    $"A mismatch at pixel {i}"
                );
            }
        }

        private T Track<T>(T obj)
            where T : Object
        {
            if (obj != null)
                _createdObjects.Add(obj);
            return obj;
        }

        private static Texture2D CreateLinearTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear: true);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
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

        private static Texture2D CreateGradientTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = (float)x / (width - 1);
                    pixels[y * width + x] = new Color(t, 1f - t, 0.5f, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateCheckerboardTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = ((x + y) % 2 == 0) ? Color.white : Color.black;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
