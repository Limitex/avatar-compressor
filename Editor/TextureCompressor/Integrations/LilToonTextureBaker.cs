using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.editor.texture.integrations
{
    /// <summary>
    /// Bakes lilToon's texture adjustments — main-color adjustments (HSVG, gradation map, active
    /// 2nd/3rd layers), the alpha mask, and outline tone correction — into their textures,
    /// using lilToon's baker shader (<c>Hidden/ltsother_baker</c>) via standard Unity
    /// <see cref="Graphics.Blit"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class does not call any lilToon C# API at runtime. It depends only on lilToon's
    /// baker shader being present (resolved via <see cref="Shader.Find"/>) and on the property
    /// names declared by lilToon's shaders. When lilToon is not installed the baker shader is
    /// absent, <see cref="IsAvailable"/> is false, and every call is a no-op.
    /// </para>
    /// <para>
    /// The property mapping is a port of <c>lilEditorTextureBaker</c> and must be re-checked
    /// against lilToon when it updates. A version guard based on
    /// <c>lilConstants.currentVersionValue</c> disables baking for untested versions.
    /// </para>
    /// <para>
    /// Two deliberate deviations from lilToon's inspector bakes. First, the main color
    /// (<c>_Color</c>) is never baked and is left as a runtime tint — because color-change
    /// animations are common and a baked color would freeze them. Second, source textures are
    /// sampled directly from the material on the GPU instead of re-loading the raw image files
    /// the way <c>lilTextureUtils.LoadTexture</c> does: the build must not mutate or destroy
    /// loaded assets.
    /// </para>
    /// </remarks>
    public sealed class LilToonTextureBaker : ILilToonBaker
    {
        private const string BakerShaderName = "Hidden/ltsother_baker";
        private const string LilConstantsTypeName = "lilToon.lilConstants";
        private const string VersionFieldName = "currentVersionValue";
        private const int SupportedVersionMax = 45;

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
        private const string AlphaMaskBakerKeyword = "_ALPHAMASK";

        private const string OutlineTexProperty = "_OutlineTex";
        private const string OutlineTexHsvgProperty = "_OutlineTexHSVG";

        private const string Layer2nd = "2nd";
        private const string Layer3rd = "3rd";

        /// <summary>
        /// lilToon's neutral HSVG (hue 0, saturation/value/gamma 1) — the "no adjustment" value.
        /// </summary>
        public static readonly Vector4 DefaultHsvg = new Vector4(0f, 1f, 1f, 1f);

        private static readonly string[] MainBakeInputProperties =
        {
            MainTexProperty,
            MainTexHsvgProperty,
            MainGradationStrengthProperty,
            MainGradationTexProperty,
            MainColorAdjustMaskProperty,
        };

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

        private static readonly string[] AlphaMaskBakeInputProperties =
        {
            MainTexProperty,
            AlphaMaskProperty,
            AlphaMaskModeProperty,
            AlphaMaskScaleProperty,
            AlphaMaskValueProperty,
        };

        private static readonly string[] OutlineBakeInputProperties =
        {
            OutlineTexProperty,
            OutlineTexHsvgProperty,
        };

        private readonly Shader _bakerShader;

        private readonly Dictionary<(Texture2D Source, string Key), Texture2D> _bakeCache =
            new Dictionary<(Texture2D, string), Texture2D>();

        public LilToonTextureBaker()
        {
            var version = GetLilToonVersion();

            // Without a GPU (-batchmode -nographics) Blit/ReadPixels return undefined data, so
            // baking would silently replace textures with garbage. Same guard idea as the
            // analysis backend's CPU fallback.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                if (version.HasValue)
                {
                    Debug.LogWarning(
                        "[LAC Texture Compressor] No graphics device is available "
                            + "(-nographics); texture baking is disabled for this build."
                    );
                }
                return;
            }

            if (version.HasValue && version.Value > SupportedVersionMax)
            {
                Debug.LogWarning(
                    "[LAC Texture Compressor] lilToon version "
                        + $"{version.Value} is newer than the tested version "
                        + $"({SupportedVersionMax}). Texture baking is disabled for this build."
                );
                return;
            }

            var shader = Shader.Find(BakerShaderName);
            _bakerShader = shader != null && shader.isSupported ? shader : null;

            if (version.HasValue && _bakerShader == null)
            {
                Debug.LogWarning(
                    "[LAC Texture Compressor] lilToon is installed, but the baker shader "
                        + $"'{BakerShaderName}' was not found or is not supported. "
                        + "Texture baking is disabled for this build."
                );
            }
        }

        public bool IsAvailable => _bakerShader != null;

        private enum BakeOutcome
        {
            NotApplicable,
            Baked,
            SkippedByAnimation,
        }

        public LilToonBakeResult Bake(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            Func<Texture2D, string, bool> canReplaceTexture,
            Func<Texture2D, bool> isFrozenTexture
        )
        {
            if (
                !IsAvailable
                || material == null
                || material.shader == null
                || animatedProperties == null
            )
                return default;

            if (!IsLilToonShader(material.shader))
                return default;

            // Order matters: the alpha-mask bake reads _MainTex, which the main bake may have
            // just replaced.
            var outcomes = new[]
            {
                BakeMainTexture(material, animatedProperties, canReplaceTexture, isFrozenTexture),
                BakeAlphaMask(material, animatedProperties, canReplaceTexture, isFrozenTexture),
                BakeOutlineTexture(material, animatedProperties, canReplaceTexture),
            };

            int bakedSlots = 0;
            int skippedByAnimation = 0;
            foreach (var outcome in outcomes)
            {
                if (outcome == BakeOutcome.Baked)
                    bakedSlots++;
                else if (outcome == BakeOutcome.SkippedByAnimation)
                    skippedByAnimation++;
            }

            return new LilToonBakeResult(bakedSlots, skippedByAnimation);
        }

        private BakeOutcome BakeMainTexture(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            Func<Texture2D, string, bool> canReplaceTexture,
            Func<Texture2D, bool> isFrozenTexture
        )
        {
            if (!HasBakeableColorAdjustments(material))
                return BakeOutcome.NotApplicable;

            var mainTexture = GetTexture(material, MainTexProperty) as Texture2D;
            if (mainTexture == null)
                return BakeOutcome.NotApplicable;

            if (!IsDefaultTilingOffset(material, MainTexProperty))
                return BakeOutcome.NotApplicable;

            if (canReplaceTexture != null && !canReplaceTexture(mainTexture, MainTexProperty))
                return BakeOutcome.NotApplicable;

            if (HasFrozenMainBakeInput(material, isFrozenTexture))
                return BakeOutcome.NotApplicable;

            if (HasAnimatedMainBakeInput(animatedProperties))
                return BakeOutcome.SkippedByAnimation;

            var (bake2nd, bake3rd) = SelectOverlayLayersToBake(
                material,
                animatedProperties,
                isFrozenTexture
            );

            if (!HasNonLayerAdjustments(material) && !bake2nd && !bake3rd)
                return HasAnimationVetoedLayer(material, animatedProperties)
                    ? BakeOutcome.SkippedByAnimation
                    : BakeOutcome.NotApplicable;

            var baked = BakeTexture(
                mainTexture,
                BuildMainBakeKey(material, bake2nd, bake3rd),
                baker => ConfigureMainBaker(material, baker, mainTexture, bake2nd, bake3rd)
            );
            if (baked == null)
                return BakeOutcome.NotApplicable;

            material.SetTexture(MainTexProperty, baked);
            material.SetVector(MainTexHsvgProperty, DefaultHsvg);
            material.SetFloat(MainGradationStrengthProperty, 0f);
            material.SetTexture(MainGradationTexProperty, null);
            material.SetTexture(MainColorAdjustMaskProperty, null);

            if (bake2nd)
                ClearOverlayLayer(material, Layer2nd);
            if (bake3rd)
                ClearOverlayLayer(material, Layer3rd);

            return BakeOutcome.Baked;
        }

        /// <summary>
        /// False when the shader does not declare the adjustment properties at all (e.g. lilToon
        /// Lite).
        /// </summary>
        public static bool HasBakeableColorAdjustments(Material material)
        {
            if (material == null || !material.HasProperty(MainTexHsvgProperty))
                return false;

            return HasNonLayerAdjustments(material)
                || IsOverlayLayerEnabled(material, Layer2nd)
                || IsOverlayLayerEnabled(material, Layer3rd);
        }

        /// <summary>
        /// Layer toggles and layer-specific properties are checked separately per layer — an
        /// animated layer is excluded from the bake rather than vetoing the whole operation.
        /// </summary>
        public static bool HasAnimatedMainBakeInput(IReadOnlyCollection<string> animatedProperties)
        {
            return AnyAnimated(animatedProperties, MainBakeInputProperties);
        }

        /// <summary>
        /// True when a texture consumed (and cleared) by the main bake — the gradation map or
        /// the color-adjust mask — is pinned by frozen settings.
        /// </summary>
        public static bool HasFrozenMainBakeInput(
            Material material,
            Func<Texture2D, bool> isFrozenTexture
        )
        {
            return IsFrozenSlot(material, MainGradationTexProperty, isFrozenTexture)
                || IsFrozenSlot(material, MainColorAdjustMaskProperty, isFrozenTexture);
        }

        private static bool HasNonLayerAdjustments(Material material)
        {
            return material.GetVector(MainTexHsvgProperty) != DefaultHsvg
                || GetFloat(material, MainGradationStrengthProperty, 0f) != 0f;
        }

        /// <summary>
        /// Decides which overlay layers the main bake may composite into the main texture.
        /// </summary>
        /// <remarks>
        /// Layers may only be baked while <c>_Color</c> is white and not animated: at runtime
        /// lilToon applies <c>_Color</c> to the main layer before 2nd/3rd compositing, so baking
        /// a layer under a non-white tint would tint the layer too. The 3rd layer additionally
        /// requires the 2nd to be baked as well or permanently absent — baking the 3rd under a
        /// live 2nd would invert lilToon's main → 2nd → 3rd compositing order.
        /// </remarks>
        public static (bool Bake2nd, bool Bake3rd) SelectOverlayLayersToBake(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            Func<Texture2D, bool> isFrozenTexture
        )
        {
            bool layersAllowed =
                GetColor(material, MainColorProperty, Color.white) == Color.white
                && !animatedProperties.Contains(MainColorProperty);
            if (!layersAllowed)
                return (false, false);

            bool bake2nd = CanBakeOverlayLayer(
                material,
                animatedProperties,
                isFrozenTexture,
                Layer2nd
            );
            bool bake3rd =
                CanBakeOverlayLayer(material, animatedProperties, isFrozenTexture, Layer3rd)
                && (
                    bake2nd
                    || (
                        !IsOverlayLayerEnabled(material, Layer2nd)
                        && !animatedProperties.Contains(UseLayerProperty(Layer2nd))
                    )
                );

            return (bake2nd, bake3rd);
        }

        /// <summary>
        /// True when the layer is enabled and no input that the bake would consume — the layer
        /// toggle, its parameters, or its textures — is animated, frozen, or driven by shader
        /// time.
        /// </summary>
        public static bool CanBakeOverlayLayer(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            Func<Texture2D, bool> isFrozenTexture,
            string layer
        )
        {
            return IsOverlayLayerEnabled(material, layer)
                && !HasAnimatedOverlayLayerInput(animatedProperties, layer)
                && !HasFrozenOverlayLayerInput(material, isFrozenTexture, layer)
                && !HasTimeAnimatedLayer(material, layer);
        }

        /// <summary>
        /// True when the layer's appearance changes with shader time and no animation clip — a
        /// decal flipbook (frame count &gt; 1 at nonzero FPS) or a nonzero UV scroll/rotate.
        /// Baking such a layer would freeze it at whatever frame the build happened to render;
        /// lilToon's own baker does not reproduce scroll/rotate either, so any nonzero value
        /// also makes the bake inexact.
        /// </summary>
        public static bool HasTimeAnimatedLayer(Material material, string layer)
        {
            Vector4 decalAnimation = GetVector(
                material,
                LayerDecalAnimationProperty(layer),
                new Vector4(1f, 1f, 1f, 30f)
            );
            if (decalAnimation.z > 1f && decalAnimation.w != 0f)
                return true;

            return GetVector(material, LayerScrollRotateProperty(layer), Vector4.zero)
                != Vector4.zero;
        }

        private static bool HasAnimatedOverlayLayerInput(
            IReadOnlyCollection<string> animatedProperties,
            string layer
        )
        {
            if (
                animatedProperties.Contains(UseLayerProperty(layer))
                || animatedProperties.Contains(LayerColorProperty(layer))
                || animatedProperties.Contains(LayerTexProperty(layer))
                || animatedProperties.Contains(LayerBlendMaskProperty(layer))
            )
                return true;

            foreach (var format in LayerFloatPropertyFormats)
            {
                if (animatedProperties.Contains(string.Format(format, layer)))
                    return true;
            }

            foreach (var format in LayerVectorPropertyFormats)
            {
                if (animatedProperties.Contains(string.Format(format, layer)))
                    return true;
            }

            return false;
        }

        private static bool HasFrozenOverlayLayerInput(
            Material material,
            Func<Texture2D, bool> isFrozenTexture,
            string layer
        )
        {
            return IsFrozenSlot(material, LayerTexProperty(layer), isFrozenTexture)
                || IsFrozenSlot(material, LayerBlendMaskProperty(layer), isFrozenTexture);
        }

        private static bool IsFrozenSlot(
            Material material,
            string property,
            Func<Texture2D, bool> isFrozenTexture
        )
        {
            return isFrozenTexture != null
                && GetTexture(material, property) is Texture2D texture
                && isFrozenTexture(texture);
        }

        /// <summary>
        /// True when at least one enabled overlay layer was excluded from the bake because an
        /// animated property (a layer input, or <c>_Color</c> gating all layers) vetoed it —
        /// distinguishes an animation skip from a plain no-op when the layers were the only
        /// bakeable content.
        /// </summary>
        private static bool HasAnimationVetoedLayer(
            Material material,
            IReadOnlyCollection<string> animatedProperties
        )
        {
            bool layer2ndEnabled = IsOverlayLayerEnabled(material, Layer2nd);
            bool layer3rdEnabled = IsOverlayLayerEnabled(material, Layer3rd);

            if (
                (layer2ndEnabled || layer3rdEnabled)
                && animatedProperties.Contains(MainColorProperty)
            )
                return true;

            return (layer2ndEnabled && HasAnimatedOverlayLayerInput(animatedProperties, Layer2nd))
                || (layer3rdEnabled && HasAnimatedOverlayLayerInput(animatedProperties, Layer3rd));
        }

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
            baker.SetTexture(name, texture != null ? texture : Texture2D.whiteTexture);
            baker.SetTextureScale(name, material.GetTextureScale(name));
            baker.SetTextureOffset(name, material.GetTextureOffset(name));
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

        private BakeOutcome BakeAlphaMask(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            Func<Texture2D, string, bool> canReplaceTexture,
            Func<Texture2D, bool> isFrozenTexture
        )
        {
            if (!HasBakeableAlphaMask(material))
                return BakeOutcome.NotApplicable;

            var mainTexture = GetTexture(material, MainTexProperty) as Texture2D;
            if (mainTexture == null)
                return BakeOutcome.NotApplicable;

            if (!IsDefaultTilingOffset(material, MainTexProperty))
                return BakeOutcome.NotApplicable;

            if (canReplaceTexture != null && !canReplaceTexture(mainTexture, MainTexProperty))
                return BakeOutcome.NotApplicable;

            if (IsFrozenSlot(material, AlphaMaskProperty, isFrozenTexture))
                return BakeOutcome.NotApplicable;

            if (HasAnimatedAlphaMaskBakeInput(animatedProperties))
                return BakeOutcome.SkippedByAnimation;

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
                return BakeOutcome.NotApplicable;

            material.SetTexture(MainTexProperty, baked);
            material.SetFloat(AlphaMaskModeProperty, 0f);
            material.SetTexture(AlphaMaskProperty, null);
            material.SetFloat(AlphaMaskValueProperty, 0f);
            return BakeOutcome.Baked;
        }

        /// <summary>
        /// True when the alpha-mask bake applies: mask mode on, a mask assigned, and default
        /// mask tiling. False when the shader does not declare the alpha-mask properties.
        /// </summary>
        public static bool HasBakeableAlphaMask(Material material)
        {
            if (
                material == null
                || !material.HasProperty(AlphaMaskModeProperty)
                || !material.HasProperty(AlphaMaskProperty)
            )
                return false;

            if (material.GetFloat(AlphaMaskModeProperty) == 0f)
                return false;

            if (material.GetTexture(AlphaMaskProperty) == null)
                return false;

            return IsDefaultTilingOffset(material, AlphaMaskProperty);
        }

        /// <summary>
        /// True when any input consumed by the alpha-mask bake is driven by animation.
        /// </summary>
        public static bool HasAnimatedAlphaMaskBakeInput(
            IReadOnlyCollection<string> animatedProperties
        )
        {
            return AnyAnimated(animatedProperties, AlphaMaskBakeInputProperties);
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

        private BakeOutcome BakeOutlineTexture(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            Func<Texture2D, string, bool> canReplaceTexture
        )
        {
            if (!HasBakeableOutline(material))
                return BakeOutcome.NotApplicable;

            var outlineTexture = GetTexture(material, OutlineTexProperty) as Texture2D;
            if (outlineTexture == null)
                return BakeOutcome.NotApplicable;

            if (canReplaceTexture != null && !canReplaceTexture(outlineTexture, OutlineTexProperty))
                return BakeOutcome.NotApplicable;

            if (HasAnimatedOutlineBakeInput(animatedProperties))
                return BakeOutcome.SkippedByAnimation;

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
                return BakeOutcome.NotApplicable;

            material.SetTexture(OutlineTexProperty, baked);
            material.SetVector(OutlineTexHsvgProperty, DefaultHsvg);
            return BakeOutcome.Baked;
        }

        /// <summary>
        /// True when the outline bake applies: an outline texture with a non-default tone
        /// correction and default tiling.
        /// </summary>
        public static bool HasBakeableOutline(Material material)
        {
            return material != null
                && material.HasProperty(OutlineTexProperty)
                && material.HasProperty(OutlineTexHsvgProperty)
                && material.GetTexture(OutlineTexProperty) != null
                && material.GetVector(OutlineTexHsvgProperty) != DefaultHsvg
                && IsDefaultTilingOffset(material, OutlineTexProperty);
        }

        /// <summary>
        /// True when any input consumed by the outline bake is driven by animation.
        /// </summary>
        public static bool HasAnimatedOutlineBakeInput(
            IReadOnlyCollection<string> animatedProperties
        )
        {
            return AnyAnimated(animatedProperties, OutlineBakeInputProperties);
        }

        /// <summary>
        /// Destroys baked textures that no material references anymore — intermediates
        /// superseded by a chained bake (the alpha-mask bake replaces a just-baked main) or by
        /// the compressed copy the pipeline swapped in. Full-resolution RGBA32 textures would
        /// otherwise stay alive until domain reload. Call only after the build has finished
        /// using the bake outputs, including animation-reference remapping.
        /// </summary>
        public void DestroyOrphanedBakes(IEnumerable<Material> materials)
        {
            if (_bakeCache.Count == 0)
                return;

            var orphaned = new HashSet<Texture2D>(
                FindOrphanedTextures(_bakeCache.Values, materials)
            );
            if (orphaned.Count == 0)
                return;

            var orphanedKeys = new List<(Texture2D, string)>();
            foreach (var kvp in _bakeCache)
            {
                if (orphaned.Contains(kvp.Value))
                    orphanedKeys.Add(kvp.Key);
            }
            foreach (var key in orphanedKeys)
            {
                _bakeCache.Remove(key);
            }
            foreach (var texture in orphaned)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        /// Returns the candidates that are not assigned to any texture slot of the given
        /// materials.
        /// </summary>
        public static List<Texture2D> FindOrphanedTextures(
            IReadOnlyCollection<Texture2D> candidates,
            IEnumerable<Material> materials
        )
        {
            var live = new HashSet<Texture2D>();
            foreach (var material in materials)
            {
                if (material == null || material.shader == null)
                    continue;

                foreach (var name in material.GetTexturePropertyNames())
                {
                    if (material.GetTexture(name) is Texture2D texture)
                        live.Add(texture);
                }
            }

            var orphaned = new List<Texture2D>();
            foreach (var candidate in candidates)
            {
                if (candidate != null && !live.Contains(candidate))
                    orphaned.Add(candidate);
            }
            return orphaned;
        }

        private Texture2D BakeTexture(Texture2D source, string key, Action<Material> configure)
        {
            if (_bakeCache.TryGetValue((source, key), out var cached) && cached != null)
                return cached;

            Texture2D baked;
            var baker = new Material(_bakerShader);
            try
            {
                configure(baker);
                baked = BlitBake(source, baker);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baker);
            }

            if (baked == null)
                return null;

            baked.name = source.name + "_baked";
            ObjectRegistry.RegisterReplacedObject(source, baked);
            _bakeCache[(source, key)] = baked;
            return baked;
        }

        private static Texture2D BlitBake(Texture2D source, Material baker)
        {
            int width = source.width;
            int height = source.height;
            bool mipChain = source.mipmapCount > 1;
            var rt = new RenderTexture(width, height, 0);
            RenderTexture previous = RenderTexture.active;
            Texture2D output = null;
            try
            {
                rt.Create();
                RenderTexture.active = rt;
                Graphics.Blit(source, rt, baker);

                output = new Texture2D(width, height, TextureFormat.RGBA32, mipChain);
                output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                output.Apply(mipChain);
                TextureProcessor.CopyTextureSettings(source, output);

                var result = output;
                output = null;
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                if (output != null)
                    UnityEngine.Object.DestroyImmediate(output);
            }
        }

        private static bool AnyAnimated(
            IReadOnlyCollection<string> animatedProperties,
            string[] properties
        )
        {
            foreach (var property in properties)
            {
                if (animatedProperties.Contains(property))
                    return true;
            }
            return false;
        }

        private static bool IsDefaultTilingOffset(Material material, string property)
        {
            return material.GetTextureScale(property) == Vector2.one
                && material.GetTextureOffset(property) == Vector2.zero;
        }

        private static void AppendTextureKey(StringBuilder key, Material material, string name)
        {
            var texture = GetTexture(material, name);
            key.Append('|').Append(texture != null ? texture.GetInstanceID() : 0);
            if (texture != null)
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

        private static string LayerDecalAnimationProperty(string layer) =>
            "_Main" + layer + "TexDecalAnimation";

        private static string LayerScrollRotateProperty(string layer) =>
            "_Main" + layer + "Tex_ScrollRotate";

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

        private static bool IsLilToonShader(Shader shader)
        {
            if (shader == null)
                return false;
            var name = shader.name;
            return name.Contains("lilToon");
        }

        private static int? GetLilToonVersion()
        {
            var type = OptionalStaticMethod.FindType(LilConstantsTypeName);
            var field = type?.GetField(VersionFieldName, BindingFlags.Public | BindingFlags.Static);
            if (field?.GetValue(null) is int version)
                return version;
            return null;
        }
    }
}
