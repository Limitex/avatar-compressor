using System.Collections.Generic;

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
    /// Each property is defined once per shader with its category, eliminating the risk
    /// of a property being "known" but missing a category assignment (or vice versa).
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
        /// Unity Standard / URP / HDRP texture properties.
        /// </summary>
        private static readonly (
            string Name,
            TexturePropertyCategory Category
        )[] UnityPropertyDefinitions =
        {
            // Standard
            ("_MainTex", TexturePropertyCategory.Main),
            ("_BumpMap", TexturePropertyCategory.Normal),
            ("_DetailNormalMap", TexturePropertyCategory.Normal),
            ("_EmissionMap", TexturePropertyCategory.Emission),
            ("_MetallicGlossMap", TexturePropertyCategory.Other),
            ("_SpecGlossMap", TexturePropertyCategory.Other),
            ("_OcclusionMap", TexturePropertyCategory.Other),
            ("_ParallaxMap", TexturePropertyCategory.Other),
            ("_DetailMask", TexturePropertyCategory.Other),
            ("_DetailAlbedoMap", TexturePropertyCategory.Other),
            // URP Lit
            ("_BaseMap", TexturePropertyCategory.Main),
            ("_ClearCoatMap", TexturePropertyCategory.Other),
            // HDRP Lit
            ("_BaseColorMap", TexturePropertyCategory.Main),
            ("_NormalMap", TexturePropertyCategory.Normal),
            ("_MaskMap", TexturePropertyCategory.Other),
            ("_EmissiveColorMap", TexturePropertyCategory.Emission),
            ("_DetailMap", TexturePropertyCategory.Other),
            ("_NormalMapOS", TexturePropertyCategory.Normal),
            ("_BentNormalMap", TexturePropertyCategory.Normal),
            ("_BentNormalMapOS", TexturePropertyCategory.Normal),
            ("_HeightMap", TexturePropertyCategory.Other),
            ("_TangentMap", TexturePropertyCategory.Other),
            ("_TangentMapOS", TexturePropertyCategory.Other),
            ("_AnisotropyMap", TexturePropertyCategory.Other),
            ("_SubsurfaceMaskMap", TexturePropertyCategory.Other),
            ("_ThicknessMap", TexturePropertyCategory.Other),
            ("_TransmittanceColorMap", TexturePropertyCategory.Other),
            ("_IridescenceThicknessMap", TexturePropertyCategory.Other),
            ("_IridescenceMaskMap", TexturePropertyCategory.Other),
            ("_CoatMaskMap", TexturePropertyCategory.Other),
            ("_SpecularColorMap", TexturePropertyCategory.Other),
            ("_TransmissionMaskMap", TexturePropertyCategory.Other),
        };

        /// <summary>
        /// lilToon shader texture properties.
        /// </summary>
        private static readonly (
            string Name,
            TexturePropertyCategory Category
        )[] LilToonPropertyDefinitions =
        {
            ("_MainTex", TexturePropertyCategory.Main),
            ("_BumpMap", TexturePropertyCategory.Normal),
            ("_EmissionMap", TexturePropertyCategory.Emission),
            ("_MetallicGlossMap", TexturePropertyCategory.Other),
            ("_ParallaxMap", TexturePropertyCategory.Other),
            ("_MainColorAdjustMask", TexturePropertyCategory.Other),
            ("_Main2ndTex", TexturePropertyCategory.Other),
            ("_Main2ndBlendMask", TexturePropertyCategory.Other),
            ("_Main2ndDissolveMask", TexturePropertyCategory.Other),
            ("_Main2ndDissolveNoiseMask", TexturePropertyCategory.Other),
            ("_Main3rdTex", TexturePropertyCategory.Other),
            ("_Main3rdBlendMask", TexturePropertyCategory.Other),
            ("_Main3rdDissolveMask", TexturePropertyCategory.Other),
            ("_Main3rdDissolveNoiseMask", TexturePropertyCategory.Other),
            ("_AlphaMask", TexturePropertyCategory.Other),
            ("_ShadowStrengthMask", TexturePropertyCategory.Other),
            ("_ShadowColorTex", TexturePropertyCategory.Other),
            ("_ShadowBorderMask", TexturePropertyCategory.Other),
            ("_ShadowBlurMask", TexturePropertyCategory.Other),
            ("_Shadow2ndColorTex", TexturePropertyCategory.Other),
            ("_Shadow3rdColorTex", TexturePropertyCategory.Other),
            ("_Ramp", TexturePropertyCategory.Other),
            ("_RimColorTex", TexturePropertyCategory.Other),
            ("_RimShadeMask", TexturePropertyCategory.Other),
            ("_GlitterColorTex", TexturePropertyCategory.Other),
            ("_GlitterShapeTex", TexturePropertyCategory.Other),
            ("_EmissionBlendMask", TexturePropertyCategory.Other),
            ("_EmissionGradTex", TexturePropertyCategory.Emission),
            ("_Emission2ndMap", TexturePropertyCategory.Emission),
            ("_Emission2ndBlendMask", TexturePropertyCategory.Other),
            ("_Emission2ndGradTex", TexturePropertyCategory.Emission),
            ("_Bump2ndMap", TexturePropertyCategory.Normal),
            ("_Bump2ndScaleMask", TexturePropertyCategory.Other),
            ("_AnisotropyTangentMap", TexturePropertyCategory.Normal),
            ("_AnisotropyScaleMask", TexturePropertyCategory.Other),
            ("_AnisotropyShiftNoiseMask", TexturePropertyCategory.Other),
            ("_BacklightColorTex", TexturePropertyCategory.Other),
            ("_MatCapTex", TexturePropertyCategory.Other),
            ("_MatCapBlendMask", TexturePropertyCategory.Other),
            ("_MatCapBumpMap", TexturePropertyCategory.Normal),
            ("_MatCap2ndTex", TexturePropertyCategory.Other),
            ("_MatCap2ndBlendMask", TexturePropertyCategory.Other),
            ("_MatCap2ndBumpMap", TexturePropertyCategory.Normal),
            ("_OutlineTex", TexturePropertyCategory.Other),
            ("_OutlineWidthMask", TexturePropertyCategory.Other),
            ("_OutlineVectorTex", TexturePropertyCategory.Normal),
            ("_FurNoiseMask", TexturePropertyCategory.Other),
            ("_FurMask", TexturePropertyCategory.Other),
            ("_FurLengthMask", TexturePropertyCategory.Other),
            ("_FurVectorTex", TexturePropertyCategory.Normal),
            ("_AudioLinkMask", TexturePropertyCategory.Other),
            ("_AudioLinkLocalMap", TexturePropertyCategory.Other),
            ("_DissolveMask", TexturePropertyCategory.Other),
            ("_DissolveNoiseMask", TexturePropertyCategory.Other),
            ("_TriMask", TexturePropertyCategory.Other),
            ("_SmoothnessTex", TexturePropertyCategory.Other),
            ("_ReflectionColorTex", TexturePropertyCategory.Other),
            ("_MainGradationTex", TexturePropertyCategory.Other),
        };

        /// <summary>
        /// Poiyomi Toon Shader texture properties.
        /// </summary>
        private static readonly (
            string Name,
            TexturePropertyCategory Category
        )[] PoiyomiPropertyDefinitions =
        {
            ("_MainTex", TexturePropertyCategory.Main),
            ("_BumpMap", TexturePropertyCategory.Normal),
            ("_DetailNormalMap", TexturePropertyCategory.Normal),
            ("_DetailMask", TexturePropertyCategory.Other),
            ("_EmissionMap", TexturePropertyCategory.Emission),
            ("_MetallicGlossMap", TexturePropertyCategory.Other),
            ("_AlphaMask", TexturePropertyCategory.Other),
            ("_AlphaTexture", TexturePropertyCategory.Other),
            ("_BlueTexture", TexturePropertyCategory.Other),
            ("_GreenTexture", TexturePropertyCategory.Other),
            ("_RedTexture", TexturePropertyCategory.Other),
            ("_RedTexure", TexturePropertyCategory.Other), // typo variant in v7.3
            ("_BackFaceTexture", TexturePropertyCategory.Other),
            ("_BackFaceMask", TexturePropertyCategory.Other),
            ("_DetailTex", TexturePropertyCategory.Other),
            ("_MirrorTexture", TexturePropertyCategory.Other),
            ("_MainColorAdjustTexture", TexturePropertyCategory.Other),
            ("_MainFadeTexture", TexturePropertyCategory.Other),
            ("_ClippingMask", TexturePropertyCategory.Other),
            ("_DecalTexture", TexturePropertyCategory.Other),
            ("_DecalTexture1", TexturePropertyCategory.Other),
            ("_DecalTexture2", TexturePropertyCategory.Other),
            ("_DecalTexture3", TexturePropertyCategory.Other),
            ("_DecalMask", TexturePropertyCategory.Other),
            ("_ALDecalColorMask", TexturePropertyCategory.Other),
            ("_GlobalMaskTexture0", TexturePropertyCategory.Other),
            ("_GlobalMaskTexture1", TexturePropertyCategory.Other),
            ("_GlobalMaskTexture2", TexturePropertyCategory.Other),
            ("_GlobalMaskTexture3", TexturePropertyCategory.Other),
            ("_RGBMask", TexturePropertyCategory.Other),
            ("_PPMask", TexturePropertyCategory.Other),
            ("_DepthMask", TexturePropertyCategory.Other),
            ("_DepthBulgeMask", TexturePropertyCategory.Other),
            ("_LookAtMask", TexturePropertyCategory.Other),
            ("_1st_ShadeMap", TexturePropertyCategory.Other),
            ("_2nd_ShadeMap", TexturePropertyCategory.Other),
            ("_ShadowColorTex", TexturePropertyCategory.Other),
            ("_Shadow2ndColorTex", TexturePropertyCategory.Other),
            ("_Shadow3rdColorTex", TexturePropertyCategory.Other),
            ("_ShadowStrengthMask", TexturePropertyCategory.Other),
            ("_ShadowBorderMask", TexturePropertyCategory.Other),
            ("_MultilayerMathBlurMap", TexturePropertyCategory.Other),
            ("_LightingAOMaps", TexturePropertyCategory.Other),
            ("_LightingAOTex", TexturePropertyCategory.Other),
            ("_LightingDetailShadowMaps", TexturePropertyCategory.Other),
            ("_LightingDetailShadows", TexturePropertyCategory.Other),
            ("_LightingShadowMask", TexturePropertyCategory.Other),
            ("_LightingShadowMasks", TexturePropertyCategory.Other),
            ("_MochieMetallicMaps", TexturePropertyCategory.Other),
            ("_MetallicMask", TexturePropertyCategory.Other),
            ("_MetallicTintMap", TexturePropertyCategory.Other),
            ("_SmoothnessTex", TexturePropertyCategory.Other),
            ("_SmoothnessMask", TexturePropertyCategory.Other),
            ("_RGBAMetallicMaps", TexturePropertyCategory.Other),
            ("_RGBASmoothnessMaps", TexturePropertyCategory.Other),
            ("_HeightMap", TexturePropertyCategory.Other),
            ("_Heightmask", TexturePropertyCategory.Other),
            ("_BRDFMetallicGlossMap", TexturePropertyCategory.Other),
            ("_BRDFMetallicMap", TexturePropertyCategory.Other),
            ("_BRDFSpecularMap", TexturePropertyCategory.Other),
            ("_ClearCoatMaps", TexturePropertyCategory.Other),
            ("_ClearcoatMap", TexturePropertyCategory.Other),
            ("_ClothMetallicSmoothnessMap", TexturePropertyCategory.Other),
            ("_SSSThicknessMap", TexturePropertyCategory.Other),
            ("_SkinThicknessMap", TexturePropertyCategory.Other),
            ("_SpecularMap", TexturePropertyCategory.Other),
            ("_SpecularMap1", TexturePropertyCategory.Other),
            ("_SpecularMask", TexturePropertyCategory.Other),
            ("_SpecularMask1", TexturePropertyCategory.Other),
            ("_SpecularMetallicMap", TexturePropertyCategory.Other),
            ("_SpecularMetallicMap1", TexturePropertyCategory.Other),
            ("_HighColor_Tex", TexturePropertyCategory.Other),
            ("_Set_HighColorMask", TexturePropertyCategory.Other),
            ("_AnisoColorMap", TexturePropertyCategory.Other),
            ("_AnisoNoiseMap", TexturePropertyCategory.Other),
            ("_AnisoTangentMap", TexturePropertyCategory.Other),
            ("_AnisoTangentMap1", TexturePropertyCategory.Other),
            ("_AnisotropyMap", TexturePropertyCategory.Other),
            ("_SpecularAnisoJitterMacro", TexturePropertyCategory.Other),
            ("_SpecularAnisoJitterMacro1", TexturePropertyCategory.Other),
            ("_SpecularAnisoJitterMicro", TexturePropertyCategory.Other),
            ("_SpecularAnisoJitterMicro1", TexturePropertyCategory.Other),
            ("_Matcap", TexturePropertyCategory.Other),
            ("_Matcap2", TexturePropertyCategory.Other),
            ("_Matcap3", TexturePropertyCategory.Other),
            ("_Matcap4", TexturePropertyCategory.Other),
            ("_MatcapMask", TexturePropertyCategory.Other),
            ("_Matcap2Mask", TexturePropertyCategory.Other),
            ("_Matcap3Mask", TexturePropertyCategory.Other),
            ("_Matcap4Mask", TexturePropertyCategory.Other),
            ("_Matcap0NormalMap", TexturePropertyCategory.Normal),
            ("_Matcap1NormalMap", TexturePropertyCategory.Normal),
            ("_Matcap2NormalMap", TexturePropertyCategory.Normal),
            ("_Matcap3NormalMap", TexturePropertyCategory.Normal),
            ("_CubeMapMask", TexturePropertyCategory.Other),
            ("_ReflectionColorTex", TexturePropertyCategory.Other),
            ("_RimTex", TexturePropertyCategory.Other),
            ("_RimMask", TexturePropertyCategory.Other),
            ("_Rim2Tex", TexturePropertyCategory.Other),
            ("_Rim2Mask", TexturePropertyCategory.Other),
            ("_RimColorTex", TexturePropertyCategory.Other),
            ("_Rim2ColorTex", TexturePropertyCategory.Other),
            ("_RimEnviroMask", TexturePropertyCategory.Other),
            ("_RimWidthNoiseTexture", TexturePropertyCategory.Other),
            ("_Set_RimLightMask", TexturePropertyCategory.Other),
            ("_Set_Rim2LightMask", TexturePropertyCategory.Other),
            ("_RgbNormalR", TexturePropertyCategory.Normal),
            ("_RgbNormalG", TexturePropertyCategory.Normal),
            ("_RgbNormalB", TexturePropertyCategory.Normal),
            ("_RgbNormalA", TexturePropertyCategory.Normal),
            ("_BacklightColorTex", TexturePropertyCategory.Other),
            ("_FurMask", TexturePropertyCategory.Other),
            ("_FurLengthMask", TexturePropertyCategory.Other),
            ("_FurNoiseMask", TexturePropertyCategory.Other),
            ("_FurVectorTex", TexturePropertyCategory.Normal),
            ("_IridescenceRamp", TexturePropertyCategory.Other),
            ("_IridescenceMask", TexturePropertyCategory.Other),
            ("_IridescenceNormalMap", TexturePropertyCategory.Normal),
            ("_EmissionMap1", TexturePropertyCategory.Emission),
            ("_EmissionMap2", TexturePropertyCategory.Emission),
            ("_EmissionMap3", TexturePropertyCategory.Emission),
            ("_EmissionMask", TexturePropertyCategory.Other),
            ("_EmissionMask1", TexturePropertyCategory.Other),
            ("_EmissionMask2", TexturePropertyCategory.Other),
            ("_EmissionMask3", TexturePropertyCategory.Other),
            ("_EmissionScrollingCurve", TexturePropertyCategory.Other),
            ("_EmissionScrollingCurve1", TexturePropertyCategory.Other),
            ("_EmissionScrollingCurve2", TexturePropertyCategory.Other),
            ("_EmissionScrollingCurve3", TexturePropertyCategory.Other),
            ("_DissolveToTexture", TexturePropertyCategory.Other),
            ("_DissolveNoiseTexture", TexturePropertyCategory.Other),
            ("_DissolveDetailNoise", TexturePropertyCategory.Other),
            ("_DissolveEdgeGradient", TexturePropertyCategory.Other),
            ("_DissolveMask", TexturePropertyCategory.Other),
            ("_FlipbookMask", TexturePropertyCategory.Other),
            ("_GlitterMask", TexturePropertyCategory.Other),
            ("_GlitterTexture", TexturePropertyCategory.Other),
            ("_GlitterColorMap", TexturePropertyCategory.Other),
            ("_OutlineTexture", TexturePropertyCategory.Other),
            ("_OutlineMask", TexturePropertyCategory.Other),
            ("_PanosphereTexture", TexturePropertyCategory.Other),
            ("_PanoMask", TexturePropertyCategory.Other),
            ("_ParallaxHeightMap", TexturePropertyCategory.Other),
            ("_ParallaxHeightMapMask", TexturePropertyCategory.Other),
            ("_ParallaxInternalMap", TexturePropertyCategory.Other),
            ("_ParallaxInternalMapMask", TexturePropertyCategory.Other),
            ("_PathingColorMap", TexturePropertyCategory.Other),
            ("_PathingMap", TexturePropertyCategory.Other),
            ("_DistortionFlowTexture", TexturePropertyCategory.Other),
            ("_DistortionFlowTexture1", TexturePropertyCategory.Other),
            ("_DistortionMask", TexturePropertyCategory.Other),
            ("_DepthTexture", TexturePropertyCategory.Other),
            ("_GrabPassBlendMap", TexturePropertyCategory.Other),
            ("_VideoMaskTexture", TexturePropertyCategory.Other),
            ("_VideoPixelTexture", TexturePropertyCategory.Other),
            ("_VoronoiNoise", TexturePropertyCategory.Other),
            ("_VoronoiMask", TexturePropertyCategory.Other),
            ("_TruchetTex", TexturePropertyCategory.Other),
            ("_TruchetMask", TexturePropertyCategory.Other),
            ("_VertexBasicsMask", TexturePropertyCategory.Other),
            ("_VertexGlitchMap", TexturePropertyCategory.Other),
            ("_VertexManipulationHeightMask", TexturePropertyCategory.Other),
            ("_UzumoreMask", TexturePropertyCategory.Other),
            ("_ClothDFG", TexturePropertyCategory.Other),
            ("_LightDataSDFMap", TexturePropertyCategory.Other),
            ("_MainGradationTex", TexturePropertyCategory.Other),
            ("_SDFShadingTexture", TexturePropertyCategory.Other),
            ("_SkinLUT", TexturePropertyCategory.Other),
            ("_TextGlyphs", TexturePropertyCategory.Other),
            ("_ToonRamp", TexturePropertyCategory.Other),
            ("_Udon_VideoTex", TexturePropertyCategory.Other),
            ("_VideoGameboyRamp", TexturePropertyCategory.Other),
        };

        /// <summary>
        /// UTS2 (UnityChanToonShader) texture properties.
        /// </summary>
        private static readonly (
            string Name,
            TexturePropertyCategory Category
        )[] UtsPropertyDefinitions =
        {
            ("_MainTex", TexturePropertyCategory.Main),
            ("_BaseMap", TexturePropertyCategory.Main),
            ("_NormalMap", TexturePropertyCategory.Normal),
            ("_1st_ShadeMap", TexturePropertyCategory.Other),
            ("_2nd_ShadeMap", TexturePropertyCategory.Other),
            ("_ClippingMask", TexturePropertyCategory.Other),
            ("_HighColor_Tex", TexturePropertyCategory.Other),
            ("_MatCap_Sampler", TexturePropertyCategory.Other),
            ("_NormalMapForMatCap", TexturePropertyCategory.Normal),
            ("_Set_RimLightMask", TexturePropertyCategory.Other),
            ("_Set_MatcapMask", TexturePropertyCategory.Other),
            ("_OutlineTex", TexturePropertyCategory.Other),
            ("_Outline_Sampler", TexturePropertyCategory.Other),
            ("_Set_HighColorMask", TexturePropertyCategory.Other),
            ("_Set_1st_ShadePosition", TexturePropertyCategory.Other),
            ("_Set_2nd_ShadePosition", TexturePropertyCategory.Other),
            ("_ShadingGradeMap", TexturePropertyCategory.Other),
            ("_Emissive_Tex", TexturePropertyCategory.Emission),
            ("_BakedNormal", TexturePropertyCategory.Normal),
            ("_AngelRing_Sampler", TexturePropertyCategory.Other),
        };

        /// <summary>
        /// Maps each known property name to its category.
        /// Built from per-shader definitions; the first shader to register a property
        /// determines its category (Unity → lilToon → Poiyomi → UTS).
        /// </summary>
        private static readonly Dictionary<string, TexturePropertyCategory> CategoryMap;

        static TexturePropertyDefinitions()
        {
            CategoryMap = new Dictionary<string, TexturePropertyCategory>();
            RegisterProperties(UnityPropertyDefinitions);
            RegisterProperties(LilToonPropertyDefinitions);
            RegisterProperties(PoiyomiPropertyDefinitions);
            RegisterProperties(UtsPropertyDefinitions);
        }

        private static void RegisterProperties(
            (string Name, TexturePropertyCategory Category)[] definitions
        )
        {
            foreach (var (name, category) in definitions)
            {
                if (!CategoryMap.ContainsKey(name))
                    CategoryMap[name] = category;
            }
        }

        /// <summary>
        /// Read-only access to all known texture property names.
        /// </summary>
        public static IReadOnlyCollection<string> TextureProperties => CategoryMap.Keys;

        /// <summary>
        /// Returns true if the given property name is a known, compressible texture property.
        /// </summary>
        public static bool IsKnownTextureProperty(string propertyName)
        {
            return propertyName != null && CategoryMap.ContainsKey(propertyName);
        }

        /// <summary>
        /// Returns the category for the given texture property name.
        /// Properties not in the known set return Other.
        /// </summary>
        public static TexturePropertyCategory GetCategory(string propertyName)
        {
            if (propertyName != null && CategoryMap.TryGetValue(propertyName, out var category))
                return category;
            return TexturePropertyCategory.Other;
        }
    }
}
