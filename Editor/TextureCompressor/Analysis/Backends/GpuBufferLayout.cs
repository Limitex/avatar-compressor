namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// C# mirror of the intermediate buffer layout defined in TextureAnalysisCommon.hlsl.
    /// All index constants must match the corresponding #define values in the shader.
    /// </summary>
    public static class GpuBufferLayout
    {
        // Fast strategy — color mean accumulation (indices 6-9: R, G, B, count)
        public const int IdxColorSumR = 6;
        public const int ColorMeanFieldCount = 4;

        // Perceptual strategy — block variance accumulation (indices 529-530: sum, count)
        public const int IdxBlockVarSum = 529;
        public const int BlockVarFieldCount = 2;

        // Buffer sizes
        public const int IntermediateBufferSize = 539;
        public const int ResultBufferSize = 3;

        // Result buffer layout: [0]=score, [1]=opaqueCount, [2]=hasSignificantAlpha
        public const int ResultIdxScore = 0;
        public const int ResultIdxHasAlpha = 2;

        // Fixed-point scale (must match FIXED_POINT_SCALE in HLSL)
        public const float FixedPointScale = 1000f;

        // Default thread group dimension (must match [numthreads(16, 16, 1)] in shader kernels)
        public const int ThreadGroupSize = 16;
    }
}
