using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Factory that selects the appropriate analysis backend
    /// based on system capabilities.
    /// </summary>
    public static class AnalysisBackendFactory
    {
        private const string ShaderPath =
            "Packages/dev.limitex.avatar-compressor/"
            + "Editor/TextureCompressor/Analysis/Shaders/TextureAnalysis.compute";

        /// <summary>
        /// Creates the best available analysis backend.
        /// Returns GPU backend if compute shaders are supported and the shader asset is available;
        /// otherwise falls back to the CPU backend.
        /// </summary>
        public static ITextureAnalysisBackend Create(
            AnalysisStrategyType strategy,
            float fastWeight,
            float highAccuracyWeight,
            float perceptualWeight,
            TextureProcessor processor,
            ComplexityCalculator complexityCalc,
            bool forceCpu = false
        )
        {
            if (
                !forceCpu
                && SystemInfo.supportsComputeShaders
                && SystemInfo.supportsAsyncGPUReadback
                && TryLoadShader(out var shader)
            )
            {
                try
                {
                    return new GpuAnalysisBackend(
                        shader,
                        strategy,
                        fastWeight,
                        highAccuracyWeight,
                        perceptualWeight,
                        processor,
                        complexityCalc
                    );
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[AvatarCompressor] GPU analysis backend initialization failed, "
                            + $"falling back to CPU: {e}"
                    );
                }
            }

            return CreateCpuBackend(
                strategy,
                fastWeight,
                highAccuracyWeight,
                perceptualWeight,
                processor,
                complexityCalc
            );
        }

        private static CpuAnalysisBackend CreateCpuBackend(
            AnalysisStrategyType strategy,
            float fastWeight,
            float highAccuracyWeight,
            float perceptualWeight,
            TextureProcessor processor,
            ComplexityCalculator complexityCalc
        )
        {
            var standardAnalyzer = AnalyzerFactory.Create(
                strategy,
                fastWeight,
                highAccuracyWeight,
                perceptualWeight
            );
            var normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();

            return new CpuAnalysisBackend(
                standardAnalyzer,
                normalMapAnalyzer,
                processor,
                complexityCalc
            );
        }

        /// <summary>
        /// Returns a display name indicating which backend would be selected
        /// given the current system capabilities and the force-CPU flag.
        /// </summary>
        public static string ResolveBackendName(bool forceCpu)
        {
            if (forceCpu)
                return "CPU (forced)";

            if (
                SystemInfo.supportsComputeShaders
                && SystemInfo.supportsAsyncGPUReadback
                && TryLoadShader(out _)
            )
            {
                return "GPU";
            }

            return "CPU (GPU unavailable)";
        }

        private static bool TryLoadShader(out ComputeShader shader)
        {
            shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ShaderPath);
            return shader != null;
        }
    }
}
