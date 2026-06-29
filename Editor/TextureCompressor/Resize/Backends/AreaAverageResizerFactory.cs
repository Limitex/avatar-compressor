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
                && GpuAreaAverageResizer.TryCreate(out var gpuResizer)
            )
            {
                return gpuResizer;
            }

            return new CpuAreaAverageResizer();
        }

        public static string ResolveBackendName(ResizeBackendPreference backendPreference)
        {
            if (backendPreference == ResizeBackendPreference.CPU)
                return "CPU";

            if (GpuAreaAverageResizer.TryCreate(out _))
                return "GPU";

            return "CPU (GPU unavailable)";
        }
    }
}
