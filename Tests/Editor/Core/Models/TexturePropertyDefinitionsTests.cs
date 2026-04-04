using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TexturePropertyDefinitionsTests
    {
        #region Core Properties Tests

        [Test]
        public void TextureProperties_IsNotEmpty()
        {
            Assert.That(TexturePropertyDefinitions.TextureProperties.Count, Is.GreaterThan(0));
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
                TexturePropertyDefinitions.IsKnownTextureProperty(propertyName),
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
                TexturePropertyDefinitions.IsKnownTextureProperty(propertyName),
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
                TexturePropertyDefinitions.IsKnownTextureProperty(propertyName),
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
                TexturePropertyDefinitions.IsKnownTextureProperty(propertyName),
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
                TexturePropertyDefinitions.IsKnownTextureProperty(propertyName),
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
                TexturePropertyDefinitions.IsKnownTextureProperty(propertyName),
                $"Unexpectedly contains unknown property: {propertyName}"
            );
        }

        #endregion

        #region GetCategory Tests

        [TestCase("_MainTex", TexturePropertyCategory.Main)]
        [TestCase("_BaseMap", TexturePropertyCategory.Main)]
        [TestCase("_BaseColorMap", TexturePropertyCategory.Main)]
        [TestCase("_Albedo", TexturePropertyCategory.Main)]
        [TestCase("_AlbedoMap", TexturePropertyCategory.Main)]
        [TestCase("_Diffuse", TexturePropertyCategory.Main)]
        [TestCase("_DiffuseMap", TexturePropertyCategory.Main)]
        [TestCase("_ColorMap", TexturePropertyCategory.Main)]
        [TestCase("_BumpMap", TexturePropertyCategory.Normal)]
        [TestCase("_NormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_Normal", TexturePropertyCategory.Normal)]
        [TestCase("_DetailNormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_Bump2ndMap", TexturePropertyCategory.Normal)]
        [TestCase("_EmissionMap", TexturePropertyCategory.Emission)]
        [TestCase("_EmissiveMap", TexturePropertyCategory.Emission)]
        [TestCase("_EmissiveColorMap", TexturePropertyCategory.Emission)]
        [TestCase("_Emission2ndMap", TexturePropertyCategory.Emission)]
        [TestCase("_EmissionGradTex", TexturePropertyCategory.Emission)]
        [TestCase("_Emission2ndGradTex", TexturePropertyCategory.Emission)]
        [TestCase("_EmissionMap1", TexturePropertyCategory.Emission)]
        [TestCase("_EmissionMap2", TexturePropertyCategory.Emission)]
        [TestCase("_EmissionMap3", TexturePropertyCategory.Emission)]
        [TestCase("_Emissive_Tex", TexturePropertyCategory.Emission)]
        [TestCase("_EmissionBlendMask", TexturePropertyCategory.Other)]
        [TestCase("_EmissionMask", TexturePropertyCategory.Other)]
        [TestCase("_MetallicGlossMap", TexturePropertyCategory.Other)]
        [TestCase("_OcclusionMap", TexturePropertyCategory.Other)]
        [TestCase("_MatCapTex", TexturePropertyCategory.Other)]
        public void GetCategory_ReturnsCorrectCategory(
            string propertyName,
            TexturePropertyCategory expected
        )
        {
            Assert.That(TexturePropertyDefinitions.GetCategory(propertyName), Is.EqualTo(expected));
        }

        [Test]
        public void GetCategory_NullInput_ReturnsOther()
        {
            Assert.That(
                TexturePropertyDefinitions.GetCategory(null),
                Is.EqualTo(TexturePropertyCategory.Other)
            );
        }

        [TestCase("_UnknownProperty")]
        [TestCase("_SPSBakeData")]
        [TestCase("")]
        public void GetCategory_UnknownProperty_ReturnsOther(string propertyName)
        {
            Assert.That(
                TexturePropertyDefinitions.GetCategory(propertyName),
                Is.EqualTo(TexturePropertyCategory.Other)
            );
        }

        #endregion

        #region Consistency Tests

        [Test]
        public void TextureProperties_CountIsStableAcrossAccesses()
        {
            // Verify multiple accesses return the same count (no mutation between calls)
            var first = TexturePropertyDefinitions.TextureProperties.Count;
            var second = TexturePropertyDefinitions.TextureProperties.Count;
            Assert.That(first, Is.EqualTo(second), "Count should be stable across accesses");
        }

        [Test]
        public void TextureProperties_AllEntriesAreNonEmpty()
        {
            foreach (var property in TexturePropertyDefinitions.TextureProperties)
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
            foreach (var property in TexturePropertyDefinitions.TextureProperties)
            {
                Assert.That(
                    property,
                    Does.StartWith("_"),
                    $"Property '{property}' does not start with underscore"
                );
            }
        }

        [Test]
        public void GetCategory_AllCategorizedProperties_AreKnown()
        {
            // Properties that return a specific category (Main, Normal, Emission)
            // must also be recognized as known texture properties.
            var categorizedProperties = new[]
            {
                "_MainTex",
                "_BaseMap",
                "_BaseColorMap",
                "_Albedo",
                "_AlbedoMap",
                "_Diffuse",
                "_DiffuseMap",
                "_ColorMap",
                "_BumpMap",
                "_NormalMap",
                "_Normal",
                "_DetailNormalMap",
                "_EmissionMap",
                "_EmissiveMap",
                "_EmissiveColorMap",
                "_Emission2ndMap",
                "_EmissionGradTex",
                "_Emission2ndGradTex",
                "_EmissionMap1",
                "_EmissionMap2",
                "_EmissionMap3",
                "_Emissive_Tex",
            };

            foreach (var property in categorizedProperties)
            {
                Assert.IsTrue(
                    TexturePropertyDefinitions.IsKnownTextureProperty(property),
                    $"Categorized property '{property}' "
                        + $"(category: {TexturePropertyDefinitions.GetCategory(property)}) "
                        + "is not in AllKnownProperties"
                );
            }
        }

        #endregion
    }
}
