using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// GPU compute implementation of separable two-pass area averaging.
    /// Sources are bound directly as SRVs (hardware sRGB decode); see the
    /// comment in ResizeOnGpu for why a Graphics.Blit pre-pass must never be
    /// added. Point-filtered and same-size sources delegate to the CPU path.
    /// </summary>
    public class GpuAreaAverageResizer : ITextureResizer
    {
        internal const string ShaderPath =
            "Packages/dev.limitex.avatar-compressor/"
            + "Editor/TextureCompressor/Resize/Shaders/AreaAverageResize.compute";

        private const string KernelHorizontalName = "AreaAverageHorizontal";
        private const string KernelVerticalName = "AreaAverageVertical";

        private const int ThreadGroupSize = 16;

        private readonly ComputeShader _shader;
        private readonly int _kernelHorizontal;
        private readonly int _kernelVertical;
        private readonly ITextureResizer _fallback;

        public GpuAreaAverageResizer(ComputeShader shader, ITextureResizer fallback = null)
        {
            _shader = shader;
            _kernelHorizontal = shader.FindKernel(KernelHorizontalName);
            _kernelVertical = shader.FindKernel(KernelVerticalName);
            _fallback = fallback;
        }

        /// <summary>
        /// Non-throwing availability probe shared by the factory's Create and
        /// ResolveBackendName, so the preferences UI reports the backend Create
        /// actually selects.
        /// </summary>
        internal static bool IsGpuUsable(out ComputeShader shader)
        {
            shader = null;

            if (
                !SystemInfo.supportsComputeShaders
                || ComputeShaderSupport.IsUnreliableComputeRenderer()
            )
                return false;

            return ComputeShaderSupport.TryLoadCompiledShader(
                ShaderPath,
                out shader,
                KernelHorizontalName,
                KernelVerticalName
            );
        }

        /// <summary>
        /// Resizes on the GPU; a detectable per-texture failure (RT allocation,
        /// exception) silently falls back to the injected CPU resizer when one
        /// was provided, so callers get a result rather than null.
        /// </summary>
        public Texture2D Resize(
            Texture2D source,
            int targetWidth,
            int targetHeight,
            bool forceLinearOutput
        )
        {
            if (source == null)
                return null;

            // Point-filtered sources use nearest neighbor (see CpuAreaAverageResizer);
            // route them to the CPU path instead of teaching the kernels a third mode.
            if (source.filterMode == FilterMode.Point)
            {
                var nearest = _fallback ?? new CpuAreaAverageResizer();
                return nearest.Resize(source, targetWidth, targetHeight, forceLinearOutput);
            }

            // A same-size target only needs a readable copy; the CPU path is an
            // exact byte copy, while this pipeline would spend two full-resolution
            // float RTs, two dispatches, and a readback on it. Common case: the
            // service resizes every texture, including "recompress format only".
            if (targetWidth == source.width && targetHeight == source.height && _fallback != null)
            {
                return _fallback.Resize(source, targetWidth, targetHeight, forceLinearOutput);
            }

            var result = ResizeOnGpu(source, targetWidth, targetHeight, forceLinearOutput);
            if (result == null && _fallback != null)
            {
                Debug.LogWarning(
                    $"[TextureCompressor] GPU resize failed for '{source.name}'; "
                        + "falling back to CPU area averaging"
                );
                result = _fallback.Resize(source, targetWidth, targetHeight, forceLinearOutput);
            }
            return result;
        }

        private Texture2D ResizeOnGpu(
            Texture2D source,
            int targetWidth,
            int targetHeight,
            bool forceLinearOutput
        )
        {
            int srcW = source.width;
            int srcH = source.height;

            float scaleX = (float)srcW / targetWidth;
            float scaleY = (float)srcH / targetHeight;

            RenderTexture intermediateRT = null;
            RenderTexture outputRT = null;
            Texture2D result = null;
            var previous = RenderTexture.active;

            try
            {
                // sRGB textures undergo hardware sRGB→Linear decode when read
                // through the SRV, so averaging happens in linear space; the
                // shader re-encodes to sRGB after averaging so the readback
                // bytes match the source encoding.
                // The direct bind is also load-bearing: do not add a
                // Graphics.Blit pre-pass here. Editor GUI interference
                // (e.g. VRCFury synchronously repainting its progress window
                // during play-mode builds) silently zeroes compute SRV reads
                // of blit-written RenderTextures — output turns black with no
                // exception, so the CPU fallback cannot catch it. UAV-written
                // RTs (the intermediate and output below) are unaffected.
                bool isSRGB = source.isDataSRGB;
                bool outputSRGB = isSRGB && !forceLinearOutput;

                // The intermediate holds linear-light values: half precision keeps
                // quantization (~2.4e-4) well below the smallest 8-bit sRGB step,
                // while 8-bit linear storage would band shadows.
                intermediateRT = CreateUAVRenderTexture(
                    targetWidth,
                    srcH,
                    RenderTextureFormat.ARGBHalf
                );
                if (intermediateRT == null)
                    return null;
                // The vertical kernel writes saturated, already-encoded values, so
                // an 8-bit UNORM store loses nothing over a float surface and
                // quarters the ReadPixels bandwidth.
                outputRT = CreateUAVRenderTexture(
                    targetWidth,
                    targetHeight,
                    RenderTextureFormat.ARGB32
                );
                if (outputRT == null)
                    return null;

                _shader.SetInt("_SrcWidth", srcW);
                _shader.SetInt("_SrcHeight", srcH);
                _shader.SetInt("_DstWidth", targetWidth);
                _shader.SetInt("_DstHeight", targetHeight);
                _shader.SetFloat("_ScaleX", scaleX);
                _shader.SetFloat("_ScaleY", scaleY);
                _shader.SetInt("_ReencodeSRGB", outputSRGB ? 1 : 0);

                _shader.SetTexture(_kernelHorizontal, "_SourceTexture", source);
                _shader.SetTexture(_kernelHorizontal, "_IntermediateTexture", intermediateRT);
                _shader.Dispatch(
                    _kernelHorizontal,
                    CeilDiv(targetWidth, ThreadGroupSize),
                    CeilDiv(srcH, ThreadGroupSize),
                    1
                );

                _shader.SetTexture(_kernelVertical, "_IntermediateRead", intermediateRT);
                _shader.SetTexture(_kernelVertical, "_OutputTexture", outputRT);
                _shader.Dispatch(
                    _kernelVertical,
                    CeilDiv(targetWidth, ThreadGroupSize),
                    CeilDiv(targetHeight, ThreadGroupSize),
                    1
                );

                RenderTexture.active = outputRT;
                result = new Texture2D(
                    targetWidth,
                    targetHeight,
                    TextureFormat.RGBA32,
                    source.mipmapCount > 1,
                    linear: !outputSRGB
                );
                result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                result.Apply(source.mipmapCount > 1);

                TextureProcessor.CopyTextureSettings(source, result);

                var output = result;
                result = null;
                return output;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"[TextureCompressor] GPU area average resize failed for '{source.name}': {e.Message}"
                );
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (result != null)
                    Object.DestroyImmediate(result);
                DestroyRT(intermediateRT);
                DestroyRT(outputRT);
            }
        }

        private static RenderTexture CreateUAVRenderTexture(
            int width,
            int height,
            RenderTextureFormat format
        )
        {
            var rt = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
            rt.enableRandomWrite = true;
            if (!rt.Create())
            {
                Debug.LogWarning(
                    $"[TextureCompressor] Failed to allocate {width}x{height} UAV RenderTexture"
                );
                Object.DestroyImmediate(rt);
                return null;
            }
            return rt;
        }

        private static int CeilDiv(int a, int b)
        {
            return (a + b - 1) / b;
        }

        private static void DestroyRT(RenderTexture rt)
        {
            if (rt == null)
                return;
            try
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"[TextureCompressor] Failed to destroy RenderTexture: {e.Message}"
                );
            }
        }
    }
}
