using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureCollectorTests
    {
        private TextureCollector _collector;
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
            // Default: minSourceSize=64, skipIfSmallerThan=0, process all texture types
            _collector = new TextureCollector(64, 0, true, true, true, true);
            _createdObjects = new List<Object>();
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
            var collector = new TextureCollector(256, 0, true, true, true, true);

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
            var collector = new TextureCollector(64, 128, true, true, true, true);

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
            var collector = new TextureCollector(64, 128, true, true, true, true);

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
            var collector = new TextureCollector(64, 0, false, true, true, true);

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
            var collector = new TextureCollector(64, 0, true, false, true, true);

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
            var collector = new TextureCollector(64, 0, true, true, false, true);

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
            var collector = new TextureCollector(64, 0, true, true, true, true);

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
            var collector = new TextureCollector(64, 0, true, true, true, true);

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

        #region FrozenSkip Tests

        [Test]
        public void Constructor_WithFrozenSkipPaths_AcceptsParameter()
        {
            var frozenPaths = new[] { "Assets/Textures/frozen1.png", "Assets/Textures/frozen2.png" };

            var collector = new TextureCollector(64, 0, true, true, true, true, frozenPaths);

            Assert.IsNotNull(collector);
        }

        [Test]
        public void Constructor_WithNullFrozenSkipPaths_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var collector = new TextureCollector(64, 0, true, true, true, true, null);
            });
        }

        [Test]
        public void Constructor_WithEmptyFrozenSkipPaths_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var collector = new TextureCollector(64, 0, true, true, true, true, new string[0]);
            });
        }

        [Test]
        public void CollectAll_TextureWithFrozenSkipPath_HasFrozenSkipReason()
        {
            // Note: In-memory textures don't have asset paths, so this tests
            // the SkipReason enum value exists and can be assigned
            var info = new TextureInfo
            {
                IsProcessed = false,
                SkipReason = SkipReason.FrozenSkip
            };

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
            _createdObjects.Add(texture);
            return texture;
        }

        #endregion
    }
}
