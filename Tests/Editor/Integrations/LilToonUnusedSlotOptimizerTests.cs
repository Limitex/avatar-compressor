using dev.limitex.avatar.compressor.editor.texture.integrations;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class LilToonUnusedSlotOptimizerTests
    {
        // These tests must hold whether or not lilToon is installed in the project, since lilToon is
        // an optional external dependency. Behaviour that depends on lilToon being absent is guarded
        // with Assume so the suite stays green in projects that do have it.

        [Test]
        public void Constructor_DoesNotThrow_RegardlessOfLilToonPresence()
        {
            Assert.DoesNotThrow(() => new LilToonUnusedSlotOptimizer());
        }

        [Test]
        public void ClearUnusedSlots_DoesNotThrow_OnNullMaterial()
        {
            var optimizer = new LilToonUnusedSlotOptimizer();
            Assert.DoesNotThrow(() => optimizer.ClearUnusedSlots(null, null));
        }

        [Test]
        public void ClearUnusedSlots_IsNoOp_WhenLilToonNotInstalled()
        {
            var optimizer = new LilToonUnusedSlotOptimizer();
            Assume.That(
                optimizer.IsAvailable,
                Is.False,
                "lilToon is installed in this project; skipping the absent-package no-op check."
            );

            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
            Assert.That(shader, Is.Not.Null);

            var material = new Material(shader);
            var texture = new Texture2D(4, 4);
            material.SetTexture("_EmissionMap", texture);

            optimizer.ClearUnusedSlots(material, new[] { "_SomeAnimatedProp" });

            Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));

            Object.DestroyImmediate(material);
            Object.DestroyImmediate(texture);
        }
    }
}
