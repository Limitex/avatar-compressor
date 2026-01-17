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
    }
}
