using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal class UnusedSlotPrunerTests
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

        #region Clear / drop behavior

        [Test]
        public void Prune_ClearsSlot_AndCountsDroppedTexture()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(optimizer, NoAnimation, new[] { material });

            Assert.That(material.GetTexture("_EmissionMap"), Is.Null);
            Assert.That(result.ClearedSlots, Is.EqualTo(1));
            Assert.That(result.DroppedTextures, Is.EqualTo(1));
        }

        [Test]
        public void Prune_DoesNotCountDropped_WhenAnotherSlotStillBindsTexture()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_EmissionMap", texture);

            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(optimizer, NoAnimation, new[] { material });

            Assert.That(material.GetTexture("_MainTex"), Is.EqualTo(texture));
            Assert.That(material.GetTexture("_EmissionMap"), Is.Null);
            Assert.That(result.ClearedSlots, Is.EqualTo(1));
            Assert.That(result.DroppedTextures, Is.EqualTo(0));
        }

        [Test]
        public void Prune_DoesNotCountDropped_WhenAnotherMaterialStillBindsTexture()
        {
            var materialA = CreateMaterial();
            var materialB = CreateMaterial();
            var texture = CreateTexture();
            materialA.SetTexture("_EmissionMap", texture);
            materialB.SetTexture("_MainTex", texture);

            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(
                optimizer,
                NoAnimation,
                new[] { materialA, materialB }
            );

            Assert.That(materialA.GetTexture("_EmissionMap"), Is.Null);
            Assert.That(materialB.GetTexture("_MainTex"), Is.EqualTo(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(1));
            Assert.That(
                result.DroppedTextures,
                Is.EqualTo(0),
                "Texture still bound on material B must not count as dropped"
            );
        }

        [Test]
        public void Prune_CountsNothing_WhenOptimizerClearsNothing()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var optimizer = new FakeOptimizer(available: true);

            var result = UnusedSlotPruner.Prune(optimizer, NoAnimation, new[] { material });

            Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(0));
            Assert.That(result.DroppedTextures, Is.EqualTo(0));
        }

        #endregion

        #region Protected slot restoration

        [Test]
        public void Prune_RestoresSlot_WhenTextureIsProtected()
        {
            // A frozen texture is an explicit user pin: the optimizer may clear its slot, but the
            // pruner must put it back so the texture survives collection and the upload.
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(
                optimizer,
                NoAnimation,
                new[] { material },
                isProtectedTexture: t => t == texture
            );

            Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(0));
            Assert.That(result.DroppedTextures, Is.EqualTo(0));
        }

        [Test]
        public void Prune_RestoresSlot_WhenTextureIsAnimationReferenced()
        {
            // A texture referenced by an animation PPtr curve ships with the avatar regardless of
            // slot state; restoring the slot keeps it collectable so it is compressed and the
            // curve gets rewritten, instead of shipping the original untouched.
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var usageMap = new AnimationUsageMap(
                new[] { "_SomeProp" },
                animatedTextures: new[] { texture }
            );
            var optimizer = new FakeOptimizer(available: true, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(optimizer, usageMap, new[] { material });

            Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(0));
            Assert.That(result.DroppedTextures, Is.EqualTo(0));
        }

        [Test]
        public void Prune_RestoresOnlyProtectedSlots_NotOthers()
        {
            var material = CreateMaterial();
            var protectedTexture = CreateTexture();
            var unprotectedTexture = CreateTexture();
            material.SetTexture("_EmissionMap", protectedTexture);
            material.SetTexture("_BumpMap", unprotectedTexture);

            var optimizer = new FakeOptimizer(available: true, "_EmissionMap", "_BumpMap");

            var result = UnusedSlotPruner.Prune(
                optimizer,
                NoAnimation,
                new[] { material },
                isProtectedTexture: t => t == protectedTexture
            );

            Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(protectedTexture));
            Assert.That(material.GetTexture("_BumpMap"), Is.Null);
            Assert.That(result.ClearedSlots, Is.EqualTo(1));
            Assert.That(result.DroppedTextures, Is.EqualTo(1));
        }

        #endregion

        #region Availability / guards

        [Test]
        public void Prune_NoOp_WhenOptimizerUnavailable()
        {
            var material = CreateMaterial();
            var texture = CreateTexture();
            material.SetTexture("_EmissionMap", texture);

            var optimizer = new FakeOptimizer(available: false, "_EmissionMap");

            var result = UnusedSlotPruner.Prune(optimizer, NoAnimation, new[] { material });

            Assert.That(optimizer.CallCount, Is.EqualTo(0));
            Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
            Assert.That(result.ClearedSlots, Is.EqualTo(0));
        }

        [Test]
        public void Prune_DoesNotThrow_OnNullArguments()
        {
            var material = CreateMaterial();

            Assert.DoesNotThrow(() =>
                UnusedSlotPruner.Prune(null, NoAnimation, new[] { material })
            );
            Assert.DoesNotThrow(() =>
                UnusedSlotPruner.Prune(new FakeOptimizer(true), null, new[] { material })
            );
            Assert.DoesNotThrow(() =>
                UnusedSlotPruner.Prune(new FakeOptimizer(true), NoAnimation, null)
            );
        }

        [Test]
        public void Prune_ForwardsAnimatedProperties_ToOptimizer()
        {
            // The optimizer owns the animation veto, so the pruner must hand it the animated
            // property set untouched — losing it here would silently break animated toggles.
            var material = CreateMaterial();
            var usageMap = new AnimationUsageMap(new[] { "_UseEmission", "_MainTex" });
            var optimizer = new FakeOptimizer(available: true);

            UnusedSlotPruner.Prune(optimizer, usageMap, new[] { material });

            Assert.That(
                optimizer.LastAnimatedProperties,
                Is.EquivalentTo(new[] { "_UseEmission", "_MainTex" })
            );
        }

        [Test]
        public void Prune_CallsOptimizerOncePerDistinctMaterial()
        {
            var material = CreateMaterial();
            var optimizer = new FakeOptimizer(available: true);

            UnusedSlotPruner.Prune(optimizer, NoAnimation, new[] { material, material, null });

            Assert.That(optimizer.CallCount, Is.EqualTo(1));
        }

        #endregion

        #region Helpers

        private static AnimationUsageMap NoAnimation => new AnimationUsageMap(new string[0]);

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
