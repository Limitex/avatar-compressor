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

        // lilToon keeps _AudioLinkMask only for static UV mode 3, but the shader samples it in
        // modes 3 AND 4 (Spectrum Mask), and an animated _AudioLinkUVMode is treated as a reason
        // to CLEAR the mask — the inverse of its protective animatedProps handling everywhere
        // else. The guard below decides when the wrapper must preserve the mask across the
        // lilToon call. It is testable without lilToon.
        #region ShouldPreserveAudioLinkMask

        [Test]
        public void ShouldPreserveAudioLinkMask_True_WhenUvModeAnimatedAndToggleOn()
        {
            WithAudioLinkMaterial(
                useAudioLink: 1f,
                uvMode: 3f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                            material,
                            new[] { "_AudioLinkUVMode" }
                        ),
                        Is.True
                    )
            );
        }

        [Test]
        public void ShouldPreserveAudioLinkMask_True_WhenUvModeAndToggleBothAnimated()
        {
            WithAudioLinkMaterial(
                useAudioLink: 0f,
                uvMode: 3f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                            material,
                            new[] { "_AudioLinkUVMode", "_UseAudioLink" }
                        ),
                        Is.True
                    )
            );
        }

        [Test]
        public void ShouldPreserveAudioLinkMask_True_WhenStaticUvModeIsSpectrumMask()
        {
            // lilToon clears the mask whenever the static mode is not exactly 3, but mode 4
            // (Spectrum Mask) samples the mask too — no animation involved at all.
            WithAudioLinkMaterial(
                useAudioLink: 1f,
                uvMode: 4f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                            material,
                            new string[0]
                        ),
                        Is.True
                    )
            );
        }

        [Test]
        public void ShouldPreserveAudioLinkMask_False_WhenStaticUvModeDoesNotUseMask()
        {
            // Static non-mask mode (e.g. 0 = Rim) and nothing animated: the mask is genuinely
            // unused, lilToon clears it, and the guard must not interfere.
            WithAudioLinkMaterial(
                useAudioLink: 1f,
                uvMode: 0f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                            material,
                            new[] { "_UseEmission" }
                        ),
                        Is.False
                    )
            );
        }

        [Test]
        public void ShouldPreserveAudioLinkMask_False_WhenStaticUvModeIsMaskMode()
        {
            // Static mode 3 needs no preservation: lilToon itself keeps the mask for it.
            WithAudioLinkMaterial(
                useAudioLink: 1f,
                uvMode: 3f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                            material,
                            new[] { "_UseEmission" }
                        ),
                        Is.False
                    )
            );
        }

        [Test]
        public void ShouldPreserveAudioLinkMask_False_WhenFeatureCannotBeActive()
        {
            // Toggle statically off and not animated: lilToon clears the mask legitimately,
            // even in spectrum-mask mode.
            WithAudioLinkMaterial(
                useAudioLink: 0f,
                uvMode: 4f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                            material,
                            new[] { "_AudioLinkUVMode" }
                        ),
                        Is.False
                    )
            );
        }

        [Test]
        public void ShouldPreserveAudioLinkMask_False_WhenShaderLacksAudioLinkProperties()
        {
            var shader = Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(
                    LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                        material,
                        new[] { "_AudioLinkUVMode" }
                    ),
                    Is.False
                );
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ShouldPreserveAudioLinkMask_HandlesNullArguments()
        {
            Assert.That(
                LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(
                    null,
                    new[] { "_AudioLinkUVMode" }
                ),
                Is.False
            );

            // A null animated array must not disable the static spectrum-mask preservation.
            WithAudioLinkMaterial(
                useAudioLink: 1f,
                uvMode: 4f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(material, null),
                        Is.True
                    )
            );
            WithAudioLinkMaterial(
                useAudioLink: 1f,
                uvMode: 3f,
                material =>
                    Assert.That(
                        LilToonUnusedSlotOptimizer.ShouldPreserveAudioLinkMask(material, null),
                        Is.False
                    )
            );
        }

        private static void WithAudioLinkMaterial(
            float useAudioLink,
            float uvMode,
            System.Action<Material> assert
        )
        {
            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
            Assert.That(shader, Is.Not.Null);

            var material = new Material(shader);
            try
            {
                material.SetFloat("_UseAudioLink", useAudioLink);
                material.SetFloat("_AudioLinkUVMode", uvMode);
                assert(material);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        #endregion
    }
}
