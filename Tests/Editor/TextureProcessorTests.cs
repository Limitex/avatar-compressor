using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureProcessorTests
    {
        private TextureProcessor _processor;

        [SetUp]
        public void SetUp()
        {
            // Default: min=32, max=2048, forcePowerOfTwo=true
            _processor = new TextureProcessor(32, 2048, true);
        }

        #region CalculateNewDimensions Tests

        [Test]
        public void CalculateNewDimensions_Divisor1_ReturnsSameSize()
        {
            var result = _processor.CalculateNewDimensions(512, 512, 1);
            Assert.AreEqual(512, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_Divisor2_ReturnsHalfSize()
        {
            var result = _processor.CalculateNewDimensions(512, 512, 2);
            Assert.AreEqual(256, result.x);
            Assert.AreEqual(256, result.y);
        }

        [Test]
        public void CalculateNewDimensions_Divisor4_ReturnsQuarterSize()
        {
            var result = _processor.CalculateNewDimensions(1024, 1024, 4);
            Assert.AreEqual(256, result.x);
            Assert.AreEqual(256, result.y);
        }

        [Test]
        public void CalculateNewDimensions_RespectsMinResolution()
        {
            // With divisor 8, 128/8=16, but min is 32
            var result = _processor.CalculateNewDimensions(128, 128, 8);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_RespectsMaxResolution()
        {
            // 4096 with divisor 1 should be clamped to max (2048)
            var result = _processor.CalculateNewDimensions(4096, 4096, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(2048, result.y);
        }

        [Test]
        public void CalculateNewDimensions_NonSquare_PreservesAspectRatio()
        {
            var result = _processor.CalculateNewDimensions(1024, 512, 2);
            Assert.AreEqual(512, result.x);
            Assert.AreEqual(256, result.y);
        }

        [Test]
        public void CalculateNewDimensions_ForcePowerOfTwo_ReturnsPowerOfTwo()
        {
            // 300/2=150, closest power of 2 is 128
            var result = _processor.CalculateNewDimensions(300, 300, 2);
            Assert.That(IsPowerOfTwo(result.x), Is.True, $"Width {result.x} is not power of 2");
            Assert.That(IsPowerOfTwo(result.y), Is.True, $"Height {result.y} is not power of 2");
        }

        [Test]
        public void CalculateNewDimensions_WithoutForcePowerOfTwo_ReturnsExactValue()
        {
            var resizerNoPow2 = new TextureProcessor(32, 2048, false);
            var result = resizerNoPow2.CalculateNewDimensions(300, 300, 2);
            Assert.AreEqual(150, result.x);
            Assert.AreEqual(150, result.y);
        }

        [Test]
        public void CalculateNewDimensions_VerySmallTexture_ClampsToMin()
        {
            var result = _processor.CalculateNewDimensions(16, 16, 1);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_LargeDivisor_ClampsToMin()
        {
            // 1024/16=64, but with large divisor would be 32 (min)
            var result = _processor.CalculateNewDimensions(256, 256, 16);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_AsymmetricClamping_Works()
        {
            // Width needs clamping to max, height doesn't
            var result = _processor.CalculateNewDimensions(4096, 512, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_CustomMinMax_Respected()
        {
            var customResizer = new TextureProcessor(64, 512, true);
            var result = customResizer.CalculateNewDimensions(1024, 1024, 1);
            Assert.AreEqual(512, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_NonPowerOfTwoSource_HandlesCorrectly()
        {
            // 1000/2=500, closest power of 2 is 512
            var result = _processor.CalculateNewDimensions(1000, 1000, 2);
            Assert.That(result.x, Is.InRange(256, 512));
            Assert.That(result.y, Is.InRange(256, 512));
        }

        [Test]
        public void CalculateNewDimensions_PortraitOrientation_Works()
        {
            var result = _processor.CalculateNewDimensions(256, 1024, 2);
            Assert.AreEqual(128, result.x);
            Assert.AreEqual(512, result.y);
        }

        [Test]
        public void CalculateNewDimensions_ExtremeAspectRatio_Works()
        {
            var result = _processor.CalculateNewDimensions(2048, 64, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(64, result.y);
        }

        #endregion

        #region Power of Two Edge Cases

        [Test]
        public void CalculateNewDimensions_PowerOfTwoMaxClamp_HandlesCorrectly()
        {
            // When result exceeds max and needs power of 2 adjustment
            var customResizer = new TextureProcessor(32, 300, true);
            var result = customResizer.CalculateNewDimensions(1024, 1024, 1);
            // Max is 300, closest power of 2 should be 256 or adjusted
            Assert.That(result.x, Is.LessThanOrEqualTo(300));
            Assert.That(result.y, Is.LessThanOrEqualTo(300));
        }

        [Test]
        public void CalculateNewDimensions_AllPowerOfTwoDivisors_ReturnPowerOfTwo()
        {
            int[] divisors = { 1, 2, 4, 8, 16 };
            foreach (int div in divisors)
            {
                var result = _processor.CalculateNewDimensions(1024, 1024, div);
                Assert.That(IsPowerOfTwo(result.x), Is.True,
                    $"Divisor {div}: Width {result.x} is not power of 2");
                Assert.That(IsPowerOfTwo(result.y), Is.True,
                    $"Divisor {div}: Height {result.y} is not power of 2");
            }
        }

        #endregion

        #region Boundary Value Tests

        [Test]
        public void CalculateNewDimensions_ExactlyAtMin_ReturnsMin()
        {
            var result = _processor.CalculateNewDimensions(32, 32, 1);
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_ExactlyAtMax_ReturnsMax()
        {
            var result = _processor.CalculateNewDimensions(2048, 2048, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(2048, result.y);
        }

        [Test]
        public void CalculateNewDimensions_JustBelowMin_ClampsToMin()
        {
            var result = _processor.CalculateNewDimensions(64, 64, 4);
            // 64/4=16, min is 32
            Assert.AreEqual(32, result.x);
            Assert.AreEqual(32, result.y);
        }

        [Test]
        public void CalculateNewDimensions_JustAboveMax_ClampsToMax()
        {
            var result = _processor.CalculateNewDimensions(2049, 2049, 1);
            Assert.AreEqual(2048, result.x);
            Assert.AreEqual(2048, result.y);
        }

        #endregion

        #region Helper Methods

        private static bool IsPowerOfTwo(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }

        #endregion
    }
}
