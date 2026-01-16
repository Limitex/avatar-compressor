using NUnit.Framework;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ComplexityCalculatorTests
    {
        private ComplexityCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            // Default thresholds: high=0.7, low=0.3, divisor range 1-8
            _calculator = new ComplexityCalculator(0.7f, 0.3f, 1, 8);
        }

        [Test]
        public void CalculateRecommendedDivisor_HighComplexity_ReturnsMinDivisor()
        {
            // High complexity (0.9 > 0.7) should return minimum divisor (1)
            int divisor = _calculator.CalculateRecommendedDivisor(0.9f);
            Assert.AreEqual(1, divisor);
        }

        [Test]
        public void CalculateRecommendedDivisor_LowComplexity_ReturnsMaxDivisor()
        {
            // Low complexity (0.1 < 0.3) should return maximum divisor (8)
            int divisor = _calculator.CalculateRecommendedDivisor(0.1f);
            Assert.AreEqual(8, divisor);
        }

        [Test]
        public void CalculateRecommendedDivisor_MediumComplexity_ReturnsIntermediateDivisor()
        {
            // Medium complexity (0.5) should return an intermediate divisor
            int divisor = _calculator.CalculateRecommendedDivisor(0.5f);
            Assert.That(divisor, Is.GreaterThan(1));
            Assert.That(divisor, Is.LessThan(8));
        }

        [Test]
        public void CalculateRecommendedDivisor_ExactHighThreshold_ReturnsMinDivisor()
        {
            int divisor = _calculator.CalculateRecommendedDivisor(0.7f);
            Assert.AreEqual(1, divisor);
        }

        [Test]
        public void CalculateRecommendedDivisor_ExactLowThreshold_ReturnsMaxDivisor()
        {
            int divisor = _calculator.CalculateRecommendedDivisor(0.3f);
            Assert.AreEqual(8, divisor);
        }

        [Test]
        public void CalculateRecommendedDivisor_EqualThresholds_ReturnsMiddleDivisor()
        {
            // When thresholds are equal, should return middle divisor
            var equalThresholdCalc = new ComplexityCalculator(0.5f, 0.5f, 1, 8);
            int divisor = equalThresholdCalc.CalculateRecommendedDivisor(0.5f);
            // With t=0.5, should be around sqrt(1*8) ≈ 2.83, rounded to power of 2 = 2 or 4
            Assert.That(divisor, Is.InRange(2, 4));
        }

        [Test]
        public void CalculateRecommendedDivisor_ZeroComplexity_ReturnsMaxDivisor()
        {
            int divisor = _calculator.CalculateRecommendedDivisor(0f);
            Assert.AreEqual(8, divisor);
        }

        [Test]
        public void CalculateRecommendedDivisor_OneComplexity_ReturnsMinDivisor()
        {
            int divisor = _calculator.CalculateRecommendedDivisor(1f);
            Assert.AreEqual(1, divisor);
        }

        [Test]
        public void CalculateRecommendedDivisor_ReturnsPowerOfTwo()
        {
            // Test that divisor is always a power of 2
            for (float c = 0f; c <= 1f; c += 0.1f)
            {
                int divisor = _calculator.CalculateRecommendedDivisor(c);
                Assert.That(IsPowerOfTwo(divisor), Is.True,
                    $"Divisor {divisor} for complexity {c} is not a power of 2");
            }
        }

        [Test]
        public void CalculateRecommendedDivisor_NeverExceedsBounds()
        {
            var calc = new ComplexityCalculator(0.8f, 0.2f, 2, 16);

            for (float c = -0.5f; c <= 1.5f; c += 0.1f)
            {
                int divisor = calc.CalculateRecommendedDivisor(c);
                Assert.That(divisor, Is.GreaterThanOrEqualTo(2));
                Assert.That(divisor, Is.LessThanOrEqualTo(16));
            }
        }

        private static bool IsPowerOfTwo(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }
    }
}
