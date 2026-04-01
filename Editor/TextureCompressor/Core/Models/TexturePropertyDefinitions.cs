using System.Collections.Generic;
using System.Linq;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Categories for texture properties, used to control per-type processing toggles.
    /// </summary>
    public enum TexturePropertyCategory
    {
        Main,
        Normal,
        Emission,
        Other,
    }

    /// <summary>
    /// Known texture property names from common VRChat shaders.
    /// Provides both property recognition (known vs unknown) and category classification
    /// (Main, Normal, Emission, Other) from a single source of truth.
    ///
    /// Property recognition is used by the "skip unknown uncompressed textures" safety feature.
    /// Uncompressed textures assigned to unknown properties may contain non-visual data
    /// (e.g., SPS bake data, masks, lookup tables), where compression could corrupt the data.
    ///
    /// Category classification is used by per-type processing toggles to let users
    /// selectively enable or disable compression for each texture category.
    ///
    /// Sources verified against actual repositories:
    ///   - Unity Standard / URP / HDRP (built-in shader documentation)
    ///   - lilToon (https://github.com/lilxyzw/lilToon)
    ///   - Poiyomi Toon Shader (https://github.com/poiyomi/PoiyomiToonShader)
    ///   - UTS2 (https://github.com/unity3d-jp/UnityChanToonShaderVer2_Project)
    /// </summary>
    public static class TexturePropertyDefinitions
    {
        /// <summary>
        /// Property names classified as main (albedo/diffuse) textures.
        /// These are the primary color textures used across common shaders.
        /// </summary>
        private static readonly HashSet<string> MainProperties = new HashSet<string>
        {
            "_MainTex",
            "_BaseMap",
            "_BaseColorMap",
            "_Albedo",
            "_AlbedoMap",
            "_Diffuse",
            "_DiffuseMap",
            "_ColorMap",
        };

        /// <summary>
        /// Property names classified as normal map textures.
        /// </summary>
        private static readonly HashSet<string> NormalProperties = new HashSet<string>
        {
            "_BumpMap",
            "_NormalMap",
            "_Normal",
            "_DetailNormalMap",
        };

        /// <summary>
        /// Property names classified as emission textures.
        /// </summary>
        private static readonly HashSet<string> EmissionProperties = new HashSet<string>
        {
            "_EmissionMap",
            "_EmissiveMap",
        };

        private static readonly HashSet<string> UnityProperties = new HashSet<string>
        {
            // Standard
            "_MainTex",
            "_BumpMap",
            "_DetailNormalMap",
            "_EmissionMap",
            "_MetallicGlossMap",
            "_SpecGlossMap",
            "_OcclusionMap",
            "_ParallaxMap",
            "_DetailMask",
            "_DetailAlbedoMap",
            // URP Lit
            "_BaseMap",
            "_SpecularColorMap",
            "_ClearCoatMap",
            // HDRP Lit
            "_BaseColorMap",
            "_NormalMap",
            "_MaskMap",
            "_EmissiveColorMap",
            "_DetailMap",
            "_NormalMapOS",
            "_BentNormalMap",
            "_BentNormalMapOS",
            "_HeightMap",
            "_TangentMap",
            "_TangentMapOS",
            "_AnisotropyMap",
            "_SubsurfaceMaskMap",
            "_ThicknessMap",
            "_TransmittanceColorMap",
            "_IridescenceThicknessMap",
            "_IridescenceMaskMap",
            "_CoatMaskMap",
            "_SpecularOcclusionMap",
            // Common aliases (used by various third-party shaders)
            "_Albedo",
            "_AlbedoMap",
            "_Diffuse",
            "_DiffuseMap",
            "_ColorMap",
            "_Normal",
            "_NormalMapForMatCap",
            "_EmissiveMap",
        };

        private static readonly HashSet<string> LilToonProperties = new HashSet<string>
        {
            "_MainTex",
            "_BaseMap",
            "_BaseColorMap",
            "_BumpMap",
            "_EmissionMap",
            "_MetallicGlossMap",
            "_ParallaxMap",
            "_MainColorAdjustMask",
            "_Main2ndTex",
            "_Main2ndBlendMask",
            "_Main2ndDissolveMask",
            "_Main2ndDissolveNoiseMask",
            "_Main3rdTex",
            "_Main3rdBlendMask",
            "_Main3rdDissolveMask",
            "_Main3rdDissolveNoiseMask",
            "_AlphaMask",
            "_ShadowStrengthMask",
            "_ShadowColorTex",
            "_ShadowBorderMask",
            "_ShadowBlurMask",
            "_Shadow2ndColorTex",
            "_Shadow3rdColorTex",
            "_Ramp",
            "_RimColorTex",
            "_RimShadeMask",
            "_GlitterColorTex",
            "_GlitterShapeTex",
            "_EmissionBlendMask",
            "_EmissionGradTex",
            "_Emission2ndMap",
            "_Emission2ndBlendMask",
            "_Emission2ndGradTex",
            "_Bump2ndMap",
            "_Bump2ndScaleMask",
            "_AnisotropyTangentMap",
            "_AnisotropyScaleMask",
            "_AnisotropyShiftNoiseMask",
            "_BacklightColorTex",
            "_MatCapTex",
            "_MatCapBlendMask",
            "_MatCapBumpMap",
            "_MatCap2ndTex",
            "_MatCap2ndBlendMask",
            "_MatCap2ndBumpMap",
            "_OutlineTex",
            "_OutlineWidthMask",
            "_OutlineVectorTex",
            "_FurNoiseMask",
            "_FurMask",
            "_FurLengthMask",
            "_FurVectorTex",
            "_AudioLinkMask",
            "_AudioLinkLocalMap",
            "_DissolveMask",
            "_DissolveNoiseMask",
            "_TriMask",
            "_SmoothnessTex",
            "_ReflectionColorTex",
        };

        private static readonly HashSet<string> PoiyomiProperties = new HashSet<string>
        {
            "_MainTex",
            "_BumpMap",
            "_DetailNormalMap",
            "_DetailMask",
            "_EmissionMap",
            "_MetallicGlossMap",
            "_AlphaMask",
            "_AlphaTexture",
            "_BlueTexture",
            "_GreenTexture",
            "_RedTexture",
            "_RedTexure", // typo variant in v7.3
            "_BackFaceTexture",
            "_BackFaceMask",
            "_DetailTex",
            "_MirrorTexture",
            "_MainColorAdjustTexture",
            "_MainFadeTexture",
            "_ClippingMask",
            "_DecalTexture",
            "_DecalTexture1",
            "_DecalTexture2",
            "_DecalTexture3",
            "_DecalMask",
            "_ALDecalColorMask",
            "_GlobalMaskTexture0",
            "_GlobalMaskTexture1",
            "_GlobalMaskTexture2",
            "_GlobalMaskTexture3",
            "_RGBMask",
            "_PPMask",
            "_DepthMask",
            "_DepthBulgeMask",
            "_LookAtMask",
            "_1st_ShadeMap",
            "_2nd_ShadeMap",
            "_ShadowColorTex",
            "_Shadow2ndColorTex",
            "_Shadow3rdColorTex",
            "_ShadowStrengthMask",
            "_ShadowBorderMask",
            "_MultilayerMathBlurMap",
            "_LightingAOMaps",
            "_LightingAOTex",
            "_LightingDetailShadowMaps",
            "_LightingDetailShadows",
            "_LightingShadowMask",
            "_LightingShadowMasks",
            "_MochieMetallicMaps",
            "_MetallicMask",
            "_MetallicTintMap",
            "_SmoothnessTex",
            "_SmoothnessMask",
            "_RGBAMetallicMaps",
            "_RGBASmoothnessMaps",
            "_HeightMap",
            "_Heightmask",
            "_BRDFMetallicGlossMap",
            "_BRDFMetallicMap",
            "_BRDFSpecularMap",
            "_ClearCoatMaps",
            "_ClearcoatMap",
            "_ClearCoatMask",
            "_ClearCoatSmoothnessMask",
            "_ClothMetallicSmoothnessMap",
            "_SSSThicknessMap",
            "_SkinThicknessMap",
            "_SpecularMap",
            "_SpecularMap1",
            "_SpecularMask",
            "_SpecularMask1",
            "_SpecularMetallicMap",
            "_SpecularMetallicMap1",
            "_HighColor_Tex",
            "_Set_HighColorMask",
            "_AnisoColorMap",
            "_AnisoNoiseMap",
            "_AnisoTangentMap",
            "_AnisoTangentMap1",
            "_AnisotropyMap",
            "_SpecularAnisoJitterMacro",
            "_SpecularAnisoJitterMacro1",
            "_SpecularAnisoJitterMicro",
            "_SpecularAnisoJitterMicro1",
            "_Matcap",
            "_Matcap2",
            "_Matcap3",
            "_Matcap4",
            "_MatcapMask",
            "_Matcap2Mask",
            "_Matcap3Mask",
            "_Matcap4Mask",
            "_Matcap0NormalMap",
            "_Matcap1NormalMap",
            "_Matcap2NormalMap",
            "_Matcap3NormalMap",
            "_CubeMapMask",
            "_ReflectionColorTex",
            "_RimTex",
            "_RimMask",
            "_Rim2Tex",
            "_Rim2Mask",
            "_RimColorTex",
            "_Rim2ColorTex",
            "_RimEnviroMask",
            "_RimWidthNoiseTexture",
            "_Set_RimLightMask",
            "_Set_Rim2LightMask",
            "_RgbNormalR",
            "_RgbNormalG",
            "_RgbNormalB",
            "_RgbNormalA",
            "_BacklightColorTex",
            "_FurMask",
            "_FurLengthMask",
            "_FurNoiseMask",
            "_FurVectorTex",
            "_IridescenceRamp",
            "_IridescenceMask",
            "_IridescenceNormalMap",
            "_EmissionMap1",
            "_EmissionMap2",
            "_EmissionMap3",
            "_EmissionMask",
            "_EmissionMask1",
            "_EmissionMask2",
            "_EmissionMask3",
            "_EmissionScrollingCurve",
            "_EmissionScrollingCurve1",
            "_EmissionScrollingCurve2",
            "_EmissionScrollingCurve3",
            "_DissolveToTexture",
            "_DissolveNoiseTexture",
            "_DissolveDetailNoise",
            "_DissolveEdgeGradient",
            "_DissolveMask",
            "_FlipbookMask",
            "_GlitterMask",
            "_GlitterTexture",
            "_GlitterColorMap",
            "_OutlineTexture",
            "_OutlineMask",
            "_PanosphereTexture",
            "_PanoMask",
            "_PanoMapTexture",
            "_ParallaxHeightMap",
            "_ParallaxHeightMapMask",
            "_ParallaxInternalMap",
            "_ParallaxInternalMapMask",
            "_PathingColorMap",
            "_PathingMap",
            "_DistortionFlowTexture",
            "_DistortionFlowTexture1",
            "_DistortionMask",
            "_DepthTexture",
            "_GrabPassBlendMap",
            "_VideoMaskTexture",
            "_VideoPixelTexture",
            "_VoronoiNoise",
            "_VoronoiMask",
            "_TruchetTex",
            "_TruchetMask",
            "_VertexBasicsMask",
            "_VertexGlitchMap",
            "_VertexManipulationHeightMask",
            "_UzumoreMask",
        };

        private static readonly HashSet<string> UtsProperties = new HashSet<string>
        {
            "_MainTex",
            "_BaseMap",
            "_NormalMap",
            "_1st_ShadeMap",
            "_2nd_ShadeMap",
            "_ClippingMask",
            "_HighColor_Tex",
            "_MatCap_Sampler",
            "_NormalMapForMatCap",
            "_Set_RimLightMask",
            "_Set_MatcapMask",
            "_OutlineTex",
            "_Outline_Sampler",
            "_Set_HighColorMask",
            "_Set_1st_ShadePosition",
            "_Set_2nd_ShadePosition",
            "_ShadingGradeMap",
            "_Emissive_Tex",
            "_BakedNormal",
            "_AngelRing_Sampler",
        };

        private static readonly HashSet<string> AllKnownProperties = new HashSet<string>(
            UnityProperties
                .Concat(LilToonProperties)
                .Concat(PoiyomiProperties)
                .Concat(UtsProperties)
        );

        /// <summary>
        /// Read-only access to all known texture property names.
        /// </summary>
        public static IReadOnlyCollection<string> TextureProperties => AllKnownProperties;

        /// <summary>
        /// Returns true if the given property name is a known, compressible texture property.
        /// </summary>
        public static bool IsKnownTextureProperty(string propertyName)
        {
            return propertyName != null && AllKnownProperties.Contains(propertyName);
        }

        /// <summary>
        /// Returns the category for the given texture property name.
        /// Properties not in the Main, Normal, or Emission category sets return Other.
        /// </summary>
        public static TexturePropertyCategory GetCategory(string propertyName)
        {
            if (propertyName == null)
                return TexturePropertyCategory.Other;
            if (MainProperties.Contains(propertyName))
                return TexturePropertyCategory.Main;
            if (NormalProperties.Contains(propertyName))
                return TexturePropertyCategory.Normal;
            if (EmissionProperties.Contains(propertyName))
                return TexturePropertyCategory.Emission;
            return TexturePropertyCategory.Other;
        }
    }
}
