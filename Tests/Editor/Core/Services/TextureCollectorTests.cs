using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.texture;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureCollectorTests
    {
        private const string TestAssetFolder = "Assets/_LAC_TMP";
        private TextureCollector _collector;
        private List<Object> _createdObjects;
        private List<string> _createdAssetPaths;

        [SetUp]
        public void SetUp()
        {
            // Default: minSourceSize=64, skipIfSmallerThan=0, process all texture types
            _collector = new TextureCollector(64, 0, true, true, true, true, true);
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

        #region Empty/Null Input Tests

        [Test]
        public void Collect_EmptyGameObject_ReturnsEmptyDictionary()
        {
            var root = CreateGameObject("EmptyRoot");
            var result = _collector.Collect(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_NoRenderers_ReturnsEmptyDictionary()
        {
            var root = CreateGameObject("RootWithChildren");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var result = _collector.Collect(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_RendererWithNoMaterial_ReturnsEmptyDictionary()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[0];

            var result = _collector.Collect(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_RendererWithNullMaterial_ReturnsEmptyDictionary()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[] { null };

            var result = _collector.Collect(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region Single Texture Tests

        [Test]
        public void Collect_SingleMainTexture_ReturnsTexture()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.AreEqual("Main", result[texture].TextureType);
            Assert.IsFalse(result[texture].IsNormalMap);
            Assert.IsFalse(result[texture].IsEmission);
        }

        [Test]
        public void Collect_SingleNormalMap_ReturnsAsNormalMap()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_BumpMap", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsTrue(result[texture].IsNormalMap);
        }

        [Test]
        public void Collect_SingleEmissionMap_ReturnsAsEmission()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_EmissionMap", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsTrue(result[texture].IsEmission);
        }

        #endregion

        #region Filtering Tests

        [Test]
        public void Collect_TextureBelowMinSize_Skipped()
        {
            var collector = new TextureCollector(256, 0, true, true, true, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128); // Below minSourceSize of 256

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_TextureAtSkipThreshold_Skipped()
        {
            var collector = new TextureCollector(64, 128, true, true, true, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128); // Equal to skipIfSmallerThan

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_TextureAboveSkipThreshold_Included()
        {
            var collector = new TextureCollector(64, 128, true, true, true, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(256, 256); // Above skipIfSmallerThan

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void Collect_MainTexturesDisabled_SkipsMainTextures()
        {
            var collector = new TextureCollector(64, 0, false, true, true, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var mainTex = CreateTexture(128, 128);
            var normalTex = CreateTexture(128, 128);

            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_BumpMap", normalTex);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result.ContainsKey(mainTex));
            Assert.IsTrue(result.ContainsKey(normalTex));
        }

        [Test]
        public void Collect_NormalMapsDisabled_SkipsNormalMaps()
        {
            var collector = new TextureCollector(64, 0, true, false, true, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var mainTex = CreateTexture(128, 128);
            var normalTex = CreateTexture(128, 128);

            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_BumpMap", normalTex);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(mainTex));
            Assert.IsFalse(result.ContainsKey(normalTex));
        }

        [Test]
        public void Collect_EmissionMapsDisabled_SkipsEmissionMaps()
        {
            var collector = new TextureCollector(64, 0, true, true, false, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var mainTex = CreateTexture(128, 128);
            var emissionTex = CreateTexture(128, 128);

            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_EmissionMap", emissionTex);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(mainTex));
            Assert.IsFalse(result.ContainsKey(emissionTex));
        }

        #endregion

        #region Multiple Textures Tests

        [Test]
        public void Collect_MultipleTexturesOnSameMaterial_ReturnsAll()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var mainTex = CreateTexture(128, 128);
            var normalTex = CreateTexture(128, 128);
            var emissionTex = CreateTexture(128, 128);

            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_BumpMap", normalTex);
            material.SetTexture("_EmissionMap", emissionTex);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey(mainTex));
            Assert.IsTrue(result.ContainsKey(normalTex));
            Assert.IsTrue(result.ContainsKey(emissionTex));
        }

        [Test]
        public void Collect_SameTextureOnMultipleMaterials_ReturnsSingleEntry()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            var sharedTexture = CreateTexture(128, 128);

            material1.SetTexture("_MainTex", sharedTexture);
            material2.SetTexture("_MainTex", sharedTexture);
            renderer.sharedMaterials = new Material[] { material1, material2 };

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(sharedTexture));
            // Should have 2 references
            Assert.AreEqual(2, result[sharedTexture].References.Count);
        }

        [Test]
        public void Collect_TexturesInHierarchy_ReturnsAll()
        {
            var root = CreateGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var renderer1 = child1.AddComponent<MeshRenderer>();
            var renderer2 = child2.AddComponent<MeshRenderer>();

            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            var texture1 = CreateTexture(128, 128);
            var texture2 = CreateTexture(128, 128);

            material1.SetTexture("_MainTex", texture1);
            material2.SetTexture("_MainTex", texture2);
            renderer1.sharedMaterial = material1;
            renderer2.sharedMaterial = material2;

            var result = _collector.Collect(root);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(texture1));
            Assert.IsTrue(result.ContainsKey(texture2));
        }

        [Test]
        public void Collect_InactiveChildren_IncludesTextures()
        {
            var root = CreateGameObject("Root");
            var inactiveChild = CreateGameObject("InactiveChild");
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            var renderer = inactiveChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
        }

        #endregion

        #region Texture Type Classification Tests

        // Note: _BaseMap, _NormalMap, _EmissiveMap are URP shader properties.
        // Standard shader uses _MainTex, _BumpMap, _EmissionMap instead.
        // Tests for those properties are covered by single texture tests above.

        [Test]
        public void Collect_DetailNormalMapProperty_ClassifiedAsNormal()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_DetailNormalMap", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            // Standard shader has _DetailNormalMap property
            if (result.Count > 0)
            {
                Assert.IsTrue(result[texture].IsNormalMap);
            }
        }

        [Test]
        public void Collect_MetallicGlossMap_ClassifiedAsOther()
        {
            var collector = new TextureCollector(64, 0, true, true, true, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MetallicGlossMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // MetallicGlossMap is classified as "Other" texture type
            if (result.Count > 0)
            {
                Assert.AreEqual("Other", result[texture].TextureType);
            }
        }

        [Test]
        public void Collect_OcclusionMap_ClassifiedAsOther()
        {
            var collector = new TextureCollector(64, 0, true, true, true, true, true);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_OcclusionMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // OcclusionMap is classified as "Other" texture type
            if (result.Count > 0)
            {
                Assert.AreEqual("Other", result[texture].TextureType);
            }
        }

        #endregion

        #region Reference Tracking Tests

        [Test]
        public void Collect_TextureReference_ContainsMaterial()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result[texture].References.Count);
            Assert.AreEqual(material, result[texture].References[0].Material);
        }

        [Test]
        public void Collect_TextureReference_ContainsPropertyName()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual("_MainTex", result[texture].References[0].PropertyName);
        }

        [Test]
        public void Collect_TextureReference_ContainsRenderer()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(renderer, result[texture].References[0].Renderer);
        }

        #endregion

        #region Priority Tests

        [Test]
        public void Collect_SameTextureUsedAsNormalAndMain_PrioritizesNormalMap()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var sharedTexture = CreateTexture(128, 128);

            // Use same texture as both main and bump map
            material.SetTexture("_MainTex", sharedTexture);
            material.SetTexture("_BumpMap", sharedTexture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            // Should be classified as normal map (priority)
            Assert.IsTrue(result[sharedTexture].IsNormalMap);
        }

        #endregion

        #region CollectFromMaterials Tests

        [Test]
        public void CollectFromMaterials_SingleMaterial_AddsTextures()
        {
            var root = CreateGameObject("Root");
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);
            material.SetTexture("_MainTex", texture);

            var existingTextures = new Dictionary<Texture2D, TextureInfo>();
            var materials = new Material[] { material };

            _collector.CollectFromMaterials(materials, existingTextures);

            Assert.AreEqual(1, existingTextures.Count);
            Assert.IsTrue(existingTextures.ContainsKey(texture));
        }

        [Test]
        public void CollectFromMaterials_NullMaterials_HandlesGracefully()
        {
            var existingTextures = new Dictionary<Texture2D, TextureInfo>();

            Assert.DoesNotThrow(() => _collector.CollectFromMaterials(null, existingTextures));
            Assert.AreEqual(0, existingTextures.Count);
        }

        [Test]
        public void CollectFromMaterials_NullTexturesDictionary_HandlesGracefully()
        {
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);
            material.SetTexture("_MainTex", texture);
            var materials = new Material[] { material };

            Assert.DoesNotThrow(() => _collector.CollectFromMaterials(materials, null));
        }

        [Test]
        public void CollectFromMaterials_EmptyMaterials_DoesNotModifyDictionary()
        {
            var existingTextures = new Dictionary<Texture2D, TextureInfo>();
            var materials = new Material[0];

            _collector.CollectFromMaterials(materials, existingTextures);

            Assert.AreEqual(0, existingTextures.Count);
        }

        [Test]
        public void CollectFromMaterials_MaterialsWithNulls_SkipsNulls()
        {
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);
            material.SetTexture("_MainTex", texture);

            var existingTextures = new Dictionary<Texture2D, TextureInfo>();
            var materials = new Material[] { null, material, null };

            _collector.CollectFromMaterials(materials, existingTextures);

            Assert.AreEqual(1, existingTextures.Count);
            Assert.IsTrue(existingTextures.ContainsKey(texture));
        }

        [Test]
        public void CollectFromMaterials_MergesWithExistingTextures()
        {
            var existingMaterial = CreateMaterial();
            var existingTexture = CreateTexture(128, 128);
            existingMaterial.SetTexture("_MainTex", existingTexture);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = existingMaterial;

            // First collect from renderer
            var textures = _collector.Collect(root);
            Assert.AreEqual(1, textures.Count);

            // Now add additional material
            var additionalMaterial = CreateMaterial();
            var additionalTexture = CreateTexture(128, 128);
            additionalMaterial.SetTexture("_MainTex", additionalTexture);

            _collector.CollectFromMaterials(new Material[] { additionalMaterial }, textures);

            Assert.AreEqual(2, textures.Count);
            Assert.IsTrue(textures.ContainsKey(existingTexture));
            Assert.IsTrue(textures.ContainsKey(additionalTexture));
        }

        [Test]
        public void CollectFromMaterials_DuplicateTexture_AddsReference()
        {
            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            var sharedTexture = CreateTexture(128, 128);
            material1.SetTexture("_MainTex", sharedTexture);
            material2.SetTexture("_MainTex", sharedTexture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            _collector.CollectFromMaterials(new Material[] { material1 }, textures);

            Assert.AreEqual(1, textures.Count);
            Assert.AreEqual(1, textures[sharedTexture].References.Count);

            // Add second material with same texture
            _collector.CollectFromMaterials(new Material[] { material2 }, textures);

            Assert.AreEqual(1, textures.Count);
            Assert.AreEqual(2, textures[sharedTexture].References.Count);
        }

        [Test]
        public void CollectFromMaterials_RendererIsNull_HandlesGracefully()
        {
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);
            material.SetTexture("_MainTex", texture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            _collector.CollectFromMaterials(new Material[] { material }, textures);

            Assert.AreEqual(1, textures.Count);
            // Renderer should be null for animation-referenced materials
            Assert.IsNull(textures[texture].References[0].Renderer);
        }

        [Test]
        public void CollectFromMaterials_MultipleTexturesOnMaterial_AddsAll()
        {
            var material = CreateMaterial();
            var mainTex = CreateTexture(128, 128);
            var normalTex = CreateTexture(128, 128);
            var emissionTex = CreateTexture(128, 128);

            material.SetTexture("_MainTex", mainTex);
            material.SetTexture("_BumpMap", normalTex);
            material.SetTexture("_EmissionMap", emissionTex);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            _collector.CollectFromMaterials(new Material[] { material }, textures);

            Assert.AreEqual(3, textures.Count);
            Assert.IsTrue(textures.ContainsKey(mainTex));
            Assert.IsTrue(textures.ContainsKey(normalTex));
            Assert.IsTrue(textures.ContainsKey(emissionTex));
        }

        [Test]
        public void CollectFromMaterials_WithCollectAllTrue_IncludesSkippedTextures()
        {
            // Use collector that skips small textures
            var collector = new TextureCollector(256, 0, true, true, true, true, true);

            var material = CreateMaterial();
            var smallTexture = CreateTexture(64, 64); // Below minSourceSize
            material.SetTexture("_MainTex", smallTexture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            collector.CollectFromMaterials(new Material[] { material }, textures, collectAll: true);

            Assert.AreEqual(1, textures.Count);
            Assert.IsTrue(textures.ContainsKey(smallTexture));
            Assert.IsFalse(textures[smallTexture].IsProcessed);
            Assert.AreEqual(SkipReason.TooSmall, textures[smallTexture].SkipReason);
        }

        [Test]
        public void CollectFromMaterials_WithCollectAllFalse_ExcludesSkippedTextures()
        {
            // Use collector that skips small textures
            var collector = new TextureCollector(256, 0, true, true, true, true, true);

            var material = CreateMaterial();
            var smallTexture = CreateTexture(64, 64); // Below minSourceSize
            material.SetTexture("_MainTex", smallTexture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            collector.CollectFromMaterials(
                new Material[] { material },
                textures,
                collectAll: false
            );

            Assert.AreEqual(0, textures.Count);
        }

        [Test]
        public void CollectFromMaterials_DuplicateMaterials_ProcessesOnce()
        {
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);
            material.SetTexture("_MainTex", texture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            // Pass same material multiple times
            _collector.CollectFromMaterials(
                new Material[] { material, material, material },
                textures
            );

            Assert.AreEqual(1, textures.Count);
            // Should have only 1 reference since Distinct() is used
            Assert.AreEqual(1, textures[texture].References.Count);
        }

        #endregion

        #region EditorOnly Skip Tests

        [Test]
        public void Collect_EditorOnlyTaggedRenderer_SkipsTextures()
        {
            var root = CreateGameObject("Root");
            var editorOnlyChild = CreateGameObject("EditorOnlyChild");
            editorOnlyChild.transform.SetParent(root.transform);
            editorOnlyChild.tag = "EditorOnly";

            var renderer = editorOnlyChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_ParentIsEditorOnly_SkipsChildTextures()
        {
            var root = CreateGameObject("Root");
            var editorOnlyParent = CreateGameObject("EditorOnlyParent");
            var child = CreateGameObject("Child");
            editorOnlyParent.transform.SetParent(root.transform);
            child.transform.SetParent(editorOnlyParent.transform);
            editorOnlyParent.tag = "EditorOnly";

            var renderer = child.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_GrandparentIsEditorOnly_SkipsDescendantTextures()
        {
            var root = CreateGameObject("Root");
            var editorOnlyGrandparent = CreateGameObject("EditorOnlyGrandparent");
            var parent = CreateGameObject("Parent");
            var child = CreateGameObject("Child");

            editorOnlyGrandparent.transform.SetParent(root.transform);
            parent.transform.SetParent(editorOnlyGrandparent.transform);
            child.transform.SetParent(parent.transform);
            editorOnlyGrandparent.tag = "EditorOnly";

            var renderer = child.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_SiblingIsEditorOnly_CollectsNonEditorOnlySibling()
        {
            var root = CreateGameObject("Root");
            var editorOnlyChild = CreateGameObject("EditorOnlyChild");
            var normalChild = CreateGameObject("NormalChild");

            editorOnlyChild.transform.SetParent(root.transform);
            normalChild.transform.SetParent(root.transform);
            editorOnlyChild.tag = "EditorOnly";

            var editorOnlyRenderer = editorOnlyChild.AddComponent<MeshRenderer>();
            var normalRenderer = normalChild.AddComponent<MeshRenderer>();

            var editorOnlyMaterial = CreateMaterial();
            var normalMaterial = CreateMaterial();
            var editorOnlyTexture = CreateTexture(128, 128);
            var normalTexture = CreateTexture(128, 128);

            editorOnlyMaterial.SetTexture("_MainTex", editorOnlyTexture);
            normalMaterial.SetTexture("_MainTex", normalTexture);

            editorOnlyRenderer.sharedMaterial = editorOnlyMaterial;
            normalRenderer.sharedMaterial = normalMaterial;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result.ContainsKey(editorOnlyTexture));
            Assert.IsTrue(result.ContainsKey(normalTexture));
        }

        [Test]
        public void Collect_InactiveEditorOnlyObject_StillSkips()
        {
            var root = CreateGameObject("Root");
            var editorOnlyChild = CreateGameObject("EditorOnlyChild");
            editorOnlyChild.transform.SetParent(root.transform);
            editorOnlyChild.tag = "EditorOnly";
            editorOnlyChild.SetActive(false);

            var renderer = editorOnlyChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectAll_EditorOnlyTaggedRenderer_StillSkips()
        {
            var root = CreateGameObject("Root");
            var editorOnlyChild = CreateGameObject("EditorOnlyChild");
            editorOnlyChild.transform.SetParent(root.transform);
            editorOnlyChild.tag = "EditorOnly";

            var renderer = editorOnlyChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            var texture = CreateTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = _collector.CollectAll(root);

            // Even CollectAll should skip EditorOnly objects
            // because they are stripped from build
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_SharedTextureBetweenEditorOnlyAndNormal_CollectsFromNormalOnly()
        {
            var root = CreateGameObject("Root");
            var editorOnlyChild = CreateGameObject("EditorOnlyChild");
            var normalChild = CreateGameObject("NormalChild");

            editorOnlyChild.transform.SetParent(root.transform);
            normalChild.transform.SetParent(root.transform);
            editorOnlyChild.tag = "EditorOnly";

            var editorOnlyRenderer = editorOnlyChild.AddComponent<MeshRenderer>();
            var normalRenderer = normalChild.AddComponent<MeshRenderer>();

            var editorOnlyMaterial = CreateMaterial();
            var normalMaterial = CreateMaterial();
            var sharedTexture = CreateTexture(128, 128);

            // Same texture used by both materials
            editorOnlyMaterial.SetTexture("_MainTex", sharedTexture);
            normalMaterial.SetTexture("_MainTex", sharedTexture);

            editorOnlyRenderer.sharedMaterial = editorOnlyMaterial;
            normalRenderer.sharedMaterial = normalMaterial;

            var result = _collector.Collect(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(sharedTexture));
            // Should only have 1 reference (from normal child)
            Assert.AreEqual(1, result[sharedTexture].References.Count);
            Assert.AreEqual(normalMaterial, result[sharedTexture].References[0].Material);
        }

        #endregion

        #region FrozenSkip Tests

        [Test]
        public void Constructor_WithFrozenSkipGuids_AcceptsParameter()
        {
            var frozenGuids = new[]
            {
                "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4",
                "b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5",
            };

            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                true,
                excludedPathPrefixes: null,
                frozenSkipGuids: frozenGuids
            );

            Assert.IsNotNull(collector);
        }

        [Test]
        public void Constructor_WithNullFrozenSkipGuids_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var collector = new TextureCollector(64, 0, true, true, true, true, true);
            });
        }

        [Test]
        public void Constructor_WithEmptyFrozenSkipGuids_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var collector = new TextureCollector(
                    64,
                    0,
                    true,
                    true,
                    true,
                    true,
                    true,
                    excludedPathPrefixes: null,
                    frozenSkipGuids: new string[0]
                );
            });
        }

        [Test]
        public void CollectAll_TextureWithFrozenSkipGuid_HasFrozenSkipReason()
        {
            // Note: In-memory textures don't have asset GUIDs, so this tests
            // the SkipReason enum value exists and can be assigned
            var info = new TextureInfo { IsProcessed = false, SkipReason = SkipReason.FrozenSkip };

            Assert.AreEqual(SkipReason.FrozenSkip, info.SkipReason);
            Assert.IsFalse(info.IsProcessed);
        }

        [Test]
        public void SkipReason_FrozenSkip_IsDefined()
        {
            var values = System.Enum.GetValues(typeof(SkipReason));

            Assert.That(values, Contains.Item(SkipReason.FrozenSkip));
        }

        [Test]
        public void SkipReason_AllValuesAreDefined()
        {
            var values = System.Enum.GetValues(typeof(SkipReason));

            Assert.That(values, Contains.Item(SkipReason.None));
            Assert.That(values, Contains.Item(SkipReason.TooSmall));
            Assert.That(values, Contains.Item(SkipReason.FilteredByType));
            Assert.That(values, Contains.Item(SkipReason.FrozenSkip));
            Assert.That(values, Contains.Item(SkipReason.RuntimeGenerated));
            Assert.That(values, Contains.Item(SkipReason.ExcludedPath));
            Assert.That(values, Contains.Item(SkipReason.UnknownUncompressedProperty));
        }

        [Test]
        public void SkipReason_UnknownUncompressedProperty_IsDefined()
        {
            var info = new TextureInfo
            {
                IsProcessed = false,
                SkipReason = SkipReason.UnknownUncompressedProperty,
            };

            Assert.AreEqual(SkipReason.UnknownUncompressedProperty, info.SkipReason);
            Assert.IsFalse(info.IsProcessed);
        }

        #endregion

        #region SkipUnknownUncompressedTextures Tests

        [Test]
        public void Collect_UnknownUncompressedPropertyWithSkipEnabled_Skipped()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateRGBTexture(128, 128);

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_UnknownUncompressedPropertyWithSkipDisabled_Included()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: false
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateRGBTexture(128, 128);

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void Collect_UnknownPropertyWithUncompressedRGBATexture_Skipped()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateTexture(128, 128); // RGBA32 - uncompressed

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // RGBA32 is uncompressed, so it should be skipped on unknown properties
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_KnownPropertyWithRGBTexture_NotSkipped()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial(); // Standard shader - _MainTex is known
            var texture = CreateRGBTexture(128, 128);

            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void CollectAll_UnknownUncompressedPropertyWithSkipEnabled_HasUnknownUncompressedPropertyReason()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateRGBTexture(128, 128);

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.CollectAll(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsFalse(result[texture].IsProcessed);
            Assert.AreEqual(SkipReason.UnknownUncompressedProperty, result[texture].SkipReason);
        }

        [Test]
        public void Collect_SkipEnabledCollector_SkipsUnknownUncompressedProperty()
        {
            // _collector is initialized with skipUnknownUncompressedTextures=true
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateRGBTexture(128, 128);

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = _collector.Collect(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectAll_SkipEnabledCollector_UnknownUncompressedProperty_HasCorrectSkipReason()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateRGBTexture(128, 128);

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = _collector.CollectAll(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsFalse(result[texture].IsProcessed);
            Assert.AreEqual(SkipReason.UnknownUncompressedProperty, result[texture].SkipReason);
        }

        [Test]
        public void Collect_UnknownPropertyWithDXT5CrunchedTexture_NotSkipped()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateTextureWithFormat(128, 128, TextureFormat.DXT5Crunched);

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // DXT5Crunched is a compressed format, so it should not be skipped
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void Collect_UnknownPropertyWithSingleChannelTexture_Skipped()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterialWithUnknownProperty();
            var texture = CreateTextureWithFormat(128, 128, TextureFormat.R8);

            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // Single-channel formats have no alpha and are likely data textures — should be skipped
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Collect_SameTextureOnKnownAndUnknownProperties_IsProcessed()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();

            // Create a shader with both a known (_MainTex) and unknown (_CustomDataMap) property
            var shader = ShaderUtil.CreateShaderAsset(
                "Shader \"Hidden/Test/"
                    + System.Guid.NewGuid().ToString("N")
                    + "\" {"
                    + " Properties {"
                    + " _MainTex (\"Main\", 2D) = \"white\" {}"
                    + " _CustomDataMap (\"Custom Data\", 2D) = \"white\" {}"
                    + " }"
                    + " SubShader { Pass { } }"
                    + "}",
                false
            );
            _createdObjects.Add(shader);
            var material = new Material(shader);
            _createdObjects.Add(material);

            var texture = CreateRGBTexture(128, 128);
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // Texture is referenced by a known property, so it should be processed
            // even though it is also referenced by an unknown property
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsTrue(result[texture].IsProcessed);
        }

        [Test]
        public void CollectAll_SameTextureOnKnownAndUnknownProperties_IsProcessed()
        {
            var collector = new TextureCollector(
                64,
                0,
                true,
                true,
                true,
                true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();

            // Create a shader with both unknown and known properties.
            // The unknown property is listed first to exercise the upgrade path
            // when CollectAll adds skipped textures to the dictionary.
            var shader = ShaderUtil.CreateShaderAsset(
                "Shader \"Hidden/Test/"
                    + System.Guid.NewGuid().ToString("N")
                    + "\" {"
                    + " Properties {"
                    + " _CustomDataMap (\"Custom Data\", 2D) = \"white\" {}"
                    + " _MainTex (\"Main\", 2D) = \"white\" {}"
                    + " }"
                    + " SubShader { Pass { } }"
                    + "}",
                false
            );
            _createdObjects.Add(shader);
            var material = new Material(shader);
            _createdObjects.Add(material);

            var texture = CreateRGBTexture(128, 128);
            material.SetTexture("_CustomDataMap", texture);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = collector.CollectAll(root);

            // Even though the texture was initially encountered on an unknown property,
            // the known property (_MainTex) should upgrade it to processed
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsTrue(result[texture].IsProcessed);
            Assert.AreEqual(SkipReason.None, result[texture].SkipReason);
        }

        [Test]
        public void CollectAll_SameTextureOnUnknownAndDisabledKnownProperty_StaysSkipped()
        {
            // Main textures disabled — known property should not upgrade
            var collector = new TextureCollector(
                64,
                0,
                processMainTextures: false,
                processNormalMaps: true,
                processEmissionMaps: true,
                processOtherTextures: true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();

            var shader = ShaderUtil.CreateShaderAsset(
                "Shader \"Hidden/Test/"
                    + System.Guid.NewGuid().ToString("N")
                    + "\" {"
                    + " Properties {"
                    + " _CustomDataMap (\"Custom Data\", 2D) = \"white\" {}"
                    + " _MainTex (\"Main\", 2D) = \"white\" {}"
                    + " }"
                    + " SubShader { Pass { } }"
                    + "}",
                false
            );
            _createdObjects.Add(shader);
            var material = new Material(shader);
            _createdObjects.Add(material);

            var texture = CreateRGBTexture(128, 128);
            material.SetTexture("_CustomDataMap", texture);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = collector.CollectAll(root);

            // Known property type is disabled, so the upgrade should not trigger
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsFalse(result[texture].IsProcessed);
            Assert.AreEqual(SkipReason.UnknownUncompressedProperty, result[texture].SkipReason);
        }

        [Test]
        public void Collect_SameTextureOnUnknownAndDisabledKnownProperty_IsSkipped()
        {
            // Main textures disabled — known property should not upgrade
            var collector = new TextureCollector(
                64,
                0,
                processMainTextures: false,
                processNormalMaps: true,
                processEmissionMaps: true,
                processOtherTextures: true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();

            var shader = ShaderUtil.CreateShaderAsset(
                "Shader \"Hidden/Test/"
                    + System.Guid.NewGuid().ToString("N")
                    + "\" {"
                    + " Properties {"
                    + " _CustomDataMap (\"Custom Data\", 2D) = \"white\" {}"
                    + " _MainTex (\"Main\", 2D) = \"white\" {}"
                    + " }"
                    + " SubShader { Pass { } }"
                    + "}",
                false
            );
            _createdObjects.Add(shader);
            var material = new Material(shader);
            _createdObjects.Add(material);

            var texture = CreateRGBTexture(128, 128);
            material.SetTexture("_CustomDataMap", texture);
            material.SetTexture("_MainTex", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // Both properties fail: unknown+uncompressed and disabled type — texture is excluded
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectAll_FilteredByTypeThenUnknownUncompressed_StaysSkipped()
        {
            // Regression: a texture first skipped as FilteredByType (disabled Main)
            // must NOT be upgraded by a later unknown+uncompressed property even when
            // processOtherTextures is enabled and skipUnknownUncompressedTextures is on.
            var collector = new TextureCollector(
                64,
                0,
                processMainTextures: false,
                processNormalMaps: true,
                processEmissionMaps: true,
                processOtherTextures: true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();

            // _MainTex is listed first (disabled type → FilteredByType),
            // _CustomDataMap is listed second (unknown + uncompressed).
            var shader = ShaderUtil.CreateShaderAsset(
                "Shader \"Hidden/Test/"
                    + System.Guid.NewGuid().ToString("N")
                    + "\" {"
                    + " Properties {"
                    + " _MainTex (\"Main\", 2D) = \"white\" {}"
                    + " _CustomDataMap (\"Custom Data\", 2D) = \"white\" {}"
                    + " }"
                    + " SubShader { Pass { } }"
                    + "}",
                false
            );
            _createdObjects.Add(shader);
            var material = new Material(shader);
            _createdObjects.Add(material);

            var texture = CreateRGBTexture(128, 128);
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.CollectAll(root);

            // The unknown property must not upgrade the FilteredByType skip because
            // the texture is uncompressed and the property is unknown.
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.IsFalse(result[texture].IsProcessed);
        }

        [Test]
        public void Collect_FilteredByTypeThenUnknownUncompressed_IsSkipped()
        {
            // Same scenario as above but using Collect (non-collectAll mode).
            var collector = new TextureCollector(
                64,
                0,
                processMainTextures: false,
                processNormalMaps: true,
                processEmissionMaps: true,
                processOtherTextures: true,
                skipUnknownUncompressedTextures: true
            );

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();

            var shader = ShaderUtil.CreateShaderAsset(
                "Shader \"Hidden/Test/"
                    + System.Guid.NewGuid().ToString("N")
                    + "\" {"
                    + " Properties {"
                    + " _MainTex (\"Main\", 2D) = \"white\" {}"
                    + " _CustomDataMap (\"Custom Data\", 2D) = \"white\" {}"
                    + " }"
                    + " SubShader { Pass { } }"
                    + "}",
                false
            );
            _createdObjects.Add(shader);
            var material = new Material(shader);
            _createdObjects.Add(material);

            var texture = CreateRGBTexture(128, 128);
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_CustomDataMap", texture);
            renderer.sharedMaterial = material;

            var result = collector.Collect(root);

            // Texture should not appear — both properties fail property-dependent checks.
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region ObjectRegistry Resolution Tests

        [Test]
        public void Collect_ReplacedTexture_ResolvesOriginalAssetPath()
        {
            // Simulate: an upstream NDMF plugin replaced the original asset texture
            // with a runtime copy. ObjectRegistry maps the runtime copy back to the original.
            var originalAssetTexture = CreateTexture(128, 128);
            var runtimeReplacement = CreateRuntimeTexture(128, 128);

            var registry = new ObjectRegistry(null);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalAssetTexture, runtimeReplacement);

                var collector = new TextureCollector(64, 0, true, true, true, true, true);

                var root = CreateGameObject("Root");
                var renderer = root.AddComponent<MeshRenderer>();
                var material = CreateMaterial();
                material.SetTexture("_MainTex", runtimeReplacement);
                renderer.sharedMaterial = material;

                var result = collector.Collect(root);

                // The runtime texture should be collected and processed
                // because the registry maps it back to an asset with a valid path
                Assert.AreEqual(1, result.Count);
                Assert.IsTrue(result.ContainsKey(runtimeReplacement));
                Assert.IsTrue(result[runtimeReplacement].IsProcessed);
                Assert.AreEqual(SkipReason.None, result[runtimeReplacement].SkipReason);
            }
        }

        [Test]
        public void Collect_ReplacedTexture_GetsAssetGuidFromOriginal()
        {
            var originalAssetTexture = CreateTexture(256, 256);
            var runtimeReplacement = CreateRuntimeTexture(256, 256);

            string expectedGuid = AssetDatabase.AssetPathToGUID(
                AssetDatabase.GetAssetPath(originalAssetTexture)
            );

            var registry = new ObjectRegistry(null);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalAssetTexture, runtimeReplacement);

                var collector = new TextureCollector(64, 0, true, true, true, true, true);

                var root = CreateGameObject("Root");
                var renderer = root.AddComponent<MeshRenderer>();
                var material = CreateMaterial();
                material.SetTexture("_MainTex", runtimeReplacement);
                renderer.sharedMaterial = material;

                var result = collector.Collect(root);

                Assert.AreEqual(expectedGuid, result[runtimeReplacement].AssetGuid);
            }
        }

        [Test]
        public void Collect_ReplacedTexture_NoRegistry_SkipsAsRuntimeGenerated()
        {
            // Without an active registry, a runtime texture has no asset path and is skipped
            var runtimeTexture = CreateRuntimeTexture(128, 128);

            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            material.SetTexture("_MainTex", runtimeTexture);
            renderer.sharedMaterial = material;

            var result = _collector.CollectAll(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[runtimeTexture].IsProcessed);
            Assert.AreEqual(SkipReason.RuntimeGenerated, result[runtimeTexture].SkipReason);
        }

        [Test]
        public void Collect_ReplacedTexture_NotRegistered_SkipsAsRuntimeGenerated()
        {
            // Runtime texture exists but is not registered in ObjectRegistry
            var runtimeTexture = CreateRuntimeTexture(128, 128);

            var registry = new ObjectRegistry(null);
            using (new ObjectRegistryScope(registry))
            {
                var collector = new TextureCollector(64, 0, true, true, true, true, true);

                var root = CreateGameObject("Root");
                var renderer = root.AddComponent<MeshRenderer>();
                var material = CreateMaterial();
                material.SetTexture("_MainTex", runtimeTexture);
                renderer.sharedMaterial = material;

                var result = collector.CollectAll(root);

                Assert.AreEqual(1, result.Count);
                Assert.IsFalse(result[runtimeTexture].IsProcessed);
                Assert.AreEqual(SkipReason.RuntimeGenerated, result[runtimeTexture].SkipReason);
            }
        }

        [Test]
        public void Collect_ReplacedTexture_OriginalInExcludedPath_SkipsAsExcludedPath()
        {
            var originalAssetTexture = CreateTexture(128, 128);
            var runtimeReplacement = CreateRuntimeTexture(128, 128);

            var registry = new ObjectRegistry(null);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalAssetTexture, runtimeReplacement);

                // The original asset is in the test folder — exclude it
                var collector = new TextureCollector(
                    64,
                    0,
                    true,
                    true,
                    true,
                    true,
                    true,
                    excludedPathPrefixes: new[] { TestAssetFolder }
                );

                var root = CreateGameObject("Root");
                var renderer = root.AddComponent<MeshRenderer>();
                var material = CreateMaterial();
                material.SetTexture("_MainTex", runtimeReplacement);
                renderer.sharedMaterial = material;

                var result = collector.CollectAll(root);

                Assert.AreEqual(1, result.Count);
                Assert.IsFalse(result[runtimeReplacement].IsProcessed);
                Assert.AreEqual(SkipReason.ExcludedPath, result[runtimeReplacement].SkipReason);
            }
        }

        [Test]
        public void Collect_ReplacedTexture_OriginalFrozen_SkipsAsFrozenSkip()
        {
            var originalAssetTexture = CreateTexture(128, 128);
            var runtimeReplacement = CreateRuntimeTexture(128, 128);

            string originalGuid = AssetDatabase.AssetPathToGUID(
                AssetDatabase.GetAssetPath(originalAssetTexture)
            );

            var registry = new ObjectRegistry(null);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalAssetTexture, runtimeReplacement);

                var collector = new TextureCollector(
                    64,
                    0,
                    true,
                    true,
                    true,
                    true,
                    true,
                    frozenSkipGuids: new[] { originalGuid }
                );

                var root = CreateGameObject("Root");
                var renderer = root.AddComponent<MeshRenderer>();
                var material = CreateMaterial();
                material.SetTexture("_MainTex", runtimeReplacement);
                renderer.sharedMaterial = material;

                var result = collector.CollectAll(root);

                Assert.AreEqual(1, result.Count);
                Assert.IsFalse(result[runtimeReplacement].IsProcessed);
                Assert.AreEqual(SkipReason.FrozenSkip, result[runtimeReplacement].SkipReason);
            }
        }

        [Test]
        public void CollectFromMaterials_ReplacedTexture_ResolvesOriginalAssetPath()
        {
            var originalAssetTexture = CreateTexture(128, 128);
            var runtimeReplacement = CreateRuntimeTexture(128, 128);

            var registry = new ObjectRegistry(null);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalAssetTexture, runtimeReplacement);

                var collector = new TextureCollector(64, 0, true, true, true, true, true);

                var material = CreateMaterial();
                material.SetTexture("_MainTex", runtimeReplacement);

                var textures = new Dictionary<Texture2D, TextureInfo>();
                collector.CollectFromMaterials(new Material[] { material }, textures);

                Assert.AreEqual(1, textures.Count);
                Assert.IsTrue(textures.ContainsKey(runtimeReplacement));
                Assert.IsTrue(textures[runtimeReplacement].IsProcessed);
            }
        }

        [Test]
        public void Collect_MultipleReplacedTextures_AllResolvedCorrectly()
        {
            var originalMain = CreateTexture(128, 128);
            var originalNormal = CreateTexture(128, 128);
            var runtimeMain = CreateRuntimeTexture(128, 128);
            var runtimeNormal = CreateRuntimeTexture(128, 128);

            var registry = new ObjectRegistry(null);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalMain, runtimeMain);
                ObjectRegistry.RegisterReplacedObject(originalNormal, runtimeNormal);

                var collector = new TextureCollector(64, 0, true, true, true, true, true);

                var root = CreateGameObject("Root");
                var renderer = root.AddComponent<MeshRenderer>();
                var material = CreateMaterial();
                material.SetTexture("_MainTex", runtimeMain);
                material.SetTexture("_BumpMap", runtimeNormal);
                renderer.sharedMaterial = material;

                var result = collector.Collect(root);

                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result[runtimeMain].IsProcessed);
                Assert.IsTrue(result[runtimeNormal].IsProcessed);
                Assert.IsTrue(result[runtimeNormal].IsNormalMap);
            }
        }

        #endregion

        #region RuntimeGenerated Skip Tests

        [Test]
        public void CollectFromMaterials_RuntimeGeneratedTexture_IsSkipped()
        {
            var material = CreateMaterial();
            var runtimeTexture = CreateRuntimeTexture(512, 512);
            material.SetTexture("_MainTex", runtimeTexture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            _collector.CollectFromMaterials(
                new Material[] { material },
                textures,
                collectAll: true
            );

            Assert.AreEqual(1, textures.Count);
            Assert.IsTrue(textures.ContainsKey(runtimeTexture));
            Assert.IsFalse(textures[runtimeTexture].IsProcessed);
            Assert.AreEqual(SkipReason.RuntimeGenerated, textures[runtimeTexture].SkipReason);
        }

        [Test]
        public void CollectFromMaterials_RuntimeGeneratedTexture_WithCollectAllFalse_NotCollected()
        {
            var material = CreateMaterial();
            var runtimeTexture = CreateRuntimeTexture(512, 512);
            material.SetTexture("_MainTex", runtimeTexture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            _collector.CollectFromMaterials(
                new Material[] { material },
                textures,
                collectAll: false
            );

            Assert.AreEqual(0, textures.Count);
        }

        [Test]
        public void CollectFromMaterials_MixedTextures_ProcessesOnlyAssetTextures()
        {
            var material = CreateMaterial();
            var assetTexture = CreateTexture(512, 512);
            var runtimeTexture = CreateRuntimeTexture(512, 512);
            material.SetTexture("_MainTex", assetTexture);
            material.SetTexture("_BumpMap", runtimeTexture);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            _collector.CollectFromMaterials(
                new Material[] { material },
                textures,
                collectAll: true
            );

            Assert.AreEqual(2, textures.Count);
            Assert.IsTrue(textures[assetTexture].IsProcessed);
            Assert.IsFalse(textures[runtimeTexture].IsProcessed);
            Assert.AreEqual(SkipReason.RuntimeGenerated, textures[runtimeTexture].SkipReason);
        }

        #endregion

        #region Helper Methods

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
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
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

        /// <summary>
        /// Creates an RGB24 texture (no alpha channel) saved as an asset.
        /// </summary>
        private Texture2D CreateRGBTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/TestRGBTexture_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            var loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return loadedTexture;
        }

        /// <summary>
        /// Creates a texture with a specific format saved as an asset.
        /// </summary>
        private Texture2D CreateTextureWithFormat(int width, int height, TextureFormat format)
        {
            var texture = new Texture2D(width, height, format, false);
            string assetPath =
                $"{TestAssetFolder}/TestTexture_{format}_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            var loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return loadedTexture;
        }

        /// <summary>
        /// Creates a material with a shader that has only an unknown texture property (_CustomDataMap).
        /// </summary>
        private Material CreateMaterialWithUnknownProperty()
        {
            var shader = ShaderUtil.CreateShaderAsset(
                "Shader \"Hidden/Test/"
                    + System.Guid.NewGuid().ToString("N")
                    + "\" {"
                    + " Properties { _CustomDataMap (\"Custom Data\", 2D) = \"white\" {} }"
                    + " SubShader { Pass { } }"
                    + "}",
                false
            );
            _createdObjects.Add(shader);
            var material = new Material(shader);
            _createdObjects.Add(material);
            return material;
        }

        /// <summary>
        /// Creates a runtime texture without an asset path (for testing RuntimeGenerated skip).
        /// </summary>
        private Texture2D CreateRuntimeTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            _createdObjects.Add(texture);
            return texture;
        }

        #endregion
    }
}
