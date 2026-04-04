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
        [TestCase("_SpecularColorMap")]
        [TestCase("_TransmissionMaskMap")]
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
        [TestCase("_MainGradationTex")]
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
        [TestCase("_ToonRamp")]
        [TestCase("_SDFShadingTexture")]
        [TestCase("_SkinLUT")]
        [TestCase("_ClothDFG")]
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
        [TestCase("_BumpMap", TexturePropertyCategory.Normal)]
        [TestCase("_NormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_DetailNormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_Bump2ndMap", TexturePropertyCategory.Normal)]
        [TestCase("_NormalMapForMatCap", TexturePropertyCategory.Normal)]
        [TestCase("_MatCapBumpMap", TexturePropertyCategory.Normal)]
        [TestCase("_MatCap2ndBumpMap", TexturePropertyCategory.Normal)]
        [TestCase("_Matcap0NormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_Matcap1NormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_Matcap2NormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_Matcap3NormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_RgbNormalR", TexturePropertyCategory.Normal)]
        [TestCase("_RgbNormalG", TexturePropertyCategory.Normal)]
        [TestCase("_RgbNormalB", TexturePropertyCategory.Normal)]
        [TestCase("_RgbNormalA", TexturePropertyCategory.Normal)]
        [TestCase("_IridescenceNormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_AnisotropyTangentMap", TexturePropertyCategory.Normal)]
        [TestCase("_OutlineVectorTex", TexturePropertyCategory.Normal)]
        [TestCase("_FurVectorTex", TexturePropertyCategory.Normal)]
        [TestCase("_BakedNormal", TexturePropertyCategory.Normal)]
        [TestCase("_NormalMapOS", TexturePropertyCategory.Normal)]
        [TestCase("_BentNormalMap", TexturePropertyCategory.Normal)]
        [TestCase("_BentNormalMapOS", TexturePropertyCategory.Normal)]
        [TestCase("_EmissionMap", TexturePropertyCategory.Emission)]
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
                "_BumpMap",
                "_NormalMap",
                "_DetailNormalMap",
                "_NormalMapForMatCap",
                "_MatCapBumpMap",
                "_MatCap2ndBumpMap",
                "_Matcap0NormalMap",
                "_Matcap1NormalMap",
                "_Matcap2NormalMap",
                "_Matcap3NormalMap",
                "_RgbNormalR",
                "_RgbNormalG",
                "_RgbNormalB",
                "_RgbNormalA",
                "_IridescenceNormalMap",
                "_AnisotropyTangentMap",
                "_OutlineVectorTex",
                "_FurVectorTex",
                "_BakedNormal",
                "_NormalMapOS",
                "_BentNormalMap",
                "_BentNormalMapOS",
                "_EmissionMap",
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
