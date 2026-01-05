using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.common;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    /// <summary>
    /// Tests to verify that the compression process is non-destructive.
    /// After compression, original assets (materials, textures) should remain unchanged.
    /// </summary>
    [TestFixture]
    public class NonDestructiveTests
    {
        private const string CloneSuffix = "_clone";
        private const string CompressedSuffix = "_compressed";

        private List<Object> _createdObjects;
        private List<GameObject> _rootObjects;
        private Shader _standardShader;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<Object>();
            _rootObjects = new List<GameObject>();
            _standardShader = Shader.Find("Standard");
            Assert.IsNotNull(_standardShader, "Standard shader not found. Tests require Unity Editor environment.");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up compressed materials and textures created by the compression service
            foreach (var root in _rootObjects)
            {
                if (root == null) continue;
                CleanupCompressedAssets(root);
            }

            // Clean up all test-created objects
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
            _rootObjects.Clear();
        }

        private void CleanupCompressedAssets(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null || _createdObjects.Contains(mat)) continue;

                    // Destroy compressed textures on cloned materials
                    DestroyCompressedTextures(mat);
                    Object.DestroyImmediate(mat);
                }
            }
        }

        private void DestroyCompressedTextures(Material mat)
        {
            var shader = mat.shader;
            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Texture)
                    continue;

                var tex = mat.GetTexture(shader.GetPropertyName(i));
                if (tex != null && !_createdObjects.Contains(tex))
                {
                    Object.DestroyImmediate(tex);
                }
            }
        }

        #region Material Non-Destructive Tests

        [Test]
        public void Compress_OriginalMaterialInstance_RemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            // Store original material reference
            var textureBeforeCompress = originalMaterial.GetTexture("_MainTex");

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            // Original material instance should still have the original texture
            Assert.AreEqual(textureBeforeCompress, originalMaterial.GetTexture("_MainTex"),
                "Original material's texture reference should not be changed");
        }

        [Test]
        public void Compress_OriginalMaterialShader_RemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalShader = originalMaterial.shader;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual(originalShader, originalMaterial.shader,
                "Original material's shader should not be changed");
        }

        [Test]
        public void Compress_OriginalMaterialColor_RemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.color = new Color(0.5f, 0.25f, 0.75f, 1f);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalColor = originalMaterial.color;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual(originalColor, originalMaterial.color,
                "Original material's color should not be changed");
        }

        [Test]
        public void Compress_OriginalMaterialName_RemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("MyTestMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual("MyTestMaterial", originalMaterial.name,
                "Original material's name should not be changed");
        }

        [Test]
        public void Compress_OriginalMaterialRenderQueue_RemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.renderQueue = 3000;
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalRenderQueue = originalMaterial.renderQueue;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual(originalRenderQueue, originalMaterial.renderQueue,
                "Original material's render queue should not be changed");
        }

        #endregion

        #region Texture Non-Destructive Tests

        [Test]
        public void Compress_OriginalTexturePixels_RemainUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            // Store original pixels
            var originalPixels = originalTexture.GetPixels();

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            // Verify original texture pixels are unchanged
            var pixelsAfterCompress = originalTexture.GetPixels();
            CollectionAssert.AreEqual(originalPixels, pixelsAfterCompress,
                "Original texture pixels should not be changed");
        }

        [Test]
        public void Compress_OriginalTextureDimensions_RemainUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(512, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalWidth = originalTexture.width;
            var originalHeight = originalTexture.height;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual(originalWidth, originalTexture.width,
                "Original texture width should not be changed");
            Assert.AreEqual(originalHeight, originalTexture.height,
                "Original texture height should not be changed");
        }

        [Test]
        public void Compress_OriginalTextureName_RemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalTexture.name = "MyOriginalTexture";
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual("MyOriginalTexture", originalTexture.name,
                "Original texture name should not be changed");
        }

        [Test]
        public void Compress_OriginalTextureFormat_RemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalFormat = originalTexture.format;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual(originalFormat, originalTexture.format,
                "Original texture format should not be changed");
        }

        #endregion

        #region Multiple Textures Non-Destructive Tests

        [Test]
        public void Compress_MultipleTextures_AllOriginalsRemainUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var mainTexture = CreateTexture(256, 256);
            var normalTexture = CreateTexture(256, 256);
            mainTexture.name = "MainTexture";
            normalTexture.name = "NormalTexture";

            originalMaterial.SetTexture("_MainTex", mainTexture);
            originalMaterial.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = originalMaterial;

            // Store original pixels
            var mainPixels = mainTexture.GetPixels();
            var normalPixels = normalTexture.GetPixels();

            service.Compress(root, false);

            // Verify compression was performed
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial,
                "Renderer should use cloned material after compression");

            // Verify all original textures are unchanged
            Assert.AreEqual("MainTexture", mainTexture.name);
            Assert.AreEqual("NormalTexture", normalTexture.name);

            var mainPixelsAfter = mainTexture.GetPixels();
            var normalPixelsAfter = normalTexture.GetPixels();

            CollectionAssert.AreEqual(mainPixels, mainPixelsAfter,
                "Main texture pixels should not be changed");
            CollectionAssert.AreEqual(normalPixels, normalPixelsAfter,
                "Normal texture pixels should not be changed");
        }

        [Test]
        public void Compress_SharedTextureAcrossMaterials_OriginalRemainsUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var renderer1 = child1.AddComponent<MeshRenderer>();
            var renderer2 = child2.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            var material2 = CreateMaterial("Material2");
            var sharedTexture = CreateTexture(256, 256);
            sharedTexture.name = "SharedTexture";

            material1.SetTexture("_MainTex", sharedTexture);
            material2.SetTexture("_MainTex", sharedTexture);
            renderer1.sharedMaterial = material1;
            renderer2.sharedMaterial = material2;

            var originalPixels = sharedTexture.GetPixels();
            var originalWidth = sharedTexture.width;
            var originalHeight = sharedTexture.height;

            service.Compress(root, false);

            // Verify compression was performed on both renderers
            Assert.AreNotSame(material1, renderer1.sharedMaterial,
                "Renderer1 should use cloned material after compression");
            Assert.AreNotSame(material2, renderer2.sharedMaterial,
                "Renderer2 should use cloned material after compression");

            Assert.AreEqual("SharedTexture", sharedTexture.name,
                "Original shared texture name should not be changed");
            Assert.AreEqual(originalWidth, sharedTexture.width,
                "Original shared texture width should not be changed");
            Assert.AreEqual(originalHeight, sharedTexture.height,
                "Original shared texture height should not be changed");
            CollectionAssert.AreEqual(originalPixels, sharedTexture.GetPixels(),
                "Original shared texture pixels should not be changed");
        }

        #endregion

        #region Hierarchy Non-Destructive Tests

        [Test]
        public void Compress_DeepHierarchy_AllOriginalAssetsRemainUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var level1 = CreateGameObject("Level1");
            var level2 = CreateGameObject("Level2");
            var level3 = CreateGameObject("Level3");

            level1.transform.SetParent(root.transform);
            level2.transform.SetParent(level1.transform);
            level3.transform.SetParent(level2.transform);

            var renderer = level3.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("DeepMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();
            var materialTextureRef = originalMaterial.GetTexture("_MainTex");

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            // Original material should still reference original texture
            Assert.AreEqual(materialTextureRef, originalMaterial.GetTexture("_MainTex"),
                "Original material in deep hierarchy should still reference original texture");
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture in deep hierarchy should have unchanged pixels");
        }

        [Test]
        public void Compress_InactiveGameObjects_OriginalAssetsRemainUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var inactiveChild = CreateGameObject("InactiveChild");
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            var renderer = inactiveChild.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("InactiveMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();
            var materialTextureRef = originalMaterial.GetTexture("_MainTex");

            service.Compress(root, false);

            // Verify compression was performed (inactive objects should also be processed)
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual(materialTextureRef, originalMaterial.GetTexture("_MainTex"),
                "Original material on inactive GameObject should still reference original texture");
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture on inactive GameObject should have unchanged pixels");
        }

        #endregion

        #region Post-Compression State Tests

        [Test]
        public void Compress_RendererUsesClonedMaterial_NotOriginal()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            service.Compress(root, false);

            // Renderer should now use a cloned material, not the original
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial,
                "Renderer should use cloned material, not original");
            Assert.That(renderer.sharedMaterial.name, Does.Contain(CloneSuffix),
                $"Cloned material should have '{CloneSuffix}' suffix");
        }

        [Test]
        public void Compress_ClonedMaterialUsesCompressedTexture_NotOriginal()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalTexture.name = "OriginalTexture";
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            service.Compress(root, false);

            var clonedMaterial = renderer.sharedMaterial;
            var textureOnClone = clonedMaterial.GetTexture("_MainTex") as Texture2D;

            Assert.IsNotNull(textureOnClone);
            Assert.AreNotSame(originalTexture, textureOnClone,
                "Cloned material should use compressed texture, not original");
            Assert.That(textureOnClone.name, Does.Contain(CompressedSuffix),
                $"Compressed texture should have '{CompressedSuffix}' suffix");
        }

        [Test]
        public void Compress_OriginalCanBeRestored_ByReassigningToRenderer()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalTexture.name = "OriginalTexture";
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            service.Compress(root, false);

            // Restore original material
            renderer.sharedMaterial = originalMaterial;

            // Verify restoration
            Assert.AreEqual(originalMaterial, renderer.sharedMaterial,
                "Original material should be restorable");
            Assert.AreEqual(originalTexture, renderer.sharedMaterial.GetTexture("_MainTex"),
                "Original texture should be accessible after restoration");
        }

        #endregion

        #region Frozen Texture Non-Destructive Tests

        /// <summary>
        /// Tests that the compression process remains non-destructive when frozen settings exist.
        /// Note: This test uses runtime-created textures which don't have asset paths,
        /// so frozen lookup won't match. This test verifies that the presence of frozen
        /// settings in the config doesn't break the non-destructive behavior.
        /// For full frozen functionality testing, integration tests with actual asset files are needed.
        /// </summary>
        [Test]
        public void Compress_WithFrozenSettingsInConfig_NonDestructiveBehaviorMaintained()
        {
            var config = CreateConfig();
            // Add frozen settings to config (won't match runtime textures due to no asset path)
            config.FrozenTextures.Add(new FrozenTextureSettings
            {
                TexturePath = "Assets/NonExistent/Texture.png",
                Divisor = 2,
                Format = FrozenTextureFormat.DXT5,
                Skip = false
            });
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();

            service.Compress(root, false);

            // Verify compression was performed (frozen settings don't affect this texture)
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            // Original texture should remain unchanged
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture should remain unchanged when frozen settings exist in config");
        }

        /// <summary>
        /// Tests that frozen skip settings don't break the material cloning process.
        /// </summary>
        [Test]
        public void Compress_WithFrozenSkipSettingsInConfig_MaterialStillCloned()
        {
            var config = CreateConfig();
            // Add frozen skip settings
            config.FrozenTextures.Add(new FrozenTextureSettings
            {
                TexturePath = "Assets/NonExistent/SkippedTexture.png",
                Skip = true
            });
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();

            service.Compress(root, false);

            // Material should be cloned regardless of frozen settings
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial,
                "Material should be cloned even with frozen skip settings in config");

            // Original texture should remain unchanged
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture should remain unchanged");
        }

        #endregion

        #region Mixed Renderer Types Non-Destructive Tests

        [Test]
        public void Compress_SkinnedMeshRenderer_OriginalAssetsRemainUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<SkinnedMeshRenderer>();
            var originalMaterial = CreateMaterial("SkinnedMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();
            var materialTextureRef = originalMaterial.GetTexture("_MainTex");

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);

            Assert.AreEqual(materialTextureRef, originalMaterial.GetTexture("_MainTex"),
                "Original material on SkinnedMeshRenderer should still reference original texture");
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture on SkinnedMeshRenderer should have unchanged pixels");
        }

        [Test]
        public void Compress_MixedRendererTypes_AllOriginalAssetsRemainUnchanged()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var meshChild = CreateGameObject("MeshChild");
            var skinnedChild = CreateGameObject("SkinnedChild");
            meshChild.transform.SetParent(root.transform);
            skinnedChild.transform.SetParent(root.transform);

            var meshRenderer = meshChild.AddComponent<MeshRenderer>();
            var skinnedRenderer = skinnedChild.AddComponent<SkinnedMeshRenderer>();

            var meshMaterial = CreateMaterial("MeshMaterial");
            var skinnedMaterial = CreateMaterial("SkinnedMaterial");
            var meshTexture = CreateTexture(256, 256);
            var skinnedTexture = CreateTexture(256, 256);

            meshMaterial.SetTexture("_MainTex", meshTexture);
            skinnedMaterial.SetTexture("_MainTex", skinnedTexture);
            meshRenderer.sharedMaterial = meshMaterial;
            skinnedRenderer.sharedMaterial = skinnedMaterial;

            var meshPixels = meshTexture.GetPixels();
            var skinnedPixels = skinnedTexture.GetPixels();

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(meshRenderer, meshMaterial, meshTexture);
            AssertCompressionWasPerformed(skinnedRenderer, skinnedMaterial, skinnedTexture);

            CollectionAssert.AreEqual(meshPixels, meshTexture.GetPixels(),
                "Original MeshRenderer texture should have unchanged pixels");
            CollectionAssert.AreEqual(skinnedPixels, skinnedTexture.GetPixels(),
                "Original SkinnedMeshRenderer texture should have unchanged pixels");
        }

        #endregion

        #region Preset Non-Destructive Tests

        [TestCase(CompressorPreset.HighQuality)]
        [TestCase(CompressorPreset.Quality)]
        [TestCase(CompressorPreset.Balanced)]
        [TestCase(CompressorPreset.Aggressive)]
        [TestCase(CompressorPreset.Maximum)]
        public void Compress_WithPreset_OriginalAssetsRemainUnchanged(CompressorPreset preset)
        {
            var root = CreateRootGameObject($"Root_{preset}");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial($"Material_{preset}");
            var originalTexture = CreateTexture(256, 256);
            originalTexture.name = $"Texture_{preset}";
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();
            var originalWidth = originalTexture.width;
            var originalHeight = originalTexture.height;

            var config = CreateConfig();
            config.ApplyPreset(preset);
            var service = new TextureCompressorService(config);

            service.Compress(root, false);

            // Verify material cloning was performed (texture compression may be skipped
            // depending on preset's MinSourceSize setting)
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture, verifyTextureCompression: false);

            // Verify original assets are unchanged
            Assert.AreEqual(originalWidth, originalTexture.width,
                $"Original texture width should not change with {preset} preset");
            Assert.AreEqual(originalHeight, originalTexture.height,
                $"Original texture height should not change with {preset} preset");
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                $"Original texture pixels should not change with {preset} preset");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Compress_MaterialWithNullTexture_MaterialClonedWithNullTexture()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("MaterialWithoutTexture");
            // Material has no texture set
            renderer.sharedMaterial = originalMaterial;

            Assert.DoesNotThrow(() => service.Compress(root, false),
                "Compression should not throw when material has no texture");

            // Material should still be cloned
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial,
                "Material should be cloned even without texture");
            Assert.That(renderer.sharedMaterial.name, Does.Contain(CloneSuffix),
                $"Cloned material should have '{CloneSuffix}' suffix");

            // Cloned material should also have null texture
            Assert.IsNull(renderer.sharedMaterial.GetTexture("_MainTex"),
                "Cloned material should have null texture like original");
        }

        [Test]
        public void Compress_RendererWithNullMaterial_DoesNotThrow()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = null;

            Assert.DoesNotThrow(() => service.Compress(root, false),
                "Compression should not throw when renderer has null material");

            // Renderer should still have null material
            Assert.IsNull(renderer.sharedMaterial,
                "Renderer with null material should remain null after compression");
        }

        [Test]
        public void Compress_EmptyMaterialArray_DoesNotThrow()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[0];

            Assert.DoesNotThrow(() => service.Compress(root, false),
                "Compression should not throw when renderer has empty material array");

            // Material array should remain empty
            Assert.AreEqual(0, renderer.sharedMaterials.Length,
                "Empty material array should remain empty after compression");
        }

        [Test]
        public void Compress_MultipleMaterialsWithSomeNull_NonNullMaterialsCloned()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", originalTexture);

            // Set materials array with null elements
            renderer.sharedMaterials = new Material[] { null, originalMaterial, null };

            var originalPixels = originalTexture.GetPixels();

            Assert.DoesNotThrow(() => service.Compress(root, false),
                "Compression should not throw when material array contains null elements");

            // Verify array structure is preserved
            var materials = renderer.sharedMaterials;
            Assert.AreEqual(3, materials.Length, "Material array length should be preserved");
            Assert.IsNull(materials[0], "First null element should remain null");
            Assert.IsNull(materials[2], "Third null element should remain null");

            // Non-null material should be cloned
            Assert.IsNotNull(materials[1], "Non-null material should not become null");
            Assert.AreNotSame(originalMaterial, materials[1],
                "Non-null material should be cloned");
            Assert.That(materials[1].name, Does.Contain(CloneSuffix),
                $"Cloned material should have '{CloneSuffix}' suffix");

            // Verify original texture is unchanged
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture should remain unchanged when material array contains null elements");
        }

        [Test]
        public void Compress_NoRenderers_DoesNotThrow()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            // No renderer attached

            Assert.DoesNotThrow(() => service.Compress(root, false),
                "Compression should not throw when there are no renderers");

            // Root should remain unchanged
            Assert.AreEqual(0, root.GetComponentsInChildren<Renderer>(true).Length,
                "No renderers should be added after compression");
        }

        [Test]
        public void Compress_TextureAtSkipIfSmallerThan_MaterialClonedButTextureNotCompressed()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 128; // Texture at this size should be skipped (<=)
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(128, 128); // Exactly at SkipIfSmallerThan
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();

            service.Compress(root, false);

            // Material should be cloned
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial,
                "Material should be cloned even when texture is at SkipIfSmallerThan boundary");
            Assert.That(renderer.sharedMaterial.name, Does.Contain(CloneSuffix),
                $"Cloned material should have '{CloneSuffix}' suffix");

            // Texture should NOT be compressed (at boundary, uses <=)
            var textureOnClone = renderer.sharedMaterial.GetTexture("_MainTex");
            Assert.AreSame(originalTexture, textureOnClone,
                "Cloned material should reference original texture when at SkipIfSmallerThan boundary");

            // Original texture should remain unchanged
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture at SkipIfSmallerThan boundary should remain unchanged");
        }

        [Test]
        public void Compress_TextureAboveSkipIfSmallerThan_TextureIsCompressed()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 128;
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            // Use 132x132 (above 128, and multiple of 4 for BC7 compression)
            var originalTexture = CreateTexture(132, 132);
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            service.Compress(root, false);

            // Verify compression was performed
            AssertCompressionWasPerformed(renderer, originalMaterial, originalTexture);
        }

        [Test]
        public void Compress_TextureBelowMinSize_MaterialClonedButTextureNotCompressed()
        {
            var config = CreateConfig();
            config.MinSourceSize = 128; // Set minimum size higher than texture
            var service = new TextureCompressorService(config);

            var root = CreateRootGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var originalTexture = CreateTexture(64, 64); // Below MinSourceSize
            originalMaterial.SetTexture("_MainTex", originalTexture);
            renderer.sharedMaterial = originalMaterial;

            var originalPixels = originalTexture.GetPixels();

            service.Compress(root, false);

            // Material should still be cloned (MaterialCloner runs unconditionally)
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial,
                "Material should be cloned even when texture is below min size");
            Assert.That(renderer.sharedMaterial.name, Does.Contain(CloneSuffix),
                $"Cloned material should have '{CloneSuffix}' suffix");

            // But texture should NOT be compressed (kept as original reference)
            var textureOnClone = renderer.sharedMaterial.GetTexture("_MainTex");
            Assert.AreSame(originalTexture, textureOnClone,
                "Cloned material should reference original texture when below min size");

            // Original texture should remain unchanged
            CollectionAssert.AreEqual(originalPixels, originalTexture.GetPixels(),
                "Original texture below min size should remain unchanged");
        }

        #endregion

        #region Helper Methods

        private TextureCompressor CreateConfig()
        {
            var go = new GameObject("ConfigObject");
            _createdObjects.Add(go);
            var config = go.AddComponent<TextureCompressor>();
            config.ApplyPreset(CompressorPreset.Balanced);
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            return config;
        }

        private GameObject CreateGameObject(string name, bool isRoot = false)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            if (isRoot)
            {
                _rootObjects.Add(go);
            }
            return go;
        }

        private GameObject CreateRootGameObject(string name)
        {
            return CreateGameObject(name, isRoot: true);
        }

        private Material CreateMaterial(string name)
        {
            var material = new Material(_standardShader);
            material.name = name;
            _createdObjects.Add(material);
            return material;
        }

        private Texture2D CreateTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            System.Random random = new System.Random(42); // Fixed seed for reproducibility

            for (int i = 0; i < pixels.Length; i++)
            {
                float r = (float)random.NextDouble();
                float g = (float)random.NextDouble();
                float b = (float)random.NextDouble();
                pixels[i] = new Color(r, g, b, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            _createdObjects.Add(texture);
            return texture;
        }

        /// <summary>
        /// Verifies that compression was actually performed by checking that
        /// the renderer uses a cloned material.
        /// Note: Texture compression may be skipped based on preset settings (e.g., MinSourceSize),
        /// so we only verify texture compression when explicitly requested.
        /// </summary>
        private void AssertCompressionWasPerformed(Renderer renderer, Material originalMaterial, Texture2D originalTexture, bool verifyTextureCompression = true)
        {
            Assert.AreNotSame(originalMaterial, renderer.sharedMaterial,
                "Renderer should use cloned material after compression");
            Assert.That(renderer.sharedMaterial.name, Does.Contain(CloneSuffix),
                $"Cloned material should have '{CloneSuffix}' suffix");

            if (verifyTextureCompression)
            {
                var textureOnClone = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
                if (textureOnClone != null)
                {
                    Assert.AreNotSame(originalTexture, textureOnClone,
                        "Cloned material should use compressed texture, not original");
                    Assert.That(textureOnClone.name, Does.Contain(CompressedSuffix),
                        $"Compressed texture should have '{CompressedSuffix}' suffix");
                }
            }
        }

        #endregion
    }
}
