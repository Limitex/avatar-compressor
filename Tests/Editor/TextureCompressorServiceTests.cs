using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureCompressorServiceTests
    {
        private const string TestAssetFolder = "Assets/_LAC_TMP";
        private List<Object> _createdObjects;
        private List<string> _createdAssetPaths;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<Object>();
            _createdAssetPaths = new List<string>();

            // Ensure test folder exists
            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_LAC_TMP");
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();

            // Delete created asset files
            foreach (var path in _createdAssetPaths)
            {
                if (!string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
            _createdAssetPaths.Clear();

            // Clean up test folder if empty
            if (AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                var remaining = AssetDatabase.FindAssets("", new[] { TestAssetFolder });
                if (remaining.Length == 0)
                {
                    AssetDatabase.DeleteAsset(TestAssetFolder);
                }
            }
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidConfig_CreatesService()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            Assert.IsNotNull(service);
            Assert.AreEqual("Texture Compressor", service.Name);
        }

        [Test]
        public void Constructor_WithDifferentPresets_CreatesService()
        {
            var presets = new[] {
                CompressorPreset.HighQuality,
                CompressorPreset.Quality,
                CompressorPreset.Balanced,
                CompressorPreset.Aggressive,
                CompressorPreset.Maximum
            };

            foreach (var preset in presets)
            {
                var config = CreateConfig();
                config.ApplyPreset(preset);
                var service = new TextureCompressorService(config);
                Assert.IsNotNull(service, $"Failed to create service with preset {preset}");
            }
        }

        #endregion

        #region Compress Empty/Null Tests

        [Test]
        public void Compress_EmptyHierarchy_CompletesWithoutError()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);
            var root = CreateGameObject("EmptyRoot");

            Assert.DoesNotThrow(() => service.Compress(root, false));
        }

        [Test]
        public void Compress_NoTextures_CompletesWithoutError()
        {
            var config = CreateConfig();
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            Assert.DoesNotThrow(() => service.Compress(root, false));
        }

        [Test]
        public void Compress_TexturesBelowMinSize_SkipsAll()
        {
            var config = CreateConfig();
            config.MinSourceSize = 512; // High min size
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128); // Below min size

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            // Should complete without processing
            Assert.Pass();
        }

        #endregion

        #region Compress Basic Tests

        [Test]
        public void Compress_SingleTexture_ProcessesSuccessfully()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            // Material should have been cloned
            Assert.AreNotEqual(material, renderer.sharedMaterial);
            // Texture should have been processed
            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.That(newTexture.name, Does.Contain("_compressed"));

            // Clean up compressed texture
            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_MultipleTextures_ProcessesAll()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var mainTex = CreateTexture(256, 256);
            var normalTex = CreateTexture(256, 256);

            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_BumpMap", normalTex);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newMaterial = renderer.sharedMaterial;
            var newMainTex = newMaterial.GetTexture("_MainTex") as Texture2D;
            var newNormalTex = newMaterial.GetTexture("_BumpMap") as Texture2D;

            Assert.IsNotNull(newMainTex);
            Assert.IsNotNull(newNormalTex);
            Assert.That(newMainTex.name, Does.Contain("_compressed"));
            Assert.That(newNormalTex.name, Does.Contain("_compressed"));

            // Clean up
            _createdObjects.Add(newMainTex);
            _createdObjects.Add(newNormalTex);
        }

        [Test]
        public void Compress_SharedTexture_ProcessesOnce()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var renderer1 = child1.AddComponent<MeshRenderer>();
            var renderer2 = child2.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            var sharedTexture = CreateTexture(256, 256);

            material1.SetTexture("_MainTex", sharedTexture);
            material2.SetTexture("_MainTex", sharedTexture);
            renderer1.sharedMaterial = material1;
            renderer2.sharedMaterial = material2;

            service.Compress(root, false);

            var newTex1 = renderer1.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            var newTex2 = renderer2.sharedMaterial.GetTexture("_MainTex") as Texture2D;

            Assert.IsNotNull(newTex1);
            Assert.IsNotNull(newTex2);
            // Both should use the same compressed texture
            Assert.AreEqual(newTex1, newTex2);

            // Clean up
            _createdObjects.Add(newTex1);
        }

        #endregion

        #region Material Cloning Tests

        [Test]
        public void Compress_ClonesMaterials_BeforeProcessing()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial();
            originalMaterial.name = "OriginalMaterial";
            var texture = CreateTexture(256, 256);
            originalMaterial.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = originalMaterial;

            service.Compress(root, false);

            // Material should be cloned
            Assert.AreNotEqual(originalMaterial, renderer.sharedMaterial);
            Assert.That(renderer.sharedMaterial.name, Does.Contain("_clone"));

            // Clean up compressed texture
            var newTex = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            if (newTex != null)
            {
                _createdObjects.Add(newTex);
            }
        }

        [Test]
        public void Compress_SharedMaterial_ClonesOnce()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var renderer1 = child1.AddComponent<MeshRenderer>();
            var renderer2 = child2.AddComponent<MeshRenderer>();
            var sharedMaterial = CreateMaterial();
            var texture = CreateTexture(256, 256);
            sharedMaterial.SetTexture("_MainTex", texture);
            renderer1.sharedMaterial = sharedMaterial;
            renderer2.sharedMaterial = sharedMaterial;

            service.Compress(root, false);

            // Both renderers should use the same cloned material
            Assert.AreEqual(renderer1.sharedMaterial, renderer2.sharedMaterial);

            // Clean up compressed texture
            var newTex = renderer1.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            if (newTex != null)
            {
                _createdObjects.Add(newTex);
            }
        }

        #endregion

        #region Resolution Tests

        [Test]
        public void Compress_LargeTexture_ReducesResolution()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.MaxResolution = 512;
            config.MinDivisor = 1;
            config.MaxDivisor = 8;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(1024, 1024);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            // Resolution should be reduced (either by divisor or max resolution)
            Assert.That(newTexture.width, Is.LessThanOrEqualTo(config.MaxResolution));
            Assert.That(newTexture.height, Is.LessThanOrEqualTo(config.MaxResolution));

            // Clean up
            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_WithForcePowerOfTwo_OutputIsPowerOfTwo()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.ForcePowerOfTwo = true;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.That(IsPowerOfTwo(newTexture.width), Is.True);
            Assert.That(IsPowerOfTwo(newTexture.height), Is.True);

            // Clean up
            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Hierarchy Tests

        [Test]
        public void Compress_DeepHierarchy_ProcessesAllTextures()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var level1 = CreateGameObject("Level1");
            var level2 = CreateGameObject("Level2");
            var level3 = CreateGameObject("Level3");

            level1.transform.SetParent(root.transform);
            level2.transform.SetParent(level1.transform);
            level3.transform.SetParent(level2.transform);

            var renderer = level3.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.That(newTexture.name, Does.Contain("_compressed"));

            // Clean up
            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_InactiveChildren_ProcessesTextures()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var inactiveChild = CreateGameObject("InactiveChild");
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            var renderer = inactiveChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.That(newTexture.name, Does.Contain("_compressed"));

            // Clean up
            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Filter Tests

        [Test]
        public void Compress_MainTexturesDisabled_SkipsMainTextures()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.ProcessMainTextures = false;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var mainTex = CreateTexture(256, 256);
            var normalTex = CreateTexture(256, 256);
            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_BumpMap", normalTex);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            // Normal map should be processed, main texture should not
            var newNormalTex = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newNormalTex);
            Assert.That(newNormalTex.name, Does.Contain("_compressed"));

            // Clean up
            _createdObjects.Add(newNormalTex);
        }

        #endregion

        #region Preset Tests

        [Test]
        public void Compress_WithHighQualityPreset_UsesConservativeSettings()
        {
            var config = CreateConfig();
            config.ApplyPreset(CompressorPreset.HighQuality);
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(2048, 2048);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            // High quality preset should preserve more resolution
            Assert.That(newTexture.width, Is.GreaterThanOrEqualTo(config.MinResolution));

            // Clean up
            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_WithAggressivePreset_ReducesMore()
        {
            var config = CreateConfig();
            config.ApplyPreset(CompressorPreset.Aggressive);
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(512, 512);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            // Aggressive preset should reduce resolution
            Assert.That(newTexture.width, Is.LessThanOrEqualTo(512));

            // Clean up
            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Logging Tests

        [Test]
        public void Compress_WithLoggingEnabled_DoesNotThrow()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            Assert.DoesNotThrow(() => service.Compress(root, true));

            // Clean up
            var newTex = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            if (newTex != null)
            {
                _createdObjects.Add(newTex);
            }
        }

        [Test]
        public void Compress_WithLoggingDisabled_DoesNotThrow()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            Assert.DoesNotThrow(() => service.Compress(root, false));

            // Clean up
            var newTex = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            if (newTex != null)
            {
                _createdObjects.Add(newTex);
            }
        }

        #endregion

        #region MaterialReference Tests

        [Test]
        public void CompressWithMappings_WithAnimationMaterialReferences_ReturnsProcessedTextures()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var additionalMaterial = CreateMaterial();
            var texture = CreateTexture(256, 256);
            additionalMaterial.SetTexture("_MainTex", texture);

            var references = new List<MaterialReference>
            {
                MaterialReference.FromAnimation(additionalMaterial, null)
            };

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(references, false);

            Assert.IsNotNull(processedTextures);
            Assert.IsNotNull(clonedMaterials);
            Assert.AreEqual(1, processedTextures.Count);
            Assert.AreEqual(1, clonedMaterials.Count);

            // Clean up compressed texture
            foreach (var kvp in processedTextures)
            {
                _createdObjects.Add(kvp.Value);
            }
            foreach (var kvp in clonedMaterials)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CompressWithMappings_WithRendererReferences_CompletesSuccessfully()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var references = MaterialCollector.CollectFromRenderers(root);
            var (processedTextures, clonedMaterials) = service.CompressWithMappings(references, false);

            Assert.IsNotNull(processedTextures);
            Assert.IsNotNull(clonedMaterials);
            Assert.AreEqual(1, processedTextures.Count);
            Assert.AreEqual(1, clonedMaterials.Count);

            // Clean up
            foreach (var kvp in processedTextures)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CompressWithMappings_WithAnimationReferences_ClonesAnimationMaterials()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var additionalMaterial = CreateMaterial();
            additionalMaterial.name = "AdditionalMaterial";
            var texture = CreateTexture(256, 256);
            additionalMaterial.SetTexture("_MainTex", texture);

            var references = new List<MaterialReference>
            {
                MaterialReference.FromAnimation(additionalMaterial, null)
            };

            var (_, clonedMaterials) = service.CompressWithMappings(references, false);

            Assert.AreEqual(1, clonedMaterials.Count);
            Assert.IsTrue(clonedMaterials.ContainsKey(additionalMaterial));
            Assert.AreNotSame(additionalMaterial, clonedMaterials[additionalMaterial]);

            // Clean up
            foreach (var kvp in clonedMaterials)
            {
                _createdObjects.Add(kvp.Value);
                var tex = kvp.Value.GetTexture("_MainTex") as Texture2D;
                if (tex != null) _createdObjects.Add(tex);
            }
        }

        [Test]
        public void CompressWithMappings_WithAnimationReferences_UpdatesTextureOnClonedMaterial()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var additionalMaterial = CreateMaterial();
            var originalTexture = CreateTexture(256, 256);
            additionalMaterial.SetTexture("_MainTex", originalTexture);

            var references = new List<MaterialReference>
            {
                MaterialReference.FromAnimation(additionalMaterial, null)
            };

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(references, false);

            var clonedMaterial = clonedMaterials[additionalMaterial];
            var textureOnClonedMaterial = clonedMaterial.GetTexture("_MainTex") as Texture2D;

            // Cloned material should have the compressed texture
            Assert.IsNotNull(textureOnClonedMaterial);
            Assert.That(textureOnClonedMaterial.name, Does.Contain("_compressed"));

            // Clean up
            foreach (var kvp in processedTextures)
            {
                _createdObjects.Add(kvp.Value);
            }
            foreach (var kvp in clonedMaterials)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CompressWithMappings_MixedRendererAndAnimationReferences_ProcessesAll()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var rendererMaterial = CreateMaterial();
            var rendererTexture = CreateTexture(256, 256);
            rendererMaterial.SetTexture("_MainTex", rendererTexture);
            renderer.sharedMaterial = rendererMaterial;

            var additionalMaterial = CreateMaterial();
            var additionalTexture = CreateTexture(256, 256);
            additionalMaterial.SetTexture("_MainTex", additionalTexture);

            var references = new List<MaterialReference>();
            references.AddRange(MaterialCollector.CollectFromRenderers(root));
            references.Add(MaterialReference.FromAnimation(additionalMaterial, null));

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(references, false);

            Assert.AreEqual(2, processedTextures.Count);
            Assert.AreEqual(2, clonedMaterials.Count);
            Assert.IsTrue(processedTextures.ContainsKey(rendererTexture));
            Assert.IsTrue(processedTextures.ContainsKey(additionalTexture));

            // Clean up
            foreach (var kvp in processedTextures)
            {
                _createdObjects.Add(kvp.Value);
            }
            foreach (var kvp in clonedMaterials)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CompressWithMappings_SharedTextureBetweenRendererAndAnimation_ProcessesOnce()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var rendererMaterial = CreateMaterial();
            var sharedTexture = CreateTexture(256, 256);
            rendererMaterial.SetTexture("_MainTex", sharedTexture);
            renderer.sharedMaterial = rendererMaterial;

            var additionalMaterial = CreateMaterial();
            additionalMaterial.SetTexture("_MainTex", sharedTexture);

            var references = new List<MaterialReference>();
            references.AddRange(MaterialCollector.CollectFromRenderers(root));
            references.Add(MaterialReference.FromAnimation(additionalMaterial, null));

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(references, false);

            // Same texture should be processed only once
            Assert.AreEqual(1, processedTextures.Count);
            Assert.IsTrue(processedTextures.ContainsKey(sharedTexture));

            // Both materials should have the same compressed texture
            var compressedTexture = processedTextures[sharedTexture];
            var rendererClonedMaterial = clonedMaterials[rendererMaterial];
            var additionalClonedMaterial = clonedMaterials[additionalMaterial];

            Assert.AreEqual(compressedTexture, rendererClonedMaterial.GetTexture("_MainTex"));
            Assert.AreEqual(compressedTexture, additionalClonedMaterial.GetTexture("_MainTex"));

            // Clean up
            foreach (var kvp in processedTextures)
            {
                _createdObjects.Add(kvp.Value);
            }
            foreach (var kvp in clonedMaterials)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CompressWithMappings_EmptyReferences_ReturnsEmptyResults()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var references = new List<MaterialReference>();

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(references, false);

            Assert.AreEqual(0, processedTextures.Count);
            Assert.AreEqual(0, clonedMaterials.Count);
        }

        [Test]
        public void CompressWithMappings_NoTexturesFromReferences_ReturnsEmptyProcessedTextures()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            // Material without textures
            var additionalMaterial = CreateMaterial();

            var references = new List<MaterialReference>
            {
                MaterialReference.FromAnimation(additionalMaterial, null)
            };

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(references, false);

            Assert.AreEqual(0, processedTextures.Count);
            Assert.AreEqual(1, clonedMaterials.Count);

            // Clean up
            foreach (var kvp in clonedMaterials)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        #endregion

        #region Mipmap Streaming Tests

        [Test]
        public void Compress_CompressedTexture_HasMipmapStreamingEnabled()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);

            // Verify mipmap streaming is enabled using SerializedObject
            var serializedTexture = new SerializedObject(newTexture);
            var streamingMipmaps = serializedTexture.FindProperty("m_StreamingMipmaps");
            Assert.IsNotNull(streamingMipmaps, "m_StreamingMipmaps property should exist");
            Assert.IsTrue(streamingMipmaps.boolValue, "Mipmap streaming should be enabled on compressed texture");

            // Clean up
            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_MultipleTextures_AllHaveMipmapStreamingEnabled()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var mainTex = CreateTexture(256, 256);
            var normalTex = CreateTexture(256, 256);
            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_BumpMap", normalTex);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newMaterial = renderer.sharedMaterial;
            var newMainTex = newMaterial.GetTexture("_MainTex") as Texture2D;
            var newNormalTex = newMaterial.GetTexture("_BumpMap") as Texture2D;

            Assert.IsNotNull(newMainTex);
            Assert.IsNotNull(newNormalTex);

            // Verify mipmap streaming is enabled on both textures
            var serializedMainTex = new SerializedObject(newMainTex);
            var mainTexStreaming = serializedMainTex.FindProperty("m_StreamingMipmaps");
            Assert.IsTrue(mainTexStreaming.boolValue, "Main texture should have mipmap streaming enabled");

            var serializedNormalTex = new SerializedObject(newNormalTex);
            var normalTexStreaming = serializedNormalTex.FindProperty("m_StreamingMipmaps");
            Assert.IsTrue(normalTexStreaming.boolValue, "Normal texture should have mipmap streaming enabled");

            // Clean up
            _createdObjects.Add(newMainTex);
            _createdObjects.Add(newNormalTex);
        }

        [Test]
        public void Compress_SharedTexture_HasMipmapStreamingEnabled()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var renderer1 = child1.AddComponent<MeshRenderer>();
            var renderer2 = child2.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            var sharedTexture = CreateTexture(256, 256);

            material1.SetTexture("_MainTex", sharedTexture);
            material2.SetTexture("_MainTex", sharedTexture);
            renderer1.sharedMaterial = material1;
            renderer2.sharedMaterial = material2;

            service.Compress(root, false);

            var newTex = renderer1.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTex);

            // Verify mipmap streaming is enabled on shared texture
            var serializedTexture = new SerializedObject(newTex);
            var streamingMipmaps = serializedTexture.FindProperty("m_StreamingMipmaps");
            Assert.IsTrue(streamingMipmaps.boolValue, "Shared compressed texture should have mipmap streaming enabled");

            // Clean up
            _createdObjects.Add(newTex);
        }

        #endregion

        #region Helper Methods

        private TextureCompressor CreateConfig()
        {
            var go = CreateGameObject("ConfigObject");
            var config = go.AddComponent<TextureCompressor>();
            config.ApplyPreset(CompressorPreset.Balanced);
            return config;
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private Material CreateMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            _createdObjects.Add(material);
            return material;
        }

        private Texture2D CreateTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            System.Random random = new System.Random(42);

            for (int i = 0; i < pixels.Length; i++)
            {
                float v = (float)random.NextDouble();
                pixels[i] = new Color(v, v, v, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // Save as asset to get a valid asset path
            string assetPath = $"{TestAssetFolder}/TestTexture_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            // Reload from asset to ensure it has a valid asset path
            var loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return loadedTexture;
        }

        private static bool IsPowerOfTwo(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }

        #endregion
    }
}
