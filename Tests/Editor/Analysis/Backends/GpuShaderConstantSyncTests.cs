using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class GpuShaderConstantSyncTests
    {
        private Dictionary<string, string> _hlslDefines;

        [SetUp]
        public void SetUp()
        {
            var hlslPath = Path.GetFullPath(
                "Packages/dev.limitex.avatar-compressor/"
                    + "Editor/TextureCompressor/Analysis/Shaders/TextureAnalysisCommon.hlsl"
            );
            Assert.IsTrue(File.Exists(hlslPath), $"HLSL file not found at: {hlslPath}");
            _hlslDefines = ParseHlslDefines(hlslPath);
        }

        #region Infrastructure

        [Test]
        public void HlslFile_ContainsExpectedDefines()
        {
            Assert.That(_hlslDefines.Count, Is.GreaterThanOrEqualTo(20));
        }

        #endregion

        #region AnalysisConstants Sync

        [Test]
        public void GlcmLevels_MatchesAnalysisConstants()
        {
            AssertIntDefine("GLCM_LEVELS", AnalysisConstants.GlcmLevels);
        }

        [Test]
        public void DctBlockSize_MatchesAnalysisConstants()
        {
            AssertIntDefine("DCT_BLOCK_SIZE", AnalysisConstants.DctBlockSize);
        }

        [Test]
        public void HistogramBins_MatchesAnalysisConstants()
        {
            AssertIntDefine("HISTOGRAM_BINS", AnalysisConstants.HistogramBins);
        }

        [Test]
        public void PerceptualBlockSize_MatchesAnalysisConstants()
        {
            AssertIntDefine("PERCEPTUAL_BLOCK_SIZE", AnalysisConstants.PerceptualBlockSize);
        }

        [Test]
        public void DetailDensityBlockSize_MatchesAnalysisConstants()
        {
            AssertIntDefine("DETAIL_DENSITY_BLOCK_SIZE", AnalysisConstants.DetailDensityBlockSize);
        }

        [Test]
        public void NormalMapSampleStep_MatchesAnalysisConstants()
        {
            AssertIntDefine("NORMAL_MAP_SAMPLE_STEP", AnalysisConstants.NormalMapSampleStep);
        }

        [Test]
        public void MinAnalysisDimension_MatchesAnalysisConstants()
        {
            AssertIntDefine("MIN_ANALYSIS_DIMENSION", AnalysisConstants.MinAnalysisDimension);
        }

        [Test]
        public void MinNormalMapDimension_MatchesAnalysisConstants()
        {
            AssertIntDefine("MIN_NORMAL_MAP_DIMENSION", AnalysisConstants.MinNormalMapDimension);
        }

        [Test]
        public void MinOpaquePixels_MatchesAnalysisConstants()
        {
            AssertIntDefine(
                "MIN_OPAQUE_PIXELS",
                AnalysisConstants.MinOpaquePixelsForStandardAnalysis
            );
        }

        [Test]
        public void DefaultComplexityScore_MatchesAnalysisConstants()
        {
            AssertFloatDefine("DEFAULT_COMPLEXITY_SCORE", AnalysisConstants.DefaultComplexityScore);
        }

        [Test]
        public void SparseTexturePenalty_MatchesAnalysisConstants()
        {
            AssertFloatDefine("SPARSE_TEXTURE_PENALTY", AnalysisConstants.SparseTexturePenalty);
        }

        [Test]
        public void DetailDensityMinThreshold_MatchesAnalysisConstants()
        {
            AssertFloatDefine(
                "DETAIL_DENSITY_MIN_THRESHOLD",
                AnalysisConstants.DetailDensityMinThreshold
            );
        }

        [Test]
        public void DetailDensityVarianceMultiplier_MatchesAnalysisConstants()
        {
            AssertFloatDefine(
                "DETAIL_DENSITY_VARIANCE_MULTIPLIER",
                AnalysisConstants.DetailDensityVarianceMultiplier
            );
        }

        [Test]
        public void SignificantAlphaThreshold_MatchesAnalysisConstants()
        {
            Assert.IsTrue(
                _hlslDefines.ContainsKey("SIGNIFICANT_ALPHA_THRESHOLD"),
                "SIGNIFICANT_ALPHA_THRESHOLD not found in HLSL"
            );
            // HLSL defines as (250.0 / 255.0), C# stores as byte 250
            float expected = AnalysisConstants.SignificantAlphaThreshold / 255f;
            float hlslValue = EvaluateExpression(_hlslDefines["SIGNIFICANT_ALPHA_THRESHOLD"]);
            Assert.That(
                hlslValue,
                Is.EqualTo(expected).Within(0.0001f),
                "SIGNIFICANT_ALPHA_THRESHOLD mismatch"
            );
        }

        #endregion

        #region GpuBufferLayout Sync

        [Test]
        public void FixedPointScale_MatchesGpuBufferLayout()
        {
            AssertFloatDefine("FIXED_POINT_SCALE", GpuBufferLayout.FixedPointScale);
        }

        [Test]
        public void IdxColorSumR_MatchesGpuBufferLayout()
        {
            AssertIntDefine("IDX_COLOR_SUM_R", GpuBufferLayout.IdxColorSumR);
        }

        [Test]
        public void IdxBlockVarSum_MatchesGpuBufferLayout()
        {
            AssertIntDefine("IDX_BLOCK_VAR_SUM", GpuBufferLayout.IdxBlockVarSum);
        }

        [Test]
        public void IntermediateBufferSize_MatchesGpuBufferLayout()
        {
            AssertIntDefine("INTERMEDIATE_BUFFER_SIZE", GpuBufferLayout.IntermediateBufferSize);
        }

        #endregion

        #region Strategy Index Sync

        [Test]
        public void StrategyFast_MatchesExpectedIndex()
        {
            AssertIntDefine("STRATEGY_FAST", 0);
        }

        [Test]
        public void StrategyHighAccuracy_MatchesExpectedIndex()
        {
            AssertIntDefine("STRATEGY_HIGH_ACCURACY", 1);
        }

        [Test]
        public void StrategyPerceptual_MatchesExpectedIndex()
        {
            AssertIntDefine("STRATEGY_PERCEPTUAL", 2);
        }

        [Test]
        public void StrategyCombined_MatchesExpectedIndex()
        {
            AssertIntDefine("STRATEGY_COMBINED", 3);
        }

        #endregion

        #region Helpers

        private void AssertIntDefine(string defineName, int expected)
        {
            Assert.IsTrue(_hlslDefines.ContainsKey(defineName), $"{defineName} not found in HLSL");
            Assert.IsTrue(
                int.TryParse(_hlslDefines[defineName], out int hlslValue),
                $"Cannot parse {defineName} value '{_hlslDefines[defineName]}' as int"
            );
            Assert.AreEqual(expected, hlslValue, $"{defineName} mismatch");
        }

        private void AssertFloatDefine(string defineName, float expected)
        {
            Assert.IsTrue(_hlslDefines.ContainsKey(defineName), $"{defineName} not found in HLSL");
            float hlslValue = EvaluateExpression(_hlslDefines[defineName]);
            Assert.That(hlslValue, Is.EqualTo(expected).Within(0.0001f), $"{defineName} mismatch");
        }

        private static Dictionary<string, string> ParseHlslDefines(string filePath)
        {
            var defines = new Dictionary<string, string>();
            var regex = new Regex(@"^\s*#define\s+(\w+)\s+(.+?)(?:\s*//.*)?$");

            foreach (var line in File.ReadAllLines(filePath))
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    string name = match.Groups[1].Value;
                    string value = match.Groups[2].Value.Trim();
                    defines[name] = value;
                }
            }

            return defines;
        }

        private static float EvaluateExpression(string expression)
        {
            // Handle simple parenthesized division: (A / B)
            var divMatch = Regex.Match(expression, @"\(?\s*([\d.]+)\s*/\s*([\d.]+)\s*\)?");
            if (divMatch.Success)
            {
                float a = float.Parse(
                    divMatch.Groups[1].Value,
                    System.Globalization.CultureInfo.InvariantCulture
                );
                float b = float.Parse(
                    divMatch.Groups[2].Value,
                    System.Globalization.CultureInfo.InvariantCulture
                );
                return a / b;
            }

            // Simple numeric value
            return float.Parse(expression, System.Globalization.CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
