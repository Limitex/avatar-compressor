// TextureAnalysisCommon.hlsl
// Shared utilities for texture analysis compute shaders.

#ifndef TEXTURE_ANALYSIS_COMMON_INCLUDED
#define TEXTURE_ANALYSIS_COMMON_INCLUDED

// Constants (must match AnalysisConstants.cs)

#define ALPHA_THRESHOLD 0.1
#define GLCM_LEVELS 16
#define DCT_BLOCK_SIZE 8
#define HISTOGRAM_BINS 256
#define PERCEPTUAL_BLOCK_SIZE 4
#define DETAIL_DENSITY_BLOCK_SIZE 16
#define NORMAL_MAP_SAMPLE_STEP 2
#define MIN_ANALYSIS_DIMENSION 8
#define MIN_NORMAL_MAP_DIMENSION 4
#define MIN_OPAQUE_PIXELS 100
#define DEFAULT_COMPLEXITY_SCORE 0.5
#define EPSILON 0.0001
#define DETAIL_DENSITY_MIN_THRESHOLD 0.005
#define DETAIL_DENSITY_VARIANCE_MULTIPLIER 0.5
#define DCT_HIGH_FREQ_THRESHOLD 2

// Strategy type indices (must match GpuAnalysisBackend.GetStrategyIndex)
#define STRATEGY_FAST 0
#define STRATEGY_HIGH_ACCURACY 1
#define STRATEGY_PERCEPTUAL 2
#define STRATEGY_COMBINED 3

// Sparse texture penalty (must match AnalysisConstants.SparseTexturePenalty)
#define SPARSE_TEXTURE_PENALTY 0.2

// Sub-sampling denominators (must match ImageMath.cs CPU sub-sampling)
#define SOBEL_SAMPLING_DENOMINATOR 256
#define EDGE_DENSITY_SAMPLING_DENOMINATOR 128

// Fixed-point scale for atomic float accumulation.
// Using 4000 for high precision while preventing uint32 overflow.
// Worst-case: ColorVariance max 3.0 * 4000 * 262144 = 3.15B < 4.29B (uint32 max).
#define FIXED_POINT_SCALE 4000.0

// Input / Output Bindings

Texture2D<float4> _InputTexture;
SamplerState sampler_InputTexture;

uint _Width;        // Sampled analysis width
uint _Height;       // Sampled analysis height
uint _SourceWidth;  // Original texture width (for nearest-neighbor sampling)
uint _SourceHeight; // Original texture height
uint _IsNormalMap;

// Result buffer: [0]=score
RWStructuredBuffer<float> _ResultBuffer;

// Intermediate accumulation buffer (uint for InterlockedAdd)
RWStructuredBuffer<uint> _IntermediateBuffer;

// Intermediate Buffer Layout

// Fast strategy
#define IDX_SOBEL_SUM           0
#define IDX_SOBEL_COUNT         1
#define IDX_SPATIAL_ROW_SUM     2
#define IDX_SPATIAL_ROW_COUNT   3
#define IDX_SPATIAL_COL_SUM     4
#define IDX_SPATIAL_COL_COUNT   5
#define IDX_COLOR_SUM_R         6
#define IDX_COLOR_SUM_G         7
#define IDX_COLOR_SUM_B         8
#define IDX_COLOR_VALID_COUNT   9
#define IDX_COLOR_VARIANCE_SUM  10

// HighAccuracy strategy
#define IDX_DCT_HIGH_FREQ       11
#define IDX_DCT_TOTAL_ENERGY    12
#define IDX_GLCM_MATRIX         13   // 13..268 (256 cells)
#define IDX_GLCM_PAIRS          269
#define IDX_ENTROPY_HISTOGRAM   270  // 270..525 (256 bins)
#define IDX_ENTROPY_VALID_COUNT 526
#define IDX_ENTROPY_RESULT      527

// Perceptual strategy
#define IDX_BLOCK_VAR_SUM       528
#define IDX_BLOCK_VAR_COUNT     529
#define IDX_EDGE_SUM            530
#define IDX_EDGE_COUNT          531
#define IDX_DETAIL_BLOCKS       532
#define IDX_DETAIL_TOTAL        533

// Normal map
#define IDX_NORMAL_VAR_SUM      534
#define IDX_NORMAL_VAR_COUNT    535

// Common
#define IDX_OPAQUE_COUNT        536

// Total intermediate buffer size
#define INTERMEDIATE_BUFFER_SIZE 537

// Utility Functions

// Sample a pixel at sampled-space coordinates using nearest-neighbor.
// Matches CPU PixelSampler.SampleIfNeeded index math for identical results.
// sRGB-to-linear conversion is handled on the C# side by blitting sRGB textures
// to a linear RenderTexture before binding, so no shader-side conversion is needed.
float4 SamplePixel(uint sx, uint sy)
{
    float xStep = (float)_SourceWidth / (float)_Width;
    float yStep = (float)_SourceHeight / (float)_Height;
    int srcX = min((int)(sx * xStep), (int)_SourceWidth - 1);
    int srcY = min((int)(sy * yStep), (int)_SourceHeight - 1);
    return _InputTexture.Load(int3(srcX, srcY, 0));
}

// Rec.709 luminance
float ToGrayscale(float3 rgb)
{
    return dot(rgb, float3(0.2126, 0.7152, 0.0722));
}

bool IsTransparent(float4 pixel)
{
    return pixel.a < ALPHA_THRESHOLD;
}

// Safe normalize: returns flat normal (0,0,1) for degenerate zero vectors.
// Matches CPU NormalMapPreprocessor behavior (commit cfe11fa).
float3 SafeNormalize(float3 v)
{
    float len = length(v);
    return len > EPSILON ? v / len : float3(0, 0, 1);
}

// Percentile normalization (matches MathUtils.NormalizeWithPercentile)
float NormalizeWithPercentile(float value, float low, float high)
{
    float range = high - low;
    return range > EPSILON ? saturate((value - low) / range) : 0.0;
}

// Atomic add for fixed-point float values.
// Converts float to fixed-point uint with rounding, atomically adds.
void AtomicAddFixed(uint index, float value)
{
    uint fixedVal = (uint)(value * FIXED_POINT_SCALE + 0.5);
    uint dummy;
    InterlockedAdd(_IntermediateBuffer[index], fixedVal, dummy);
}

// Atomic increment by 1
void AtomicIncrement(uint index)
{
    uint dummy;
    InterlockedAdd(_IntermediateBuffer[index], 1u, dummy);
}

// Atomic add uint
void AtomicAddUint(uint index, uint value)
{
    uint dummy;
    InterlockedAdd(_IntermediateBuffer[index], value, dummy);
}

// Read fixed-point value back as float
float ReadFixed(uint index)
{
    return (float)_IntermediateBuffer[index] / FIXED_POINT_SCALE;
}

// Read uint value as float
float ReadUint(uint index)
{
    return (float)_IntermediateBuffer[index];
}

#endif // TEXTURE_ANALYSIS_COMMON_INCLUDED
