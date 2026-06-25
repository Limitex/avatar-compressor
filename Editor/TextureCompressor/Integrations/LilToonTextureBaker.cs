using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using nadena.dev.ndmf;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.integrations
{
    /// <summary>
    /// Bakes lilToon's texture adjustments — main-color adjustments (HSVG, gradation map, active
    /// 2nd/3rd layers), the alpha mask, and outline tone correction — into their textures,
    /// replicating what lilToon's inspector bake buttons produce, through lilToon's public
    /// <c>lilToonInspector.RunBake</c> and its baker shader (<c>Hidden/ltsother_baker</c>)
    /// accessed via reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// lilToon is an optional, external package and is not a declared dependency of this project,
    /// so it cannot be referenced at compile time. Reflection keeps the build green whether or not
    /// lilToon is installed: when its types are absent, <see cref="IsAvailable"/> is false and
    /// every call is a no-op. Unlike unused-slot removal, lilToon exposes no non-interactive entry
    /// point for baking (its <c>AutoBake*</c> methods are private and show modal dialogs), so the
    /// property mapping below is a port of <c>lilEditorTextureBaker</c> — the pixel math itself
    /// still runs through lilToon's own shader and <c>RunBake</c>, but the mapping is ours and
    /// must be re-checked against lilToon when it updates.
    /// </para>
    /// <para>
    /// The catalog covers every lilToon inspector bake that mutates the material itself: the main
    /// bake button (<c>AutoBakeMainTexture</c>), the alpha-mask bake button
    /// (<c>AutoBakeAlphaMask</c>), and the outline bake button (<c>AutoBakeOutlineTexture</c>).
    /// The remaining <c>AutoBake*</c> operations are deliberately excluded: the shadow, matcap
    /// and trimask bakes exist only for the convert-to-lite flow — lilToon never assigns their
    /// output to a standard-shader material, and doing so here would alter rendering through
    /// paths lilToon itself never exercises (matcap baking would additionally clamp HDR colors
    /// to 8-bit) — and the colored-mask "②" buttons only generate PNG files without touching the
    /// material, so there is no material mutation, and no download-size benefit, to replicate.
    /// </para>
    /// <para>
    /// Two deliberate deviations from lilToon's inspector bakes. First, the main color
    /// (<c>_Color</c>) is never baked and is left as a runtime tint — the same choice lilToon
    /// itself makes when converting to the lite shader — because color-change animations are
    /// common and a baked color would freeze them. Second, source textures are sampled directly
    /// from the material on the GPU instead of re-loading the raw image files the way
    /// <c>lilTextureUtils.LoadTexture</c> does: the build must not mutate or destroy loaded
    /// assets, and the quality difference is washed out because the baked result is recompressed
    /// by the pipeline afterwards.
    /// </para>
    /// <para>
    /// A bake is skipped entirely when any of its input properties is driven by animation (see
    /// the <c>HasAnimated*BakeInput</c> methods): baking freezes the current value into pixels,
    /// which would break the animation. Partial baking (e.g. HSVG only while a layer toggle is
    /// animated) is deliberately not attempted. Identical adjustments on the same source texture
    /// share one baked texture per build instance.
    /// </para>
    /// </remarks>
    public sealed class LilToonTextureBaker : ILilToonBaker
    {
        private const string InspectorTypeName = "lilToon.lilToonInspector";
        private const string RunBakeMethodName = "RunBake";
        private const string MaterialUtilsTypeName = "lilToon.lilMaterialUtils";
        private const string CheckShaderMethodName = "CheckShaderIslilToon";
        private const string ShaderManagerTypeName = "lilToon.lilShaderManager";
        private const string BakerShaderFieldName = "ltsbaker";
        private const string BakerShaderName = "Hidden/ltsother_baker";

        private const string MainTexProperty = "_MainTex";
        private const string MainColorProperty = "_Color";
        private const string MainTexHsvgProperty = "_MainTexHSVG";
        private const string MainGradationStrengthProperty = "_MainGradationStrength";
        private const string MainGradationTexProperty = "_MainGradationTex";
        private const string MainColorAdjustMaskProperty = "_MainColorAdjustMask";

        private const string AlphaMaskModeProperty = "_AlphaMaskMode";
        private const string AlphaMaskProperty = "_AlphaMask";
        private const string AlphaMaskScaleProperty = "_AlphaMaskScale";
        private const string AlphaMaskValueProperty = "_AlphaMaskValue";

        // Baker-shader keyword selecting its alpha-mask pass, mirroring AutoBakeAlphaMask.
        private const string AlphaMaskBakerKeyword = "_ALPHAMASK";

        private const string OutlineTexProperty = "_OutlineTex";
        private const string OutlineTexHsvgProperty = "_OutlineTexHSVG";

        private const string Layer2nd = "2nd";
        private const string Layer3rd = "3rd";

        /// <summary>lilToon's <c>lilConstants.defaultHSVG</c> — the "no adjustment" value.</summary>
        public static readonly Vector4 DefaultHsvg = new Vector4(0f, 1f, 1f, 1f);

        // Inputs of the main-texture bake that are read regardless of layer state. The layer
        // toggles are listed here (not per-layer) so an animated toggle vetoes the whole bake even
        // while the layer is currently off — baking must never change what an animation can show.
        private static readonly string[] MainBakeInputProperties =
        {
            MainTexProperty,
            MainTexHsvgProperty,
            MainGradationStrengthProperty,
            MainGradationTexProperty,
            MainColorAdjustMaskProperty,
            "_UseMain2ndTex",
            "_UseMain3rdTex",
        };

        // Per-layer scalar parameters consumed by the bake, mirroring the SetFloat list in
        // lilToon's AutoBakeMainTexture ({0} is the layer, "2nd" or "3rd"). This single catalog
        // drives the baker-material setup, the animation veto, and the dedup key, so a lilToon
        // update only needs to be reflected in one place.
        private static readonly string[] LayerFloatPropertyFormats =
        {
            "_Main{0}TexAngle",
            "_Main{0}TexIsDecal",
            "_Main{0}TexIsLeftOnly",
            "_Main{0}TexIsRightOnly",
            "_Main{0}TexShouldCopy",
            "_Main{0}TexShouldFlipMirror",
            "_Main{0}TexShouldFlipCopy",
            "_Main{0}TexIsMSDF",
            "_Main{0}TexBlendMode",
            "_Main{0}TexAlphaMode",
        };

        private static readonly string[] LayerVectorPropertyFormats =
        {
            "_Main{0}TexDecalAnimation",
            "_Main{0}TexDecalSubParam",
        };

        // Inputs of the alpha-mask bake, mirroring AutoBakeAlphaMask's reads.
        private static readonly string[] AlphaMaskBakeInputProperties =
        {
            MainTexProperty,
            AlphaMaskProperty,
            AlphaMaskModeProperty,
            AlphaMaskScaleProperty,
            AlphaMaskValueProperty,
        };

        // Inputs of the outline bake, mirroring AutoBakeOutlineTexture's reads.
        private static readonly string[] OutlineBakeInputProperties =
        {
            OutlineTexProperty,
            OutlineTexHsvgProperty,
        };

        // Cleared on the first invocation failure so a broken lilToon API disables the feature
        // for the rest of the build instead of throwing or spamming warnings.
        private MethodInfo _runBake;
        private readonly MethodInfo _checkShaderIsLilToon;
        private readonly Shader _bakerShader;

        // One shared baked texture per (source texture, bake operation + adjustment values), per
        // build instance, so materials applying the same adjustment to the same texture (e.g. an
        // outfit split into several materials over one atlas) do not multiply the upload.
        private readonly Dictionary<(Texture2D Source, string Key), Texture2D> _bakeCache =
            new Dictionary<(Texture2D, string), Texture2D>();

        public LilToonTextureBaker()
        {
            _runBake = ResolveRunBake();
            _checkShaderIsLilToon = ResolveCheckShaderIsLilToon();
            _bakerShader = _runBake != null ? ResolveBakerShader() : null;
        }

        public bool IsAvailable =>
            _runBake != null && _checkShaderIsLilToon != null && _bakerShader != null;

        public LilToonBakeResult Bake(
            Material material,
            AnimationUsageMap animationUsageMap,
            Func<Texture2D, string, bool> canReplaceTexture
        )
        {
            if (
                !IsAvailable
                || material == null
                || material.shader == null
                || animationUsageMap == null
            )
                return default;

            int bakedSlots = 0;
            int skippedByAnimation = 0;

            try
            {
                if (!IsLilToonMaterial(material))
                    return default;

                // Catalog order matters: the alpha-mask bake reads the (possibly just baked)
                // main texture, producing the same result as pressing lilToon's bake buttons
                // in that order. The outline bake is independent of both.
                BakeMainTexture(
                    material,
                    animationUsageMap,
                    canReplaceTexture,
                    ref bakedSlots,
                    ref skippedByAnimation
                );
                BakeAlphaMask(
                    material,
                    animationUsageMap,
                    canReplaceTexture,
                    ref bakedSlots,
                    ref skippedByAnimation
                );
                BakeOutlineTexture(
                    material,
                    animationUsageMap,
                    canReplaceTexture,
                    ref bakedSlots,
                    ref skippedByAnimation
                );
            }
            catch (Exception ex)
            {
                _runBake = null; // disable for the rest of this build
                Debug.LogWarning(
                    "[LAC Texture Compressor] lilToon texture baking failed: "
                        + $"{(ex.InnerException ?? ex).Message}. Baking is disabled for the rest "
                        + "of this build."
                );
            }

            return new LilToonBakeResult(bakedSlots, skippedByAnimation);
        }

        // Main texture bake (port of AutoBakeMainTexture)

        private void BakeMainTexture(
            Material material,
            AnimationUsageMap animationUsageMap,
            Func<Texture2D, string, bool> canReplaceTexture,
            ref int bakedSlots,
            ref int skippedByAnimation
        )
        {
            if (!HasBakeableColorAdjustments(material))
                return;

            // Mirrors lilToon's cannotBake1st: without a main texture there is nothing to bake
            // into, and generating one from scratch would only inflate the avatar.
            var mainTexture = GetTexture(material, MainTexProperty) as Texture2D;
            if (mainTexture == null)
                return;

            if (HasAnimatedMainBakeInput(material, animationUsageMap))
            {
                skippedByAnimation++;
                return;
            }

            if (canReplaceTexture != null && !canReplaceTexture(mainTexture, MainTexProperty))
                return;

            bool bake2nd = IsOverlayLayerEnabled(material, Layer2nd);
            bool bake3rd = IsOverlayLayerEnabled(material, Layer3rd);

            var baked = BakeTexture(
                mainTexture,
                BuildMainBakeKey(material, bake2nd, bake3rd),
                baker => ConfigureMainBaker(material, baker, mainTexture, bake2nd, bake3rd)
            );
            if (baked == null)
                return;

            ApplyBakedMainTexture(material, baked, bake2nd, bake3rd);
            bakedSlots++;
        }

        /// <summary>
        /// True if the material carries main-color adjustments the bake would consume: a
        /// non-default HSVG, an active gradation, or an enabled 2nd/3rd layer. Mirrors the
        /// inverse of lilToon's <c>shouldNotBakeAll</c>, minus the main color term (never baked
        /// here). False when the shader does not declare the adjustment properties at all
        /// (e.g. lilToon Lite, which has no color adjustment to bake).
        /// </summary>
        public static bool HasBakeableColorAdjustments(Material material)
        {
            if (material == null || !material.HasProperty(MainTexHsvgProperty))
                return false;

            return material.GetVector(MainTexHsvgProperty) != DefaultHsvg
                || GetFloat(material, MainGradationStrengthProperty, 0f) != 0f
                || IsOverlayLayerEnabled(material, Layer2nd)
                || IsOverlayLayerEnabled(material, Layer3rd);
        }

        /// <summary>
        /// True if any input property of the main-texture bake is driven by animation, in which
        /// case the bake must be skipped entirely. Layer parameters only count while their layer
        /// is enabled (a disabled, non-animated layer can never become visible), but the layer
        /// toggles themselves always count.
        /// </summary>
        public static bool HasAnimatedMainBakeInput(
            Material material,
            AnimationUsageMap animationUsageMap
        )
        {
            if (material == null || animationUsageMap == null)
                return false;

            return AnyAnimated(animationUsageMap, MainBakeInputProperties)
                || HasAnimatedOverlayLayerInput(material, animationUsageMap, Layer2nd)
                || HasAnimatedOverlayLayerInput(material, animationUsageMap, Layer3rd);
        }

        private static bool HasAnimatedOverlayLayerInput(
            Material material,
            AnimationUsageMap animationUsageMap,
            string layer
        )
        {
            if (!IsOverlayLayerEnabled(material, layer))
                return false;

            if (
                animationUsageMap.IsMaterialPropertyAnimated(LayerColorProperty(layer))
                || animationUsageMap.IsMaterialPropertyAnimated(LayerTexProperty(layer))
                || animationUsageMap.IsMaterialPropertyAnimated(LayerBlendMaskProperty(layer))
            )
                return true;

            foreach (var format in LayerFloatPropertyFormats)
            {
                if (animationUsageMap.IsMaterialPropertyAnimated(string.Format(format, layer)))
                    return true;
            }

            foreach (var format in LayerVectorPropertyFormats)
            {
                if (animationUsageMap.IsMaterialPropertyAnimated(string.Format(format, layer)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sets up the baker material exactly like lilToon's <c>AutoBakeMainTexture</c>, except
        /// textures are the material's own (sampled on the GPU by the bake blit) and the main
        /// color is pinned to white so it stays a runtime tint.
        /// </summary>
        private static void ConfigureMainBaker(
            Material material,
            Material baker,
            Texture2D mainTexture,
            bool bake2nd,
            bool bake3rd
        )
        {
            baker.SetColor(MainColorProperty, Color.white);
            baker.SetVector(MainTexHsvgProperty, material.GetVector(MainTexHsvgProperty));
            baker.SetFloat(
                MainGradationStrengthProperty,
                GetFloat(material, MainGradationStrengthProperty, 0f)
            );
            baker.SetTexture(
                MainGradationTexProperty,
                GetTexture(material, MainGradationTexProperty)
            );
            baker.SetTexture(
                MainColorAdjustMaskProperty,
                GetTexture(material, MainColorAdjustMaskProperty)
            );
            baker.SetTexture(MainTexProperty, mainTexture);

            if (bake2nd)
                ConfigureOverlayLayer(material, baker, Layer2nd);
            if (bake3rd)
                ConfigureOverlayLayer(material, baker, Layer3rd);
        }

        private static void ConfigureOverlayLayer(Material material, Material baker, string layer)
        {
            baker.SetFloat(
                UseLayerProperty(layer),
                GetFloat(material, UseLayerProperty(layer), 1f)
            );
            baker.SetColor(
                LayerColorProperty(layer),
                GetColor(material, LayerColorProperty(layer), Color.white)
            );

            foreach (var format in LayerFloatPropertyFormats)
            {
                string name = string.Format(format, layer);
                baker.SetFloat(name, GetFloat(material, name, 0f));
            }

            foreach (var format in LayerVectorPropertyFormats)
            {
                string name = string.Format(format, layer);
                baker.SetVector(name, GetVector(material, name, Vector4.zero));
            }

            CopyLayerTexture(material, baker, LayerTexProperty(layer));
            CopyLayerTexture(material, baker, LayerBlendMaskProperty(layer));
        }

        private static void CopyLayerTexture(Material material, Material baker, string name)
        {
            if (!material.HasProperty(name))
            {
                baker.SetTexture(name, Texture2D.whiteTexture);
                return;
            }

            var texture = material.GetTexture(name);
            // White means "no contribution change", matching lilToon's fallback for empty slots.
            baker.SetTexture(name, texture != null ? texture : Texture2D.whiteTexture);
            baker.SetTextureScale(name, material.GetTextureScale(name));
            baker.SetTextureOffset(name, material.GetTextureOffset(name));
        }

        /// <summary>
        /// Restores the material to the post-bake state lilToon's own bake leaves behind, except
        /// <c>_Color</c> (never baked, so never reset). Inputs fully consumed by the bake are
        /// cleared even where lilToon leaves them assigned (color-adjust mask, layer blend masks):
        /// with HSVG back at default and the layer toggles off they can never affect rendering
        /// again, and clearing them lets the textures drop out of the upload.
        /// </summary>
        private static void ApplyBakedMainTexture(
            Material material,
            Texture2D baked,
            bool bake2nd,
            bool bake3rd
        )
        {
            material.SetTexture(MainTexProperty, baked);
            material.SetVector(MainTexHsvgProperty, DefaultHsvg);
            material.SetFloat(MainGradationStrengthProperty, 0f);
            material.SetTexture(MainGradationTexProperty, null);
            material.SetTexture(MainColorAdjustMaskProperty, null);

            if (bake2nd)
                ClearOverlayLayer(material, Layer2nd);
            if (bake3rd)
                ClearOverlayLayer(material, Layer3rd);
        }

        private static void ClearOverlayLayer(Material material, string layer)
        {
            material.SetFloat(UseLayerProperty(layer), 0f);
            material.SetTexture(LayerTexProperty(layer), null);
            material.SetTexture(LayerBlendMaskProperty(layer), null);
        }

        private static string BuildMainBakeKey(Material material, bool bake2nd, bool bake3rd)
        {
            var key = new StringBuilder("main");
            key.Append('|').Append(material.GetVector(MainTexHsvgProperty).ToString("R"));
            key.Append('|')
                .Append(GetFloat(material, MainGradationStrengthProperty, 0f).ToString("R"));
            AppendTextureKey(key, material, MainGradationTexProperty);
            AppendTextureKey(key, material, MainColorAdjustMaskProperty);

            if (bake2nd)
                AppendOverlayLayerKey(key, material, Layer2nd);
            if (bake3rd)
                AppendOverlayLayerKey(key, material, Layer3rd);

            return key.ToString();
        }

        private static void AppendOverlayLayerKey(
            StringBuilder key,
            Material material,
            string layer
        )
        {
            key.Append('|').Append(layer);
            key.Append('|')
                .Append(GetColor(material, LayerColorProperty(layer), Color.white).ToString("R"));

            foreach (var format in LayerFloatPropertyFormats)
            {
                key.Append('|')
                    .Append(GetFloat(material, string.Format(format, layer), 0f).ToString("R"));
            }

            foreach (var format in LayerVectorPropertyFormats)
            {
                key.Append('|')
                    .Append(
                        GetVector(material, string.Format(format, layer), Vector4.zero)
                            .ToString("R")
                    );
            }

            AppendTextureKey(key, material, LayerTexProperty(layer));
            AppendTextureKey(key, material, LayerBlendMaskProperty(layer));
        }

        // Alpha mask bake (port of AutoBakeAlphaMask + AlphamaskToTextureGUI)

        /// <summary>
        /// Merges the alpha mask into the main texture's alpha channel and disables the mask,
        /// mirroring lilToon's "bake alphamask" button. Runs after the main bake so it reads the
        /// (possibly just baked) main texture. Dropping the now-consumed mask texture is the
        /// size win this bake exists for.
        /// </summary>
        private void BakeAlphaMask(
            Material material,
            AnimationUsageMap animationUsageMap,
            Func<Texture2D, string, bool> canReplaceTexture,
            ref int bakedSlots,
            ref int skippedByAnimation
        )
        {
            if (!HasBakeableAlphaMask(material))
                return;

            var mainTexture = GetTexture(material, MainTexProperty) as Texture2D;
            if (mainTexture == null)
                return;

            if (HasAnimatedAlphaMaskBakeInput(animationUsageMap))
            {
                skippedByAnimation++;
                return;
            }

            if (canReplaceTexture != null && !canReplaceTexture(mainTexture, MainTexProperty))
                return;

            var baked = BakeTexture(
                mainTexture,
                BuildAlphaMaskBakeKey(material),
                baker =>
                {
                    baker.EnableKeyword(AlphaMaskBakerKeyword);
                    baker.SetColor(MainColorProperty, Color.white);
                    baker.SetVector(MainTexHsvgProperty, DefaultHsvg);
                    baker.SetFloat(
                        AlphaMaskModeProperty,
                        GetFloat(material, AlphaMaskModeProperty, 0f)
                    );
                    baker.SetFloat(
                        AlphaMaskScaleProperty,
                        GetFloat(material, AlphaMaskScaleProperty, 1f)
                    );
                    baker.SetFloat(
                        AlphaMaskValueProperty,
                        GetFloat(material, AlphaMaskValueProperty, 0f)
                    );
                    baker.SetTexture(MainTexProperty, mainTexture);
                    baker.SetTexture(AlphaMaskProperty, GetTexture(material, AlphaMaskProperty));
                }
            );
            if (baked == null)
                return;

            // Mirror lilToon's post-bake reset: the mask is fully consumed into the main alpha.
            material.SetTexture(MainTexProperty, baked);
            material.SetFloat(AlphaMaskModeProperty, 0f);
            material.SetTexture(AlphaMaskProperty, null);
            material.SetFloat(AlphaMaskValueProperty, 0f);
            bakedSlots++;
        }

        /// <summary>
        /// True if the material has an active alpha mask the bake would consume: mask mode on and
        /// a mask texture assigned. Skips masks with a non-default tiling/offset — the bake
        /// samples the mask without its ST (mirroring lilToon's bake button), so such a mask
        /// would bake differently from how it renders.
        /// </summary>
        public static bool HasBakeableAlphaMask(Material material)
        {
            if (
                material == null
                || !material.HasProperty(AlphaMaskModeProperty)
                || !material.HasProperty(AlphaMaskProperty)
            )
                return false;

            // Mode 0 = off: the runtime ignores the mask, so baking it would change the look.
            if (material.GetFloat(AlphaMaskModeProperty) == 0f)
                return false;

            if (material.GetTexture(AlphaMaskProperty) == null)
                return false;

            return material.GetTextureScale(AlphaMaskProperty) == Vector2.one
                && material.GetTextureOffset(AlphaMaskProperty) == Vector2.zero;
        }

        /// <summary>
        /// True if any input property of the alpha-mask bake is driven by animation.
        /// </summary>
        public static bool HasAnimatedAlphaMaskBakeInput(AnimationUsageMap animationUsageMap)
        {
            return AnyAnimated(animationUsageMap, AlphaMaskBakeInputProperties);
        }

        private static string BuildAlphaMaskBakeKey(Material material)
        {
            var key = new StringBuilder("alpha");
            key.Append('|').Append(GetFloat(material, AlphaMaskModeProperty, 0f).ToString("R"));
            key.Append('|').Append(GetFloat(material, AlphaMaskScaleProperty, 1f).ToString("R"));
            key.Append('|').Append(GetFloat(material, AlphaMaskValueProperty, 0f).ToString("R"));
            AppendTextureKey(key, material, AlphaMaskProperty);
            return key.ToString();
        }

        // Outline texture bake (port of AutoBakeOutlineTexture)

        /// <summary>
        /// Bakes the outline tone correction (<c>_OutlineTexHSVG</c>) into the outline texture
        /// and resets it, mirroring the "Bake" button in lilToon's outline advanced settings.
        /// </summary>
        private void BakeOutlineTexture(
            Material material,
            AnimationUsageMap animationUsageMap,
            Func<Texture2D, string, bool> canReplaceTexture,
            ref int bakedSlots,
            ref int skippedByAnimation
        )
        {
            if (!HasBakeableOutline(material))
                return;

            var outlineTexture = GetTexture(material, OutlineTexProperty) as Texture2D;
            if (outlineTexture == null)
                return;

            if (HasAnimatedOutlineBakeInput(animationUsageMap))
            {
                skippedByAnimation++;
                return;
            }

            if (canReplaceTexture != null && !canReplaceTexture(outlineTexture, OutlineTexProperty))
                return;

            Vector4 outlineHsvg = material.GetVector(OutlineTexHsvgProperty);
            var baked = BakeTexture(
                outlineTexture,
                "outline|" + outlineHsvg.ToString("R"),
                baker =>
                {
                    baker.SetColor(MainColorProperty, Color.white);
                    baker.SetVector(MainTexHsvgProperty, outlineHsvg);
                    baker.SetTexture(MainTexProperty, outlineTexture);
                }
            );
            if (baked == null)
                return;

            material.SetTexture(OutlineTexProperty, baked);
            material.SetVector(OutlineTexHsvgProperty, DefaultHsvg);
            bakedSlots++;
        }

        /// <summary>
        /// True if the material has outline tone correction the bake would consume: an outline
        /// texture with a non-default HSVG. Mirrors the inverse of lilToon's
        /// <c>shouldNotBakeOutline</c> (<c>_OutlineColor</c> stays a runtime tint, like
        /// <c>_Color</c> on the main bake).
        /// </summary>
        public static bool HasBakeableOutline(Material material)
        {
            return material != null
                && material.HasProperty(OutlineTexProperty)
                && material.HasProperty(OutlineTexHsvgProperty)
                && material.GetTexture(OutlineTexProperty) != null
                && material.GetVector(OutlineTexHsvgProperty) != DefaultHsvg;
        }

        /// <summary>
        /// True if any input property of the outline bake is driven by animation.
        /// </summary>
        public static bool HasAnimatedOutlineBakeInput(AnimationUsageMap animationUsageMap)
        {
            return AnyAnimated(animationUsageMap, OutlineBakeInputProperties);
        }

        // Shared bake plumbing

        private static bool AnyAnimated(AnimationUsageMap animationUsageMap, string[] properties)
        {
            if (animationUsageMap == null)
                return false;

            foreach (var property in properties)
            {
                if (animationUsageMap.IsMaterialPropertyAnimated(property))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Runs one bake through lilToon's <c>RunBake</c> with a freshly configured baker
        /// material, deduplicating identical bakes via the per-build cache.
        /// </summary>
        private Texture2D BakeTexture(Texture2D source, string key, Action<Material> configure)
        {
            if (_bakeCache.TryGetValue((source, key), out var cached) && cached != null)
                return cached;

            Texture2D baked;
            var baker = new Material(_bakerShader);
            try
            {
                configure(baker);
                baked = InvokeRunBake(source, baker);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baker);
            }

            if (baked == null)
                return null;

            baked.name = source.name + "_baked";

            // Register provenance once, on creation: materials reusing this bake via the cache
            // share the same replacement, and downstream collection resolves it back to the
            // original asset (so the user's exclusion/frozen settings keep applying to it).
            // Chained bakes (main then alpha mask) collapse to the same original in NDMF.
            ObjectRegistry.RegisterReplacedObject(source, baked);
            _bakeCache[(source, key)] = baked;
            return baked;
        }

        private Texture2D InvokeRunBake(Texture2D source, Material baker)
        {
            // RunBake(ref Texture2D outTexture, Texture2D srcTexture, Material material,
            //         Texture2D referenceTexture = null) — output is sized to srcTexture.
            var args = new object[] { null, source, baker, null };
            _runBake.Invoke(null, args);
            return args[0] as Texture2D;
        }

        private static void AppendTextureKey(StringBuilder key, Material material, string name)
        {
            var texture = GetTexture(material, name);
            key.Append('|').Append(texture != null ? texture.GetInstanceID() : 0);
            if (texture != null && material.HasProperty(name))
            {
                key.Append('@').Append(material.GetTextureScale(name).ToString("R"));
                key.Append('+').Append(material.GetTextureOffset(name).ToString("R"));
            }
        }

        private static bool IsOverlayLayerEnabled(Material material, string layer)
        {
            return GetFloat(material, UseLayerProperty(layer), 0f) != 0f;
        }

        private static string UseLayerProperty(string layer) => "_UseMain" + layer + "Tex";

        private static string LayerColorProperty(string layer) => "_Color" + layer;

        private static string LayerTexProperty(string layer) => "_Main" + layer + "Tex";

        private static string LayerBlendMaskProperty(string layer) => "_Main" + layer + "BlendMask";

        private static float GetFloat(Material material, string name, float fallback)
        {
            return material.HasProperty(name) ? material.GetFloat(name) : fallback;
        }

        private static Vector4 GetVector(Material material, string name, Vector4 fallback)
        {
            return material.HasProperty(name) ? material.GetVector(name) : fallback;
        }

        private static Color GetColor(Material material, string name, Color fallback)
        {
            return material.HasProperty(name) ? material.GetColor(name) : fallback;
        }

        private static Texture GetTexture(Material material, string name)
        {
            return material.HasProperty(name) ? material.GetTexture(name) : null;
        }

        private bool IsLilToonMaterial(Material material)
        {
            return (bool)_checkShaderIsLilToon.Invoke(null, new object[] { material });
        }

        private static MethodInfo ResolveRunBake()
        {
            Type type = FindType(InspectorTypeName);
            return type?.GetMethod(
                RunBakeMethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(Texture2D).MakeByRefType(),
                    typeof(Texture2D),
                    typeof(Material),
                    typeof(Texture2D),
                },
                modifiers: null
            );
        }

        private static MethodInfo ResolveCheckShaderIsLilToon()
        {
            Type type = FindType(MaterialUtilsTypeName);
            return type?.GetMethod(
                CheckShaderMethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Material) },
                modifiers: null
            );
        }

        private static Shader ResolveBakerShader()
        {
            // Prefer lilToon's own pointer to the baker shader so a future rename follows
            // automatically; fall back to the well-known shader name.
            Type type = FindType(ShaderManagerTypeName);
            var field = type?.GetField(
                BakerShaderFieldName,
                BindingFlags.Public | BindingFlags.Static
            );
            if (field?.GetValue(null) is Shader shader && shader != null)
                return shader;

            return Shader.Find(BakerShaderName);
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
