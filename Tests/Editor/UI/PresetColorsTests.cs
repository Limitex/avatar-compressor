using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class PresetColorsTests
    {
        #region Color Definition Tests

        [Test]
        public void HighQuality_IsGreenish()
        {
            // High quality should be green-ish (low R, high G)
            Assert.That(PresetColors.HighQuality.g, Is.GreaterThan(PresetColors.HighQuality.r));
        }

        [Test]
        public void Quality_IsGreenish()
        {
            // Quality should be green-ish
            Assert.That(PresetColors.Quality.g, Is.GreaterThan(PresetColors.Quality.r));
        }

        [Test]
        public void Standard_IsBluish()
        {
            // Standard should be blue-ish
            Assert.That(PresetColors.Standard.b, Is.GreaterThan(PresetColors.Standard.r));
        }

        [Test]
        public void Balanced_IsYellowOrange()
        {
            // Balanced should be yellow/orange (high R and G)
            Assert.That(PresetColors.Balanced.r, Is.GreaterThan(0.5f));
            Assert.That(PresetColors.Balanced.g, Is.GreaterThan(0.5f));
        }

        [Test]
        public void Aggressive_IsReddish()
        {
            // Aggressive should be red-ish (warning color)
            Assert.That(PresetColors.Aggressive.r, Is.GreaterThan(PresetColors.Aggressive.g));
            Assert.That(PresetColors.Aggressive.r, Is.GreaterThan(PresetColors.Aggressive.b));
        }

        [Test]
        public void Custom_IsPurplish()
        {
            // Custom should be purple-ish
            Assert.That(PresetColors.Custom.r, Is.GreaterThan(0.5f));
            Assert.That(PresetColors.Custom.b, Is.GreaterThan(0.5f));
        }

        #endregion

        #region Color Validity Tests

        [Test]
        public void AllColors_HaveValidRGBRange()
        {
            var colors = new[]
            {
                PresetColors.HighQuality,
                PresetColors.Quality,
                PresetColors.Standard,
                PresetColors.Balanced,
                PresetColors.Aggressive,
                PresetColors.Custom,
            };

            foreach (var color in colors)
            {
                Assert.That(color.r, Is.InRange(0f, 1f), "Red component should be in [0, 1]");
                Assert.That(color.g, Is.InRange(0f, 1f), "Green component should be in [0, 1]");
                Assert.That(color.b, Is.InRange(0f, 1f), "Blue component should be in [0, 1]");
            }
        }

        [Test]
        public void AllColors_AreDistinct()
        {
            var colors = new[]
            {
                PresetColors.HighQuality,
                PresetColors.Quality,
                PresetColors.Standard,
                PresetColors.Balanced,
                PresetColors.Aggressive,
                PresetColors.Custom,
            };

            for (int i = 0; i < colors.Length; i++)
            {
                for (int j = i + 1; j < colors.Length; j++)
                {
                    Assert.That(
                        colors[i],
                        Is.Not.EqualTo(colors[j]),
                        $"Colors at index {i} and {j} should be distinct"
                    );
                }
            }
        }

        [Test]
        public void AllColors_HaveDefaultAlpha()
        {
            // Colors should have default alpha (1.0) unless explicitly set
            var colors = new[]
            {
                PresetColors.HighQuality,
                PresetColors.Quality,
                PresetColors.Standard,
                PresetColors.Balanced,
                PresetColors.Aggressive,
                PresetColors.Custom,
            };

            foreach (var color in colors)
            {
                Assert.That(color.a, Is.EqualTo(1f), "Alpha should be 1.0 by default");
            }
        }

        #endregion

        #region Visual Hierarchy Tests

        [Test]
        public void ColorProgression_HighQualityToAggressive_GreenToRed()
        {
            // Visual progression from safe (green) to warning (red)
            Assert.That(
                PresetColors.HighQuality.g,
                Is.GreaterThan(PresetColors.Aggressive.g),
                "High quality should be more green than Aggressive"
            );
            Assert.That(
                PresetColors.Aggressive.r,
                Is.GreaterThan(PresetColors.HighQuality.r),
                "Aggressive should be more red than High quality"
            );
        }

        #endregion
    }
}
