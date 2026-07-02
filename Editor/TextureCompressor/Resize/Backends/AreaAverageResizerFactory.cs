namespace dev.limitex.avatar.compressor.editor.texture
{
    public static class AreaAverageResizerFactory
    {
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
        /// Returns a display name indicating which backend Create would select
        /// given the current system capabilities and the backend preference.
        /// </summary>
        public static string ResolveBackendName(ResizeBackendPreference backendPreference)
        {
            if (backendPreference == ResizeBackendPreference.CPU)
                return "CPU";

            return GpuAreaAverageResizer.IsGpuUsable(out _) ? "GPU" : "CPU (GPU unavailable)";
        }
    }
}
