// TextureAnalysisCommon.hlsl
// Shared utilities for texture analysis compute shaders.

#ifndef TEXTURE_ANALYSIS_COMMON_INCLUDED
#define TEXTURE_ANALYSIS_COMMON_INCLUDED

// Constants (must match AnalysisConstants.cs)

#define ALPHA_THRESHOLD 0.1
#define SIGNIFICANT_ALPHA_THRESHOLD (250.0 / 255.0)
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

// Fixed-point scale for atomic float accumulation.
// Using 1000 to prevent uint32 overflow at 512x512 resolution.
// Worst-case: Sobel max gradient sqrt(4^2+4^2) = 5.66 * 1000 * 260100 = 1.47B < 4.29B (uint32 max).
// Precision after averaging 260K samples: ~3.8e-9, well within float32 limits.
#define FIXED_POINT_SCALE 1000.0
#define FIXED_POINT_SCALE_UINT 1000

// Input / Output Bindings

Texture2D<float4> _InputTexture;
SamplerState sampler_InputTexture;

uint _Width;    // Sampled analysis width  (max 512)
uint _Height;   // Sampled analysis height (max 512)
uint _IsNormalMap;

// Result buffer: [0]=score, [1]=opaqueCount (as float), [2]=hasSignificantAlpha (0 or 1)
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
#define IDX_DCT_BLOCK_COUNT     13
#define IDX_GLCM_MATRIX         14   // 14..269 (256 cells)
#define IDX_GLCM_PAIRS          270
#define IDX_ENTROPY_HISTOGRAM   271  // 271..526 (256 bins)
#define IDX_ENTROPY_VALID_COUNT 527

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
#define IDX_HAS_SIGNIFICANT_ALPHA 537

// Total intermediate buffer size
#define INTERMEDIATE_BUFFER_SIZE 538

// Utility Functions

// Sample a pixel at sampled-space coordinates (bilinear filtering).
// The hardware sampler performs downsampling when source > 512x512.
float4 SamplePixel(uint sx, uint sy)
{
    float2 uv = float2(
        ((float)sx + 0.5) / (float)_Width,
        ((float)sy + 0.5) / (float)_Height
    );
    return _InputTexture.SampleLevel(sampler_InputTexture, uv, 0);
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

// Percentile normalization (matches MathUtils.NormalizeWithPercentile)
float NormalizeWithPercentile(float value, float low, float high)
{
    return saturate((value - low) / (high - low));
}

// Atomic add for fixed-point float values.
// Converts float to fixed-point uint, atomically adds, result is read in CombineResults.
void AtomicAddFixed(uint index, float value)
{
    uint fixedVal = (uint)(value * FIXED_POINT_SCALE);
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
