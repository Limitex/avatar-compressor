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
        public void Bake_ReturnsEmpty_WhenLilToonNotInstalled()
        {
            var baker = new LilToonTextureBaker();
            Assume.That(
                baker.IsAvailable,
                Is.False,
                "lilToon is installed in this project; skipping the absent-package no-op check."
            );

            var material = new Material(Shader.Find("Standard"));
            var result = baker.Bake(material, Array.Empty<string>());

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);

            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void Bake_ReturnsEmpty_OnNullMaterial()
        {
            var baker = new LilToonTextureBaker();
            var result = baker.Bake(null, Array.Empty<string>());

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void Bake_ReturnsEmpty_OnNullShader()
        {
            var baker = new LilToonTextureBaker();
            var material = new Material(Shader.Find("Standard"));
            material.shader = null;

            var result = baker.Bake(material, Array.Empty<string>());

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);

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
            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
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
            Assert.IsFalse(
                LilToonTextureBaker.HasAnimatedMainBakeInput(
                    _material,
                    Array.Empty<string>()
                )
            );
        }

        [Test]
        public void HasAnimatedMainBakeInput_UnrelatedAnimatedProperty_ReturnsFalse()
        {
            var props = new HashSet<string> { "_UseEmission", "_Color" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedHsvg_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainTexHSVG" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedMainTexture_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainTex" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedLayerToggle_ReturnsFalse()
        {
            var props = new HashSet<string> { "_UseMain2ndTex" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedLayerParameter_ReturnsFalse()
        {
            _material.SetFloat("_UseMain2ndTex", 1f);
            var props = new HashSet<string> { "_Color2nd" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedThirdLayerToggle_ReturnsFalse()
        {
            var props = new HashSet<string> { "_UseMain3rdTex" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedGradationStrength_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainGradationStrength" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedGradationTex_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainGradationTex" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_AnimatedColorAdjustMask_ReturnsTrue()
        {
            var props = new HashSet<string> { "_MainColorAdjustMask" };
            Assert.IsTrue(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_NullMaterial_ReturnsFalse()
        {
            var props = new HashSet<string> { "_MainTexHSVG" };
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(null, props));
        }

        [Test]
        public void HasAnimatedMainBakeInput_NullProperties_ReturnsFalse()
        {
            Assert.IsFalse(LilToonTextureBaker.HasAnimatedMainBakeInput(_material, null));
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
            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
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
            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
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
            Assert.IsFalse(
                LilToonTextureBaker.HasAnimatedOutlineBakeInput(Array.Empty<string>())
            );
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
    }
}
