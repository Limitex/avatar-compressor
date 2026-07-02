using System;
using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using dev.limitex.avatar.compressor.editor.texture.integrations;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class LilToonTextureBakerTests
    {
        private const string BakeTestShaderName = "Hidden/LAC/Tests/LilToonBake";
        private const string SlotTestShaderName = "Hidden/LAC/Tests/UnusedSlot";

        private Material _material;
        private Texture2D _texture;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find(BakeTestShaderName);
            Assert.That(shader, Is.Not.Null, $"Test shader '{BakeTestShaderName}' is missing");
            _material = new Material(shader);
            _texture = new Texture2D(4, 4);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_material);
            UnityEngine.Object.DestroyImmediate(_texture);
        }

        [Test]
        public void Constructor_DoesNotThrow_RegardlessOfLilToonPresence()
        {
            Assert.DoesNotThrow(() => new LilToonTextureBaker());
        }

        [Test]
        public void Bake_ReturnsZeroResult_WhenLilToonNotInstalled()
        {
            var baker = new LilToonTextureBaker();
            Assume.That(
                baker.IsAvailable,
                Is.False,
                "lilToon is installed in this project; skipping the absent-package no-op check."
            );

            var material = new Material(Shader.Find("Standard"));
            var result = baker.Bake(material, Array.Empty<string>(), null, null);

            Assert.AreEqual(0, result.BakedSlots);
            Assert.AreEqual(0, result.SkippedByAnimation);

            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void Bake_ReturnsZeroResult_OnNullMaterial()
        {
            var baker = new LilToonTextureBaker();
            var result = baker.Bake(null, Array.Empty<string>(), null, null);

            Assert.AreEqual(0, result.BakedSlots);
            Assert.AreEqual(0, result.SkippedByAnimation);
        }

        [Test]
        public void Bake_ReturnsZeroResult_OnNullShader()
        {
            var baker = new LilToonTextureBaker();
            var material = new Material(Shader.Find("Standard"));
            material.shader = null;

            var result = baker.Bake(material, Array.Empty<string>(), null, null);

            Assert.AreEqual(0, result.BakedSlots);
            Assert.AreEqual(0, result.SkippedByAnimation);

            UnityEngine.Object.DestroyImmediate(material);
        }

        #region HasBakeableColorAdjustments

        [Test]
        public void HasBakeableColorAdjustments_DefaultMaterial_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasBakeableColorAdjustments(_material));
        }

        [Test]
        public void HasBakeableColorAdjustments_NullMaterial_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasBakeableColorAdjustments(null));
        }

        [Test]
        public void HasBakeableColorAdjustments_NonDefaultHsvg_ReturnsTrue()
        {
            _material.SetVector("_MainTexHSVG", new Vector4(0.5f, 1f, 1f, 1f));
            Assert.IsTrue(LilToonTextureBaker.HasBakeableColorAdjustments(_material));
        }

        [Test]
        public void HasBakeableColorAdjustments_GradationStrength_ReturnsTrue()
        {
            _material.SetFloat("_MainGradationStrength", 0.5f);
            Assert.IsTrue(LilToonTextureBaker.HasBakeableColorAdjustments(_material));
        }

        [Test]
        public void HasBakeableColorAdjustments_SecondLayerEnabled_ReturnsTrue()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            Assert.IsTrue(LilToonTextureBaker.HasBakeableColorAdjustments(_material));
        }

        [Test]
        public void HasBakeableColorAdjustments_ThirdLayerEnabled_ReturnsTrue()
        {
            _material.SetFloat("_UseMain3rdTex", 1f);
            Assert.IsTrue(LilToonTextureBaker.HasBakeableColorAdjustments(_material));
        }

        [Test]
        public void HasBakeableColorAdjustments_MainColorOnly_ReturnsFalse()
        {
            _material.SetColor("_Color", Color.red);
            Assert.IsFalse(LilToonTextureBaker.HasBakeableColorAdjustments(_material));
        }

        [Test]
        public void HasBakeableColorAdjustments_ShaderWithoutHsvg_ReturnsFalse()
        {
            var shader = Shader.Find(SlotTestShaderName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasBakeableColorAdjustments(material));

            UnityEngine.Object.DestroyImmediate(material);
        }

        #endregion

        #region HasAnimatedMainBakeInput

        [Test]
        public void HasAnimatedMainBakeInput_EmptySet_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(Array.Empty<string>()));
        }

        [Test]
        public void HasAnimatedMainBakeInput_UnrelatedAnimatedProperty_ReturnsFalse()
        {
            var props = new HashSet<string> { "_UseEmission", "_Color" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedHsvg_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainTexHSVG" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedMainTexture_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainTex" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedLayerToggle_ReturnsFalse()
        {
            var props = new HashSet<string> { "_UseMain2ndTex" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedGradationStrength_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainGradationStrength" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedGradationTex_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainGradationTex" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedColorAdjustMask_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainColorAdjustMask" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(props));
        }

        #endregion

        #region Protected Input Gates

        [Test]
        public void HasProtectedMainBakeInput_NullPredicate_ReturnsFalse()
        {
            _material.SetTexture("_MainGradationTex", _texture);
            Assert.IsFalse(LilToonTextureBaker.HasProtectedMainBakeInput(_material, null));
        }

        [Test]
        public void HasProtectedMainBakeInput_NoInputTextures_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasProtectedMainBakeInput(_material, _ => true));
        }

        [Test]
        public void HasProtectedMainBakeInput_ProtectedGradationTex_ReturnsTrue()
        {
            _material.SetTexture("_MainGradationTex", _texture);
            Assert.IsTrue(
                LilToonTextureBaker.HasProtectedMainBakeInput(_material, t => t == _texture)
            );
        }

        [Test]
        public void HasProtectedMainBakeInput_ProtectedColorAdjustMask_ReturnsTrue()
        {
            _material.SetTexture("_MainColorAdjustMask", _texture);
            Assert.IsTrue(
                LilToonTextureBaker.HasProtectedMainBakeInput(_material, t => t == _texture)
            );
        }

        [Test]
        public void HasProtectedMainBakeInput_UnprotectedInputTextures_ReturnsFalse()
        {
            _material.SetTexture("_MainGradationTex", _texture);
            Assert.IsFalse(LilToonTextureBaker.HasProtectedMainBakeInput(_material, _ => false));
        }

        [Test]
        public void HasProtectedMainBakeInput_RenderTextureInput_ReturnsTrue()
        {
            // A non-Texture2D input (e.g. a CustomRenderTexture) must block the bake even with
            // no protection predicate: its content changes at runtime and cannot be consumed.
            var renderTexture = new RenderTexture(4, 4, 0);
            _material.SetTexture("_MainGradationTex", renderTexture);

            Assert.IsTrue(LilToonTextureBaker.HasProtectedMainBakeInput(_material, null));

            _material.SetTexture("_MainGradationTex", null);
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }

        #endregion

        #region CanBakeOverlayLayer

        [Test]
        public void CanBakeOverlayLayer_EnabledLayer_ReturnsTrue()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            Assert.IsTrue(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    null,
                    "2nd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_DisabledLayer_ReturnsFalse()
        {
            Assert.IsFalse(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    null,
                    "2nd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_AnimatedToggle_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_UseMain2ndTex" };
            Assert.IsFalse(LilToonTextureBaker.CanBakeOverlayLayer(_material, props, null, "2nd"));
        }

        [Test]
        public void CanBakeOverlayLayer_AnimatedLayerParameter_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Main2ndTexAngle" };
            Assert.IsFalse(LilToonTextureBaker.CanBakeOverlayLayer(_material, props, null, "2nd"));
        }

        [Test]
        public void CanBakeOverlayLayer_ProtectedLayerTexture_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetTexture("_Main2ndTex", _texture);
            Assert.IsFalse(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    t => t == _texture,
                    "2nd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_ProtectedBlendMask_ReturnsFalse()
        {
            _material.SetFloat("_UseMain3rdTex", 1f);
            _material.SetTexture("_Main3rdBlendMask", _texture);
            Assert.IsFalse(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    t => t == _texture,
                    "3rd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_UnprotectedLayerTexture_ReturnsTrue()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetTexture("_Main2ndTex", _texture);
            Assert.IsTrue(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    _ => false,
                    "2nd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_RenderTextureLayerTexture_ReturnsFalse()
        {
            var renderTexture = new RenderTexture(4, 4, 0);
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetTexture("_Main2ndTex", renderTexture);

            Assert.IsFalse(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    null,
                    "2nd"
                )
            );

            _material.SetTexture("_Main2ndTex", null);
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }

        [Test]
        public void CanBakeOverlayLayer_DecalFlipbook_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetVector("_Main2ndTexDecalAnimation", new Vector4(4f, 4f, 16f, 30f));
            Assert.IsFalse(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    null,
                    "2nd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_AnimatedLayerSt_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Main2ndTex_ST" };
            Assert.IsFalse(LilToonTextureBaker.CanBakeOverlayLayer(_material, props, null, "2nd"));
        }

        [Test]
        public void CanBakeOverlayLayer_AnimatedScrollRotate_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Main2ndTex_ScrollRotate" };
            Assert.IsFalse(LilToonTextureBaker.CanBakeOverlayLayer(_material, props, null, "2nd"));
        }

        [Test]
        public void CanBakeOverlayLayer_NonZeroUVMode_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetFloat("_Main2ndTex_UVMode", 1f);
            Assert.IsFalse(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    null,
                    "2nd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_AnimatedUVMode_ReturnsFalse()
        {
            _material.SetFloat("_UseMain3rdTex", 1f);
            var props = new HashSet<string> { "_Main3rdTex_UVMode" };
            Assert.IsFalse(LilToonTextureBaker.CanBakeOverlayLayer(_material, props, null, "3rd"));
        }

        #endregion

        #region HasTimeAnimatedLayer

        [Test]
        public void HasTimeAnimatedLayer_DefaultValues_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasTimeAnimatedLayer(_material, "2nd"));
        }

        [Test]
        public void HasTimeAnimatedLayer_Flipbook_ReturnsTrue()
        {
            _material.SetVector("_Main2ndTexDecalAnimation", new Vector4(4f, 4f, 16f, 30f));
            Assert.IsTrue(LilToonTextureBaker.HasTimeAnimatedLayer(_material, "2nd"));
        }

        [Test]
        public void HasTimeAnimatedLayer_MultiFrameZeroFps_ReturnsFalse()
        {
            _material.SetVector("_Main2ndTexDecalAnimation", new Vector4(4f, 4f, 16f, 0f));
            Assert.IsFalse(LilToonTextureBaker.HasTimeAnimatedLayer(_material, "2nd"));
        }

        [Test]
        public void HasTimeAnimatedLayer_ScrollRotate_ReturnsTrue()
        {
            _material.SetVector("_Main3rdTex_ScrollRotate", new Vector4(0.1f, 0f, 0f, 0f));
            Assert.IsTrue(LilToonTextureBaker.HasTimeAnimatedLayer(_material, "3rd"));
        }

        [Test]
        public void HasTimeAnimatedLayer_ShaderWithoutProperties_ReturnsFalse()
        {
            var shader = Shader.Find(SlotTestShaderName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasTimeAnimatedLayer(material, "2nd"));

            UnityEngine.Object.DestroyImmediate(material);
        }

        #endregion

        #region HasUnbakeableLayerFeature

        [Test]
        public void HasUnbakeableLayerFeature_DefaultValues_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasUnbakeableLayerFeature(_material, "2nd"));
        }

        [Test]
        public void HasUnbakeableLayerFeature_DissolveEnabled_ReturnsTrue()
        {
            _material.SetVector("_Main2ndDissolveParams", new Vector4(1f, 0f, 0.5f, 0.1f));
            Assert.IsTrue(LilToonTextureBaker.HasUnbakeableLayerFeature(_material, "2nd"));
        }

        [Test]
        public void HasUnbakeableLayerFeature_ReducedLighting_ReturnsTrue()
        {
            _material.SetFloat("_Main2ndEnableLighting", 0.5f);
            Assert.IsTrue(LilToonTextureBaker.HasUnbakeableLayerFeature(_material, "2nd"));
        }

        [Test]
        public void HasUnbakeableLayerFeature_DistanceFadeActive_ReturnsTrue()
        {
            _material.SetVector("_Main2ndDistanceFade", new Vector4(0.1f, 0.01f, 0.8f, 0f));
            Assert.IsTrue(LilToonTextureBaker.HasUnbakeableLayerFeature(_material, "2nd"));
        }

        [Test]
        public void HasUnbakeableLayerFeature_CullSet_ReturnsTrue()
        {
            _material.SetFloat("_Main3rdTex_Cull", 1f);
            Assert.IsTrue(LilToonTextureBaker.HasUnbakeableLayerFeature(_material, "3rd"));
        }

        [Test]
        public void HasUnbakeableLayerFeature_AudioLinkToLayer_ReturnsTrue()
        {
            _material.SetFloat("_AudioLink2Main2nd", 1f);
            Assert.IsTrue(LilToonTextureBaker.HasUnbakeableLayerFeature(_material, "2nd"));
        }

        [Test]
        public void HasUnbakeableLayerFeature_NonDefaultAlphaMode_ReturnsTrue()
        {
            _material.SetFloat("_Main2ndTexAlphaMode", 1f);
            Assert.IsTrue(LilToonTextureBaker.HasUnbakeableLayerFeature(_material, "2nd"));
        }

        [Test]
        public void HasUnbakeableLayerFeature_ShaderWithoutProperties_ReturnsFalse()
        {
            var shader = Shader.Find(SlotTestShaderName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasUnbakeableLayerFeature(material, "2nd"));

            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void CanBakeOverlayLayer_DissolveLayer_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetVector("_Main2ndDissolveParams", new Vector4(1f, 0f, 0.5f, 0.1f));
            Assert.IsFalse(
                LilToonTextureBaker.CanBakeOverlayLayer(
                    _material,
                    Array.Empty<string>(),
                    null,
                    "2nd"
                )
            );
        }

        [Test]
        public void CanBakeOverlayLayer_AnimatedEnableLighting_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Main2ndEnableLighting" };
            Assert.IsFalse(LilToonTextureBaker.CanBakeOverlayLayer(_material, props, null, "2nd"));
        }

        [Test]
        public void CanBakeOverlayLayer_AnimatedAudioLinkToggle_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_AudioLink2Main2nd" };
            Assert.IsFalse(LilToonTextureBaker.CanBakeOverlayLayer(_material, props, null, "2nd"));
        }

        #endregion

        #region SelectOverlayLayersToBake

        [Test]
        public void SelectOverlayLayersToBake_WhiteColor_BakesEnabledLayers()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetFloat("_UseMain3rdTex", 1f);

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                Array.Empty<string>(),
                null
            );

            Assert.IsTrue(bake2nd);
            Assert.IsTrue(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_NonWhiteColor_BakesNoLayers()
        {
            _material.SetColor("_Color", new Color(1f, 0.4f, 0.4f, 1f));
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetFloat("_UseMain3rdTex", 1f);

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                Array.Empty<string>(),
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsFalse(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_AnimatedColor_BakesNoLayers()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Color" };

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsFalse(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_ThirdLayerUnderLiveSecond_NotBaked()
        {
            // 2nd enabled but animation-vetoed (stays live at runtime), 3rd clean: baking the
            // 3rd would put it under the live 2nd, inverting the compositing order.
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetFloat("_UseMain3rdTex", 1f);
            var props = new HashSet<string> { "_UseMain2ndTex" };

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsFalse(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_ThirdLayerWithSecondPermanentlyOff_Baked()
        {
            _material.SetFloat("_UseMain3rdTex", 1f);

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                Array.Empty<string>(),
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsTrue(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_AnimatedMainTexSt_BakesNoLayers()
        {
            // Layers sample uv0 while the main texture rides uvMain: baking a layer under an
            // animated main ST would make it wrongly follow the main UV movement.
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_MainTex_ST" };

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsFalse(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_StaticMainScrollRotate_BakesNoLayers()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetVector("_MainTex_ScrollRotate", new Vector4(0.1f, 0f, 0f, 0f));

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                Array.Empty<string>(),
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsFalse(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_AnimatedMainScrollRotate_BakesNoLayers()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_MainTex_ScrollRotate" };

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsFalse(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_ThirdLayerWithSecondToggleAnimatedOff_NotBaked()
        {
            // 2nd currently off but its toggle is animated, so it can appear at runtime.
            _material.SetFloat("_UseMain3rdTex", 1f);
            var props = new HashSet<string> { "_UseMain2ndTex" };

            var (bake2nd, bake3rd, _) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsFalse(bake2nd);
            Assert.IsFalse(bake3rd);
        }

        [Test]
        public void SelectOverlayLayersToBake_ThirdBlockedByAnimatedSecondToggle_ReportsAnimationVeto()
        {
            // Removing the 2nd-toggle animation would allow the 3rd layer to bake, so the
            // layers-only no-op must be classified as animation-caused.
            _material.SetFloat("_UseMain3rdTex", 1f);
            var props = new HashSet<string> { "_UseMain2ndTex" };

            var (_, _, vetoedByAnimation) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsTrue(vetoedByAnimation);
        }

        [Test]
        public void SelectOverlayLayersToBake_NonWhiteColorWithAnimatedLayerInput_ReportsNoAnimationVeto()
        {
            // The static non-white tint blocks layer baking regardless of the animation, so
            // this no-op is not animation-caused.
            _material.SetColor("_Color", new Color(1f, 0.4f, 0.4f, 1f));
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Main2ndTexAngle" };

            var (_, _, vetoedByAnimation) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsFalse(vetoedByAnimation);
        }

        [Test]
        public void SelectOverlayLayersToBake_AnimatedColorWithBakeableLayer_ReportsAnimationVeto()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Color" };

            var (_, _, vetoedByAnimation) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsTrue(vetoedByAnimation);
        }

        [Test]
        public void SelectOverlayLayersToBake_AnimatedColorWithProtectedLayer_ReportsNoAnimationVeto()
        {
            // Even without the _Color animation the protected layer texture would block the
            // bake, so the no-op is not animation-caused.
            _material.SetFloat("_UseMain2ndTex", 1f);
            _material.SetTexture("_Main2ndTex", _texture);
            var props = new HashSet<string> { "_Color" };

            var (_, _, vetoedByAnimation) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                t => t == _texture
            );

            Assert.IsFalse(vetoedByAnimation);
        }

        [Test]
        public void SelectOverlayLayersToBake_AnimatedLayerInput_ReportsAnimationVeto()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Main2ndTexAngle" };

            var (_, _, vetoedByAnimation) = LilToonTextureBaker.SelectOverlayLayersToBake(
                _material,
                props,
                null
            );

            Assert.IsTrue(vetoedByAnimation);
        }

        #endregion

        #region HasBakeableAlphaMask

        [Test]
        public void HasBakeableAlphaMask_NullMaterial_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(null));
        }

        [Test]
        public void HasBakeableAlphaMask_DefaultMaterial_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(_material));
        }

        [Test]
        public void HasBakeableAlphaMask_ModeWithoutMask_ReturnsFalse()
        {
            _material.SetFloat("_AlphaMaskMode", 1f);
            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(_material));
        }

        [Test]
        public void HasBakeableAlphaMask_MaskWithModeOff_ReturnsFalse()
        {
            _material.SetTexture("_AlphaMask", _texture);
            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(_material));
        }

        [Test]
        public void HasBakeableAlphaMask_ModeAndMask_ReturnsTrue()
        {
            _material.SetFloat("_AlphaMaskMode", 1f);
            _material.SetTexture("_AlphaMask", _texture);
            Assert.IsTrue(LilToonTextureBaker.HasBakeableAlphaMask(_material));
        }

        [Test]
        public void HasBakeableAlphaMask_NonDefaultMaskTiling_ReturnsFalse()
        {
            _material.SetFloat("_AlphaMaskMode", 1f);
            _material.SetTexture("_AlphaMask", _texture);
            _material.SetTextureScale("_AlphaMask", new Vector2(2f, 2f));
            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(_material));
        }

        [Test]
        public void HasBakeableAlphaMask_ShaderWithoutAlphaMask_ReturnsFalse()
        {
            var shader = Shader.Find(SlotTestShaderName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(material));

            UnityEngine.Object.DestroyImmediate(material);
        }

        #endregion

        #region HasAnimatedAlphaMaskBakeInput

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_EmptySet_ReturnsFalse()
        {
            Assert.IsFalse(
                LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(Array.Empty<string>())
            );
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedMask_ReturnsTrue()
        {
            var props = new HashSet<string> { "_AlphaMask" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(props));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedMode_ReturnsTrue()
        {
            var props = new HashSet<string> { "_AlphaMaskMode" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(props));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_UnrelatedProperty_ReturnsFalse()
        {
            var props = new HashSet<string> { "_Color", "_UseEmission" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(props));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedScale_ReturnsTrue()
        {
            var props = new HashSet<string> { "_AlphaMaskScale" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(props));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedValue_ReturnsTrue()
        {
            var props = new HashSet<string> { "_AlphaMaskValue" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(props));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedMainTex_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainTex" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(props));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedMaskSt_ReturnsTrue()
        {
            var props = new HashSet<string> { "_AlphaMask_ST" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(props));
        }

        #endregion

        #region HasBakeableOutline

        [Test]
        public void HasBakeableOutline_NullMaterial_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(null));
        }

        [Test]
        public void HasBakeableOutline_DefaultMaterial_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(_material));
        }

        [Test]
        public void HasBakeableOutline_TextureWithDefaultHsvg_ReturnsFalse()
        {
            _material.SetTexture("_OutlineTex", _texture);
            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(_material));
        }

        [Test]
        public void HasBakeableOutline_HsvgWithoutTexture_ReturnsFalse()
        {
            _material.SetVector("_OutlineTexHSVG", new Vector4(0.5f, 1f, 1f, 1f));
            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(_material));
        }

        [Test]
        public void HasBakeableOutline_TextureAndHsvg_ReturnsTrue()
        {
            _material.SetTexture("_OutlineTex", _texture);
            _material.SetVector("_OutlineTexHSVG", new Vector4(0.5f, 1f, 1f, 1f));
            Assert.IsTrue(LilToonTextureBaker.HasBakeableOutline(_material));
        }

        [Test]
        public void HasBakeableOutline_ShaderWithoutOutline_ReturnsFalse()
        {
            var shader = Shader.Find(SlotTestShaderName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(material));

            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void HasBakeableOutline_NonDefaultOutlineTiling_ReturnsFalse()
        {
            _material.SetTexture("_OutlineTex", _texture);
            _material.SetVector("_OutlineTexHSVG", new Vector4(0.5f, 1f, 1f, 1f));
            _material.SetTextureScale("_OutlineTex", new Vector2(2f, 2f));
            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(_material));
        }

        [Test]
        public void HasBakeableOutline_NonDefaultOutlineOffset_ReturnsFalse()
        {
            _material.SetTexture("_OutlineTex", _texture);
            _material.SetVector("_OutlineTexHSVG", new Vector4(0.5f, 1f, 1f, 1f));
            _material.SetTextureOffset("_OutlineTex", new Vector2(0.5f, 0f));
            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(_material));
        }

        #endregion

        #region HasAnimatedOutlineBakeInput

        [Test]
        public void HasAnimatedOutlineBakeInput_EmptySet_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedOutlineBakeInput(Array.Empty<string>()));
        }

        [Test]
        public void HasAnimatedOutlineBakeInput_AnimatedHsvg_ReturnsTrue()
        {
            var props = new HashSet<string> { "_OutlineTexHSVG" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedOutlineBakeInput(props));
        }

        [Test]
        public void HasAnimatedOutlineBakeInput_AnimatedTexture_ReturnsTrue()
        {
            var props = new HashSet<string> { "_OutlineTex" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedOutlineBakeInput(props));
        }

        [Test]
        public void HasAnimatedOutlineBakeInput_OutlineColor_ReturnsFalse()
        {
            var props = new HashSet<string> { "_OutlineColor" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedOutlineBakeInput(props));
        }

        #endregion

        #region Orphaned Bake Cleanup

        [Test]
        public void CollectLiveTextures_ReferencedTexture_IsLive()
        {
            _material.SetTexture("_MainTex", _texture);

            var live = LilToonTextureBaker.CollectLiveTextures(new[] { _material });

            Assert.IsTrue(live.Contains(_texture));
        }

        [Test]
        public void CollectLiveTextures_UnreferencedTexture_IsNotLive()
        {
            var live = LilToonTextureBaker.CollectLiveTextures(new[] { _material });

            Assert.IsFalse(live.Contains(_texture));
        }

        [Test]
        public void CollectLiveTextures_NullMaterial_IsIgnored()
        {
            var live = LilToonTextureBaker.CollectLiveTextures(new Material[] { null });

            Assert.AreEqual(0, live.Count);
        }

        [Test]
        public void DestroyOrphanedBakes_EmptyCache_DoesNotThrow()
        {
            var baker = new LilToonTextureBaker();
            Assert.DoesNotThrow(() => baker.DestroyOrphanedBakes(new[] { _material }));
        }

        #endregion
    }
}
