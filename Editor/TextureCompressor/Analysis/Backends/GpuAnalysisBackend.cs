using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// GPU compute shader-based texture analysis backend.
    /// Dispatches analysis kernels directly on source textures without
    /// CPU pixel readback. Only the final scalar scores are read back
    /// via AsyncGPUReadback.
    /// Must be called from the main thread (Unity GPU dispatch constraint).
    /// </summary>
    public class GpuAnalysisBackend : ITextureAnalysisBackend
    {
        private const int IntermediateBufferSize = 538;
        private const int ResultBufferSize = 3;
        private const float FixedPointScale = 1000f;

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

            // Compute sampled dimensions (capped at 512x512, matching PixelSampler)
            const int maxSampledDim = 512;

            try
            {
                foreach (var kvp in textures)
                {
                    var texture = kvp.Key;
                    var info = kvp.Value;
                    if (texture == null)
                        continue;

                    int sampledWidth = texture.width;
                    int sampledHeight = texture.height;
                    if (sampledWidth * sampledHeight > AnalysisConstants.MaxSampledPixels)
                    {
                        float aspect = (float)sampledWidth / sampledHeight;
                        if (aspect >= 1f)
                        {
                            sampledWidth = Mathf.Min(sampledWidth, maxSampledDim);
                            sampledHeight = Mathf.Max(
                                AnalysisConstants.MinSampledDimension,
                                Mathf.RoundToInt(sampledWidth / aspect)
                            );
                        }
                        else
                        {
                            sampledHeight = Mathf.Min(sampledHeight, maxSampledDim);
                            sampledWidth = Mathf.Max(
                                AnalysisConstants.MinSampledDimension,
                                Mathf.RoundToInt(sampledHeight * aspect)
                            );
                        }
                    }

                    var resultBuffer = new ComputeBuffer(ResultBufferSize, sizeof(float));
                    var intermediateBuffer = new ComputeBuffer(
                        IntermediateBufferSize,
                        sizeof(uint)
                    );

                    // Clear intermediate buffer
                    var zeros = new uint[IntermediateBufferSize];
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
                    DispatchAnalysis(
                        texture,
                        info,
                        sampledWidth,
                        sampledHeight,
                        resultBuffer,
                        intermediateBuffer
                    );

                    // Queue async readback
                    var request = AsyncGPUReadback.Request(resultBuffer);
                    pendingReadbacks.Add(
                        (texture, request, resultBuffer, intermediateBuffer, info)
                    );
                }

                // Wait for all readbacks and assemble results
                foreach (var pending in pendingReadbacks)
                {
                    pending.Request.WaitForCompletion();

                    if (!pending.Request.hasError)
                    {
                        var data = pending.Request.GetData<float>();
                        float score = Mathf.Clamp01(data[0]);
                        bool hasAlpha = data[2] > 0.5f;

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
                        results[pending.Tex] = AnalysisResultHelper.BuildResult(
                            AnalysisConstants.DefaultComplexityScore,
                            pending.Tex.width,
                            pending.Tex.height,
                            false,
                            false,
                            false,
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
            _shader.SetInt("_IsNormalMap", isNormalMap ? 1 : 0);
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
            Texture2D texture,
            TextureInfo info,
            int width,
            int height,
            ComputeBuffer resultBuffer,
            ComputeBuffer intermediateBuffer
        )
        {
            int groupsX16 = CeilDiv(width, 16);
            int groupsY16 = CeilDiv(height, 16);

            // Always run preprocessing
            _shader.Dispatch(_kernelPreprocess, groupsX16, groupsY16, 1);

            if (info.IsNormalMap)
            {
                // Normal map: only NormalMapVariation kernel
                int nmGroupsX = CeilDiv(width / AnalysisConstants.NormalMapSampleStep, 16);
                int nmGroupsY = CeilDiv(height / AnalysisConstants.NormalMapSampleStep, 16);
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
                    DispatchFastKernels(width, height, groupsX16, groupsY16, intermediateBuffer);
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
            int width,
            int height,
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
            var colorData = new uint[4]; // sumR, sumG, sumB, count at indices 6-9
            intermediateBuffer.GetData(colorData, 0, 6, 4);
            float count = colorData[3];
            if (count > 0)
            {
                _shader.SetFloat("_ColorMeanR", (colorData[0] / FixedPointScale) / count);
                _shader.SetFloat("_ColorMeanG", (colorData[1] / FixedPointScale) / count);
                _shader.SetFloat("_ColorMeanB", (colorData[2] / FixedPointScale) / count);
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
            // DCT: one thread group per 8x8 block
            int dctBlocksX = width / AnalysisConstants.DctBlockSize;
            int dctBlocksY = height / AnalysisConstants.DctBlockSize;
            if (dctBlocksX > 0 && dctBlocksY > 0)
            {
                _shader.Dispatch(_kernelDctHighFreqRatio, dctBlocksX, dctBlocksY, 1);
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
            var blockVarData = new uint[2]; // sum, count at indices 528-529
            intermediateBuffer.GetData(blockVarData, 0, 528, 2);
            float bvCount = blockVarData[1];
            float avgBlockVariance =
                bvCount > 0 ? (blockVarData[0] / FixedPointScale) / bvCount : 0f;
            _shader.SetFloat("_AvgBlockVariance", avgBlockVariance);

            // Detail density: one thread group per 16x16 block
            int ddBlocksX = width / 16;
            int ddBlocksY = height / 16;
            if (ddBlocksX > 0 && ddBlocksY > 0)
            {
                _shader.Dispatch(_kernelDetailDensity, ddBlocksX, ddBlocksY, 1);
            }
        }

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
                    return 3;
            }
        }

        private static int CeilDiv(int a, int b)
        {
            return (a + b - 1) / b;
        }
    }
}
