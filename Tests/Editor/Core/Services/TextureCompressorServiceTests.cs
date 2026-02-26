using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureCompressorServiceTests
    {
        private static bool IsSoftwareRenderer =>
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

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

        #region Compression Format Tests

        [Test]
        public void Compress_OpaqueTexture_ProducesCompressedFormat()
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
            Assert.IsTrue(
                TextureFormatSelector.IsCompressedFormat(newTexture.format),
                $"Output should be a compressed format, got {newTexture.format}"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_NormalMap_ProducesCompressedFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256);
            material.SetTexture("_BumpMap", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var newTexture = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(newTexture);
            Assert.IsTrue(
                TextureFormatSelector.IsCompressedFormat(newTexture.format),
                $"Normal map should be compressed, got {newTexture.format}"
            );
            Assert.That(newTexture.name, Does.Contain("_compressed"));

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_NormalMapAndMainTexture_BothCompressed()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.ProcessMainTextures = true;
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

            var newMainTex = renderer.sharedMaterial.GetTexture("_MainTex") as Texture2D;
            var newNormalTex = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;

            Assert.IsNotNull(newMainTex);
            Assert.IsNotNull(newNormalTex);
            Assert.IsTrue(
                TextureFormatSelector.IsCompressedFormat(newMainTex.format),
                $"Main texture should be compressed, got {newMainTex.format}"
            );
            Assert.IsTrue(
                TextureFormatSelector.IsCompressedFormat(newNormalTex.format),
                $"Normal map should be compressed, got {newNormalTex.format}"
            );

            _createdObjects.Add(newMainTex);
            _createdObjects.Add(newNormalTex);
        }

        #endregion

        #region Frozen Format Override Tests

        [Test]
        public void Compress_WithFrozenFormatDXT1_UsesSpecifiedFormat()
        {
            var texture = CreateTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Divisor = 1,
                    Format = FrozenTextureFormat.DXT1,
                    Skip = false,
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
                TextureFormat.DXT1,
                newTexture.format,
                "Frozen DXT1 format should be applied"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_WithFrozenFormatBC7_UsesSpecifiedFormat()
        {
            var texture = CreateTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Divisor = 1,
                    Format = FrozenTextureFormat.BC7,
                    Skip = false,
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
                "Frozen BC7 format should be applied"
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
        public void Compress_WithFrozenSkip_DoesNotProcessTexture()
        {
            var texture = CreateTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Divisor = 1,
                    Format = FrozenTextureFormat.Auto,
                    Skip = true,
                },
            };
            var service = new TextureCompressorService(config);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            // Skipped texture should not be replaced (material may still be cloned but texture unchanged)
            var newMaterial = renderer.sharedMaterial;
            var resultTex = newMaterial.GetTexture("_MainTex") as Texture2D;
            // The texture should either be the original or not contain "_compressed"
            if (resultTex != null && resultTex != texture)
            {
                Assert.That(
                    resultTex.name,
                    Does.Not.Contain("_compressed"),
                    "Skipped frozen texture should not be compressed"
                );
                _createdObjects.Add(resultTex);
            }
        }

        [Test]
        public void Compress_WithFrozenDivisor_UsesSpecifiedDivisor()
        {
            var texture = CreateTexture(512, 512);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.ForcePowerOfTwo = true;
            config.FrozenTextures = new List<FrozenTextureSettings>
            {
                new FrozenTextureSettings
                {
                    TextureGuid = guid,
                    Divisor = 4,
                    Format = FrozenTextureFormat.Auto,
                    Skip = false,
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
            // 512/4 = 128
            Assert.AreEqual(
                128,
                newTexture.width,
                "Frozen divisor 4 on 512px should produce 128px"
            );
            Assert.AreEqual(128, newTexture.height);

            _createdObjects.Add(newTexture);
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
            config.HighComplexityThreshold = 0.3f;
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

        [Test]
        public void Compress_Mobile_HighComplexityTexture_CompressesToASTC4x4()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Mobile;
            config.UseHighQualityFormatForHighComplexity = true;
            config.HighComplexityThreshold = 0.3f;
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
            config.HighComplexityThreshold = 0.9f;
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
                TextureFormat.ASTC_8x8,
                newTexture.format,
                "Low complexity mobile texture should compress to ASTC_8x8"
            );

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Frozen Format Override Tests (Additional)

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

        #region Normal Map Pixel Data Integrity Tests

        [Test]
        public void Compress_BC5NormalMap_ProducesValidNormalizedVectors()
        {
            if (IsSoftwareRenderer)
                Assert.Ignore("Normal vector precision test requires a GPU renderer.");

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
            int sampleCount = 100;
            int step = Mathf.Max(1, compressedPixels.Length / sampleCount);

            for (int i = 0; i < compressedPixels.Length; i += step)
            {
                float x = compressedPixels[i].r * 2f - 1f;
                float y = compressedPixels[i].g * 2f - 1f;

                float xyLengthSq = x * x + y * y;
                Assert.That(
                    xyLengthSq,
                    Is.LessThanOrEqualTo(1.02f),
                    $"Normal at index {i} has X^2+Y^2={xyLengthSq:F4} which exceeds 1"
                );

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
            int sampleCount = 100;
            int step = Mathf.Max(1, compressedPixels.Length / sampleCount);

            for (int i = 0; i < compressedPixels.Length; i += step)
            {
                float x = compressedPixels[i].r * 2f - 1f;
                float y = compressedPixels[i].g * 2f - 1f;
                float zSq = 1f - x * x - y * y;
                float z = zSq > 0f ? Mathf.Sqrt(zSq) : 0f;

                float length = Mathf.Sqrt(x * x + y * y + z * z);
                Assert.That(
                    length,
                    Is.EqualTo(1f).Within(0.03f),
                    $"Normal at index {i} should have unit length, got {length:F4}"
                );

                Assert.That(
                    z,
                    Is.GreaterThanOrEqualTo(0f),
                    $"Normal at index {i} should have non-negative Z for tangent space"
                );
            }

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_NormalMapWithDownscale_ProducesValidNormals()
        {
            var config = CreateConfig();
            config.MinSourceSize = 32;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            config.MinDivisor = 2;
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
            Assert.That(newTexture.width, Is.LessThan(128), "Texture should be downscaled");

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

            Assert.AreEqual(0, invalidCount, "All normals should have valid X^2+Y^2 <= 1");

            _createdObjects.Add(newTexture);
        }

        #endregion

        #region Compressed Source Format Tests

        [Test]
        public void IsCompressedFormat_IdentifiesAllCompressedFormats()
        {
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC5));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.BC7));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT1));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.DXT5));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_4x4));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_6x6));
            Assert.IsTrue(TextureFormatSelector.IsCompressedFormat(TextureFormat.ASTC_8x8));
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.RGBA32));
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.RGB24));
            Assert.IsFalse(TextureFormatSelector.IsCompressedFormat(TextureFormat.ARGB32));
        }

        [Test]
        public void Compress_SourceAlreadyCompressedBC5_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

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
                "Already compressed BC5 source should preserve format"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_SourceAlreadyCompressedDXT5_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

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
                "Already compressed DXT5 source should preserve format"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_SourceAlreadyCompressedDXT5_OnBumpMap_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateCompressedDXT5NormalMap(
                128,
                128,
                0.35f,
                -0.25f,
                sourceIsAGLayout: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(
                TextureFormat.DXT5,
                result.format,
                "Already compressed DXT5 source on _BumpMap should preserve format"
            );

            _createdObjects.Add(result);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Compress_DXT5NormalMapSource_UsesCorrectInputLayout(bool sourceIsAGLayout)
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            config.MinDivisor = 1;
            config.MaxDivisor = 1;
            var service = new TextureCompressorService(config);

            const float expectedX = 0.35f;
            const float expectedY = -0.25f;
            var source = CreateCompressedDXT5NormalMap(
                128,
                128,
                expectedX,
                expectedY,
                sourceIsAGLayout
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.DXT5, result.format);

            var pixels = result.GetPixels();
            int centerIndex = (result.height / 2) * result.width + (result.width / 2);

            float decodedX = pixels[centerIndex].a * 2f - 1f;
            float decodedY = pixels[centerIndex].g * 2f - 1f;

            string layoutLabel = sourceIsAGLayout ? "AG" : "RG";
            Assert.That(
                decodedX,
                Is.EqualTo(expectedX).Within(0.2f),
                $"DXT5 {layoutLabel} source should preserve X direction during recompression"
            );
            Assert.That(
                decodedY,
                Is.EqualTo(expectedY).Within(0.2f),
                $"DXT5 {layoutLabel} source should preserve Y direction during recompression"
            );

            _createdObjects.Add(result);
        }

        [Test]
        public void Compress_SourceAlreadyCompressedBC7_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

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
                "Already compressed BC7 source should preserve format"
            );

            _createdObjects.Add(newTexture);
        }

        [Test]
        public void Compress_SourceAlreadyCompressedASTC_PreservesFormat()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessMainTextures = true;
            var service = new TextureCompressorService(config);

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

        #region BC7 Semantic Alpha Preservation Tests

        [Test]
        public void Compress_BC7RGBLayoutNormalMapSource_PreservesSemanticAlpha()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateNormalMapTextureWithAlpha(128, 128);
            var preprocessor = new NormalMapPreprocessor();
            preprocessor.PrepareForCompression(
                source,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true
            );
            EditorUtility.CompressTexture(
                source,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.BC7, source.format);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.BC7, result.format);

            var pixels = result.GetPixels();
            float minAlpha = pixels.Min(p => p.a);
            float maxAlpha = pixels.Max(p => p.a);
            Assert.That(
                minAlpha,
                Is.LessThan(0.85f),
                "BC7 RGB-layout source should keep semantic alpha after recompression"
            );
            Assert.That(
                maxAlpha,
                Is.GreaterThan(0.55f),
                "Alpha should retain high-value range when preservation is enabled"
            );

            _createdObjects.Add(result);
        }

        [Test]
        public void Compress_BC7RGBLayoutNormalMapSource_WithBinaryAlpha_PreservesSemanticAlpha()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateNormalMapTextureWithBinaryAlpha(128, 128);
            var preprocessor = new NormalMapPreprocessor();
            preprocessor.PrepareForCompression(
                source,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true
            );
            EditorUtility.CompressTexture(
                source,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );
            Assert.AreEqual(TextureFormat.BC7, source.format);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.BC7, result.format);

            var pixels = result.GetPixels();
            float minAlpha = pixels.Min(p => p.a);
            float maxAlpha = pixels.Max(p => p.a);
            Assert.That(
                minAlpha,
                Is.LessThan(0.25f),
                "Binary alpha low values should be preserved"
            );
            Assert.That(
                maxAlpha,
                Is.GreaterThan(0.75f),
                "Binary alpha high values should be preserved"
            );

            _createdObjects.Add(result);
        }

        [Test]
        public void Compress_BC7RGBLayoutNormalMapSource_WithOpaqueAlpha_PreservesNegativeZ()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateBC7RGBLayoutObjectSpaceNormalWithOpaqueAlpha(128, 128);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.BC7, result.format);

            var pixels = result.GetPixels();
            int row = result.height / 2;
            int leftIndex = row * result.width + (result.width / 4);
            int rightIndex = row * result.width + (result.width * 3 / 4);

            float leftZ = pixels[leftIndex].b * 2f - 1f;
            float rightZ = pixels[rightIndex].b * 2f - 1f;

            Assert.That(
                leftZ,
                Is.LessThan(-0.2f),
                "RGB-layout opaque-alpha source should keep negative Z on left half"
            );
            Assert.That(
                rightZ,
                Is.GreaterThan(0.2f),
                "RGB-layout opaque-alpha source should keep positive Z on right half"
            );

            _createdObjects.Add(result);
        }

        [Test]
        public void Compress_BC7RGBLayoutNormalMapSource_WithSingleNegativeZOpaqueAlpha_PreservesNegativeZ()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateBC7RGBLayoutObjectSpaceNormalWithOpaqueAlphaSingleSign(
                128,
                128,
                encodedZ: 0
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.BC7, result.format);

            var pixels = result.GetPixels();
            int centerIndex = (result.height / 2) * result.width + (result.width / 2);
            float centerZ = pixels[centerIndex].b * 2f - 1f;
            Assert.That(
                centerZ,
                Is.LessThan(-0.2f),
                "Single-sign negative-Z source should keep negative Z after recompression"
            );

            _createdObjects.Add(result);
        }

        [Test]
        public void Compress_BC7RGLayoutNormalMapSource_WithBinaryAlpha_PreservesSemanticAlpha()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateBC7RGLayoutNormalMapWithBinaryAlpha(128, 128);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.BC7, result.format);

            var pixels = result.GetPixels();
            float minAlpha = pixels.Min(p => p.a);
            float maxAlpha = pixels.Max(p => p.a);
            Assert.That(
                minAlpha,
                Is.LessThan(0.25f),
                "RG-layout source should keep low binary alpha values"
            );
            Assert.That(
                maxAlpha,
                Is.GreaterThan(0.75f),
                "RG-layout source should keep high binary alpha values"
            );

            _createdObjects.Add(result);
        }

        [Test]
        public void Compress_BC7RGLayoutNormalMapSource_WithZeroBlue_PreservesPositiveZ()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateBC7RGLayoutNormalMapWithBinaryAlpha(128, 128, bValue: 0);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.BC7, result.format);

            var pixels = result.GetPixels();
            int centerIndex = (result.height / 2) * result.width + (result.width / 2);
            float centerZ = pixels[centerIndex].b * 2f - 1f;
            Assert.That(
                centerZ,
                Is.GreaterThan(0.2f),
                "RG-layout source with B=0 should keep positive Z after recompression"
            );

            _createdObjects.Add(result);
        }

        [Test]
        public void Compress_BC7RGLayoutNormalMapSource_WithOpaqueAlpha_PreservesNormalDirection()
        {
            var config = CreateConfig();
            config.MinSourceSize = 64;
            config.SkipIfSmallerThan = 0;
            config.TargetPlatform = CompressionPlatform.Desktop;
            config.ProcessNormalMaps = true;
            var service = new TextureCompressorService(config);

            var source = CreateBC7RGLayoutNormalMapWithOpaqueAlpha(128, 128, bValue: 0);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_BumpMap", source);
            renderer.sharedMaterial = material;

            service.Compress(root, false);

            var result = renderer.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
            Assert.IsNotNull(result);
            Assert.AreEqual(TextureFormat.BC7, result.format);

            var pixels = result.GetPixels();
            int sampleX = result.width * 3 / 4;
            int sampleY = result.height / 4;
            int sampleIndex = sampleY * result.width + sampleX;

            float decodedX = pixels[sampleIndex].a * 2f - 1f;
            float decodedY = pixels[sampleIndex].g * 2f - 1f;
            float expectedX = (sampleX / (float)result.width - 0.5f) * 0.6f;
            float expectedY = (sampleY / (float)result.height - 0.5f) * 0.6f;

            Assert.That(
                decodedX,
                Is.EqualTo(expectedX).Within(0.2f),
                "Opaque-alpha RG-layout source should preserve X direction"
            );
            Assert.That(
                decodedY,
                Is.EqualTo(expectedY).Within(0.2f),
                "Opaque-alpha RG-layout source should preserve Y direction"
            );

            _createdObjects.Add(result);
        }

        #endregion

        #region Layout Detection Integration Tests

        [Test]
        public void DetectDXTnmLikeSourceLayout_BC7RGBLayout_WithMixedSignedZAndOpaqueAlpha_ReturnsRGB()
        {
            var source = CreateBC7RGBLayoutObjectSpaceNormalWithOpaqueAlpha(128, 128);

            var layout = NormalMapSourceLayoutDetector.DetectDXTnmLike(source);

            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.RGB,
                layout,
                "Mixed signed-Z BC7 RGB-layout source should be detected as RGB"
            );
        }

        [Test]
        [TestCase((byte)0, "NegativeZ", NormalMapPreprocessor.SourceLayout.RGB)]
        [TestCase((byte)255, "PositiveZ", NormalMapPreprocessor.SourceLayout.RGB)]
        public void DetectDXTnmLikeSourceLayout_BC7RGBLayout_WithSingleSignedZOpaqueAlpha_ReturnsExpectedLayout(
            byte encodedZ,
            string signedZLabel,
            NormalMapPreprocessor.SourceLayout expectedLayout
        )
        {
            var source = CreateBC7RGBLayoutObjectSpaceNormalWithOpaqueAlphaSingleSign(
                128,
                128,
                encodedZ
            );

            var layout = NormalMapSourceLayoutDetector.DetectDXTnmLike(source);

            Assert.AreEqual(
                expectedLayout,
                layout,
                $"Single-sign {signedZLabel} detection mismatch"
            );
        }

        [Test]
        public void DetectDXTnmLike_AGLayout_WithRBNearOne_ReturnsAG()
        {
            var source = CreateCompressedDXT5NormalMap(
                128,
                128,
                0.35f,
                -0.25f,
                sourceIsAGLayout: true
            );

            var layout = NormalMapSourceLayoutDetector.DetectDXTnmLike(source);

            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.AG,
                layout,
                "Standard DXTnm layout (R/B near 1, XY in AG) should be detected as AG"
            );
        }

        [Test]
        public void DetectDXTnmLike_AGLayout_WithoutRBNearOne_ReturnsAG()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            _createdObjects.Add(texture);
            var pixels = new Color32[64 * 64];

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float nx = (x / 63f - 0.5f) * 0.6f;
                    float ny = (y / 63f - 0.5f) * 0.6f;

                    byte encodedA = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte encodedG = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);

                    pixels[y * 64 + x] = new Color32(0, encodedG, 0, encodedA);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var layout = NormalMapSourceLayoutDetector.DetectDXTnmLike(texture);

            Assert.AreEqual(
                NormalMapPreprocessor.SourceLayout.AG,
                layout,
                "AG layout with non-DXTnm R/B should still be detected as AG when AG is more plausible"
            );
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
                float x = ((i % width) / (float)width - 0.5f) * 0.6f;
                float y = ((i / width) / (float)height - 0.5f) * 0.6f;

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

        private Texture2D CreateNormalMapTextureWithBinaryAlpha(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float nx = (x / (float)width - 0.5f) * 0.6f;
                    float ny = (y / (float)height - 0.5f) * 0.6f;

                    byte r = (byte)((nx * 0.5f + 0.5f) * 255f);
                    byte g = (byte)((ny * 0.5f + 0.5f) * 255f);
                    byte a = ((x / 8 + y / 8) % 2 == 0) ? (byte)0 : (byte)255;
                    pixels[index] = new Color32(r, g, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/NormalMapBinaryAlpha_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateCompressedDXT5NormalMap(
            int width,
            int height,
            float x,
            float y,
            bool sourceIsAGLayout
        )
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];

            byte encodedX = (byte)Mathf.Clamp((x * 0.5f + 0.5f) * 255f, 0f, 255f);
            byte encodedY = (byte)Mathf.Clamp((y * 0.5f + 0.5f) * 255f, 0f, 255f);

            Color32 packedPixel = sourceIsAGLayout
                ? new Color32(255, encodedY, 255, encodedX)
                : new Color32(encodedX, encodedY, 255, 255);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = packedPixel;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            EditorUtility.CompressTexture(
                texture,
                TextureFormat.DXT5,
                TextureCompressionQuality.Best
            );

            string layout = sourceIsAGLayout ? "AG" : "RG";
            string assetPath =
                $"{TestAssetFolder}/NormalMapDXT5_{layout}_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateBC7RGLayoutNormalMapWithBinaryAlpha(
            int width,
            int height,
            byte bValue = 128
        )
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float nx = (x / (float)width - 0.5f) * 0.6f;
                    float ny = (y / (float)height - 0.5f) * 0.6f;

                    byte r = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte g = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte a = ((x / 8 + y / 8) % 2 == 0) ? (byte)0 : (byte)255;
                    pixels[index] = new Color32(r, g, bValue, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            EditorUtility.CompressTexture(
                texture,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            string assetPath =
                $"{TestAssetFolder}/NormalMapBC7_RG_BinaryAlpha_B{bValue}_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateBC7RGLayoutNormalMapWithOpaqueAlpha(
            int width,
            int height,
            byte bValue = 128
        )
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float nx = (x / (float)width - 0.5f) * 0.6f;
                    float ny = (y / (float)height - 0.5f) * 0.6f;

                    byte r = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                    byte g = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                    pixels[index] = new Color32(r, g, bValue, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            EditorUtility.CompressTexture(
                texture,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            string assetPath =
                $"{TestAssetFolder}/NormalMapBC7_RG_OpaqueAlpha_B{bValue}_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateBC7RGBLayoutObjectSpaceNormalWithOpaqueAlpha(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    byte encodedZ = x < width / 2 ? (byte)0 : (byte)255;
                    pixels[index] = new Color32(128, 128, encodedZ, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var preprocessor = new NormalMapPreprocessor();
            preprocessor.PrepareForCompression(
                texture,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true,
                sourceLayout: NormalMapPreprocessor.SourceLayout.RGB
            );
            EditorUtility.CompressTexture(
                texture,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            string assetPath =
                $"{TestAssetFolder}/NormalMapBC7_RGB_OpaqueAlpha_ObjectSpace_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private Texture2D CreateBC7RGBLayoutObjectSpaceNormalWithOpaqueAlphaSingleSign(
            int width,
            int height,
            byte encodedZ
        )
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(128, 128, encodedZ, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var preprocessor = new NormalMapPreprocessor();
            preprocessor.PrepareForCompression(
                texture,
                TextureFormat.RGBA32,
                TextureFormat.BC7,
                preserveAlpha: true,
                sourceLayout: NormalMapPreprocessor.SourceLayout.RGB
            );
            EditorUtility.CompressTexture(
                texture,
                TextureFormat.BC7,
                TextureCompressionQuality.Best
            );

            string assetPath =
                $"{TestAssetFolder}/NormalMapBC7_RGB_SingleSign_{encodedZ}_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        #endregion
    }
}
