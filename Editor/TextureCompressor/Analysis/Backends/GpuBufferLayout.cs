namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// C# mirror of the intermediate buffer layout defined in TextureAnalysisCommon.hlsl.
    /// All index constants must match the corresponding #define values in the shader.
    /// </summary>
    internal static class GpuBufferLayout
    {
        // Fast strategy
        internal const int IdxColorSumR = 6;
        internal const int ColorMeanFieldCount = 4; // R, G, B, count (indices 6-9)

        // Perceptual strategy
        internal const int IdxBlockVarSum = 529;
        internal const int BlockVarFieldCount = 2; // sum, count (indices 529-530)

        // Buffer sizes
        internal const int IntermediateBufferSize = 539;
        internal const int ResultBufferSize = 3;

        // Result buffer layout: [0]=score, [1]=opaqueCount, [2]=hasSignificantAlpha
        internal const int ResultIdxScore = 0;
        internal const int ResultIdxHasAlpha = 2;

        // Fixed-point scale (must match FIXED_POINT_SCALE in HLSL)
        internal const float FixedPointScale = 1000f;
    }
}
