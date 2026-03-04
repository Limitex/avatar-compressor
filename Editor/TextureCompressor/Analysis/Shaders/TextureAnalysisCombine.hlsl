// TextureAnalysisCombine.hlsl
// Final kernel that reads accumulated metrics, normalizes,
// and computes the final complexity score.

#ifndef TEXTURE_ANALYSIS_COMBINE_INCLUDED
#define TEXTURE_ANALYSIS_COMBINE_INCLUDED

// Kernel 13: Combine Results
// Reads accumulated metrics, normalizes, and computes final score.
[numthreads(1, 1, 1)]
void CombineResults(uint3 id : SV_DispatchThreadID)
{
    uint opaqueCount = _IntermediateBuffer[IDX_OPAQUE_COUNT];
    uint hasAlpha = _IntermediateBuffer[IDX_HAS_SIGNIFICANT_ALPHA];

    // Fast strategy score
    float fastScore = 0.0;
    {
        float sobelCount = ReadUint(IDX_SOBEL_COUNT);
        float gradient = sobelCount > 0.0 ? ReadFixed(IDX_SOBEL_SUM) / sobelCount : 0.0;
        float normalizedGradient = NormalizeWithPercentile(gradient, _GradientLow, _GradientHigh);

        float rowCount = ReadUint(IDX_SPATIAL_ROW_COUNT);
        float colCount = ReadUint(IDX_SPATIAL_COL_COUNT);
        float rowFreq = rowCount > 0.0 ? sqrt(ReadFixed(IDX_SPATIAL_ROW_SUM) / rowCount) : 0.0;
        float colFreq = colCount > 0.0 ? sqrt(ReadFixed(IDX_SPATIAL_COL_SUM) / colCount) : 0.0;
        float spatialFreq = sqrt(rowFreq * rowFreq + colFreq * colFreq);
        float normalizedSpatialFreq = NormalizeWithPercentile(spatialFreq, _SpatialFreqLow, _SpatialFreqHigh);

        float colorCount = ReadUint(IDX_COLOR_VALID_COUNT);
        float colorVar = colorCount > 0.0 ? ReadFixed(IDX_COLOR_VARIANCE_SUM) / colorCount : 0.0;
        float normalizedColorVar = NormalizeWithPercentile(colorVar, _ColorVarLow, _ColorVarHigh);

        fastScore = clamp(
            _FastGradientWeight * normalizedGradient +
            _FastSpatialFreqWeight * normalizedSpatialFreq +
            _FastColorVarWeight * normalizedColorVar,
            0.0, 1.0);
    }

    // HighAccuracy strategy score
    float highAccScore = 0.0;
    {
        float dctTotalEnergy = ReadFixed(IDX_DCT_TOTAL_ENERGY);
        float dctRatio = dctTotalEnergy > EPSILON ? ReadFixed(IDX_DCT_HIGH_FREQ) / dctTotalEnergy : 0.0;

        // GLCM features (stored by GlcmFeatures kernel)
        float contrast = (float)_IntermediateBuffer[IDX_GLCM_MATRIX + 0] / FIXED_POINT_SCALE;
        float homogeneity = (float)_IntermediateBuffer[IDX_GLCM_MATRIX + 1] / FIXED_POINT_SCALE;
        float energy = (float)_IntermediateBuffer[IDX_GLCM_MATRIX + 2] / FIXED_POINT_SCALE;

        float normalizedContrast = NormalizeWithPercentile(contrast, _ContrastLow, _ContrastHigh);

        // Entropy (stored by EntropyFinalize kernel)
        float entropy = (float)_IntermediateBuffer[IDX_ENTROPY_RESULT] / FIXED_POINT_SCALE;
        float normalizedEntropy = NormalizeWithPercentile(entropy, _EntropyLow, _EntropyHigh);

        highAccScore = clamp(
            _HighAccDctWeight * dctRatio +
            _HighAccContrastWeight * normalizedContrast +
            _HighAccHomogeneityWeight * (1.0 - homogeneity) +
            _HighAccEnergyWeight * (1.0 - sqrt(energy)) +
            _HighAccEntropyWeight * normalizedEntropy,
            0.0, 1.0);
    }

    // Perceptual strategy score
    float perceptualScore = 0.0;
    {
        float blockVarCount = ReadUint(IDX_BLOCK_VAR_COUNT);
        float avgVariance = blockVarCount > 0.0 ? ReadFixed(IDX_BLOCK_VAR_SUM) / blockVarCount : 0.0;
        float varianceScore = NormalizeWithPercentile(avgVariance, _VarianceLow, _VarianceHigh);

        float edgeCount = ReadUint(IDX_EDGE_COUNT);
        float avgEdge = edgeCount > 0.0 ? ReadFixed(IDX_EDGE_SUM) / edgeCount : 0.0;
        float edgeScore = NormalizeWithPercentile(avgEdge, _EdgeLow, _EdgeHigh);

        float detailTotal = ReadUint(IDX_DETAIL_TOTAL);
        float detailDensity = detailTotal > 0.0 ? ReadUint(IDX_DETAIL_BLOCKS) / detailTotal : 0.0;

        // Check minimum dimension guard (same as CPU PerceptualStrategy)
        if (_Width < MIN_ANALYSIS_DIMENSION || _Height < MIN_ANALYSIS_DIMENSION)
        {
            perceptualScore = DEFAULT_COMPLEXITY_SCORE;
        }
        else
        {
            perceptualScore = clamp(
                _PerceptualVarianceWeight * varianceScore +
                _PerceptualEdgeWeight * edgeScore +
                _PerceptualDetailWeight * detailDensity,
                0.0, 1.0);
        }
    }

    // Normal map score
    float normalMapScore = 0.0;
    {
        if (_Width < MIN_NORMAL_MAP_DIMENSION || _Height < MIN_NORMAL_MAP_DIMENSION)
        {
            normalMapScore = DEFAULT_COMPLEXITY_SCORE;
        }
        else
        {
            float normalCount = ReadUint(IDX_NORMAL_VAR_COUNT);
            float avgVariation = normalCount > 0.0 ? ReadFixed(IDX_NORMAL_VAR_SUM) / normalCount : 0.0;
            normalMapScore = clamp(avgVariation * _NormalMapVariationMultiplier, 0.0, 1.0);
        }
    }

    // Select final score based on strategy
    float finalScore = 0.0;

    if (_IsNormalMap)
    {
        finalScore = normalMapScore;
    }
    else if (_StrategyType == 0) // Fast
    {
        finalScore = fastScore;
    }
    else if (_StrategyType == 1) // HighAccuracy
    {
        finalScore = highAccScore;
    }
    else if (_StrategyType == 2) // Perceptual
    {
        finalScore = perceptualScore;
    }
    else if (_StrategyType == 3) // Combined
    {
        float totalWeight = _CombinedFastWeight + _CombinedHighAccWeight + _CombinedPerceptualWeight;
        if (totalWeight < EPSILON)
        {
            finalScore = clamp((fastScore + highAccScore + perceptualScore) / 3.0, 0.0, 1.0);
        }
        else
        {
            finalScore = clamp(
                (fastScore * _CombinedFastWeight +
                 highAccScore * _CombinedHighAccWeight +
                 perceptualScore * _CombinedPerceptualWeight) / totalWeight,
                0.0, 1.0);
        }
    }

    // Handle too few opaque pixels
    if (!_IsNormalMap && opaqueCount < MIN_OPAQUE_PIXELS)
    {
        finalScore = DEFAULT_COMPLEXITY_SCORE * 0.2;
    }

    _ResultBuffer[0] = finalScore;
    _ResultBuffer[1] = (float)opaqueCount;
    _ResultBuffer[2] = (float)hasAlpha;
}

#endif // TEXTURE_ANALYSIS_COMBINE_INCLUDED
