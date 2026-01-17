using dev.limitex.avatar.compressor.editor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Mathematical utilities for image processing.
    /// Supports transparent pixel skipping via negative grayscale values.
    /// </summary>
    public static class ImageMath
    {
        private const int DctBlockSize = AnalysisConstants.DctBlockSize;
        private const int GlcmLevels = AnalysisConstants.GlcmLevels;
        private const int HistogramBins = AnalysisConstants.HistogramBins;

        // Cached DCT cosine table (thread-safe static initialization)
        private static readonly float[,] DctCosTable = InitializeDctCosTable();

        private static float[,] InitializeDctCosTable()
        {
            var table = new float[DctBlockSize, DctBlockSize];
            for (int i = 0; i < DctBlockSize; i++)
            {
                for (int j = 0; j < DctBlockSize; j++)
                {
                    table[i, j] = Mathf.Cos((2f * i + 1f) * j * Mathf.PI / (2f * DctBlockSize));
                }
            }
            return table;
        }

        #region Gradient Analysis

        /// <summary>
        /// Calculates Sobel gradient magnitude.
        /// Skips transparent pixels (marked with negative values).
        /// </summary>
        public static float CalculateSobelGradient(float[] grayscale, int width, int height, int opaqueCount)
        {
            if (opaqueCount == 0) return 0f;

            float total = 0f;
            int count = 0;
            int step = Mathf.Max(1, width / 256);
            int totalPixels = width * height;

            for (int y = 1; y < height - 1; y += step)
            {
                for (int x = 1; x < width - 1; x += step)
                {
                    int idx = y * width + x;
                    int idxUpLeft = idx - width - 1;
                    int idxUp = idx - width;
                    int idxUpRight = idx - width + 1;
                    int idxLeft = idx - 1;
                    int idxRight = idx + 1;
                    int idxDownLeft = idx + width - 1;
                    int idxDown = idx + width;
                    int idxDownRight = idx + width + 1;

                    if (idxDownRight >= totalPixels || idxUpLeft < 0) continue;

                    // Skip if any neighbor is transparent
                    if (AlphaExtractor.IsTransparent(grayscale[idx]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxUpLeft]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxUp]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxUpRight]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxLeft]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxRight]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxDownLeft]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxDown]) ||
                        AlphaExtractor.IsTransparent(grayscale[idxDownRight]))
                        continue;

                    float gx =
                        -grayscale[idxUpLeft] + grayscale[idxUpRight] +
                        -2f * grayscale[idxLeft] + 2f * grayscale[idxRight] +
                        -grayscale[idxDownLeft] + grayscale[idxDownRight];

                    float gy =
                        -grayscale[idxUpLeft] - 2f * grayscale[idxUp] - grayscale[idxUpRight] +
                        grayscale[idxDownLeft] + 2f * grayscale[idxDown] + grayscale[idxDownRight];

                    total += Mathf.Sqrt(gx * gx + gy * gy);
                    count++;
                }
            }

            return count > 0 ? total / count : 0f;
        }

        /// <summary>
        /// Calculates spatial frequency.
        /// Skips transparent pixels (marked with negative values).
        /// </summary>
        public static float CalculateSpatialFrequency(float[] grayscale, int width, int height, int opaqueCount)
        {
            if (opaqueCount == 0) return 0f;

            float rowFreq = 0f;
            float colFreq = 0f;
            int rowCount = 0;
            int colCount = 0;
            int step = Mathf.Max(1, width / 256);
            int totalPixels = width * height;

            for (int y = 0; y < height; y += step)
            {
                for (int x = step; x < width; x += step)
                {
                    int idx = y * width + x;
                    int prevIdx = y * width + (x - step);

                    if (idx < totalPixels && prevIdx >= 0 &&
                        !AlphaExtractor.IsTransparent(grayscale[idx]) &&
                        !AlphaExtractor.IsTransparent(grayscale[prevIdx]))
                    {
                        float diff = grayscale[idx] - grayscale[prevIdx];
                        rowFreq += diff * diff;
                        rowCount++;
                    }
                }
            }

            for (int y = step; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    int idx = y * width + x;
                    int prevIdx = (y - step) * width + x;

                    if (idx < totalPixels && prevIdx >= 0 &&
                        !AlphaExtractor.IsTransparent(grayscale[idx]) &&
                        !AlphaExtractor.IsTransparent(grayscale[prevIdx]))
                    {
                        float diff = grayscale[idx] - grayscale[prevIdx];
                        colFreq += diff * diff;
                        colCount++;
                    }
                }
            }

            rowFreq = rowCount > 0 ? Mathf.Sqrt(rowFreq / rowCount) : 0f;
            colFreq = colCount > 0 ? Mathf.Sqrt(colFreq / colCount) : 0f;

            return Mathf.Sqrt(rowFreq * rowFreq + colFreq * colFreq);
        }

        #endregion

        #region Color Analysis

        /// <summary>
        /// Calculates color variance in RGB space.
        /// Skips transparent pixels (alpha < threshold).
        /// </summary>
        public static float CalculateColorVariance(Color[] pixels, int opaqueCount)
        {
            if (opaqueCount == 0) return 0f;

            Vector3 mean = Vector3.zero;
            int validCount = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 0.1f) continue;
                mean.x += pixels[i].r;
                mean.y += pixels[i].g;
                mean.z += pixels[i].b;
                validCount++;
            }

            if (validCount == 0) return 0f;
            mean /= validCount;

            float variance = 0f;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 0.1f) continue;
                Vector3 diff = new Vector3(
                    pixels[i].r - mean.x,
                    pixels[i].g - mean.y,
                    pixels[i].b - mean.z);
                variance += diff.sqrMagnitude;
            }

            return variance / validCount;
        }

        #endregion

        #region DCT Analysis

        /// <summary>
        /// Calculates DCT high frequency energy ratio.
        /// Skips blocks containing transparent pixels.
        /// </summary>
        public static float CalculateDctHighFrequencyRatio(float[] grayscale, int width, int height, int opaqueCount)
        {
            if (opaqueCount == 0) return 0f;

            int blocksX = width / DctBlockSize;
            int blocksY = height / DctBlockSize;

            if (blocksX == 0 || blocksY == 0) return 0f;

            float totalHighFreq = 0f;
            float totalEnergy = 0f;
            int totalPixels = width * height;

            int blockStep = Mathf.Max(1, blocksX / 16);
            float[,] block = new float[DctBlockSize, DctBlockSize];
            float[,] dct = new float[DctBlockSize, DctBlockSize];

            for (int by = 0; by < blocksY; by += blockStep)
            {
                for (int bx = 0; bx < blocksX; bx += blockStep)
                {
                    bool validBlock = true;
                    for (int y = 0; y < DctBlockSize && validBlock; y++)
                    {
                        for (int x = 0; x < DctBlockSize && validBlock; x++)
                        {
                            int px = bx * DctBlockSize + x;
                            int py = by * DctBlockSize + y;
                            int idx = py * width + px;

                            if (idx >= totalPixels || AlphaExtractor.IsTransparent(grayscale[idx]))
                            {
                                validBlock = false;
                                break;
                            }
                            block[y, x] = grayscale[idx];
                        }
                    }

                    if (!validBlock) continue;

                    for (int v = 0; v < DctBlockSize; v++)
                    {
                        for (int u = 0; u < DctBlockSize; u++)
                        {
                            float sum = 0f;
                            for (int y = 0; y < DctBlockSize; y++)
                            {
                                for (int x = 0; x < DctBlockSize; x++)
                                {
                                    sum += block[y, x] * DctCosTable[x, u] * DctCosTable[y, v];
                                }
                            }

                            float cu = u == 0 ? 1f / Mathf.Sqrt(2f) : 1f;
                            float cv = v == 0 ? 1f / Mathf.Sqrt(2f) : 1f;
                            dct[v, u] = 0.25f * cu * cv * sum;
                        }
                    }

                    for (int v = 0; v < DctBlockSize; v++)
                    {
                        for (int u = 0; u < DctBlockSize; u++)
                        {
                            float energy = dct[v, u] * dct[v, u];
                            totalEnergy += energy;
                            if (u + v > 2) totalHighFreq += energy;
                        }
                    }
                }
            }

            return totalEnergy > 0.0001f ? totalHighFreq / totalEnergy : 0f;
        }

        #endregion

        #region GLCM Analysis

        /// <summary>
        /// Calculates GLCM features (contrast, homogeneity, energy).
        /// Skips transparent pixels.
        /// </summary>
        public static (float contrast, float homogeneity, float energy) CalculateGlcmFeatures(
            float[] grayscale, int width, int height, int opaqueCount)
        {
            if (opaqueCount == 0) return (0f, 1f, 1f);

            int totalPixels = width * height;
            float[,] glcm = new float[GlcmLevels, GlcmLevels];
            int pairs = 0;

            // Horizontal direction
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int idx1 = y * width + x;
                    int idx2 = y * width + x + 1;

                    if (idx1 < totalPixels && idx2 < totalPixels &&
                        !AlphaExtractor.IsTransparent(grayscale[idx1]) &&
                        !AlphaExtractor.IsTransparent(grayscale[idx2]))
                    {
                        int i = Mathf.Clamp((int)(grayscale[idx1] * (GlcmLevels - 1)), 0, GlcmLevels - 1);
                        int j = Mathf.Clamp((int)(grayscale[idx2] * (GlcmLevels - 1)), 0, GlcmLevels - 1);
                        glcm[i, j]++;
                        glcm[j, i]++;
                        pairs += 2;
                    }
                }
            }

            // Vertical direction
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx1 = y * width + x;
                    int idx2 = (y + 1) * width + x;

                    if (idx1 < totalPixels && idx2 < totalPixels &&
                        !AlphaExtractor.IsTransparent(grayscale[idx1]) &&
                        !AlphaExtractor.IsTransparent(grayscale[idx2]))
                    {
                        int i = Mathf.Clamp((int)(grayscale[idx1] * (GlcmLevels - 1)), 0, GlcmLevels - 1);
                        int j = Mathf.Clamp((int)(grayscale[idx2] * (GlcmLevels - 1)), 0, GlcmLevels - 1);
                        glcm[i, j]++;
                        glcm[j, i]++;
                        pairs += 2;
                    }
                }
            }

            if (pairs == 0) return (0f, 1f, 1f);

            for (int i = 0; i < GlcmLevels; i++)
            {
                for (int j = 0; j < GlcmLevels; j++)
                {
                    glcm[i, j] /= pairs;
                }
            }

            float contrast = 0f;
            float homogeneity = 0f;
            float energy = 0f;

            for (int i = 0; i < GlcmLevels; i++)
            {
                for (int j = 0; j < GlcmLevels; j++)
                {
                    float p = glcm[i, j];
                    int diff = i - j;
                    contrast += diff * diff * p;
                    homogeneity += p / (1f + Mathf.Abs(diff));
                    energy += p * p;
                }
            }

            return (contrast, homogeneity, energy);
        }

        #endregion

        #region Entropy

        /// <summary>
        /// Calculates Shannon entropy.
        /// Skips transparent pixels.
        /// </summary>
        public static float CalculateEntropy(float[] grayscale, int opaqueCount)
        {
            if (opaqueCount == 0) return 0f;

            int[] histogram = new int[HistogramBins];
            int validCount = 0;

            for (int i = 0; i < grayscale.Length; i++)
            {
                if (AlphaExtractor.IsTransparent(grayscale[i])) continue;
                int bin = Mathf.Clamp((int)(grayscale[i] * (HistogramBins - 1)), 0, HistogramBins - 1);
                histogram[bin]++;
                validCount++;
            }

            if (validCount == 0) return 0f;

            float entropy = 0f;
            float total = validCount;

            for (int i = 0; i < HistogramBins; i++)
            {
                if (histogram[i] > 0)
                {
                    float prob = histogram[i] / total;
                    entropy -= prob * Mathf.Log(prob, 2f);
                }
            }

            return entropy;
        }

        #endregion

        #region Block Variance

        /// <summary>
        /// Calculates average block variance.
        /// Skips transparent pixels.
        /// </summary>
        public static float CalculateBlockVariance(float[] grayscale, int width, int height, int opaqueCount, int blockSize)
        {
            if (opaqueCount == 0) return 0f;

            float totalVariance = 0f;
            int blockCount = 0;
            int totalPixels = width * height;

            int blocksX = width / blockSize;
            int blocksY = height / blockSize;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    float blockMean = 0f;
                    int validPixels = 0;

                    for (int y = 0; y < blockSize; y++)
                    {
                        for (int x = 0; x < blockSize; x++)
                        {
                            int idx = (by * blockSize + y) * width + (bx * blockSize + x);
                            if (idx < totalPixels && !AlphaExtractor.IsTransparent(grayscale[idx]))
                            {
                                blockMean += grayscale[idx];
                                validPixels++;
                            }
                        }
                    }

                    if (validPixels == 0) continue;
                    blockMean /= validPixels;

                    float blockVariance = 0f;
                    for (int y = 0; y < blockSize; y++)
                    {
                        for (int x = 0; x < blockSize; x++)
                        {
                            int idx = (by * blockSize + y) * width + (bx * blockSize + x);
                            if (idx < totalPixels && !AlphaExtractor.IsTransparent(grayscale[idx]))
                            {
                                float diff = grayscale[idx] - blockMean;
                                blockVariance += diff * diff;
                            }
                        }
                    }
                    blockVariance /= validPixels;

                    totalVariance += blockVariance;
                    blockCount++;
                }
            }

            return blockCount > 0 ? totalVariance / blockCount : 0f;
        }

        /// <summary>
        /// Calculates edge density with sampling.
        /// Skips transparent pixels.
        /// </summary>
        public static float CalculateEdgeDensity(float[] grayscale, int width, int height, int opaqueCount)
        {
            if (opaqueCount == 0) return 0f;

            float edgeSum = 0f;
            int edgeCount = 0;
            int step = Mathf.Max(1, width / 128);
            int totalPixels = width * height;

            for (int y = 1; y < height - 1; y += step)
            {
                for (int x = 1; x < width - 1; x += step)
                {
                    int idx = y * width + x;
                    int idxRight = idx + 1;
                    int idxLeft = idx - 1;
                    int idxDown = idx + width;
                    int idxUp = idx - width;

                    if (idxDown < totalPixels && idxUp >= 0 &&
                        !AlphaExtractor.IsTransparent(grayscale[idx]) &&
                        !AlphaExtractor.IsTransparent(grayscale[idxRight]) &&
                        !AlphaExtractor.IsTransparent(grayscale[idxLeft]) &&
                        !AlphaExtractor.IsTransparent(grayscale[idxDown]) &&
                        !AlphaExtractor.IsTransparent(grayscale[idxUp]))
                    {
                        float grad = Mathf.Abs(grayscale[idxRight] - grayscale[idxLeft]) +
                                     Mathf.Abs(grayscale[idxDown] - grayscale[idxUp]);
                        edgeSum += grad;
                        edgeCount++;
                    }
                }
            }

            return edgeCount > 0 ? edgeSum / edgeCount : 0f;
        }

        /// <summary>
        /// Calculates detail density with adaptive threshold.
        /// Skips transparent pixels.
        /// </summary>
        public static float CalculateDetailDensity(float[] grayscale, int width, int height, int opaqueCount, float avgVariance)
        {
            if (opaqueCount == 0) return 0f;

            const int blockSize = 16;
            int detailBlocks = 0;
            int totalBlocks = 0;
            int totalPixels = width * height;

            float threshold = Mathf.Max(0.005f, avgVariance * 0.5f);

            int blocksX = width / blockSize;
            int blocksY = height / blockSize;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    float mean = 0f;
                    int validPixels = 0;

                    for (int y = 0; y < blockSize; y++)
                    {
                        for (int x = 0; x < blockSize; x++)
                        {
                            int idx = (by * blockSize + y) * width + (bx * blockSize + x);
                            if (idx < totalPixels && !AlphaExtractor.IsTransparent(grayscale[idx]))
                            {
                                mean += grayscale[idx];
                                validPixels++;
                            }
                        }
                    }

                    if (validPixels == 0) continue;
                    mean /= validPixels;

                    float variance = 0f;
                    for (int y = 0; y < blockSize; y++)
                    {
                        for (int x = 0; x < blockSize; x++)
                        {
                            int idx = (by * blockSize + y) * width + (bx * blockSize + x);
                            if (idx < totalPixels && !AlphaExtractor.IsTransparent(grayscale[idx]))
                            {
                                float diff = grayscale[idx] - mean;
                                variance += diff * diff;
                            }
                        }
                    }
                    variance /= validPixels;

                    if (variance > threshold) detailBlocks++;
                    totalBlocks++;
                }
            }

            return totalBlocks > 0 ? (float)detailBlocks / totalBlocks : 0f;
        }

        #endregion
    }
}
