using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using nadena.dev.ndmf;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.integrations
{
    /// <summary>
    /// Bakes lilToon's texture adjustments — main-color adjustments (HSVG, gradation map, active
    /// 2nd/3rd layers), the alpha mask, and outline tone correction — into their textures,
    /// through lilToon's public <c>lilToonInspector.RunBake</c> and its baker shader
    /// (<c>Hidden/ltsother_baker</c>) accessed via reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// lilToon is an optional, external package and is not a declared dependency of this project,
    /// so it cannot be referenced at compile time. The API is resolved through
    /// <see cref="OptionalStaticMethod"/> and raw reflection, which keeps the build green whether
    /// or not lilToon is installed: when its types are absent, <see cref="IsAvailable"/> is false
    /// and every call is a no-op (the bake feature simply passes through).
    /// </para>
    /// <para>
    /// lilToon exposes no non-interactive entry point for baking (its <c>AutoBake*</c> methods
    /// are private and show modal dialogs), so the property mapping below is a port of
    /// <c>lilEditorTextureBaker</c> — the pixel math itself still runs through lilToon's own
    /// shader and <c>RunBake</c>, but the mapping is ours and must be re-checked against lilToon
    /// when it updates.
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
        private const string AlphaMaskBakerKeyword = "_ALPHAMASK";

        private const string OutlineTexProperty = "_OutlineTex";
        private const string OutlineTexHsvgProperty = "_OutlineTexHSVG";

        private const string Layer2nd = "2nd";
        private const string Layer3rd = "3rd";

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

        private readonly OptionalStaticMethod _runBake;
        private readonly OptionalStaticMethod _checkShaderIsLilToon;
        private readonly Shader _bakerShader;

        private readonly Dictionary<(Texture2D Source, string Key), Texture2D> _bakeCache =
            new Dictionary<(Texture2D, string), Texture2D>();

        public LilToonTextureBaker()
        {
            _runBake = new OptionalStaticMethod(
                InspectorTypeName,
                RunBakeMethodName,
                typeof(Texture2D).MakeByRefType(),
                typeof(Texture2D),
                typeof(Material),
                typeof(Texture2D)
            );

            _checkShaderIsLilToon = new OptionalStaticMethod(
                MaterialUtilsTypeName,
                CheckShaderMethodName,
                typeof(Material)
            );

            _bakerShader = _runBake.IsAvailable ? ResolveBakerShader() : null;

            if (
                _runBake.Status == OptionalStaticMethod.ResolutionStatus.MethodNotFound
                || _checkShaderIsLilToon.Status
                    == OptionalStaticMethod.ResolutionStatus.MethodNotFound
            )
            {
                Debug.LogWarning(
                    "[LAC Texture Compressor] lilToon is installed, but a required bake method "
                        + "was not found. Texture baking is disabled for this build."
                );
            }
            else if (
                _runBake.IsAvailable
                && _checkShaderIsLilToon.IsAvailable
                && _bakerShader == null
            )
            {
                Debug.LogWarning(
                    "[LAC Texture Compressor] lilToon is installed, but the baker shader "
                        + $"'{BakerShaderName}' was not found or is not supported. "
                        + "Texture baking is disabled for this build."
                );
            }
        }

        public bool IsAvailable =>
            _runBake.IsAvailable && _checkShaderIsLilToon.IsAvailable && _bakerShader != null;

        public Texture2D[] Bake(Material material, IReadOnlyCollection<string> animatedProperties)
        {
            if (
                !IsAvailable
                || material == null
                || material.shader == null
                || animatedProperties == null
            )
                return Array.Empty<Texture2D>();

            if (!IsLilToonMaterial(material))
                return Array.Empty<Texture2D>();

            var bakedTextures = new List<Texture2D>();

            BakeMainTexture(material, animatedProperties, bakedTextures);
            BakeAlphaMask(material, animatedProperties, bakedTextures);
            BakeOutlineTexture(material, animatedProperties, bakedTextures);

            return bakedTextures.ToArray();
        }

        private void BakeMainTexture(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            List<Texture2D> bakedTextures
        )
        {
            if (!HasBakeableColorAdjustments(material))
                return;

            var mainTexture = GetTexture(material, MainTexProperty) as Texture2D;
            if (mainTexture == null)
                return;

            if (!IsDefaultTilingOffset(material, MainTexProperty))
                return;

            if (AnyAnimated(animatedProperties, MainBakeInputProperties))
                return;

            bool bake2nd = CanBakeOverlayLayer(material, animatedProperties, Layer2nd);
            bool bake3rd = CanBakeOverlayLayer(material, animatedProperties, Layer3rd);

            if (!HasNonLayerAdjustments(material) && !bake2nd && !bake3rd)
                return;

            var baked = BakeTexture(
                mainTexture,
                BuildMainBakeKey(material, bake2nd, bake3rd),
                baker => ConfigureMainBaker(material, baker, mainTexture, bake2nd, bake3rd)
            );
            if (baked == null)
                return;

            material.SetTexture(MainTexProperty, baked);
            material.SetVector(MainTexHsvgProperty, DefaultHsvg);
            material.SetFloat(MainGradationStrengthProperty, 0f);
            material.SetTexture(MainGradationTexProperty, null);
            material.SetTexture(MainColorAdjustMaskProperty, null);

            if (bake2nd)
                ClearOverlayLayer(material, Layer2nd);
            if (bake3rd)
                ClearOverlayLayer(material, Layer3rd);

            bakedTextures.Add(baked);
        }

        /// <summary>
        /// False when the shader does not declare the adjustment properties at all (e.g. lilToon
        /// Lite).
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
        /// Layer toggles and layer-specific properties are checked separately per layer — an
        /// animated layer is excluded from the bake rather than vetoing the whole operation.
        /// </summary>
        public static bool HasAnimatedMainBakeInput(
            Material material,
            IReadOnlyCollection<string> animatedProperties
        )
        {
            if (material == null || animatedProperties == null)
                return false;

            return AnyAnimated(animatedProperties, MainBakeInputProperties);
        }

        private static bool HasNonLayerAdjustments(Material material)
        {
            return material.GetVector(MainTexHsvgProperty) != DefaultHsvg
                || GetFloat(material, MainGradationStrengthProperty, 0f) != 0f;
        }

        private static bool CanBakeOverlayLayer(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            string layer
        )
        {
            return IsOverlayLayerEnabled(material, layer)
                && !animatedProperties.Contains(UseLayerProperty(layer))
                && !HasAnimatedOverlayLayerInput(material, animatedProperties, layer);
        }

        private static bool HasAnimatedOverlayLayerInput(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            string layer
        )
        {
            if (!IsOverlayLayerEnabled(material, layer))
                return false;

            if (
                animatedProperties.Contains(LayerColorProperty(layer))
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

        private void BakeAlphaMask(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            List<Texture2D> bakedTextures
        )
        {
            if (!HasBakeableAlphaMask(material))
                return;

            var mainTexture = GetTexture(material, MainTexProperty) as Texture2D;
            if (mainTexture == null)
                return;

            if (!IsDefaultTilingOffset(material, MainTexProperty))
                return;

            if (HasAnimatedAlphaMaskBakeInput(animatedProperties))
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

            material.SetTexture(MainTexProperty, baked);
            material.SetFloat(AlphaMaskModeProperty, 0f);
            material.SetTexture(AlphaMaskProperty, null);
            material.SetFloat(AlphaMaskValueProperty, 0f);
            bakedTextures.Add(baked);
        }

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

        private void BakeOutlineTexture(
            Material material,
            IReadOnlyCollection<string> animatedProperties,
            List<Texture2D> bakedTextures
        )
        {
            if (!HasBakeableOutline(material))
                return;

            var outlineTexture = GetTexture(material, OutlineTexProperty) as Texture2D;
            if (outlineTexture == null)
                return;

            if (HasAnimatedOutlineBakeInput(animatedProperties))
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
            bakedTextures.Add(baked);
        }

        public static bool HasBakeableOutline(Material material)
        {
            return material != null
                && material.HasProperty(OutlineTexProperty)
                && material.HasProperty(OutlineTexHsvgProperty)
                && material.GetTexture(OutlineTexProperty) != null
                && material.GetVector(OutlineTexHsvgProperty) != DefaultHsvg
                && IsDefaultTilingOffset(material, OutlineTexProperty);
        }

        public static bool HasAnimatedOutlineBakeInput(
            IReadOnlyCollection<string> animatedProperties
        )
        {
            return AnyAnimated(animatedProperties, OutlineBakeInputProperties);
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
                baked = InvokeRunBake(source, baker);
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

        private Texture2D InvokeRunBake(Texture2D source, Material baker)
        {
            var args = new object[] { null, source, baker, null };
            if (!_runBake.TryInvoke(args, out _, out var error))
            {
                if (error != null)
                {
                    Debug.LogWarning(
                        "[LAC Texture Compressor] lilToon texture baking failed: "
                            + $"{error.Message}. Baking is disabled for the rest of this build."
                    );
                }
                return null;
            }
            return args[0] as Texture2D;
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
            if (
                !_checkShaderIsLilToon.TryInvoke(
                    new object[] { material },
                    out var result,
                    out var error
                )
            )
            {
                if (error != null)
                {
                    Debug.LogWarning(
                        "[LAC Texture Compressor] lilToon shader check failed: "
                            + $"{error.Message}. Baking is disabled for the rest of this build."
                    );
                }
                return false;
            }
            return result is bool b && b;
        }

        private static Shader ResolveBakerShader()
        {
            Type shaderManagerType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                shaderManagerType = assembly.GetType(ShaderManagerTypeName, throwOnError: false);
                if (shaderManagerType != null)
                    break;
            }

            if (shaderManagerType != null)
            {
                var field = shaderManagerType.GetField(
                    BakerShaderFieldName,
                    BindingFlags.Public | BindingFlags.Static
                );
                if (field?.GetValue(null) is Shader shader && shader != null && shader.isSupported)
                    return shader;
            }

            var found = Shader.Find(BakerShaderName);
            return found != null && found.isSupported ? found : null;
        }
    }
}
