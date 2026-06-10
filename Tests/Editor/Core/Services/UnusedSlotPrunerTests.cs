using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class UnusedSlotPrunerTests
    {
        private const string TestShaderName = "Hidden/LAC/Tests/UnusedSlot";

        private List<Object> _createdObjects;
        private Shader _testShader;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<Object>();
            _testShader = Shader.Find(TestShaderName);
            Assert.That(
                _testShader,
                Is.Not.Null,
                $"Test shader '{TestShaderName}' was not found. Ensure UnusedSlotTestShader.shader is imported."
            );
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            _createdObjects.Clear();
        }

        #region Drop / keep behavior

        [Test]
        public void Prune_DropsTexture_WhenItsOnlySlotIsCleared()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var textures = BuildTextures((texture, material, "_EmissionMap"));
            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(
                textures,
                optimizer,
                NoAnimation,
                new[] { material }
            );

            Assert.IsFalse(textures.ContainsKey(texture));
            Assert.That(result.DroppedTextures, Is.EqualTo(1));
            Assert.That(result.ClearedSlots, Is.EqualTo(1));
            Assert.That(material.GetTexture("_EmissionMap"), Is.Null);
        }

        [Test]
        public void Prune_KeepsTexture_WhenAnotherSlotStillReferencesIt()
        {
            // Same texture bound to both _MainTex (kept) and _EmissionMap (cleared).
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_EmissionMap", texture);

            var textures = BuildTextures(
                (texture, material, "_MainTex"),
                (texture, material, "_EmissionMap")
            );
            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(
                textures,
                optimizer,
                NoAnimation,
                new[] { material }
            );

            Assert.IsTrue(textures.ContainsKey(texture));
            Assert.That(textures[texture].References, Has.Count.EqualTo(1));
            Assert.That(textures[texture].References[0].PropertyName, Is.EqualTo("_MainTex"));
            Assert.That(result.DroppedTextures, Is.EqualTo(0));
            Assert.That(result.ClearedSlots, Is.EqualTo(1));
            Assert.That(material.GetTexture("_MainTex"), Is.EqualTo(texture));
        }

        [Test]
        public void Prune_KeepsEverything_WhenOptimizerClearsNothing()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var textures = BuildTextures((texture, material, "_EmissionMap"));
            var optimizer = new FakeOptimizer(available: true); // clears nothing

            var result = UnusedSlotPruner.Prune(
                textures,
                optimizer,
                NoAnimation,
                new[] { material }
            );

            Assert.IsTrue(textures.ContainsKey(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(0));
            Assert.That(result.DroppedTextures, Is.EqualTo(0));
        }

        [Test]
        public void Prune_DoesNotCountStaleNullMaterialReference_AsCleared()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_MainTex", texture);

            var textures = BuildTextures((texture, material, "_MainTex"));
            // Stale reference whose material was destroyed: dropped silently, never counted as a
            // slot the optimizer cleared.
            textures[texture]
                .References.Add(
                    new MaterialTextureReference { Material = null, PropertyName = "_EmissionMap" }
                );
            var optimizer = new FakeOptimizer(available: true); // clears nothing

            var result = UnusedSlotPruner.Prune(
                textures,
                optimizer,
                NoAnimation,
                new[] { material }
            );

            Assert.That(result.ClearedSlots, Is.EqualTo(0));
            Assert.That(textures[texture].References, Has.Count.EqualTo(1));
            Assert.That(result.DroppedTextures, Is.EqualTo(0));
        }

        #endregion

        #region Coverage

        [Test]
        public void Prune_OptimizesMaterial_EvenWhenNoneOfItsTexturesAreTracked()
        {
            // Regression guard: step 1 must visit every cloned material, not only those reachable
            // from the collected textures. Here the material's texture was filtered out, so the
            // textures dict is empty, yet the slot must still be cleared.
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            UnusedSlotPruner.Prune(textures, optimizer, NoAnimation, new[] { material });

            Assert.That(optimizer.CallCount, Is.EqualTo(1));
            Assert.That(material.GetTexture("_EmissionMap"), Is.Null);
        }

        #endregion

        #region Availability / guards

        [Test]
        public void Prune_NoOp_WhenOptimizerUnavailable()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var textures = BuildTextures((texture, material, "_EmissionMap"));
            var optimizer = new FakeOptimizer(available: false, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(
                textures,
                optimizer,
                NoAnimation,
                new[] { material }
            );

            Assert.IsTrue(textures.ContainsKey(texture));
            Assert.That(optimizer.CallCount, Is.EqualTo(0));
            Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(0));
        }

        [Test]
        public void Prune_DoesNotThrow_WhenTexturesOrOptimizerNull()
        {
            Assert.DoesNotThrow(() =>
                UnusedSlotPruner.Prune(null, new FakeOptimizer(true), NoAnimation, NoMaterials)
            );
            Assert.DoesNotThrow(() =>
                UnusedSlotPruner.Prune(
                    new Dictionary<Texture2D, TextureInfo>(),
                    null,
                    NoAnimation,
                    NoMaterials
                )
            );
        }

        [Test]
        public void Prune_DoesNotThrow_WhenMaterialsNull()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);
            var textures = BuildTextures((texture, material, "_EmissionMap"));

            // Nothing to clear without a material list, so the texture survives untouched.
            var result = UnusedSlotPruner.Prune(
                textures,
                new FakeOptimizer(true, "_EmissionMap"),
                NoAnimation,
                null
            );

            Assert.IsTrue(textures.ContainsKey(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(0));
        }

        [Test]
        public void Prune_ForwardsAnimatedProperties_ToOptimizer()
        {
            // The optimizer owns the animation veto, so the pruner must hand it the animated
            // property set untouched — losing it here would silently break animated toggles.
            var material = CreateMaterial();
            var animated = new[] { "_UseEmission", "_MainTex" };
            var optimizer = new FakeOptimizer(available: true);

            UnusedSlotPruner.Prune(
                new Dictionary<Texture2D, TextureInfo>(),
                optimizer,
                animated,
                new[] { material }
            );

            Assert.That(optimizer.LastAnimatedProperties, Is.SameAs(animated));
        }

        [Test]
        public void Prune_CallsOptimizerOncePerDistinctMaterial()
        {
            var material = CreateMaterial();
            var texA = CreateTexture();
            var texB = CreateTexture();
            material.SetTexture("_MainTex", texA);
            material.SetTexture("_EmissionMap", texB);

            var textures = BuildTextures(
                (texA, material, "_MainTex"),
                (texB, material, "_EmissionMap")
            );
            var optimizer = new FakeOptimizer(available: true);

            // Same material passed twice => still optimized exactly once.
            UnusedSlotPruner.Prune(textures, optimizer, NoAnimation, new[] { material, material });

            Assert.That(optimizer.CallCount, Is.EqualTo(1));
        }

        #endregion

        #region Helpers

        private static readonly IReadOnlyCollection<string> NoAnimation = new string[0];
        private static readonly Material[] NoMaterials = new Material[0];

        private Material CreateMaterial()
        {
            var material = new Material(_testShader);
            _createdObjects.Add(material);
            return material;
        }

        private Texture2D CreateTexture()
        {
            var texture = new Texture2D(4, 4);
            _createdObjects.Add(texture);
            return texture;
        }

        private static Dictionary<Texture2D, TextureInfo> BuildTextures(
            params (Texture2D Texture, Material Material, string Property)[] references
        )
        {
            var textures = new Dictionary<Texture2D, TextureInfo>();
            foreach (var (texture, material, property) in references)
            {
                if (!textures.TryGetValue(texture, out var info))
                {
                    info = new TextureInfo();
                    textures[texture] = info;
                }

                info.References.Add(
                    new MaterialTextureReference { Material = material, PropertyName = property }
                );
            }
            return textures;
        }

        // Optimizer test double: nulls the configured properties on any material it is given.
        private sealed class FakeOptimizer : IUnusedSlotOptimizer
        {
            private readonly bool _available;
            private readonly string[] _clearProps;

            public FakeOptimizer(bool available, params string[] clearProps)
            {
                _available = available;
                _clearProps = clearProps;
            }

            public int CallCount { get; private set; }

            public IReadOnlyCollection<string> LastAnimatedProperties { get; private set; }

            public bool IsAvailable => _available;

            public void ClearUnusedSlots(
                Material material,
                IReadOnlyCollection<string> animatedProperties
            )
            {
                CallCount++;
                LastAnimatedProperties = animatedProperties;
                if (material == null)
                    return;

                foreach (var prop in _clearProps)
                {
                    if (material.HasProperty(prop))
                        material.SetTexture(prop, null);
                }
            }
        }

        #endregion
    }
}
