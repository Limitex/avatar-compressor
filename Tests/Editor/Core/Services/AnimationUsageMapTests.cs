using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AnimationUsageMapTests
    {
        [Test]
        public void Constructor_FiltersNullAndEmptyEntries()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission", null, string.Empty });

            Assert.That(map.AnimatedProperties, Is.EquivalentTo(new[] { "_UseEmission" }));
        }

        [Test]
        public void Constructor_NullEnumerable_ProducesEmptyMap()
        {
            var map = new AnimationUsageMap(null);

            Assert.That(map.AnimatedProperties, Is.Empty);
        }

        [Test]
        public void Constructor_DeduplicatesProperties()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission", "_UseEmission" });

            Assert.That(map.AnimatedProperties, Is.EquivalentTo(new[] { "_UseEmission" }));
        }

        [Test]
        public void Empty_HasNoAnimatedProperties()
        {
            Assert.That(AnimationUsageMap.Empty.AnimatedProperties, Is.Empty);
        }

        [Test]
        public void Build_NullContext_ReturnsNull()
        {
            Assert.That(AnimationUsageMap.Build(null), Is.Null);
        }

        [Test]
        public void IsTextureAnimated_ReturnsTrue_ForRecordedTexture()
        {
            var texture = new Texture2D(4, 4);
            try
            {
                var map = new AnimationUsageMap(null, new[] { texture });

                Assert.That(map.IsTextureAnimated(texture), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void IsTextureAnimated_ReturnsFalse_ForUnrecordedOrNullTexture()
        {
            var recorded = new Texture2D(4, 4);
            var other = new Texture2D(4, 4);
            try
            {
                var map = new AnimationUsageMap(null, new[] { recorded });

                Assert.That(map.IsTextureAnimated(other), Is.False);
                Assert.That(map.IsTextureAnimated(null), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(recorded);
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void Constructor_FiltersNullAnimatedTextures()
        {
            var texture = new Texture2D(4, 4);
            try
            {
                var map = new AnimationUsageMap(null, new Texture2D[] { texture, null });

                Assert.That(map.IsTextureAnimated(texture), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
