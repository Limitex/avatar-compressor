using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// GPU compute shader-based texture analysis backend.
    /// Dispatches analysis kernels directly on source textures without
    /// full CPU pixel readback. Some intermediate values (color mean,
    /// block variance) are read back synchronously mid-dispatch for
    /// multi-pass kernels. Final scalar scores are read back via
    /// AsyncGPUReadback.
    /// Must be called from the main thread (Unity GPU dispatch constraint).
    /// </summary>
    public class GpuAnalysisBackend : ITextureAnalysisBackend
    {
        private const int IntermediateBufferSize = GpuBufferLayout.IntermediateBufferSize;
        private const int ResultBufferSize = GpuBufferLayout.ResultBufferSize;
        private const float FixedPointScale = GpuBufferLayout.FixedPointScale;

        private readonly ComputeShader _shader;
        private readonly AnalysisStrategyType _strategyType;
        private readonly float _fastWeight;
        private readonly float _highAccuracyWeight;
        private readonly float _perceptualWeight;
        private readonly ComplexityCalculator _complexityCalc;
        private readonly TextureProcessor _processor;

        // Kernel indices
        private readonly int _kernelPreprocess;
        private readonly int _kernelSobelGradient;
        private readonly int _kernelSpatialFrequency;
        private readonly int _kernelColorMean;
        private readonly int _kernelColorVariance;
        private readonly int _kernelDctHighFreqRatio;
        private readonly int _kernelGlcmAccumulate;
        private readonly int _kernelGlcmFeatures;
        private readonly int _kernelEntropy;
        private readonly int _kernelEntropyFinalize;
        private readonly int _kernelBlockVariance;
        private readonly int _kernelEdgeDensity;
        private readonly int _kernelDetailDensity;
        private readonly int _kernelNormalMapVariation;
        private readonly int _kernelCombineResults;

        public GpuAnalysisBackend(
            ComputeShader shader,
            AnalysisStrategyType strategyType,
            float fastWeight,
            float highAccuracyWeight,
            float perceptualWeight,
            ComplexityCalculator complexityCalc,
            TextureProcessor processor
        )
        {
            _shader = shader;
            _strategyType = strategyType;
            _fastWeight = fastWeight;
            _highAccuracyWeight = highAccuracyWeight;
            _perceptualWeight = perceptualWeight;
            _complexityCalc = complexityCalc;
            _processor = processor;

            _kernelPreprocess = shader.FindKernel("Preprocess");
            _kernelSobelGradient = shader.FindKernel("SobelGradient");
            _kernelSpatialFrequency = shader.FindKernel("SpatialFrequency");
            _kernelColorMean = shader.FindKernel("ColorMean");
            _kernelColorVariance = shader.FindKernel("ColorVariance");
            _kernelDctHighFreqRatio = shader.FindKernel("DctHighFreqRatio");
            _kernelGlcmAccumulate = shader.FindKernel("GlcmAccumulate");
            _kernelGlcmFeatures = shader.FindKernel("GlcmFeatures");
            _kernelEntropy = shader.FindKernel("Entropy");
            _kernelEntropyFinalize = shader.FindKernel("EntropyFinalize");
            _kernelBlockVariance = shader.FindKernel("BlockVariance");
            _kernelEdgeDensity = shader.FindKernel("EdgeDensity");
            _kernelDetailDensity = shader.FindKernel("DetailDensity");
            _kernelNormalMapVariation = shader.FindKernel("NormalMapVariation");
            _kernelCombineResults = shader.FindKernel("CombineResults");
        }

        public Dictionary<Texture2D, TextureAnalysisResult> AnalyzeBatch(
            Dictionary<Texture2D, TextureInfo> textures
        )
        {
            var results = new Dictionary<Texture2D, TextureAnalysisResult>();
            var pendingReadbacks =
                new List<(
                    Texture2D Tex,
                    AsyncGPUReadbackRequest Request,
                    ComputeBuffer ResultBuf,
                    ComputeBuffer IntermediateBuf,
                    TextureInfo Info
                )>();

            try
            {
                var zeros = new uint[IntermediateBufferSize];

                foreach (var kvp in textures)
                {
                    var texture = kvp.Key;
                    var info = kvp.Value;
                    if (texture == null)
                        continue;

                    var (sampledWidth, sampledHeight) = PixelSampler.CalculateSampledDimensions(
                        texture.width,
                        texture.height
                    );

                    var resultBuffer = new ComputeBuffer(ResultBufferSize, sizeof(float));
                    var intermediateBuffer = new ComputeBuffer(
                        IntermediateBufferSize,
                        sizeof(uint)
                    );

                    try
                    {
                        // Clear intermediate buffer
                        intermediateBuffer.SetData(zeros);

                        // Set shared parameters
                        SetSharedParameters(
                            texture,
                            sampledWidth,
                            sampledHeight,
                            info.IsNormalMap,
                            resultBuffer,
                            intermediateBuffer
                        );

                        // Dispatch kernels
                        DispatchAnalysis(info, sampledWidth, sampledHeight, intermediateBuffer);

                        // Queue async readback
                        var request = AsyncGPUReadback.Request(resultBuffer);
                        pendingReadbacks.Add(
                            (texture, request, resultBuffer, intermediateBuffer, info)
                        );
                    }
                    catch (System.Exception e)
                    {
                        resultBuffer.Release();
                        intermediateBuffer.Release();
                        Debug.LogWarning(
                            $"[TextureCompressor] GPU analysis failed for '{texture.name}': {e.Message}"
                        );
                        results[texture] = AnalysisResultHelper.BuildResult(
                            AnalysisConstants.DefaultComplexityScore,
                            texture.width,
                            texture.height,
                            info.IsEmission,
                            info.IsNormalMap,
                            true,
                            _complexityCalc,
                            _processor
                        );
                    }
                }

                // Wait for all readbacks and assemble results
                foreach (var pending in pendingReadbacks)
                {
                    pending.Request.WaitForCompletion();

                    if (!pending.Request.hasError)
                    {
                        var data = pending.Request.GetData<float>();
                        float score = Mathf.Clamp01(data[GpuBufferLayout.ResultIdxScore]);
                        // GPU writes 0.0 (no alpha) or 1.0 (has alpha) to the result buffer
                        bool hasAlpha = data[GpuBufferLayout.ResultIdxHasAlpha] > 0.5f;

                        results[pending.Tex] = AnalysisResultHelper.BuildResult(
                            score,
                            pending.Tex.width,
                            pending.Tex.height,
                            pending.Info.IsEmission,
                            pending.Info.IsNormalMap,
                            hasAlpha,
                            _complexityCalc,
                            _processor
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[TextureCompressor] GPU readback failed for '{pending.Tex.name}', using default score"
                        );
                        results[pending.Tex] = AnalysisResultHelper.BuildResult(
                            AnalysisConstants.DefaultComplexityScore,
                            pending.Tex.width,
                            pending.Tex.height,
                            pending.Info.IsEmission,
                            pending.Info.IsNormalMap,
                            true,
                            _complexityCalc,
                            _processor
                        );
                    }
                }
            }
            finally
            {
                foreach (var pending in pendingReadbacks)
                {
                    pending.ResultBuf.Release();
                    pending.IntermediateBuf.Release();
                }
            }

            return results;
        }

        private void SetSharedParameters(
            Texture2D texture,
            int sampledWidth,
            int sampledHeight,
            bool isNormalMap,
            ComputeBuffer resultBuffer,
            ComputeBuffer intermediateBuffer
        )
        {
            _shader.SetInt("_Width", sampledWidth);
            _shader.SetInt("_Height", sampledHeight);
            _shader.SetInt("_SourceWidth", texture.width);
            _shader.SetInt("_SourceHeight", texture.height);
            _shader.SetInt("_IsNormalMap", isNormalMap ? 1 : 0);
            _shader.SetInt("_IsSRGB", texture.isDataSRGB ? 1 : 0);
            _shader.SetInt("_StrategyType", GetStrategyIndex());

            // Fast strategy constants
            _shader.SetFloat("_FastGradientWeight", AnalysisConstants.FastGradientWeight);
            _shader.SetFloat(
                "_FastSpatialFreqWeight",
                AnalysisConstants.FastSpatialFrequencyWeight
            );
            _shader.SetFloat("_FastColorVarWeight", AnalysisConstants.FastColorVarianceWeight);
            _shader.SetFloat("_GradientLow", AnalysisConstants.GradientPercentileLow);
            _shader.SetFloat("_GradientHigh", AnalysisConstants.GradientPercentileHigh);
            _shader.SetFloat("_SpatialFreqLow", AnalysisConstants.SpatialFreqPercentileLow);
            _shader.SetFloat("_SpatialFreqHigh", AnalysisConstants.SpatialFreqPercentileHigh);
            _shader.SetFloat("_ColorVarLow", AnalysisConstants.ColorVariancePercentileLow);
            _shader.SetFloat("_ColorVarHigh", AnalysisConstants.ColorVariancePercentileHigh);

            // HighAccuracy strategy constants
            _shader.SetFloat("_HighAccDctWeight", AnalysisConstants.HighAccuracyDctWeight);
            _shader.SetFloat(
                "_HighAccContrastWeight",
                AnalysisConstants.HighAccuracyContrastWeight
            );
            _shader.SetFloat(
                "_HighAccHomogeneityWeight",
                AnalysisConstants.HighAccuracyHomogeneityWeight
            );
            _shader.SetFloat("_HighAccEnergyWeight", AnalysisConstants.HighAccuracyEnergyWeight);
            _shader.SetFloat("_HighAccEntropyWeight", AnalysisConstants.HighAccuracyEntropyWeight);
            _shader.SetFloat("_EntropyLow", AnalysisConstants.EntropyPercentileLow);
            _shader.SetFloat("_EntropyHigh", AnalysisConstants.EntropyPercentileHigh);
            _shader.SetFloat("_ContrastLow", AnalysisConstants.ContrastPercentileLow);
            _shader.SetFloat("_ContrastHigh", AnalysisConstants.ContrastPercentileHigh);

            // Perceptual strategy constants
            _shader.SetFloat(
                "_PerceptualVarianceWeight",
                AnalysisConstants.PerceptualVarianceWeight
            );
            _shader.SetFloat("_PerceptualEdgeWeight", AnalysisConstants.PerceptualEdgeWeight);
            _shader.SetFloat("_PerceptualDetailWeight", AnalysisConstants.PerceptualDetailWeight);
            _shader.SetFloat("_VarianceLow", AnalysisConstants.VariancePercentileLow);
            _shader.SetFloat("_VarianceHigh", AnalysisConstants.VariancePercentileHigh);
            _shader.SetFloat("_EdgeLow", AnalysisConstants.EdgePercentileLow);
            _shader.SetFloat("_EdgeHigh", AnalysisConstants.EdgePercentileHigh);

            // Normal map constants
            _shader.SetFloat(
                "_NormalMapVariationMultiplier",
                AnalysisConstants.NormalMapVariationMultiplier
            );

            // Combined strategy weights
            _shader.SetFloat("_CombinedFastWeight", _fastWeight);
            _shader.SetFloat("_CombinedHighAccWeight", _highAccuracyWeight);
            _shader.SetFloat("_CombinedPerceptualWeight", _perceptualWeight);

            // Bind buffers to all kernels
            int[] allKernels =
            {
                _kernelPreprocess,
                _kernelSobelGradient,
                _kernelSpatialFrequency,
                _kernelColorMean,
                _kernelColorVariance,
                _kernelDctHighFreqRatio,
                _kernelGlcmAccumulate,
                _kernelGlcmFeatures,
                _kernelEntropy,
                _kernelEntropyFinalize,
                _kernelBlockVariance,
                _kernelEdgeDensity,
                _kernelDetailDensity,
                _kernelNormalMapVariation,
                _kernelCombineResults,
            };

            foreach (int kernel in allKernels)
            {
                _shader.SetTexture(kernel, "_InputTexture", texture);
                _shader.SetBuffer(kernel, "_ResultBuffer", resultBuffer);
                _shader.SetBuffer(kernel, "_IntermediateBuffer", intermediateBuffer);
            }
        }

        private void DispatchAnalysis(
            TextureInfo info,
            int width,
            int height,
            ComputeBuffer intermediateBuffer
        )
        {
            int groupsX16 = CeilDiv(width, GpuBufferLayout.ThreadGroupSize);
            int groupsY16 = CeilDiv(height, GpuBufferLayout.ThreadGroupSize);

            // Always run preprocessing
            _shader.Dispatch(_kernelPreprocess, groupsX16, groupsY16, 1);

            if (info.IsNormalMap)
            {
                // Normal map: only NormalMapVariation kernel
                int nmGroupsX = CeilDiv(
                    width / AnalysisConstants.NormalMapSampleStep,
                    GpuBufferLayout.ThreadGroupSize
                );
                int nmGroupsY = CeilDiv(
                    height / AnalysisConstants.NormalMapSampleStep,
                    GpuBufferLayout.ThreadGroupSize
                );
                _shader.Dispatch(
                    _kernelNormalMapVariation,
                    Mathf.Max(1, nmGroupsX),
                    Mathf.Max(1, nmGroupsY),
                    1
                );
            }
            else
            {
                bool needsFast =
                    _strategyType == AnalysisStrategyType.Fast
                    || _strategyType == AnalysisStrategyType.Combined;
                bool needsHighAcc =
                    _strategyType == AnalysisStrategyType.HighAccuracy
                    || _strategyType == AnalysisStrategyType.Combined;
                bool needsPerceptual =
                    _strategyType == AnalysisStrategyType.Perceptual
                    || _strategyType == AnalysisStrategyType.Combined;

                if (needsFast)
                {
                    DispatchFastKernels(groupsX16, groupsY16, intermediateBuffer);
                }

                if (needsHighAcc)
                {
                    DispatchHighAccuracyKernels(width, height, groupsX16, groupsY16);
                }

                if (needsPerceptual)
                {
                    DispatchPerceptualKernels(
                        width,
                        height,
                        groupsX16,
                        groupsY16,
                        intermediateBuffer
                    );
                }
            }

            // Final combine
            _shader.Dispatch(_kernelCombineResults, 1, 1, 1);
        }

        private void DispatchFastKernels(
            int groupsX16,
            int groupsY16,
            ComputeBuffer intermediateBuffer
        )
        {
            // Sobel
            _shader.Dispatch(_kernelSobelGradient, groupsX16, groupsY16, 1);

            // Spatial frequency
            _shader.Dispatch(_kernelSpatialFrequency, groupsX16, groupsY16, 1);

            // Color mean (pass 1)
            _shader.Dispatch(_kernelColorMean, groupsX16, groupsY16, 1);

            // Read back color mean for pass 2
            // We need to synchronize and read the intermediate buffer
            var colorData = new uint[GpuBufferLayout.ColorMeanFieldCount];
            intermediateBuffer.GetData(
                colorData,
                0,
                GpuBufferLayout.IdxColorSumR,
                GpuBufferLayout.ColorMeanFieldCount
            );
            // colorData layout: [R_sum, G_sum, B_sum, count] relative to IdxColorSumR
            float count = colorData[3];
            if (count > 0)
            {
                float rMean = (colorData[0] / FixedPointScale) / count;
                float gMean = (colorData[1] / FixedPointScale) / count;
                float bMean = (colorData[2] / FixedPointScale) / count;
                _shader.SetFloat("_ColorMeanR", rMean);
                _shader.SetFloat("_ColorMeanG", gMean);
                _shader.SetFloat("_ColorMeanB", bMean);
            }
            else
            {
                _shader.SetFloat("_ColorMeanR", 0f);
                _shader.SetFloat("_ColorMeanG", 0f);
                _shader.SetFloat("_ColorMeanB", 0f);
            }

            // Color variance (pass 2)
            _shader.Dispatch(_kernelColorVariance, groupsX16, groupsY16, 1);
        }

        private void DispatchHighAccuracyKernels(
            int width,
            int height,
            int groupsX16,
            int groupsY16
        )
        {
            // DCT: one thread group per sampled 8x8 block (matches CPU blockStep)
            int dctBlocksX = width / AnalysisConstants.DctBlockSize;
            int dctBlocksY = height / AnalysisConstants.DctBlockSize;
            if (dctBlocksX > 0 && dctBlocksY > 0)
            {
                int blockStep = Mathf.Max(1, dctBlocksX / 16);
                _shader.SetInt("_DctBlockStep", blockStep);
                int dispatchX = CeilDiv(dctBlocksX, blockStep);
                int dispatchY = CeilDiv(dctBlocksY, blockStep);
                _shader.Dispatch(_kernelDctHighFreqRatio, dispatchX, dispatchY, 1);
            }

            // GLCM accumulate
            _shader.Dispatch(_kernelGlcmAccumulate, groupsX16, groupsY16, 1);

            // GLCM features (single workgroup of 256 threads)
            _shader.Dispatch(_kernelGlcmFeatures, 1, 1, 1);

            // Entropy histogram
            _shader.Dispatch(_kernelEntropy, groupsX16, groupsY16, 1);

            // Entropy finalize (single workgroup of 256 threads)
            _shader.Dispatch(_kernelEntropyFinalize, 1, 1, 1);
        }

        private void DispatchPerceptualKernels(
            int width,
            int height,
            int groupsX16,
            int groupsY16,
            ComputeBuffer intermediateBuffer
        )
        {
            // Block variance: one thread group per 4x4 block
            int bvBlocksX = width / AnalysisConstants.PerceptualBlockSize;
            int bvBlocksY = height / AnalysisConstants.PerceptualBlockSize;
            if (bvBlocksX > 0 && bvBlocksY > 0)
            {
                _shader.Dispatch(_kernelBlockVariance, bvBlocksX, bvBlocksY, 1);
            }

            // Edge density
            _shader.Dispatch(_kernelEdgeDensity, groupsX16, groupsY16, 1);

            // Read back block variance average for detail density threshold
            var blockVarData = new uint[GpuBufferLayout.BlockVarFieldCount];
            intermediateBuffer.GetData(
                blockVarData,
                0,
                GpuBufferLayout.IdxBlockVarSum,
                GpuBufferLayout.BlockVarFieldCount
            );
            // blockVarData layout: [sum, count] relative to IdxBlockVarSum
            float bvCount = blockVarData[1];
            float avgBlockVariance =
                bvCount > 0 ? (blockVarData[0] / FixedPointScale) / bvCount : 0f;
            _shader.SetFloat("_AvgBlockVariance", avgBlockVariance);

            // Detail density: one thread group per block
            int ddBlocksX = width / AnalysisConstants.DetailDensityBlockSize;
            int ddBlocksY = height / AnalysisConstants.DetailDensityBlockSize;
            if (ddBlocksX > 0 && ddBlocksY > 0)
            {
                _shader.Dispatch(_kernelDetailDensity, ddBlocksX, ddBlocksY, 1);
            }
        }

        /// <summary>
        /// Maps strategy enum to HLSL integer index (must match STRATEGY_* defines in TextureAnalysisCommon.hlsl).
        /// </summary>
        private int GetStrategyIndex()
        {
            switch (_strategyType)
            {
                case AnalysisStrategyType.Fast:
                    return 0;
                case AnalysisStrategyType.HighAccuracy:
                    return 1;
                case AnalysisStrategyType.Perceptual:
                    return 2;
                case AnalysisStrategyType.Combined:
                    return 3;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(_strategyType),
                        _strategyType,
                        "Unknown analysis strategy type"
                    );
            }
        }

        private static int CeilDiv(int a, int b)
        {
            return (a + b - 1) / b;
        }
    }
}
