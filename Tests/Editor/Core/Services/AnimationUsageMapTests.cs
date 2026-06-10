using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AnimationUsageMapTests
    {
        [Test]
        public void IsMaterialPropertyAnimated_ReturnsTrue_ForRecordedProperty()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission", "_MainTex" });

            Assert.That(map.IsMaterialPropertyAnimated("_UseEmission"), Is.True);
            Assert.That(map.IsMaterialPropertyAnimated("_MainTex"), Is.True);
        }

        [Test]
        public void IsMaterialPropertyAnimated_ReturnsFalse_ForUnrecordedProperty()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission" });

            Assert.That(map.IsMaterialPropertyAnimated("_UseBumpMap"), Is.False);
        }

        [Test]
        public void IsMaterialPropertyAnimated_ReturnsFalse_ForNullOrEmpty()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission" });

            Assert.That(map.IsMaterialPropertyAnimated(null), Is.False);
            Assert.That(map.IsMaterialPropertyAnimated(string.Empty), Is.False);
        }

        [Test]
        public void Constructor_FiltersNullAndEmptyEntries()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission", null, string.Empty });

            Assert.That(map.Count, Is.EqualTo(1));
            Assert.That(map.IsMaterialPropertyAnimated("_UseEmission"), Is.True);
        }

        [Test]
        public void Constructor_NullEnumerable_ProducesEmptyMap()
        {
            var map = new AnimationUsageMap(null);

            Assert.That(map.Count, Is.EqualTo(0));
            Assert.That(map.IsMaterialPropertyAnimated("_UseEmission"), Is.False);
        }

        [Test]
        public void Constructor_DeduplicatesProperties()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission", "_UseEmission" });

            Assert.That(map.Count, Is.EqualTo(1));
        }

        [Test]
        public void Empty_HasNoAnimatedProperties()
        {
            Assert.That(AnimationUsageMap.Empty.Count, Is.EqualTo(0));
            Assert.That(
                AnimationUsageMap.Empty.IsMaterialPropertyAnimated("_UseEmission"),
                Is.False
            );
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
