using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureComplexityResultTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_SetsScore()
        {
            var result = new TextureComplexityResult(0.5f);

            Assert.That(result.Score, Is.EqualTo(0.5f));
        }

        #endregion

        #region Boundary Value Tests

        [Test]
        public void Constructor_ZeroScore_IsValid()
        {
            var result = new TextureComplexityResult(0f);

            Assert.That(result.Score, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_OneScore_IsValid()
        {
            var result = new TextureComplexityResult(1f);

            Assert.That(result.Score, Is.EqualTo(1f));
        }

        #endregion

        #region Struct Behavior Tests

        [Test]
        public void Struct_DefaultValue_HasZeroScore()
        {
            var result = default(TextureComplexityResult);

            Assert.That(result.Score, Is.EqualTo(0f));
        }

        [Test]
        public void Struct_Assignment_CopiesValues()
        {
            var original = new TextureComplexityResult(0.75f);
            var copy = original;

            Assert.That(copy.Score, Is.EqualTo(original.Score));
        }

        #endregion
    }
}
