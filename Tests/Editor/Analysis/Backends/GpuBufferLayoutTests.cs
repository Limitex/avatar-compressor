using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class GpuBufferLayoutTests
    {
        #region Buffer Sizes

        [Test]
        public void IntermediateBufferSize_IsPositive()
        {
            Assert.That(GpuBufferLayout.IntermediateBufferSize, Is.GreaterThan(0));
        }

        [Test]
        public void ResultBufferSize_IsPositive()
        {
            Assert.That(GpuBufferLayout.ResultBufferSize, Is.GreaterThan(0));
        }

        [Test]
        public void ResultBufferSize_IsThree()
        {
            Assert.AreEqual(3, GpuBufferLayout.ResultBufferSize);
        }

        #endregion

        #region Index Bounds

        [Test]
        public void IdxColorSumR_IsWithinBufferBounds()
        {
            Assert.That(GpuBufferLayout.IdxColorSumR, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                GpuBufferLayout.IdxColorSumR + GpuBufferLayout.ColorMeanFieldCount,
                Is.LessThanOrEqualTo(GpuBufferLayout.IntermediateBufferSize)
            );
        }

        [Test]
        public void IdxBlockVarSum_IsWithinBufferBounds()
        {
            Assert.That(GpuBufferLayout.IdxBlockVarSum, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                GpuBufferLayout.IdxBlockVarSum + GpuBufferLayout.BlockVarFieldCount,
                Is.LessThanOrEqualTo(GpuBufferLayout.IntermediateBufferSize)
            );
        }

        [Test]
        public void ColorMeanRegion_DoesNotOverlapBlockVarRegion()
        {
            int colorMeanEnd = GpuBufferLayout.IdxColorSumR + GpuBufferLayout.ColorMeanFieldCount;
            Assert.That(colorMeanEnd, Is.LessThanOrEqualTo(GpuBufferLayout.IdxBlockVarSum));
        }

        [Test]
        public void AllNamedIndices_AreNonNegative()
        {
            Assert.That(GpuBufferLayout.IdxColorSumR, Is.GreaterThanOrEqualTo(0));
            Assert.That(GpuBufferLayout.IdxBlockVarSum, Is.GreaterThanOrEqualTo(0));
            Assert.That(GpuBufferLayout.ResultIdxScore, Is.GreaterThanOrEqualTo(0));
            Assert.That(GpuBufferLayout.ResultIdxHasAlpha, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Result Buffer Layout

        [Test]
        public void ResultIdxScore_IsZero()
        {
            Assert.AreEqual(0, GpuBufferLayout.ResultIdxScore);
        }

        [Test]
        public void ResultIdxHasAlpha_IsWithinResultBuffer()
        {
            Assert.That(
                GpuBufferLayout.ResultIdxHasAlpha,
                Is.LessThan(GpuBufferLayout.ResultBufferSize)
            );
        }

        #endregion

        #region Fixed-Point Scale

        [Test]
        public void FixedPointScale_IsPositive()
        {
            Assert.That(GpuBufferLayout.FixedPointScale, Is.GreaterThan(0f));
        }

        [Test]
        public void FixedPointScale_Is1000()
        {
            Assert.AreEqual(1000f, GpuBufferLayout.FixedPointScale);
        }

        #endregion

        #region Thread Group Size

        [Test]
        public void ThreadGroupSize_IsPowerOfTwo()
        {
            int size = GpuBufferLayout.ThreadGroupSize;
            Assert.That(size, Is.GreaterThan(0));
            Assert.That(size & (size - 1), Is.EqualTo(0));
        }

        [Test]
        public void ThreadGroupSize_Is16()
        {
            Assert.AreEqual(16, GpuBufferLayout.ThreadGroupSize);
        }

        #endregion

        #region Field Counts

        [Test]
        public void ColorMeanFieldCount_IsFour()
        {
            Assert.AreEqual(4, GpuBufferLayout.ColorMeanFieldCount);
        }

        [Test]
        public void BlockVarFieldCount_IsTwo()
        {
            Assert.AreEqual(2, GpuBufferLayout.BlockVarFieldCount);
        }

        #endregion
    }
}
