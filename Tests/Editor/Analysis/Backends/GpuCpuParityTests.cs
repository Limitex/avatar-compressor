using System.Collections.Generic;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Integration tests that verify GPU and CPU analysis backends
    /// produce equivalent results for the same input textures.
    /// Automatically skipped when GPU compute shaders are unavailable.
    /// </summary>
    [TestFixture]
    public class GpuCpuParityTests
    {
        private const float ScoreTolerance = 0.02f;

        // sRGB textures use slightly wider tolerance than linear because GPU pow()
        // and CPU hardware sRGB decode may differ by a few ULP at float32 precision.
        // With the exact piecewise formula on GPU, divergence is typically <0.02.
        private const float SRGBScoreTolerance = 0.03f;
        private const string ShaderPath =
            "Packages/dev.limitex.avatar-compressor/"
            + "Editor/TextureCompressor/Analysis/Shaders/TextureAnalysis.compute";

        private TextureProcessor _processor;
        private ComputeShader _shader;
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<Object>();
            _processor = new TextureProcessor(32, 2048, true);

            if (!SystemInfo.supportsComputeShaders)
            {
                Assert.Ignore("Compute shaders not supported on this platform");
            }

            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                Assert.Ignore("Async GPU readback not supported on this platform");
            }

            // Software renderers (e.g. Mesa llvmpipe on CI runners without a GPU)
            // report compute shader support but produce unreliable results.
            // GPU/CPU parity can only be validated on real hardware.
            if (
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore
            )
            {
                var deviceName = SystemInfo.graphicsDeviceName ?? "";
                if (
                    deviceName.Contains("llvmpipe")
                    || deviceName.Contains("softpipe")
                    || deviceName.Contains("SwiftShader")
                    || deviceName.Contains("Mesa")
                )
                {
                    Assert.Ignore(
                        $"Software renderer detected ({deviceName}); GPU parity tests require real hardware"
                    );
                }
            }

            _shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ShaderPath);
            if (_shader == null)
            {
                Assert.Ignore("Compute shader asset not found (not running inside Unity package)");
            }
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

        #region Score Parity

        [Test]
        public void BothBackends_UniformTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateUniformTexture(64, 64, Color.gray));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Uniform texture complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_NoiseTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Noise texture complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_NormalMap_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateFlatNormalMapTexture(64, 64));
            var textures = MakeBatch(texture, isNormalMap: true, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Normal map complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_EmissionTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateNoiseTexture(64, 64, 99));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: true);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Emission texture complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_GradientTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateGradientTexture(64, 64));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Gradient texture complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_SmallTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateNoiseTexture(16, 16, 7));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Small texture complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_PartiallyTransparentTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreatePartiallyTransparentTexture(64, 64));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Partially transparent texture complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_VariedNormalMap_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateVariedNormalMapTexture(64, 64));
            var textures = MakeBatch(texture, isNormalMap: true, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                "Varied normal map complexity scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_MultipleMixedTextures_AllScoresWithinTolerance()
        {
            var uniform = TrackTexture(CreateUniformTexture(64, 64, Color.red));
            var noise = TrackTexture(CreateNoiseTexture(64, 64, 123));
            var gradient = TrackTexture(CreateGradientTexture(64, 64));

            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    uniform,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
                {
                    noise,
                    new TextureInfo { IsNormalMap = false, IsEmission = true }
                },
                {
                    gradient,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var cpuBackend = CreateCpuBackend(AnalysisStrategyType.Combined);
            var gpuBackend = CreateGpuBackend(AnalysisStrategyType.Combined);

            var cpuResult = cpuBackend.AnalyzeBatch(textures);
            var gpuResult = gpuBackend.AnalyzeBatch(textures);

            Assert.That(cpuResult.Count, Is.EqualTo(3), "CPU should analyze all 3 textures");
            Assert.That(gpuResult.Count, Is.EqualTo(3), "GPU should analyze all 3 textures");

            foreach (var tex in new[] { uniform, noise, gradient })
            {
                Assert.That(
                    gpuResult[tex],
                    Is.EqualTo(cpuResult[tex]).Within(ScoreTolerance),
                    $"Multi-texture batch: '{tex.name}' scores diverged between GPU and CPU"
                );
            }
        }

        #endregion

        #region Strategy Parity

        [Test]
        public void BothBackends_AllStrategies_ScoresWithinTolerance()
        {
            var strategies = new[]
            {
                AnalysisStrategyType.Fast,
                AnalysisStrategyType.HighAccuracy,
                AnalysisStrategyType.Perceptual,
                AnalysisStrategyType.Combined,
            };

            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            foreach (var strategy in strategies)
            {
                var (cpuResult, gpuResult) = AnalyzeBoth(strategy, textures);

                Assert.That(
                    gpuResult[texture],
                    Is.EqualTo(cpuResult[texture]).Within(ScoreTolerance),
                    $"Strategy {strategy}: scores diverged between GPU and CPU"
                );
            }
        }

        #endregion

        #region sRGB Parity

        [Test]
        public void BothBackends_SRGBNoiseTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateSRGBNoiseTexture(64, 64, 42));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(SRGBScoreTolerance),
                "sRGB noise texture: scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_SRGBGradientTexture_ScoresWithinTolerance()
        {
            var texture = TrackTexture(CreateSRGBGradientTexture(64, 64));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            var (cpuResult, gpuResult) = AnalyzeBoth(AnalysisStrategyType.Fast, textures);

            Assert.That(
                gpuResult[texture],
                Is.EqualTo(cpuResult[texture]).Within(SRGBScoreTolerance),
                "sRGB gradient texture: scores diverged between GPU and CPU"
            );
        }

        [Test]
        public void BothBackends_SRGBTexture_AllStrategies_ScoresWithinTolerance()
        {
            var strategies = new[]
            {
                AnalysisStrategyType.Fast,
                AnalysisStrategyType.HighAccuracy,
                AnalysisStrategyType.Perceptual,
                AnalysisStrategyType.Combined,
            };

            var texture = TrackTexture(CreateSRGBNoiseTexture(64, 64, 42));
            var textures = MakeBatch(texture, isNormalMap: false, isEmission: false);

            foreach (var strategy in strategies)
            {
                var (cpuResult, gpuResult) = AnalyzeBoth(strategy, textures);

                Assert.That(
                    gpuResult[texture],
                    Is.EqualTo(cpuResult[texture]).Within(SRGBScoreTolerance),
                    $"sRGB strategy {strategy}: scores diverged between GPU and CPU"
                );
            }
        }

        #endregion

        #region Helpers

        private (Dictionary<Texture2D, float> Cpu, Dictionary<Texture2D, float> Gpu) AnalyzeBoth(
            AnalysisStrategyType strategy,
            Dictionary<Texture2D, TextureInfo> textures
        )
        {
            var cpuBackend = CreateCpuBackend(strategy);
            var gpuBackend = CreateGpuBackend(strategy);

            var cpuResult = cpuBackend.AnalyzeBatch(textures);
            var gpuResult = gpuBackend.AnalyzeBatch(textures);

            return (cpuResult, gpuResult);
        }

        private CpuAnalysisBackend CreateCpuBackend(AnalysisStrategyType strategy)
        {
            var standardAnalyzer = AnalyzerFactory.Create(strategy);
            var normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();
            return new CpuAnalysisBackend(standardAnalyzer, normalMapAnalyzer, _processor);
        }

        private GpuAnalysisBackend CreateGpuBackend(AnalysisStrategyType strategy)
        {
            return new GpuAnalysisBackend(
                _shader,
                strategy,
                AnalysisConstants.CombinedDefaultFastWeight,
                AnalysisConstants.CombinedDefaultHighAccuracyWeight,
                AnalysisConstants.CombinedDefaultPerceptualWeight
            );
        }

        private static Dictionary<Texture2D, TextureInfo> MakeBatch(
            Texture2D texture,
            bool isNormalMap,
            bool isEmission
        )
        {
            return new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = isNormalMap, IsEmission = isEmission }
                },
            };
        }

        private Texture2D TrackTexture(Texture2D texture)
        {
            _createdObjects.Add(texture);
            return texture;
        }

        private static Texture2D CreateUniformTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, true);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateNoiseTexture(int width, int height, int seed)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, true);
            var pixels = new Color[width * height];
            var random = new System.Random(seed);
            for (int i = 0; i < pixels.Length; i++)
            {
                float r = (float)random.NextDouble();
                float g = (float)random.NextDouble();
                float b = (float)random.NextDouble();
                pixels[i] = new Color(r, g, b, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFlatNormalMapTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, true);
            var pixels = new Color[width * height];
            Color flatNormal = new Color(0.5f, 0.5f, 1f, 1f);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = flatNormal;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateGradientTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, true);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = (float)x / (width - 1);
                    pixels[y * width + x] = new Color(t, t, t, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSRGBNoiseTexture(int width, int height, int seed)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, false);
            var pixels = new Color[width * height];
            var random = new System.Random(seed);
            for (int i = 0; i < pixels.Length; i++)
            {
                float r = (float)random.NextDouble();
                float g = (float)random.NextDouble();
                float b = (float)random.NextDouble();
                pixels[i] = new Color(r, g, b, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateSRGBGradientTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = (float)x / (width - 1);
                    pixels[y * width + x] = new Color(t, t, t, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateVariedNormalMapTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, true);
            var pixels = new Color[width * height];
            var random = new System.Random(42);
            for (int i = 0; i < pixels.Length; i++)
            {
                float nx = (float)(random.NextDouble() * 2.0 - 1.0);
                float ny = (float)(random.NextDouble() * 2.0 - 1.0);
                float sqLen = nx * nx + ny * ny;
                float nz;
                if (sqLen >= 1f)
                {
                    float invLen = 1f / Mathf.Sqrt(sqLen);
                    nx *= invLen;
                    ny *= invLen;
                    nz = 0f;
                }
                else
                {
                    nz = Mathf.Sqrt(1f - sqLen);
                }

                pixels[i] = new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreatePartiallyTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, 1, true);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                float alpha = i < pixels.Length / 2 ? 1f : 0f;
                pixels[i] = new Color(0.5f, 0.5f, 0.5f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
