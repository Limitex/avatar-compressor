using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// GPU compute availability checks shared by the backend factories, so the
    /// preferences UI and the actual backend selection cannot drift apart.
    /// </summary>
    public static class ComputeShaderSupport
    {
        /// <summary>
        /// Software rasterizers advertise compute support but produce unreliable
        /// results, and wrong-but-non-null GPU output cannot be caught by a
        /// null-triggered CPU fallback. Bare "Mesa" is deliberately not matched —
        /// real Linux GPUs run Mesa drivers; only known software implementations
        /// are listed.
        /// </summary>
        public static bool IsUnreliableComputeRenderer()
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore)
                return false;

            var deviceName = SystemInfo.graphicsDeviceName ?? "";
            return deviceName.Contains("llvmpipe")
                || deviceName.Contains("softpipe")
                || deviceName.Contains("SwiftShader");
        }

        /// <summary>
        /// Loads a compute shader asset and verifies the given kernels compiled.
        /// HasKernel is false for compile-failed shader assets, which still load
        /// as non-null. On failure the shader is nulled out so callers cannot
        /// dispatch a half-valid asset.
        /// </summary>
        public static bool TryLoadCompiledShader(
            string path,
            out ComputeShader shader,
            params string[] kernels
        )
        {
            shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
            if (shader == null)
                return false;

            foreach (var kernel in kernels)
            {
                if (!shader.HasKernel(kernel))
                {
                    shader = null;
                    return false;
                }
            }

            return true;
        }
    }
}
