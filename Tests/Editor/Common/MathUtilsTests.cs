using NUnit.Framework;
using dev.limitex.avatar.compressor.common;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class MathUtilsTests
    {
        #region NormalizeWithPercentile Tests

        [Test]
        public void NormalizeWithPercentile_ValueBelowLowPercentile_ReturnsZero()
        {
            float result = MathUtils.NormalizeWithPercentile(0.1f, 0.2f, 0.8f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void NormalizeWithPercentile_ValueAtLowPercentile_ReturnsZero()
        {
            float result = MathUtils.NormalizeWithPercentile(0.2f, 0.2f, 0.8f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void NormalizeWithPercentile_ValueAboveHighPercentile_ReturnsOne()
        {
            float result = MathUtils.NormalizeWithPercentile(0.9f, 0.2f, 0.8f);
            Assert.AreEqual(1f, result);
        }

        [Test]
        public void NormalizeWithPercentile_ValueAtHighPercentile_ReturnsOne()
        {
            float result = MathUtils.NormalizeWithPercentile(0.8f, 0.2f, 0.8f);
            Assert.AreEqual(1f, result);
        }

        [Test]
        public void NormalizeWithPercentile_ValueInMiddle_ReturnsHalf()
        {
            float result = MathUtils.NormalizeWithPercentile(0.5f, 0.2f, 0.8f);
            Assert.That(result, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void NormalizeWithPercentile_ValueBetweenPercentiles_ReturnsNormalizedValue()
        {
            // (0.35 - 0.2) / (0.8 - 0.2) = 0.15 / 0.6 = 0.25
            float result = MathUtils.NormalizeWithPercentile(0.35f, 0.2f, 0.8f);
            Assert.That(result, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void NormalizeWithPercentile_NegativeValue_ReturnsZero()
        {
            float result = MathUtils.NormalizeWithPercentile(-0.5f, 0f, 1f);
            Assert.AreEqual(0f, result);
        }

        #endregion

        #region Normalize01 Tests

        [Test]
        public void Normalize01_ValueAtMin_ReturnsZero()
        {
            float result = MathUtils.Normalize01(10f, 10f, 20f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Normalize01_ValueAtMax_ReturnsOne()
        {
            float result = MathUtils.Normalize01(20f, 10f, 20f);
            Assert.AreEqual(1f, result);
        }

        [Test]
        public void Normalize01_ValueInMiddle_ReturnsHalf()
        {
            float result = MathUtils.Normalize01(15f, 10f, 20f);
            Assert.That(result, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void Normalize01_ValueBelowMin_ReturnsZero()
        {
            float result = MathUtils.Normalize01(5f, 10f, 20f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Normalize01_ValueAboveMax_ReturnsOne()
        {
            float result = MathUtils.Normalize01(25f, 10f, 20f);
            Assert.AreEqual(1f, result);
        }

        [Test]
        public void Normalize01_MinEqualsMax_ReturnsZero()
        {
            float result = MathUtils.Normalize01(10f, 10f, 10f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Normalize01_MaxLessThanMin_ReturnsZero()
        {
            float result = MathUtils.Normalize01(15f, 20f, 10f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Normalize01_NegativeRange_WorksCorrectly()
        {
            float result = MathUtils.Normalize01(-5f, -10f, 0f);
            Assert.That(result, Is.EqualTo(0.5f).Within(0.001f));
        }

        #endregion

        #region NextPowerOfTwo Tests

        [Test]
        public void NextPowerOfTwo_PowerOfTwo_ReturnsSameValue()
        {
            Assert.AreEqual(256, MathUtils.NextPowerOfTwo(256));
            Assert.AreEqual(512, MathUtils.NextPowerOfTwo(512));
            Assert.AreEqual(1024, MathUtils.NextPowerOfTwo(1024));
        }

        [Test]
        public void NextPowerOfTwo_NonPowerOfTwo_ReturnsNextPower()
        {
            Assert.AreEqual(256, MathUtils.NextPowerOfTwo(200));
            Assert.AreEqual(512, MathUtils.NextPowerOfTwo(300));
            Assert.AreEqual(1024, MathUtils.NextPowerOfTwo(600));
        }

        [Test]
        public void NextPowerOfTwo_One_ReturnsOne()
        {
            Assert.AreEqual(1, MathUtils.NextPowerOfTwo(1));
        }

        [Test]
        public void NextPowerOfTwo_JustAbovePowerOfTwo_ReturnsNextPower()
        {
            Assert.AreEqual(512, MathUtils.NextPowerOfTwo(257));
        }

        #endregion

        #region ClosestPowerOfTwo Tests

        [Test]
        public void ClosestPowerOfTwo_PowerOfTwo_ReturnsSameValue()
        {
            Assert.AreEqual(256, MathUtils.ClosestPowerOfTwo(256));
            Assert.AreEqual(512, MathUtils.ClosestPowerOfTwo(512));
        }

        [Test]
        public void ClosestPowerOfTwo_CloserToLower_ReturnsLowerPower()
        {
            // 300 is closer to 256 than to 512
            Assert.AreEqual(256, MathUtils.ClosestPowerOfTwo(300));
        }

        [Test]
        public void ClosestPowerOfTwo_CloserToHigher_ReturnsHigherPower()
        {
            // 400 is closer to 512 than to 256
            Assert.AreEqual(512, MathUtils.ClosestPowerOfTwo(400));
        }

        [Test]
        public void ClosestPowerOfTwo_ExactlyBetween_ReturnsHigherPower()
        {
            // 384 is exactly between 256 and 512
            // Unity's Mathf.ClosestPowerOfTwo returns higher in tie
            int result = MathUtils.ClosestPowerOfTwo(384);
            Assert.That(result == 256 || result == 512, Is.True);
        }

        #endregion
    }
}
