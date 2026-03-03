// TextureAnalysisPerceptual.hlsl
// Kernels for the Perceptual analysis strategy:
// BlockVariance, EdgeDensity, DetailDensity.

#ifndef TEXTURE_ANALYSIS_PERCEPTUAL_INCLUDED
#define TEXTURE_ANALYSIS_PERCEPTUAL_INCLUDED

// Kernel 9: Block Variance (4x4 blocks, Welford's algorithm)
// One thread group per 4x4 block.
groupshared float gs_BlockPixels[16];
groupshared uint gs_BlockValidCount;

[numthreads(4, 4, 1)]
void BlockVariance(uint3 groupId : SV_GroupID, uint3 gtid : SV_GroupThreadID)
{
    uint bx = groupId.x;
    uint by = groupId.y;
    uint tx = gtid.x;
    uint ty = gtid.y;
    uint localIdx = ty * 4 + tx;

    uint px = bx * PERCEPTUAL_BLOCK_SIZE + tx;
    uint py = by * PERCEPTUAL_BLOCK_SIZE + ty;

    if (localIdx == 0)
        gs_BlockValidCount = 0;
    GroupMemoryBarrierWithGroupSync();

    // Load pixel
    float value = -1.0; // sentinel for transparent
    if (px < _Width && py < _Height)
    {
        float4 pixel = SamplePixel(px, py);
        if (_IsNormalMap || !IsTransparent(pixel))
        {
            value = ToGrayscale(pixel.rgb);
            uint dummy;
            InterlockedAdd(gs_BlockValidCount, 1u, dummy);
        }
    }
    gs_BlockPixels[localIdx] = value;
    GroupMemoryBarrierWithGroupSync();

    // Thread 0 computes Welford's variance for this block
    if (localIdx == 0)
    {
        uint n = gs_BlockValidCount;
        if (n == 0)
            return;

        int count = 0;
        float mean = 0.0;
        float m2 = 0.0;

        for (uint k = 0; k < 16; k++)
        {
            if (gs_BlockPixels[k] >= 0.0)
            {
                count++;
                float delta = gs_BlockPixels[k] - mean;
                mean += delta / (float)count;
                float delta2 = gs_BlockPixels[k] - mean;
                m2 += delta * delta2;
            }
        }

        if (count > 0)
        {
            float variance = m2 / (float)count;
            AtomicAddFixed(IDX_BLOCK_VAR_SUM, variance);
            AtomicIncrement(IDX_BLOCK_VAR_COUNT);
        }
    }
}

// Kernel 10: Edge Density (4-neighbor gradient)
[numthreads(16, 16, 1)]
void EdgeDensity(uint3 id : SV_DispatchThreadID)
{
    uint x = id.x;
    uint y = id.y;

    if (x < 1 || x >= _Width - 1 || y < 1 || y >= _Height - 1)
        return;

    // Match CPU sub-sampling: step = max(1, width / 128)
    uint step = max(1, _Width / 128);
    if ((x - 1) % step != 0 || (y - 1) % step != 0)
        return;

    float4 c     = SamplePixel(x, y);
    float4 cLeft = SamplePixel(x - 1, y);
    float4 cRight= SamplePixel(x + 1, y);
    float4 cUp   = SamplePixel(x, y - 1);
    float4 cDown = SamplePixel(x, y + 1);

    if (!_IsNormalMap)
    {
        if (IsTransparent(c) || IsTransparent(cLeft) || IsTransparent(cRight) ||
            IsTransparent(cUp) || IsTransparent(cDown))
            return;
    }

    float gLeft  = ToGrayscale(cLeft.rgb);
    float gRight = ToGrayscale(cRight.rgb);
    float gUp    = ToGrayscale(cUp.rgb);
    float gDown  = ToGrayscale(cDown.rgb);

    float grad = abs(gRight - gLeft) + abs(gDown - gUp);

    AtomicAddFixed(IDX_EDGE_SUM, grad);
    AtomicIncrement(IDX_EDGE_COUNT);
}

// Kernel 11: Detail Density (16x16 block variance > threshold)
// Requires _AvgBlockVariance to be set from CPU after BlockVariance kernel.
groupshared float gs_DetailPixels[256];
groupshared uint gs_DetailValidCount;

[numthreads(16, 16, 1)]
void DetailDensity(uint3 groupId : SV_GroupID, uint3 gtid : SV_GroupThreadID)
{
    uint bx = groupId.x;
    uint by = groupId.y;
    uint tx = gtid.x;
    uint ty = gtid.y;
    uint localIdx = ty * DETAIL_DENSITY_BLOCK_SIZE + tx;

    uint px = bx * DETAIL_DENSITY_BLOCK_SIZE + tx;
    uint py = by * DETAIL_DENSITY_BLOCK_SIZE + ty;

    if (localIdx == 0)
        gs_DetailValidCount = 0;
    GroupMemoryBarrierWithGroupSync();

    float value = -1.0;
    if (px < _Width && py < _Height)
    {
        float4 pixel = SamplePixel(px, py);
        if (_IsNormalMap || !IsTransparent(pixel))
        {
            value = ToGrayscale(pixel.rgb);
            uint dummy;
            InterlockedAdd(gs_DetailValidCount, 1u, dummy);
        }
    }
    gs_DetailPixels[localIdx] = value;
    GroupMemoryBarrierWithGroupSync();

    // Thread 0 computes Welford's variance and checks threshold
    if (localIdx == 0)
    {
        if (gs_DetailValidCount == 0)
            return;

        int count = 0;
        float mean = 0.0;
        float m2 = 0.0;

        for (uint k = 0; k < 256; k++)
        {
            if (gs_DetailPixels[k] >= 0.0)
            {
                count++;
                float delta = gs_DetailPixels[k] - mean;
                mean += delta / (float)count;
                float delta2 = gs_DetailPixels[k] - mean;
                m2 += delta * delta2;
            }
        }

        if (count > 0)
        {
            float variance = m2 / (float)count;
            float threshold = max(0.005, _AvgBlockVariance * 0.5);

            AtomicIncrement(IDX_DETAIL_TOTAL);
            if (variance > threshold)
            {
                AtomicIncrement(IDX_DETAIL_BLOCKS);
            }
        }
    }
}

#endif // TEXTURE_ANALYSIS_PERCEPTUAL_INCLUDED
