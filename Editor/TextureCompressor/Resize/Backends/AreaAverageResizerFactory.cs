using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    public static class AreaAverageResizerFactory
    {
        // GPU availability is static per editor session (reset on domain
        // reload); cached so the Preferences window doesn't re-probe the
        // shader asset on every repaint.
        private static bool? _gpuAvailable;

        private static bool GpuAvailable
        {
            get
            {
                _gpuAvailable ??=
                    SystemInfo.supportsComputeShaders
                    && AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        GpuAreaAverageResizer.ShaderPath
                    ) != null;
                return _gpuAvailable.Value;
            }
        }

        public static ITextureResizer Create(
            ResizeBackendPreference backendPreference = ResizeBackendPreference.Auto
        )
        {
            if (
                backendPreference != ResizeBackendPreference.CPU
                && GpuAreaAverageResizer.TryCreate(out var gpuResizer, new CpuAreaAverageResizer())
            )
            {
                return gpuResizer;
            }

            return new CpuAreaAverageResizer();
        }

        /// <summary>
        /// Returns a display name indicating which backend would be selected
        /// given the current system capabilities and the backend preference.
        /// </summary>
        public static string ResolveBackendName(ResizeBackendPreference backendPreference)
        {
            if (backendPreference == ResizeBackendPreference.CPU)
                return "CPU";

            if (GpuAvailable)
                return "GPU";

            return "CPU (GPU unavailable)";
        }
    }
}
