// TextureAnalysisNormalMap.hlsl
// Kernel for normal map analysis: NormalMapVariation.

#ifndef TEXTURE_ANALYSIS_NORMAL_MAP_INCLUDED
#define TEXTURE_ANALYSIS_NORMAL_MAP_INCLUDED

// Kernel 12: Normal Map Variation (5-tap cross, dot product)
[numthreads(16, 16, 1)]
void NormalMapVariation(uint3 id : SV_DispatchThreadID)
{
    uint x = id.x * NORMAL_MAP_SAMPLE_STEP;
    uint y = id.y * NORMAL_MAP_SAMPLE_STEP;

    if (x < 1 || x >= _Width - 1 || y < 1 || y >= _Height - 1)
        return;

    float4 c0 = SamplePixel(x, y);
    float4 c1 = SamplePixel(x + 1, y);
    float4 c2 = SamplePixel(x, y + 1);
    float4 c3 = SamplePixel(x - 1, y);
    float4 c4 = SamplePixel(x, y - 1);

    // Decode normals: n = normalize(rgb * 2 - 1)
    float3 n0 = normalize(c0.rgb * 2.0 - 1.0);
    float3 n1 = normalize(c1.rgb * 2.0 - 1.0);
    float3 n2 = normalize(c2.rgb * 2.0 - 1.0);
    float3 n3 = normalize(c3.rgb * 2.0 - 1.0);
    float3 n4 = normalize(c4.rgb * 2.0 - 1.0);

    float variation = 0.0;
    variation += 1.0 - dot(n0, n1);
    variation += 1.0 - dot(n0, n2);
    variation += 1.0 - dot(n0, n3);
    variation += 1.0 - dot(n0, n4);
    variation *= 0.25;

    AtomicAddFixed(IDX_NORMAL_VAR_SUM, variation);
    AtomicIncrement(IDX_NORMAL_VAR_COUNT);
}

#endif // TEXTURE_ANALYSIS_NORMAL_MAP_INCLUDED
