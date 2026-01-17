using NUnit.Framework;
using dev.limitex.avatar.compressor.editor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureInfoTests
    {
        #region Default Values Tests

        [Test]
        public void TextureInfo_DefaultValues_IsProcessedTrue()
        {
            var info = new TextureInfo();

            Assert.That(info.IsProcessed, Is.True);
        }

        [Test]
        public void TextureInfo_DefaultValues_SkipReasonNone()
        {
            var info = new TextureInfo();

            Assert.That(info.SkipReason, Is.EqualTo(SkipReason.None));
        }

        [Test]
        public void TextureInfo_DefaultValues_ReferencesNotNull()
        {
            var info = new TextureInfo();

            Assert.That(info.References, Is.Not.Null);
            Assert.That(info.References.Count, Is.EqualTo(0));
        }

        [Test]
        public void TextureInfo_DefaultValues_PropertiesNull()
        {
            var info = new TextureInfo();

            Assert.That(info.TextureType, Is.Null);
            Assert.That(info.PropertyName, Is.Null);
        }

        [Test]
        public void TextureInfo_DefaultValues_FlagsAreFalse()
        {
            var info = new TextureInfo();

            Assert.That(info.IsNormalMap, Is.False);
            Assert.That(info.IsEmission, Is.False);
        }

        #endregion

        #region Property Setters Tests

        [Test]
        public void TextureInfo_SetTextureType_Works()
        {
            var info = new TextureInfo { TextureType = "Main" };

            Assert.That(info.TextureType, Is.EqualTo("Main"));
        }

        [Test]
        public void TextureInfo_SetPropertyName_Works()
        {
            var info = new TextureInfo { PropertyName = "_MainTex" };

            Assert.That(info.PropertyName, Is.EqualTo("_MainTex"));
        }

        [Test]
        public void TextureInfo_SetIsNormalMap_Works()
        {
            var info = new TextureInfo { IsNormalMap = true };

            Assert.That(info.IsNormalMap, Is.True);
        }

        [Test]
        public void TextureInfo_SetIsEmission_Works()
        {
            var info = new TextureInfo { IsEmission = true };

            Assert.That(info.IsEmission, Is.True);
        }

        [Test]
        public void TextureInfo_SetIsProcessed_Works()
        {
            var info = new TextureInfo { IsProcessed = false };

            Assert.That(info.IsProcessed, Is.False);
        }

        [Test]
        public void TextureInfo_SetSkipReason_Works()
        {
            var info = new TextureInfo { SkipReason = SkipReason.TooSmall };

            Assert.That(info.SkipReason, Is.EqualTo(SkipReason.TooSmall));
        }

        #endregion

        #region References Collection Tests

        [Test]
        public void TextureInfo_AddReference_IncreasesCount()
        {
            var info = new TextureInfo();
            var reference = new MaterialTextureReference();

            info.References.Add(reference);

            Assert.That(info.References.Count, Is.EqualTo(1));
        }

        [Test]
        public void TextureInfo_References_AreSharedAcrossAccesses()
        {
            var info = new TextureInfo();
            var refs1 = info.References;
            var refs2 = info.References;

            Assert.That(refs1, Is.SameAs(refs2));
        }

        #endregion
    }

    [TestFixture]
    public class MaterialTextureReferenceTests
    {
        #region Default Values Tests

        [Test]
        public void MaterialTextureReference_DefaultValues_AllNull()
        {
            var reference = new MaterialTextureReference();

            Assert.That(reference.Material, Is.Null);
            Assert.That(reference.PropertyName, Is.Null);
            Assert.That(reference.Renderer, Is.Null);
        }

        #endregion

        #region Property Setters Tests

        [Test]
        public void MaterialTextureReference_SetPropertyName_Works()
        {
            var reference = new MaterialTextureReference { PropertyName = "_BumpMap" };

            Assert.That(reference.PropertyName, Is.EqualTo("_BumpMap"));
        }

        #endregion
    }

    [TestFixture]
    public class SkipReasonTests
    {
        [Test]
        public void SkipReason_None_HasValueZero()
        {
            Assert.That((int)SkipReason.None, Is.EqualTo(0));
        }

        [Test]
        public void SkipReason_AllValues_AreDistinct()
        {
            var values = new[]
            {
                SkipReason.None,
                SkipReason.TooSmall,
                SkipReason.FilteredByType,
                SkipReason.FrozenSkip,
                SkipReason.RuntimeGenerated,
                SkipReason.ExcludedPath
            };

            var uniqueValues = new System.Collections.Generic.HashSet<SkipReason>(values);

            Assert.That(uniqueValues.Count, Is.EqualTo(values.Length));
        }
    }
}
