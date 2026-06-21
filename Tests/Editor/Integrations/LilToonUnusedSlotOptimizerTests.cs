using dev.limitex.avatar.compressor.editor.texture.integrations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class LilToonUnusedSlotOptimizerTests
    {
        // These tests must hold whether or not lilToon is installed in the project, since lilToon is
        // an optional external dependency. Tests that exercise the real lilToon API are guarded with
        // Assume so the suite stays green (inconclusive) where it is absent; the rest are lilToon-agnostic.

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

        // These tests exercise the real lilToon API through the reflection bridge, so they only run
        // when lilToon is installed (the CI test project pins it via vpm-manifest.json). They pin
        // the external behavior the integration depends on: toggle-gated clearing, the protective
        // animatedProps handling, the shader-name self-guard, and the _AudioLinkMask quirk the
        // wrapper compensates for.
        #region Real lilToon behavior (requires lilToon installed)

        [Test]
        public void ClearUnusedSlots_OnLilToonMaterial_ClearsSlot_WhenToggleOff()
        {
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterial(
                (material, texture) =>
                {
                    material.SetFloat("_UseEmission", 0f);
                    material.SetTexture("_EmissionMap", texture);

                    optimizer.ClearUnusedSlots(material, new string[0]);

                    Assert.That(material.GetTexture("_EmissionMap"), Is.Null);
                }
            );
        }

        [Test]
        public void ClearUnusedSlots_OnLilToonMaterial_KeepsSlot_WhenToggleOn()
        {
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterial(
                (material, texture) =>
                {
                    material.SetFloat("_UseEmission", 1f);
                    material.SetTexture("_EmissionMap", texture);

                    optimizer.ClearUnusedSlots(material, new string[0]);

                    Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
                }
            );
        }

        [Test]
        public void ClearUnusedSlots_OnLilToonMaterial_KeepsSlot_WhenToggleAnimated()
        {
            // A feature toggled on by a menu animation must survive even though it is
            // statically off — this is the protective animatedProps path in lilToon itself.
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterial(
                (material, texture) =>
                {
                    material.SetFloat("_UseEmission", 0f);
                    material.SetTexture("_EmissionMap", texture);

                    optimizer.ClearUnusedSlots(material, new[] { "_UseEmission" });

                    Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
                }
            );
        }

        [Test]
        public void ClearUnusedSlots_OnLilToonMaterial_PreservesAudioLinkMask_InSpectrumMaskMode()
        {
            // lilToon clears the mask for static UV mode 4 even though the shader samples it
            // there; the wrapper's preservation guard must win over the real API.
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterial(
                (material, texture) =>
                {
                    material.SetFloat("_UseAudioLink", 1f);
                    material.SetFloat("_AudioLinkUVMode", 4f);
                    material.SetTexture("_AudioLinkMask", texture);

                    optimizer.ClearUnusedSlots(material, new string[0]);

                    Assert.That(material.GetTexture("_AudioLinkMask"), Is.EqualTo(texture));
                }
            );
        }

        [Test]
        public void ClearUnusedSlots_OnLilToonMaterial_PreservesAudioLinkMask_WhenUvModeAnimated()
        {
            // lilToon treats an animated _AudioLinkUVMode as a reason to clear (its one inverted
            // animatedProps case); the wrapper must restore the mask afterwards.
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterial(
                (material, texture) =>
                {
                    material.SetFloat("_UseAudioLink", 1f);
                    material.SetFloat("_AudioLinkUVMode", 3f);
                    material.SetTexture("_AudioLinkMask", texture);

                    optimizer.ClearUnusedSlots(material, new[] { "_AudioLinkUVMode" });

                    Assert.That(material.GetTexture("_AudioLinkMask"), Is.EqualTo(texture));
                }
            );
        }

        [Test]
        public void ClearUnusedSlots_OnLilToonMaterial_KeepsAudioLinkMask_InMaskMode()
        {
            // Static mode 3 is the case lilToon itself keeps; the wrapper must not interfere.
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterial(
                (material, texture) =>
                {
                    material.SetFloat("_UseAudioLink", 1f);
                    material.SetFloat("_AudioLinkUVMode", 3f);
                    material.SetTexture("_AudioLinkMask", texture);

                    optimizer.ClearUnusedSlots(material, new string[0]);

                    Assert.That(material.GetTexture("_AudioLinkMask"), Is.EqualTo(texture));
                }
            );
        }

        [Test]
        public void ClearUnusedSlots_OnLilToonMaterial_ClearsAudioLinkMask_WhenFeatureOff()
        {
            // AudioLink statically off and not animated: the mask is genuinely unused and the
            // wrapper's preservation guard must not keep it alive.
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterial(
                (material, texture) =>
                {
                    material.SetFloat("_UseAudioLink", 0f);
                    material.SetFloat("_AudioLinkUVMode", 4f);
                    material.SetTexture("_AudioLinkMask", texture);

                    optimizer.ClearUnusedSlots(material, new string[0]);

                    Assert.That(material.GetTexture("_AudioLinkMask"), Is.Null);
                }
            );
        }

        [Test]
        public void ClearUnusedSlots_LeavesNonLilToonMaterial_Untouched()
        {
            // RemoveUnusedTexture self-guards on the shader name, which is what makes it safe
            // for the pruner to call the optimizer on every material of the avatar.
            var optimizer = CreateInstalledOptimizer();

            var shader = Shader.Find("Hidden/LAC/Tests/UnusedSlot");
            Assert.That(shader, Is.Not.Null);

            var material = new Material(shader);
            var texture = new Texture2D(4, 4);
            try
            {
                material.SetTexture("_EmissionMap", texture);

                optimizer.ClearUnusedSlots(material, new string[0]);

                Assert.That(material.GetTexture("_EmissionMap"), Is.EqualTo(texture));
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void ClearUnusedSlots_DoesNotTouchOriginalTextureAsset_WhenSlotCleared()
        {
            var optimizer = CreateInstalledOptimizer();
            WithLilToonMaterialAndAssetTexture(
                (material, texture, assetPath, guid) =>
                {
                    material.SetFloat("_UseEmission", 0f);
                    material.SetTexture("_EmissionMap", texture);

                    optimizer.ClearUnusedSlots(material, new string[0]);

                    // Precondition: prove lilToon actually ran, else the asset checks are vacuous.
                    Assert.That(
                        material.GetTexture("_EmissionMap"),
                        Is.Null,
                        "lilToon should clear an off, non-animated slot — otherwise this test proves nothing."
                    );

                    Assert.That(
                        texture == null,
                        Is.False,
                        "Original texture asset must not be destroyed by slot clearing."
                    );
                    Assert.That(
                        AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath),
                        Is.SameAs(texture),
                        "Original texture asset file must still exist and resolve to the same instance."
                    );
                    Assert.That(
                        AssetDatabase.AssetPathToGUID(assetPath),
                        Is.EqualTo(guid),
                        "Original texture asset GUID must be unchanged."
                    );
                }
            );
        }

        private static LilToonUnusedSlotOptimizer CreateInstalledOptimizer()
        {
            var optimizer = new LilToonUnusedSlotOptimizer();
            Assume.That(
                optimizer.IsAvailable,
                Is.True,
                "lilToon is not installed in this project; skipping the real-API integration check."
            );
            return optimizer;
        }

        private static void WithLilToonMaterial(System.Action<Material, Texture2D> test)
        {
            var shader = Shader.Find("lilToon");
            Assert.That(
                shader,
                Is.Not.Null,
                "The lilToon API resolved but the 'lilToon' shader was not found."
            );

            var material = new Material(shader);
            var texture = new Texture2D(4, 4);
            try
            {
                test(material, texture);
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(texture);
            }
        }

        // Like WithLilToonMaterial, but backs the texture with a real on-disk asset (path + GUID)
        // so a test can assert the original asset survives the lilToon call.
        private static void WithLilToonMaterialAndAssetTexture(
            System.Action<Material, Texture2D, string, string> test
        )
        {
            var shader = Shader.Find("lilToon");
            Assert.That(
                shader,
                Is.Not.Null,
                "The lilToon API resolved but the 'lilToon' shader was not found."
            );

            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
                AssetDatabase.CreateFolder("Assets", TestAssetFolderName);

            string assetPath =
                $"{TestAssetFolder}/UnusedSlotAssetGuard_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(new Texture2D(4, 4), assetPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var material = new Material(shader);
            try
            {
                test(material, texture, assetPath, guid);
            }
            finally
            {
                Object.DestroyImmediate(material);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private const string TestAssetFolderName = "_LAC_TMP";
        private const string TestAssetFolder = "Assets/" + TestAssetFolderName;

        #endregion

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
