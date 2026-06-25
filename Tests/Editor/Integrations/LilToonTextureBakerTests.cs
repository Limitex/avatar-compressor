using dev.limitex.avatar.compressor.editor.texture;
using dev.limitex.avatar.compressor.editor.texture.integrations;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class LilToonTextureBakerTests
    {
        // These tests must hold whether or not lilToon is installed in the project, since lilToon
        // is an optional external dependency. Behaviour that depends on lilToon being absent is
        // guarded with Assume so the suite stays green in projects that do have it. The static
        // bake decisions (no-op detection, animation veto) are testable either way through a test
        // shader declaring the same properties.

        private const string BakeTestShaderName = "Hidden/LAC/Tests/LilToonBake";

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
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_texture);
        }

        [Test]
        public void Constructor_DoesNotThrow_RegardlessOfLilToonPresence()
        {
            Assert.DoesNotThrow(() => new LilToonTextureBaker());
        }

        [Test]
        public void Bake_DoesNotThrow_OnNullMaterial()
        {
            var baker = new LilToonTextureBaker();
            Assert.DoesNotThrow(() => baker.Bake(null, AnimationUsageMap.Empty, null));
        }

        [Test]
        public void Bake_IsNoOp_WhenLilToonNotInstalled()
        {
            var baker = new LilToonTextureBaker();
            Assume.That(
                baker.IsAvailable,
                Is.False,
                "lilToon is installed in this project; skipping the absent-package no-op check."
            );

            _material.SetTexture("_MainTex", _texture);
            _material.SetVector("_MainTexHSVG", new Vector4(0.5f, 1f, 1f, 1f));

            var result = baker.Bake(_material, AnimationUsageMap.Empty, null);

            Assert.AreEqual(0, result.BakedSlots);
            Assert.AreEqual(0, result.SkippedByAnimation);
            Assert.That(_material.GetTexture("_MainTex"), Is.EqualTo(_texture));
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
        public void HasBakeableColorAdjustments_MainColorOnly_ReturnsFalse()
        {
            // The main color is never baked (it stays a runtime tint), so a tinted but otherwise
            // unadjusted material has nothing to bake.
            _material.SetColor("_Color", Color.red);
            Assert.IsFalse(LilToonTextureBaker.HasBakeableColorAdjustments(_material));
        }

        [Test]
        public void HasBakeableColorAdjustments_ShaderWithoutHsvg_ReturnsFalse()
        {
            // E.g. lilToon Lite: passes the lilToon shader check but has no color adjustment.
            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasBakeableColorAdjustments(material));

            Object.DestroyImmediate(material);
        }

        #endregion

        #region HasAnimatedMainBakeInput

        [Test]
        public void HasAnimatedMainBakeInput_EmptyMap_ReturnsFalse()
        {
            _material.SetVector("_MainTexHSVG", new Vector4(0.5f, 1f, 1f, 1f));
            Assert.IsFalse(
                LilToonTextureBaker.HasAnimatedMainBakeInput(_material, AnimationUsageMap.Empty)
            );
        }

        [Test]
        public void HasAnimatedMainBakeInput_UnrelatedAnimatedProperty_ReturnsFalse()
        {
            var map = new AnimationUsageMap(new[] { "_UseEmission", "_Color" });
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, map));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedHsvg_ReturnsTrue()
        {
            var map = new AnimationUsageMap(new[] { "_MainTexHSVG" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, map));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedMainTexture_ReturnsTrue()
        {
            var map = new AnimationUsageMap(new[] { "_MainTex" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, map));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedLayerToggle_ReturnsTrue_EvenWhileLayerOff()
        {
            // An animated toggle means the layer can become visible at runtime; the bake is
            // vetoed even though the layer is currently disabled.
            var map = new AnimationUsageMap(new[] { "_UseMain2ndTex" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, map));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedLayerParameter_ReturnsFalse_WhileLayerOff()
        {
            // A disabled, non-animated layer can never become visible, so its animated
            // parameters are not inputs of this bake.
            var map = new AnimationUsageMap(new[] { "_Color2nd" });
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, map));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedLayerParameter_ReturnsTrue_WhileLayerOn()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var map = new AnimationUsageMap(new[] { "_Color2nd" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, map));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedLayerTexture_ReturnsTrue_WhileLayerOn()
        {
            _material.SetFloat("_UseMain3rdTex", 1f);
            var map = new AnimationUsageMap(new[] { "_Main3rdTex" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, map));
        }

        #endregion

        #region HasBakeableAlphaMask

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
            // Mode 0 = off: the runtime ignores the mask, so baking it would change the look.
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
            // The bake samples the mask without its ST (mirroring lilToon's bake button), so a
            // tiled mask would bake differently from how it renders and must be left alone.
            _material.SetFloat("_AlphaMaskMode", 1f);
            _material.SetTexture("_AlphaMask", _texture);
            _material.SetTextureScale("_AlphaMask", new Vector2(2f, 2f));
            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(_material));
        }

        [Test]
        public void HasBakeableAlphaMask_ShaderWithoutAlphaMask_ReturnsFalse()
        {
            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasBakeableAlphaMask(material));

            Object.DestroyImmediate(material);
        }

        #endregion

        #region HasAnimatedAlphaMaskBakeInput

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_EmptyMap_ReturnsFalse()
        {
            Assert.IsFalse(
                LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(AnimationUsageMap.Empty)
            );
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedMask_ReturnsTrue()
        {
            var map = new AnimationUsageMap(new[] { "_AlphaMask" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(map));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_AnimatedMode_ReturnsTrue()
        {
            var map = new AnimationUsageMap(new[] { "_AlphaMaskMode" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(map));
        }

        [Test]
        public void HasAnimatedAlphaMaskBakeInput_UnrelatedProperty_ReturnsFalse()
        {
            var map = new AnimationUsageMap(new[] { "_Color", "_UseEmission" });
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedAlphaMaskBakeInput(map));
        }

        #endregion

        #region HasBakeableOutline

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
            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);

            Assert.IsFalse(LilToonTextureBaker.HasBakeableOutline(material));

            Object.DestroyImmediate(material);
        }

        #endregion

        #region HasAnimatedOutlineBakeInput

        [Test]
        public void HasAnimatedOutlineBakeInput_EmptyMap_ReturnsFalse()
        {
            Assert.IsFalse(
                LilToonTextureBaker.HasAnimatedOutlineBakeInput(AnimationUsageMap.Empty)
            );
        }

        [Test]
        public void HasAnimatedOutlineBakeInput_AnimatedHsvg_ReturnsTrue()
        {
            var map = new AnimationUsageMap(new[] { "_OutlineTexHSVG" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedOutlineBakeInput(map));
        }

        [Test]
        public void HasAnimatedOutlineBakeInput_AnimatedTexture_ReturnsTrue()
        {
            var map = new AnimationUsageMap(new[] { "_OutlineTex" });
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedOutlineBakeInput(map));
        }

        [Test]
        public void HasAnimatedOutlineBakeInput_OutlineColor_ReturnsFalse()
        {
            // _OutlineColor stays a runtime tint (never baked), so its animation is irrelevant.
            var map = new AnimationUsageMap(new[] { "_OutlineColor" });
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedOutlineBakeInput(map));
        }

        #endregion
    }
}
