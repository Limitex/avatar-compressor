// TextureAnalysisFast.hlsl
// Kernels for the Fast analysis strategy:
// Preprocess, SobelGradient, SpatialFrequency, ColorMean, ColorVariance.

#ifndef TEXTURE_ANALYSIS_FAST_INCLUDED
#define TEXTURE_ANALYSIS_FAST_INCLUDED

// Kernel 0: Preprocess — opaque count + significant alpha detection
[numthreads(16, 16, 1)]
void Preprocess(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= _Width || id.y >= _Height)
        return;

    float4 pixel = SamplePixel(id.x, id.y);

    if (!IsTransparent(pixel))
    {
        AtomicIncrement(IDX_OPAQUE_COUNT);
    }

    if (pixel.a < SIGNIFICANT_ALPHA_THRESHOLD)
    {
        uint dummy;
        InterlockedOr(_IntermediateBuffer[IDX_HAS_SIGNIFICANT_ALPHA], 1u, dummy);
    }
}

// Kernel 1: Sobel Gradient
[numthreads(16, 16, 1)]
void SobelGradient(uint3 id : SV_DispatchThreadID)
{
    uint x = id.x;
    uint y = id.y;

    if (x < 1 || x >= _Width - 1 || y < 1 || y >= _Height - 1)
        return;

    // Match CPU sub-sampling: step = max(1, width / 256)
    uint step = max(1, _Width / 256);
    if ((x - 1) % step != 0 || (y - 1) % step != 0)
        return;

    // Sample 3x3 neighborhood
    float4 c   = SamplePixel(x, y);
    float4 cUL = SamplePixel(x - 1, y - 1);
    float4 cU  = SamplePixel(x, y - 1);
    float4 cUR = SamplePixel(x + 1, y - 1);
    float4 cL  = SamplePixel(x - 1, y);
    float4 cR  = SamplePixel(x + 1, y);
    float4 cDL = SamplePixel(x - 1, y + 1);
    float4 cD  = SamplePixel(x, y + 1);
    float4 cDR = SamplePixel(x + 1, y + 1);

    // Skip if center or any neighbor is transparent (unless normal map)
    if (!_IsNormalMap)
    {
        if (IsTransparent(c) || IsTransparent(cUL) || IsTransparent(cU) ||
            IsTransparent(cUR) || IsTransparent(cL) || IsTransparent(cR) ||
            IsTransparent(cDL) || IsTransparent(cD) || IsTransparent(cDR))
            return;
    }

    float gUL = ToGrayscale(cUL.rgb);
    float gU  = ToGrayscale(cU.rgb);
    float gUR = ToGrayscale(cUR.rgb);
    float gL  = ToGrayscale(cL.rgb);
    float gR  = ToGrayscale(cR.rgb);
    float gDL = ToGrayscale(cDL.rgb);
    float gD  = ToGrayscale(cD.rgb);
    float gDR = ToGrayscale(cDR.rgb);

    float gx = -gUL + gUR - 2.0 * gL + 2.0 * gR - gDL + gDR;
    float gy = -gUL - 2.0 * gU - gUR + gDL + 2.0 * gD + gDR;

    float gradient = sqrt(gx * gx + gy * gy);

    AtomicAddFixed(IDX_SOBEL_SUM, gradient);
    AtomicIncrement(IDX_SOBEL_COUNT);
}

// Kernel 2: Spatial Frequency
[numthreads(16, 16, 1)]
void SpatialFrequency(uint3 id : SV_DispatchThreadID)
{
    uint x = id.x;
    uint y = id.y;

    if (x >= _Width || y >= _Height)
        return;

    // Match CPU sub-sampling: step = max(1, width / 256)
    uint step = max(1, _Width / 256);
    if (x % step != 0 || y % step != 0)
        return;

    float4 c = SamplePixel(x, y);
    if (!_IsNormalMap && IsTransparent(c))
        return;

    float gray = ToGrayscale(c.rgb);

    // Row frequency: compare with pixel step positions to the left (matches CPU)
    if (x >= step)
    {
        float4 cLeft = SamplePixel(x - step, y);
        if (_IsNormalMap || !IsTransparent(cLeft))
        {
            float grayLeft = ToGrayscale(cLeft.rgb);
            float diff = gray - grayLeft;
            AtomicAddFixed(IDX_SPATIAL_ROW_SUM, diff * diff);
            AtomicIncrement(IDX_SPATIAL_ROW_COUNT);
        }
    }

    // Column frequency: compare with pixel step positions above (matches CPU)
    if (y >= step)
    {
        float4 cUp = SamplePixel(x, y - step);
        if (_IsNormalMap || !IsTransparent(cUp))
        {
            float grayUp = ToGrayscale(cUp.rgb);
            float diff = gray - grayUp;
            AtomicAddFixed(IDX_SPATIAL_COL_SUM, diff * diff);
            AtomicIncrement(IDX_SPATIAL_COL_COUNT);
        }
    }
}

// Kernel 3: Color Mean (pass 1 of color variance)
[numthreads(16, 16, 1)]
void ColorMean(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= _Width || id.y >= _Height)
        return;

    float4 pixel = SamplePixel(id.x, id.y);

    if (pixel.a < ALPHA_THRESHOLD)
        return;

    AtomicAddFixed(IDX_COLOR_SUM_R, pixel.r);
    AtomicAddFixed(IDX_COLOR_SUM_G, pixel.g);
    AtomicAddFixed(IDX_COLOR_SUM_B, pixel.b);
    AtomicIncrement(IDX_COLOR_VALID_COUNT);
}

// Kernel 4: Color Variance (pass 2, requires mean set from CPU)
[numthreads(16, 16, 1)]
void ColorVariance(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= _Width || id.y >= _Height)
        return;

    float4 pixel = SamplePixel(id.x, id.y);

    if (pixel.a < ALPHA_THRESHOLD)
        return;

    float dr = pixel.r - _ColorMeanR;
    float dg = pixel.g - _ColorMeanG;
    float db = pixel.b - _ColorMeanB;

    float sqDist = dr * dr + dg * dg + db * db;
    AtomicAddFixed(IDX_COLOR_VARIANCE_SUM, sqDist);
}

#endif // TEXTURE_ANALYSIS_FAST_INCLUDED
