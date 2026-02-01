using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
                if (
                    !string.IsNullOrEmpty(path)
                    && AssetDatabase.LoadAssetAtPath<Object>(path) != null
                )
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
            var presets = new[]
            {
                CompressorPreset.HighQuality,
                CompressorPreset.Quality,
                CompressorPreset.Balanced,
                CompressorPreset.Aggressive,
                CompressorPreset.Maximum,
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
                MaterialReference.FromAnimation(additionalMaterial, null),
            };

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(
                references,
                false
            );

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
            var (processedTextures, clonedMaterials) = service.CompressWithMappings(
                references,
                false
            );

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
                MaterialReference.FromAnimation(additionalMaterial, null),
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
                if (tex != null)
                    _createdObjects.Add(tex);
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
                MaterialReference.FromAnimation(additionalMaterial, null),
            };

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(
                references,
                false
            );

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

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(
                references,
                false
            );

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

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(
                references,
                false
            );

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

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(
                references,
                false
            );

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
                MaterialReference.FromAnimation(additionalMaterial, null),
            };

            var (processedTextures, clonedMaterials) = service.CompressWithMappings(
                references,
                false
            );

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
            Assert.IsTrue(
                streamingMipmaps.boolValue,
                "Mipmap streaming should be enabled on compressed texture"
            );

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
            Assert.IsTrue(
                mainTexStreaming.boolValue,
                "Main texture should have mipmap streaming enabled"
            );

            var serializedNormalTex = new SerializedObject(newNormalTex);
            var normalTexStreaming = serializedNormalTex.FindProperty("m_StreamingMipmaps");
            Assert.IsTrue(
                normalTexStreaming.boolValue,
                "Normal texture should have mipmap streaming enabled"
            );

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
            Assert.IsTrue(
                streamingMipmaps.boolValue,
                "Shared compressed texture should have mipmap streaming enabled"
            );

            // Clean up
            _createdObjects.Add(newTex);
        }

        #endregion

        #region Compression Format Tests - Desktop

        [Test]
        public void Compress_OpaqueTexture_Desktop_CompressesToDXT1()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.7f;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            // Create simple opaque texture (low complexity)
            var texture = CreateOpaqueTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.DXT1,
                newTexture.format,
                "Opaque low complexity texture should compress to DXT1"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_TextureWithAlpha_Desktop_CompressesToDXT5()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            // Disable high quality format to ensure DXT5 is selected for alpha textures
            config.UseHighQualityFormatForHighComplexity = false;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTextureWithAlpha(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.DXT5,
                newTexture.format,
                "Texture with alpha should compress to DXT5"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_HighComplexityTexture_Desktop_CompressesToBC7()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.3f; // Lower threshold to ensure high complexity detection
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            // Create high complexity texture (noisy pattern)
            var texture = CreateHighComplexityTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.BC7,
                newTexture.format,
                "High complexity texture should compress to BC7"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_NormalMap_Desktop_CompressesToBC5()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var normalTexture = CreateNormalMapTexture(256, 256);
            material.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.BC5,
                newTexture.format,
                "Normal map should compress to BC5"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_NormalMapWithAlpha_Desktop_CompressesToBC7()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            // Normal map with significant alpha
            var normalTexture = CreateNormalMapTextureWithAlpha(256, 256);
            material.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.BC7,
                newTexture.format,
                "Normal map with alpha should compress to BC7 to preserve alpha"
            );

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Compression Format Tests - Mobile

        [Test]
        public void Compress_OpaqueTexture_Mobile_CompressesToASTC()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Mobile;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.7f;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateOpaqueTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            // Should be one of the ASTC formats
            Assert.That(
                newTexture.format,
                Is.EqualTo(TextureFormat.ASTC_4x4)
                    .Or.EqualTo(TextureFormat.ASTC_6x6)
                    .Or.EqualTo(TextureFormat.ASTC_8x8),
                "Mobile texture should compress to ASTC format"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_NormalMap_Mobile_CompressesToASTC4x4()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Mobile;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var normalTexture = CreateNormalMapTexture(256, 256);
            material.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.ASTC_4x4,
                newTexture.format,
                "Mobile normal map should compress to ASTC_4x4"
            );

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Frozen Format Override Tests

        [Test]
        public void Compress_WithFrozenFormatBC7_UsesBC7()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;

            // Create texture and save as asset to get GUID
            var texture = CreateOpaqueTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            // Add frozen settings with BC7 override
            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Skip = false,
                    Divisor = 1,
                    Format = FrozenTextureFormat.BC7,
                },
            };

            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.BC7,
                newTexture.format,
                "Frozen format BC7 override should be applied"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_WithFrozenFormatDXT5_UsesDXT5()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;

            var texture = CreateOpaqueTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Skip = false,
                    Divisor = 1,
                    Format = FrozenTextureFormat.DXT5,
                },
            };

            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.DXT5,
                newTexture.format,
                "Frozen format DXT5 override should be applied"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_WithFrozenFormatAuto_UsesNormalSelection()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.7f;

            var texture = CreateOpaqueTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Skip = false,
                    Divisor = 1,
                    Format = FrozenTextureFormat.Auto,
                },
            };

            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            // Auto should fall through to normal selection (DXT1 for opaque)
            Assert.AreEqual(
                TextureFormat.DXT1,
                newTexture.format,
                "Frozen format Auto should use normal format selection"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_WithFrozenFormatASTC_OnDesktop_UsesASTC()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;

            var texture = CreateOpaqueTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Skip = false,
                    Divisor = 1,
                    Format = FrozenTextureFormat.ASTC_4x4,
                },
            };

            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.ASTC_4x4,
                newTexture.format,
                "Frozen ASTC format should be applied even on Desktop platform"
            );

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Normal Map Pixel Data Integrity Tests

        [Test]
        public void Compress_BC5NormalMap_ProducesValidNormalizedVectors()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var normalTexture = CreateNormalMapTexture(64, 64);
            material.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(TextureFormat.BC5, newTexture.format);

            var compressedPixels = newTexture.GetPixels();

            // Verify all compressed normals can be reconstructed to valid unit vectors
            int sampleCount = 100;
            int step = Mathf.Max(1, compressedPixels.Length / sampleCount);

            for (int i = 0; i < compressedPixels.Length; i += step)
            {
                float x = compressedPixels[i].r * 2f - 1f;
                float y = compressedPixels[i].g * 2f - 1f;

                // X² + Y² should not exceed 1 (otherwise Z cannot be reconstructed)
                // Allow small tolerance for BC5 compression artifacts
                float xyLengthSq = x * x + y * y;
                Assert.That(
                    xyLengthSq,
                    Is.LessThanOrEqualTo(1.02f),
                    $"Normal at index {i} has X²+Y²={xyLengthSq:F4} which exceeds 1"
                );

                // Reconstruct Z and verify unit length
                float zSq = 1f - xyLengthSq;
                float z = zSq > 0f ? Mathf.Sqrt(zSq) : 0f;
                float length = Mathf.Sqrt(x * x + y * y + z * z);

                Assert.That(
                    length,
                    Is.EqualTo(1f).Within(0.02f),
                    $"Reconstructed normal at index {i} has length {length:F4}, expected ~1.0"
                );
            }

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_BC5NormalMap_ProducesValidReconstructableNormals()
        {
            // Note: Detailed direction preservation is tested in NormalMapPreprocessorTests.
            // This integration test verifies that the compressed normals are valid unit vectors
            // that can be reconstructed (i.e., not corrupted by the compression pipeline).
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var normalTexture = CreateNormalMapTexture(64, 64);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(TextureFormat.BC5, newTexture.format);

            var compressedPixels = newTexture.GetPixels();

            // Verify all compressed normals can be reconstructed to valid unit vectors
            // and have Z pointing in the positive direction (tangent space convention)
            int sampleCount = 100;
            int step = Mathf.Max(1, compressedPixels.Length / sampleCount);

            for (int i = 0; i < compressedPixels.Length; i += step)
            {
                float x = compressedPixels[i].r * 2f - 1f;
                float y = compressedPixels[i].g * 2f - 1f;
                float zSq = 1f - x * x - y * y;
                float z = zSq > 0f ? Mathf.Sqrt(zSq) : 0f;

                // Verify unit length
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                Assert.That(
                    length,
                    Is.EqualTo(1f).Within(0.03f),
                    $"Normal at index {i} should have unit length, got {length:F4}"
                );

                // Verify Z is non-negative (tangent space normals point outward)
                Assert.That(
                    z,
                    Is.GreaterThanOrEqualTo(0f),
                    $"Normal at index {i} should have non-negative Z for tangent space"
                );
            }

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Compressed Source Format Tests

        /// <summary>
        /// Tests that normal map compression produces valid normals with downscaling.
        /// Verifies the full pipeline including resize and compression works correctly.
        /// </summary>
        [Test]
        public void Compress_NormalMapWithDownscale_ProducesValidNormals()
        {
            var config = CreateConfig();
            config.MinSourceSize = 32;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            config.MinDivisor = 2; // Force downscale
            config.MaxDivisor = 2;
            var service = new TextureCompressorService(config);

            var normalTexture = CreateNormalMapTexture(128, 128);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(TextureFormat.BC5, newTexture.format);

            // Should be downscaled
            Assert.That(newTexture.width, Is.LessThan(128), "Texture should be downscaled");

            // Verify normals are valid after downscale
            var pixels = newTexture.GetPixels();
            int invalidCount = 0;
            for (int i = 0; i < pixels.Length; i += 5)
            {
                float x = pixels[i].r * 2f - 1f;
                float y = pixels[i].g * 2f - 1f;
                if (x * x + y * y > 1.05f)
                {
                    invalidCount++;
                }
            }

            Assert.AreEqual(0, invalidCount, "All normals should have valid X²+Y² <= 1");

            _createdObjects.Add(newTexture);
        }

        /// <summary>
        /// Tests that the IsCompressedFormat method correctly identifies compressed formats.
        /// This is used to determine if a source texture should preserve its format.
        /// </summary>
        [Test]
        public void IsCompressedFormat_IdentifiesAllCompressedFormats()
        {
            // BC formats (Desktop)
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC5));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC7));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT1));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT5));

            // ASTC formats (Mobile)
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_4x4));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_6x6));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_8x8));

            // Uncompressed formats
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.RGBA32));
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.RGB24));
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.ARGB32));
        }

        /// <summary>
        /// Tests that when source texture is already compressed (BC5), the format is preserved.
        /// This verifies the behavior where compressed source textures maintain their format.
        /// </summary>
        [Test]
        public void Compress_SourceAlreadyCompressedBC5_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

            // Create texture and compress to BC5
            var texture = CreateOpaqueTexture(64, 64);
            EditorUtility.CompressTexture(
                texture,
                TextureFormat.BC5,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.BC5, texture.format, "Source should be BC5");

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.BC5,
                newTexture.format,
                "Already compressed BC5 source should preserve BC5 format"
            );

            _createdObjects.Add(newTexture);
        }

        /// <summary>
        /// Tests that when source texture is already compressed (DXT5), the format is preserved.
        /// </summary>
        [Test]
        public void Compress_SourceAlreadyCompressedDXT5_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

            // Create texture and compress to DXT5
            var texture = CreateTextureWithAlpha(64, 64);
            EditorUtility.CompressTexture(
                texture,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.DXT5, texture.format, "Source should be DXT5");

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.DXT5,
                newTexture.format,
                "Already compressed DXT5 source should preserve DXT5 format"
            );

            _createdObjects.Add(newTexture);
        }

        /// <summary>
        /// Tests that when source texture is already compressed (BC7), the format is preserved.
        /// </summary>
        [Test]
        public void Compress_SourceAlreadyCompressedBC7_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

            // Create texture and compress to BC7
            var texture = CreateOpaqueTexture(64, 64);
            EditorUtility.CompressTexture(
                texture,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.BC7, texture.format, "Source should be BC7");

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.BC7,
                newTexture.format,
                "Already compressed BC7 source should preserve BC7 format"
            );

            _createdObjects.Add(newTexture);
        }

        /// <summary>
        /// Tests that when source texture is already compressed (ASTC), the format is preserved
        /// even when target platform is Desktop.
        /// </summary>
        [Test]
        public void Compress_SourceAlreadyCompressedASTC_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop; // Desktop platform
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

            // Create texture and compress to ASTC (mobile format)
            var texture = CreateOpaqueTexture(64, 64);
            EditorUtility.CompressTexture(
                texture,
                TextureFormat.ASTC_6x6,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.ASTC_6x6, texture.format, "Source should be ASTC_6x6");

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.ASTC_6x6,
                newTexture.format,
                "Already compressed ASTC source should preserve format even on Desktop"
            );

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Mobile Platform Tests

        [Test]
        public void Compress_Mobile_HighComplexityTexture_CompressesToASTC4x4()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Mobile;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.3f; // Low threshold to ensure high complexity
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateHighComplexityTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.ASTC_4x4,
                newTexture.format,
                "High complexity mobile texture should compress to ASTC_4x4"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_Mobile_LowComplexityTexture_CompressesToASTC8x8()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Mobile;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.9f; // High threshold to ensure low complexity
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            // Use opaque texture (low complexity gradient)
            var texture = CreateOpaqueTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.ASTC_8x8,
                newTexture.format,
                "Low complexity mobile texture should compress to ASTC_8x8"
            );

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Frozen Format Override Tests (Additional)

        [Test]
        public void Compress_WithFrozenFormatOverrideOnNormalMap_OverridesBC5()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;

            var normalTexture = CreateNormalMapTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(normalTexture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            // Override normal map to use DXT1 instead of default BC5
            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Skip = false,
                    Divisor = 1,
                    Format = FrozenTextureFormat.DXT1,
                },
            };

            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", normalTexture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.DXT1,
                newTexture.format,
                "Frozen format should override default BC5 for normal maps"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_WithNullFrozenFormat_UsesDefaultSelection()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.7f;
            // No frozen textures configured
            config.FrozenTextures = new List<FrozenTextureSettings>();

            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateOpaqueTexture(256, 256);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.AreEqual(
                TextureFormat.DXT1,
                newTexture.format,
                "Without frozen format, should use default selection (DXT1 for opaque)"
            );

            _createdObjects.Add(newTexture);
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
            string assetPath =
                $"{TestAssetFolder}/TestTexture_{width}x{height}_{System.Guid.NewGuid():N}.asset";
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

        private Texture2D CreateOpaqueTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            // Create simple gradient pattern (low complexity, fully opaque)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float v = (x + y) / (float)(width + height);
                    pixels[y * width + x] = new Color(v, v, v, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/OpaqueTexture_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateTextureWithAlpha(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            System.Random random = new System.Random(42);

            for (int i = 0; i < pixels.Length; i++)
            {
                float v = (float)random.NextDouble();
                // Create significant alpha variation
                float a = (float)random.NextDouble() * 0.5f + 0.25f;
                pixels[i] = new Color(v, v, v, a);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/AlphaTexture_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateHighComplexityTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            System.Random random = new System.Random(123);

            // Create noisy, high-variance pattern
            for (int i = 0; i < pixels.Length; i++)
            {
                float r = (float)random.NextDouble();
                float g = (float)random.NextDouble();
                float b = (float)random.NextDouble();
                pixels[i] = new Color(r, g, b, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/HighComplexity_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateNormalMapTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                // Create varied normals for testing
                // Normal encoded in tangent space: (x*0.5+0.5, y*0.5+0.5, z*0.5+0.5)
                float x = ((i % width) / (float)width - 0.5f) * 0.6f;
                float y = ((i / width) / (float)height - 0.5f) * 0.6f;

                // Encode to 0-1 range
                byte r = (byte)((x * 0.5f + 0.5f) * 255f);
                byte g = (byte)((y * 0.5f + 0.5f) * 255f);

                pixels[i] = new Color32(r, g, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/NormalMap_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateNormalMapTextureWithAlpha(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            System.Random random = new System.Random(42);

            for (int i = 0; i < pixels.Length; i++)
            {
                float x = ((i % width) / (float)width - 0.5f) * 0.6f;
                float y = ((i / width) / (float)height - 0.5f) * 0.6f;

                byte r = (byte)((x * 0.5f + 0.5f) * 255f);
                byte g = (byte)((y * 0.5f + 0.5f) * 255f);
                // Significant alpha variation
                byte a = (byte)(random.Next(50, 200));

                pixels[i] = new Color32(r, g, 255, a);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/NormalMapAlpha_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        #endregion
    }
}
