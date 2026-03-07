// TextureAnalysisHighAccuracy.hlsl
// Kernels for the HighAccuracy analysis strategy:
// DctHighFreqRatio, GlcmAccumulate, GlcmFeatures, Entropy, EntropyFinalize.

#ifndef TEXTURE_ANALYSIS_HIGH_ACCURACY_INCLUDED
#define TEXTURE_ANALYSIS_HIGH_ACCURACY_INCLUDED

// Pre-computed DCT cosine table for 8-point DCT
// DctCosTable[i][j] = cos((2*i+1)*j*PI/16)
static const float DctCosTable[8][8] = {
    { 1.000000, 0.980785, 0.923880, 0.831470, 0.707107, 0.555570, 0.382683, 0.195090 },
    { 1.000000, 0.831470, 0.382683,-0.195090,-0.707107,-0.980785,-0.923880,-0.555570 },
    { 1.000000, 0.555570,-0.382683,-0.980785,-0.707107, 0.195090, 0.923880, 0.831470 },
    { 1.000000, 0.195090,-0.923880,-0.555570, 0.707107, 0.831470,-0.382683,-0.980785 },
    { 1.000000,-0.195090,-0.923880, 0.555570, 0.707107,-0.831470,-0.382683, 0.980785 },
    { 1.000000,-0.555570,-0.382683, 0.980785,-0.707107,-0.195090, 0.923880,-0.831470 },
    { 1.000000,-0.831470, 0.382683, 0.195090,-0.707107, 0.980785,-0.923880, 0.555570 },
    { 1.000000,-0.980785, 0.923880,-0.831470, 0.707107,-0.555570, 0.382683,-0.195090 }
};

static const float InvSqrt2 = 0.707107;

// Kernel 5: DCT High Frequency Ratio (8x8 block DCT)
// One thread group per 8x8 block.
// Group thread (tx, ty) computes DCT coefficient (u=tx, v=ty).
groupshared float gs_DctBlock[8][8];
groupshared uint gs_BlockInvalid;

[numthreads(8, 8, 1)]
void DctHighFreqRatio(uint3 groupId : SV_GroupID, uint3 gtid : SV_GroupThreadID)
{
    // Map dispatch group to actual block position via step (matches CPU blockStep)
    uint bx = groupId.x * _DctBlockStep;
    uint by = groupId.y * _DctBlockStep;
    uint tx = gtid.x; // u coordinate
    uint ty = gtid.y; // v coordinate

    uint blocksX = _Width / DCT_BLOCK_SIZE;
    uint blocksY = _Height / DCT_BLOCK_SIZE;

    // Bounds check (stepped dispatch may overshoot)
    if (bx >= blocksX || by >= blocksY)
        return;

    uint px = bx * DCT_BLOCK_SIZE + tx;
    uint py = by * DCT_BLOCK_SIZE + ty;

    // Initialize shared flag (0 = valid)
    if (tx == 0 && ty == 0)
        gs_BlockInvalid = 0;
    GroupMemoryBarrierWithGroupSync();

    // Load pixel and check transparency
    if (px < _Width && py < _Height)
    {
        float4 pixel = SamplePixel(px, py);
        if (!_IsNormalMap && IsTransparent(pixel))
            InterlockedOr(gs_BlockInvalid, 1u);
        gs_DctBlock[ty][tx] = ToGrayscale(pixel.rgb);
    }
    else
    {
        InterlockedOr(gs_BlockInvalid, 1u);
        gs_DctBlock[ty][tx] = 0.0;
    }
    GroupMemoryBarrierWithGroupSync();

    if (gs_BlockInvalid != 0)
        return;

    // Compute 2D DCT coefficient (u=tx, v=ty)
    float sum = 0.0;
    for (uint y = 0; y < DCT_BLOCK_SIZE; y++)
    {
        for (uint x = 0; x < DCT_BLOCK_SIZE; x++)
        {
            sum += gs_DctBlock[y][x] * DctCosTable[x][tx] * DctCosTable[y][ty];
        }
    }

    float cu = (tx == 0) ? InvSqrt2 : 1.0;
    float cv = (ty == 0) ? InvSqrt2 : 1.0;
    float coeff = 0.25 * cu * cv * sum;
    // Store energy in gs_DctBlock (reused; pixel data no longer needed)
    float energy = coeff * coeff;
    gs_DctBlock[ty][tx] = energy;
    GroupMemoryBarrierWithGroupSync();

    // Thread 0 reduces per-block energies and accumulates globally.
    // Uses global accumulation (not per-block ratio) to match CPU implementation:
    // final ratio = totalHighFreq / totalEnergy (energy-weighted average).
    if (tx == 0 && ty == 0)
    {
        float totalEnergy = 0.0;
        float highFreqEnergy = 0.0;
        for (uint v = 0; v < DCT_BLOCK_SIZE; v++)
        {
            for (uint u = 0; u < DCT_BLOCK_SIZE; u++)
            {
                float e = gs_DctBlock[v][u];
                totalEnergy += e;
                if (u + v > 2) // exclude DC and lowest AC coefficients in 8x8 DCT
                    highFreqEnergy += e;
            }
        }

        AtomicAddFixed(IDX_DCT_HIGH_FREQ, highFreqEnergy);
        AtomicAddFixed(IDX_DCT_TOTAL_ENERGY, totalEnergy);
    }
}

// Kernel 6: GLCM Accumulate (build co-occurrence matrix)
[numthreads(16, 16, 1)]
void GlcmAccumulate(uint3 id : SV_DispatchThreadID)
{
    uint x = id.x;
    uint y = id.y;

    if (x >= _Width || y >= _Height)
        return;

    float4 c = SamplePixel(x, y);
    if (!_IsNormalMap && IsTransparent(c))
        return;

    float gray = ToGrayscale(c.rgb);
    uint i = clamp((uint)(gray * (GLCM_LEVELS - 1)), 0, GLCM_LEVELS - 1);

    // Horizontal neighbor
    if (x + 1 < _Width)
    {
        float4 cRight = SamplePixel(x + 1, y);
        if (_IsNormalMap || !IsTransparent(cRight))
        {
            float grayRight = ToGrayscale(cRight.rgb);
            uint j = clamp((uint)(grayRight * (GLCM_LEVELS - 1)), 0, GLCM_LEVELS - 1);

            AtomicIncrement(IDX_GLCM_MATRIX + i * GLCM_LEVELS + j);
            AtomicIncrement(IDX_GLCM_MATRIX + j * GLCM_LEVELS + i);
            AtomicAddUint(IDX_GLCM_PAIRS, 2u);
        }
    }

    // Vertical neighbor
    if (y + 1 < _Height)
    {
        float4 cDown = SamplePixel(x, y + 1);
        if (_IsNormalMap || !IsTransparent(cDown))
        {
            float grayDown = ToGrayscale(cDown.rgb);
            uint j = clamp((uint)(grayDown * (GLCM_LEVELS - 1)), 0, GLCM_LEVELS - 1);

            AtomicIncrement(IDX_GLCM_MATRIX + i * GLCM_LEVELS + j);
            AtomicIncrement(IDX_GLCM_MATRIX + j * GLCM_LEVELS + i);
            AtomicAddUint(IDX_GLCM_PAIRS, 2u);
        }
    }
}

// Kernel 7: GLCM Features (compute contrast, homogeneity, energy from matrix)
// Single workgroup processes the 16x16 matrix.
groupshared float gs_GlcmContrast[256];
groupshared float gs_GlcmHomogeneity[256];
groupshared float gs_GlcmEnergy[256];

[numthreads(256, 1, 1)]
void GlcmFeatures(uint3 gtid : SV_GroupThreadID)
{
    uint idx = gtid.x;
    uint pairs = _IntermediateBuffer[IDX_GLCM_PAIRS];

    // Note: no early return here — all threads must reach GroupMemoryBarrierWithGroupSync.
    // When pairs == 0, groupshared arrays are zeroed; the fallback is applied at the final write.
    if (pairs == 0 || idx >= GLCM_LEVELS * GLCM_LEVELS)
    {
        gs_GlcmContrast[idx] = 0.0;
        gs_GlcmHomogeneity[idx] = 0.0;
        gs_GlcmEnergy[idx] = 0.0;
    }
    else
    {
        uint i = idx / GLCM_LEVELS;
        uint j = idx % GLCM_LEVELS;
        float p = (float)_IntermediateBuffer[IDX_GLCM_MATRIX + idx] / (float)pairs;
        int diff = (int)i - (int)j;

        gs_GlcmContrast[idx] = (float)(diff * diff) * p;
        gs_GlcmHomogeneity[idx] = p / (1.0 + abs((float)diff));
        gs_GlcmEnergy[idx] = p * p;
    }
    GroupMemoryBarrierWithGroupSync();

    // Parallel reduction (256 threads)
    for (uint stride = 128; stride > 0; stride >>= 1)
    {
        if (idx < stride)
        {
            gs_GlcmContrast[idx] += gs_GlcmContrast[idx + stride];
            gs_GlcmHomogeneity[idx] += gs_GlcmHomogeneity[idx + stride];
            gs_GlcmEnergy[idx] += gs_GlcmEnergy[idx + stride];
        }
        GroupMemoryBarrierWithGroupSync();
    }

    // Thread 0 writes results to intermediate buffer
    // Reuse GLCM slots for the computed features
    if (idx == 0)
    {
        if (pairs == 0)
        {
            // Match CPU fallback: contrast=0, homogeneity=1, energy=1
            _IntermediateBuffer[IDX_GLCM_MATRIX + 0] = 0u;
            _IntermediateBuffer[IDX_GLCM_MATRIX + 1] = (uint)(1.0 * FIXED_POINT_SCALE + 0.5);
            _IntermediateBuffer[IDX_GLCM_MATRIX + 2] = (uint)(1.0 * FIXED_POINT_SCALE + 0.5);
        }
        else
        {
            _IntermediateBuffer[IDX_GLCM_MATRIX + 0] = (uint)(gs_GlcmContrast[0] * FIXED_POINT_SCALE + 0.5);
            _IntermediateBuffer[IDX_GLCM_MATRIX + 1] = (uint)(gs_GlcmHomogeneity[0] * FIXED_POINT_SCALE + 0.5);
            _IntermediateBuffer[IDX_GLCM_MATRIX + 2] = (uint)(gs_GlcmEnergy[0] * FIXED_POINT_SCALE + 0.5);
        }
    }
}

// Kernel 8: Shannon Entropy (histogram-based)
[numthreads(16, 16, 1)]
void Entropy(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= _Width || id.y >= _Height)
        return;

    float4 pixel = SamplePixel(id.x, id.y);
    if (!_IsNormalMap && IsTransparent(pixel))
        return;

    float gray = ToGrayscale(pixel.rgb);
    uint bin = clamp((uint)(gray * (HISTOGRAM_BINS - 1)), 0, HISTOGRAM_BINS - 1);
    AtomicIncrement(IDX_ENTROPY_HISTOGRAM + bin);
    AtomicIncrement(IDX_ENTROPY_VALID_COUNT);
}

// Kernel 8b: Entropy Finalize (compute entropy from histogram)
groupshared float gs_EntropyPartial[256];

[numthreads(256, 1, 1)]
void EntropyFinalize(uint3 gtid : SV_GroupThreadID)
{
    uint idx = gtid.x;
    uint totalCount = _IntermediateBuffer[IDX_ENTROPY_VALID_COUNT];

    if (totalCount == 0 || idx >= HISTOGRAM_BINS)
    {
        gs_EntropyPartial[idx] = 0.0;
    }
    else
    {
        uint count = _IntermediateBuffer[IDX_ENTROPY_HISTOGRAM + idx];
        if (count > 0)
        {
            float prob = (float)count / (float)totalCount;
            gs_EntropyPartial[idx] = -prob * log2(prob);
        }
        else
        {
            gs_EntropyPartial[idx] = 0.0;
        }
    }
    GroupMemoryBarrierWithGroupSync();

    // Parallel reduction
    for (uint stride = 128; stride > 0; stride >>= 1)
    {
        if (idx < stride)
        {
            gs_EntropyPartial[idx] += gs_EntropyPartial[idx + stride];
        }
        GroupMemoryBarrierWithGroupSync();
    }

    if (idx == 0)
    {
        // Store entropy as fixed-point in dedicated result slot
        _IntermediateBuffer[IDX_ENTROPY_RESULT] = (uint)(gs_EntropyPartial[0] * FIXED_POINT_SCALE + 0.5);
    }
}

#endif // TEXTURE_ANALYSIS_HIGH_ACCURACY_INCLUDED
