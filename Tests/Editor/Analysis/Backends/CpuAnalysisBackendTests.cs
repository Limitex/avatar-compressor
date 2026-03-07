using System.Collections.Generic;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class CpuAnalysisBackendTests
    {
        private TextureProcessor _processor;
        private ComplexityCalculator _complexityCalc;
        private CpuAnalysisBackend _backend;
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            _processor = new TextureProcessor(32, 2048, true);
            _complexityCalc = new ComplexityCalculator(0.7f, 0.3f, 1, 8);
            var standardAnalyzer = AnalyzerFactory.Create(AnalysisStrategyType.Fast);
            var normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();
            _backend = new CpuAnalysisBackend(
                standardAnalyzer,
                normalMapAnalyzer,
                _processor,
                _complexityCalc
            );
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

        #region Batch Tests

        [Test]
        public void AnalyzeBatch_EmptyDictionary_ReturnsEmptyResult()
        {
            var textures = new Dictionary<Texture2D, TextureInfo>();

            var result = _backend.AnalyzeBatch(textures);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void AnalyzeBatch_SingleTexture_ReturnsOneResult()
        {
            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
        }

        [Test]
        public void AnalyzeBatch_MultipleTextures_ReturnsAllResults()
        {
            var tex1 = TrackTexture(CreateNoiseTexture(64, 64, 1));
            var tex2 = TrackTexture(CreateNoiseTexture(128, 128, 2));
            var tex3 = TrackTexture(CreateNoiseTexture(256, 256, 3));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    tex1,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
                {
                    tex2,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
                {
                    tex3,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey(tex1));
            Assert.IsTrue(result.ContainsKey(tex2));
            Assert.IsTrue(result.ContainsKey(tex3));
        }

        [Test]
        public void AnalyzeBatch_MixedTextureTypes_HandlesCorrectly()
        {
            var mainTex = TrackTexture(CreateNoiseTexture(64, 64, 10));
            var normalTex = TrackTexture(CreateFlatNormalMapTexture(64, 64));
            var emissionTex = TrackTexture(CreateNoiseTexture(64, 64, 20));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    mainTex,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
                {
                    normalTex,
                    new TextureInfo { IsNormalMap = true, IsEmission = false }
                },
                {
                    emissionTex,
                    new TextureInfo { IsNormalMap = false, IsEmission = true }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.AreEqual(3, result.Count);
        }

        #endregion

        #region Complexity Scores

        [Test]
        public void AnalyzeBatch_UniformTexture_LowComplexity()
        {
            var texture = TrackTexture(CreateUniformTexture(64, 64, Color.gray));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.That(result[texture].NormalizedComplexity, Is.LessThan(0.3f));
        }

        [Test]
        public void AnalyzeBatch_NoiseTexture_HigherComplexity()
        {
            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.That(result[texture].NormalizedComplexity, Is.GreaterThan(0.1f));
        }

        [Test]
        public void AnalyzeBatch_NormalMap_FlatNormals_LowComplexity()
        {
            var texture = TrackTexture(CreateFlatNormalMapTexture(64, 64));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = true, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.That(result[texture].NormalizedComplexity, Is.LessThan(0.5f));
        }

        [Test]
        public void AnalyzeBatch_EmissionTexture_HigherOrEqualComplexity()
        {
            var texNormal = TrackTexture(CreateNoiseTexture(64, 64, 99));
            var texEmission = TrackTexture(CreateNoiseTexture(64, 64, 99));
            var normalBatch = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texNormal,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };
            var emissionBatch = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texEmission,
                    new TextureInfo { IsNormalMap = false, IsEmission = true }
                },
            };

            var resultNormal = _backend.AnalyzeBatch(normalBatch);
            var resultEmission = _backend.AnalyzeBatch(emissionBatch);

            // Emission boost (/ 0.9) raises complexity score
            Assert.That(
                resultEmission[texEmission].NormalizedComplexity,
                Is.GreaterThanOrEqualTo(resultNormal[texNormal].NormalizedComplexity)
            );
        }

        #endregion

        #region Result Validation

        [Test]
        public void AnalyzeBatch_Result_HasValidComplexity()
        {
            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.That(result[texture].NormalizedComplexity, Is.InRange(0f, 1f));
        }

        [Test]
        public void AnalyzeBatch_Result_HasValidDivisor()
        {
            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.That(result[texture].RecommendedDivisor, Is.GreaterThanOrEqualTo(1));
            Assert.That(result[texture].RecommendedDivisor, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void AnalyzeBatch_Result_HasValidResolution()
        {
            var texture = TrackTexture(CreateNoiseTexture(128, 128, 42));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.That(result[texture].RecommendedResolution.x, Is.GreaterThanOrEqualTo(32));
            Assert.That(result[texture].RecommendedResolution.y, Is.GreaterThanOrEqualTo(32));
            Assert.That(result[texture].RecommendedResolution.x, Is.LessThanOrEqualTo(2048));
            Assert.That(result[texture].RecommendedResolution.y, Is.LessThanOrEqualTo(2048));
        }

        #endregion

        #region Alpha Detection

        [Test]
        public void AnalyzeBatch_FullyOpaqueTexture_NoSignificantAlpha()
        {
            var texture = TrackTexture(
                CreateUniformTexture(64, 64, new Color(0.5f, 0.5f, 0.5f, 1f))
            );
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.IsFalse(result[texture].HasSignificantAlpha);
        }

        [Test]
        public void AnalyzeBatch_PartiallyTransparentTexture_HasSignificantAlpha()
        {
            var texture = TrackTexture(CreatePartiallyTransparentTexture(64, 64));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.IsTrue(result[texture].HasSignificantAlpha);
        }

        [Test]
        public void AnalyzeBatch_FullyTransparentTexture_HasSignificantAlpha()
        {
            var texture = TrackTexture(CreateTransparentTexture(64, 64));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            Assert.IsTrue(result[texture].HasSignificantAlpha);
        }

        #endregion

        #region Strategy Variants

        [Test]
        public void AnalyzeBatch_AllStrategies_ReturnValidResults()
        {
            var strategies = new[]
            {
                AnalysisStrategyType.Fast,
                AnalysisStrategyType.HighAccuracy,
                AnalysisStrategyType.Perceptual,
                AnalysisStrategyType.Combined,
            };

            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));

            foreach (var strategy in strategies)
            {
                var analyzer = AnalyzerFactory.Create(strategy);
                var normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();
                var backend = new CpuAnalysisBackend(
                    analyzer,
                    normalMapAnalyzer,
                    _processor,
                    _complexityCalc
                );
                var textures = new Dictionary<Texture2D, TextureInfo>
                {
                    {
                        texture,
                        new TextureInfo { IsNormalMap = false, IsEmission = false }
                    },
                };

                var result = backend.AnalyzeBatch(textures);

                Assert.That(
                    result[texture].NormalizedComplexity,
                    Is.InRange(0f, 1f),
                    $"Strategy {strategy} produced invalid complexity"
                );
            }
        }

        #endregion

        #region Sparse Texture

        [Test]
        public void AnalyzeBatch_TransparentTexture_AppliesSparseTexturePenalty()
        {
            var texture = TrackTexture(CreateTransparentTexture(64, 64));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = _backend.AnalyzeBatch(textures);

            // Expected: DefaultComplexityScore * SparseTexturePenalty = 0.5 * 0.2 = 0.1
            float expectedScore =
                AnalysisConstants.DefaultComplexityScore * AnalysisConstants.SparseTexturePenalty;
            Assert.That(
                result[texture].NormalizedComplexity,
                Is.EqualTo(expectedScore).Within(0.05f)
            );
        }

        #endregion

        #region Error Handling

        [Test]
        public void AnalyzeBatch_AnalysisException_ReturnsDefaultFallback()
        {
            var throwingAnalyzer = new ThrowingAnalyzer();
            var normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();
            var backend = new CpuAnalysisBackend(
                throwingAnalyzer,
                normalMapAnalyzer,
                _processor,
                _complexityCalc
            );

            var texture = TrackTexture(CreateNoiseTexture(64, 64, 42));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = backend.AnalyzeBatch(textures);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.That(result[texture].NormalizedComplexity, Is.InRange(0f, 1f));
            Assert.IsTrue(result[texture].HasSignificantAlpha);
        }

        [Test]
        public void AnalyzeBatch_AnalysisException_DoesNotPropagate()
        {
            var throwingAnalyzer = new ThrowingAnalyzer();
            var normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();
            var backend = new CpuAnalysisBackend(
                throwingAnalyzer,
                normalMapAnalyzer,
                _processor,
                _complexityCalc
            );

            var tex1 = TrackTexture(CreateNoiseTexture(64, 64, 1));
            var tex2 = TrackTexture(CreateNoiseTexture(64, 64, 2));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    tex1,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
                {
                    tex2,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            Assert.DoesNotThrow(() => backend.AnalyzeBatch(textures));

            var result = backend.AnalyzeBatch(textures);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(tex1));
            Assert.IsTrue(result.ContainsKey(tex2));
        }

        [Test]
        public void AnalyzeBatch_NormalMapAnalysisException_ReturnsDefaultFallback()
        {
            var standardAnalyzer = AnalyzerFactory.Create(AnalysisStrategyType.Fast);
            var throwingAnalyzer = new ThrowingAnalyzer();
            var backend = new CpuAnalysisBackend(
                standardAnalyzer,
                throwingAnalyzer,
                _processor,
                _complexityCalc
            );

            var texture = TrackTexture(CreateFlatNormalMapTexture(64, 64));
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = true, IsEmission = false }
                },
            };

            var result = backend.AnalyzeBatch(textures);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.That(result[texture].NormalizedComplexity, Is.InRange(0f, 1f));
        }

        private class ThrowingAnalyzer : ITextureComplexityAnalyzer
        {
            public TextureComplexityResult Analyze(ProcessedPixelData data)
            {
                throw new System.Exception("Simulated analysis failure");
            }
        }

        #endregion

        #region Helper Methods

        private Texture2D TrackTexture(Texture2D texture)
        {
            _createdObjects.Add(texture);
            return texture;
        }

        private static Texture2D CreateUniformTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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

        private static Texture2D CreateTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.5f, 0.5f, 0f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreatePartiallyTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Half opaque, half transparent
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
