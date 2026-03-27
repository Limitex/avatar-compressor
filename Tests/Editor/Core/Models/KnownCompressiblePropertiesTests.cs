using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class KnownCompressiblePropertiesTests
    {
        #region Core Properties Tests

        [Test]
        public void TextureProperties_IsNotNull()
        {
            Assert.That(KnownCompressibleProperties.TextureProperties, Is.Not.Null);
        }

        [Test]
        public void TextureProperties_IsNotEmpty()
        {
            Assert.That(KnownCompressibleProperties.TextureProperties.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Unity Standard Properties

        [TestCase("_MainTex")]
        [TestCase("_BumpMap")]
        [TestCase("_EmissionMap")]
        [TestCase("_MetallicGlossMap")]
        [TestCase("_OcclusionMap")]
        [TestCase("_ParallaxMap")]
        [TestCase("_DetailNormalMap")]
        [TestCase("_DetailMask")]
        [TestCase("_DetailAlbedoMap")]
        public void TextureProperties_ContainsUnityStandardProperty(string propertyName)
        {
            Assert.IsTrue(
                KnownCompressibleProperties.TextureProperties.Contains(propertyName),
                $"Missing Unity Standard property: {propertyName}"
            );
        }

        #endregion

        #region Unity URP/HDRP Properties

        [TestCase("_BaseMap")]
        [TestCase("_BaseColorMap")]
        [TestCase("_NormalMap")]
        [TestCase("_MaskMap")]
        [TestCase("_EmissiveColorMap")]
        public void TextureProperties_ContainsUnityUrpHdrpProperty(string propertyName)
        {
            Assert.IsTrue(
                KnownCompressibleProperties.TextureProperties.Contains(propertyName),
                $"Missing URP/HDRP property: {propertyName}"
            );
        }

        #endregion

        #region lilToon Properties

        [TestCase("_Main2ndTex")]
        [TestCase("_Main3rdTex")]
        [TestCase("_ShadowColorTex")]
        [TestCase("_MatCapTex")]
        [TestCase("_OutlineTex")]
        [TestCase("_FurNoiseMask")]
        [TestCase("_EmissionGradTex")]
        public void TextureProperties_ContainsLilToonProperty(string propertyName)
        {
            Assert.IsTrue(
                KnownCompressibleProperties.TextureProperties.Contains(propertyName),
                $"Missing lilToon property: {propertyName}"
            );
        }

        #endregion

        #region Poiyomi Properties

        [TestCase("_AlphaTexture")]
        [TestCase("_DecalTexture")]
        [TestCase("_Matcap")]
        [TestCase("_Matcap2")]
        [TestCase("_RimTex")]
        [TestCase("_GlitterTexture")]
        [TestCase("_OutlineTexture")]
        [TestCase("_DissolveNoiseTexture")]
        public void TextureProperties_ContainsPoiyomiProperty(string propertyName)
        {
            Assert.IsTrue(
                KnownCompressibleProperties.TextureProperties.Contains(propertyName),
                $"Missing Poiyomi property: {propertyName}"
            );
        }

        #endregion

        #region UTS Properties

        [TestCase("_1st_ShadeMap")]
        [TestCase("_2nd_ShadeMap")]
        [TestCase("_HighColor_Tex")]
        [TestCase("_MatCap_Sampler")]
        [TestCase("_Emissive_Tex")]
        [TestCase("_AngelRing_Sampler")]
        public void TextureProperties_ContainsUtsProperty(string propertyName)
        {
            Assert.IsTrue(
                KnownCompressibleProperties.TextureProperties.Contains(propertyName),
                $"Missing UTS property: {propertyName}"
            );
        }

        #endregion

        #region Unknown Properties

        [TestCase("_CustomDataMap")]
        [TestCase("_SPSBakeData")]
        [TestCase("_MyCustomTexture")]
        [TestCase("_UnknownProperty")]
        [TestCase("")]
        public void TextureProperties_DoesNotContainUnknownProperty(string propertyName)
        {
            Assert.IsFalse(
                KnownCompressibleProperties.TextureProperties.Contains(propertyName),
                $"Unexpectedly contains unknown property: {propertyName}"
            );
        }

        #endregion

        #region Consistency Tests

        [Test]
        public void TextureProperties_NoDuplicatesAffectCount()
        {
            // HashSet inherently prevents duplicates, so count should be stable
            var count = KnownCompressibleProperties.TextureProperties.Count;
            Assert.That(count, Is.GreaterThan(100), "Expected a large set of known properties");
        }

        [Test]
        public void TextureProperties_AllEntriesAreNonEmpty()
        {
            foreach (var property in KnownCompressibleProperties.TextureProperties)
            {
                Assert.That(
                    property,
                    Is.Not.Null.And.Not.Empty,
                    "Found null or empty property name in TextureProperties"
                );
            }
        }

        [Test]
        public void TextureProperties_AllEntriesStartWithUnderscore()
        {
            foreach (var property in KnownCompressibleProperties.TextureProperties)
            {
                Assert.That(
                    property,
                    Does.StartWith("_"),
                    $"Property '{property}' does not start with underscore"
                );
            }
        }

        #endregion
    }
}
