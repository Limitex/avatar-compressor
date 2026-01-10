using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class MaterialCollectorTests
    {
        private List<Object> _createdObjects;

        [SetUp]
        public void SetUp()
        {
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

        #region CollectFromRenderers - Empty/Null Tests

        [Test]
        public void CollectFromRenderers_EmptyGameObject_ReturnsEmptyList()
        {
            var root = CreateGameObject("EmptyRoot");

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromRenderers_NoRenderers_ReturnsEmptyList()
        {
            var root = CreateGameObject("Root");
            var child = CreateGameObject("Child");
            child.transform.SetParent(root.transform);

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromRenderers_RendererWithNoMaterials_ReturnsEmptyList()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[0];

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromRenderers_RendererWithNullMaterial_SkipsNull()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[] { null };

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region CollectFromRenderers - Single Material Tests

        [Test]
        public void CollectFromRenderers_SingleMaterial_ReturnsOneReference()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0].Material);
        }

        [Test]
        public void CollectFromRenderers_SingleMaterial_HasCorrectSourceType()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(MaterialSourceType.Renderer, result[0].SourceType);
        }

        [Test]
        public void CollectFromRenderers_SingleMaterial_HasCorrectSourceObject()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(renderer, result[0].SourceObject);
        }

        #endregion

        #region CollectFromRenderers - Multiple Materials Tests

        [Test]
        public void CollectFromRenderers_MultipleMaterialsOnSameRenderer_ReturnsAll()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            var material3 = CreateMaterial();
            renderer.sharedMaterials = new Material[] { material1, material2, material3 };

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(material1, result[0].Material);
            Assert.AreEqual(material2, result[1].Material);
            Assert.AreEqual(material3, result[2].Material);
        }

        [Test]
        public void CollectFromRenderers_MultipleMaterialsWithNulls_SkipsNulls()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            renderer.sharedMaterials = new Material[] { material1, null, material2 };

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(material1, result[0].Material);
            Assert.AreEqual(material2, result[1].Material);
        }

        [Test]
        public void CollectFromRenderers_SameMaterialInMultipleSlots_CreatesSeparateReferences()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterials = new Material[] { material, material };

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(material, result[0].Material);
            Assert.AreEqual(material, result[1].Material);
        }

        #endregion

        #region CollectFromRenderers - Hierarchy Tests

        [Test]
        public void CollectFromRenderers_MultipleRenderersInHierarchy_CollectsAll()
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
            renderer1.sharedMaterial = material1;
            renderer2.sharedMaterial = material2;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void CollectFromRenderers_DeepHierarchy_CollectsAll()
        {
            var root = CreateGameObject("Root");
            var level1 = CreateGameObject("Level1");
            var level2 = CreateGameObject("Level2");
            var level3 = CreateGameObject("Level3");

            level1.transform.SetParent(root.transform);
            level2.transform.SetParent(level1.transform);
            level3.transform.SetParent(level2.transform);

            var renderer = level3.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0].Material);
        }

        [Test]
        public void CollectFromRenderers_InactiveChildren_StillCollects()
        {
            var root = CreateGameObject("Root");
            var inactiveChild = CreateGameObject("InactiveChild");
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            var renderer = inactiveChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0].Material);
        }

        #endregion

        #region CollectFromRenderers - Different Renderer Types Tests

        [Test]
        public void CollectFromRenderers_SkinnedMeshRenderer_Collects()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<SkinnedMeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0].Material);
        }

        [Test]
        public void CollectFromRenderers_MixedRendererTypes_CollectsAll()
        {
            var root = CreateGameObject("Root");
            var meshChild = CreateGameObject("MeshChild");
            var skinnedChild = CreateGameObject("SkinnedChild");

            meshChild.transform.SetParent(root.transform);
            skinnedChild.transform.SetParent(root.transform);

            var meshRenderer = meshChild.AddComponent<MeshRenderer>();
            var skinnedRenderer = skinnedChild.AddComponent<SkinnedMeshRenderer>();
            var meshMaterial = CreateMaterial();
            var skinnedMaterial = CreateMaterial();
            meshRenderer.sharedMaterial = meshMaterial;
            skinnedRenderer.sharedMaterial = skinnedMaterial;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(2, result.Count);
        }

        #endregion

        #region CollectFromRenderers - EditorOnly Skip Tests

        [Test]
        public void CollectFromRenderers_EditorOnlyTaggedRenderer_Skips()
        {
            var root = CreateGameObject("Root");
            var editorOnlyChild = CreateGameObject("EditorOnlyChild");
            editorOnlyChild.transform.SetParent(root.transform);
            editorOnlyChild.tag = "EditorOnly";

            var renderer = editorOnlyChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromRenderers_ParentIsEditorOnly_SkipsChildren()
        {
            var root = CreateGameObject("Root");
            var editorOnlyParent = CreateGameObject("EditorOnlyParent");
            var child = CreateGameObject("Child");
            editorOnlyParent.transform.SetParent(root.transform);
            child.transform.SetParent(editorOnlyParent.transform);
            editorOnlyParent.tag = "EditorOnly";

            var renderer = child.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromRenderers_SiblingIsEditorOnly_CollectsNonEditorOnly()
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
            editorOnlyRenderer.sharedMaterial = editorOnlyMaterial;
            normalRenderer.sharedMaterial = normalMaterial;

            var result = MaterialCollector.CollectFromRenderers(root);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(normalMaterial, result[0].Material);
        }

        #endregion

        #region CollectFromAnimator Tests

        [Test]
        public void CollectFromAnimator_NoAnimator_ReturnsEmptyList()
        {
            var root = CreateGameObject("Root");

            var result = MaterialCollector.CollectFromAnimator(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromAnimator_AnimatorWithNoController_ReturnsEmptyList()
        {
            var root = CreateGameObject("Root");
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;

            var result = MaterialCollector.CollectFromAnimator(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region CollectFromComponents - Empty/Null Tests

        [Test]
        public void CollectFromComponents_EmptyGameObject_ReturnsEmptyList()
        {
            var root = CreateGameObject("EmptyRoot");

            var result = MaterialCollector.CollectFromComponents(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromComponents_ComponentsWithNoMaterials_ReturnsEmptyList()
        {
            var root = CreateGameObject("Root");
            root.AddComponent<BoxCollider>();
            root.AddComponent<Rigidbody>();

            var result = MaterialCollector.CollectFromComponents(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectFromComponents_SkipsRendererComponents()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectFromComponents(root);

            // Renderer materials should not be collected by CollectFromComponents
            // (they are handled by CollectFromRenderers)
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region CollectFromComponents - EditorOnly Skip Tests

        [Test]
        public void CollectFromComponents_EditorOnlyTagged_Skips()
        {
            var root = CreateGameObject("Root");
            var editorOnlyChild = CreateGameObject("EditorOnlyChild");
            editorOnlyChild.transform.SetParent(root.transform);
            editorOnlyChild.tag = "EditorOnly";

            editorOnlyChild.AddComponent<BoxCollider>();

            var result = MaterialCollector.CollectFromComponents(root);

            // Even if there were material references, EditorOnly objects should be skipped
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region CollectAll Tests

        [Test]
        public void CollectAll_EmptyGameObject_ReturnsEmptyList()
        {
            var root = CreateGameObject("EmptyRoot");

            var result = MaterialCollector.CollectAll(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CollectAll_WithRendererOnly_CollectsRendererMaterials()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var result = MaterialCollector.CollectAll(root);

            Assert.IsTrue(result.Any(r => r.Material == material));
        }

        [Test]
        public void CollectAll_CombinesAllSources()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var rendererMaterial = CreateMaterial();
            renderer.sharedMaterial = rendererMaterial;

            var result = MaterialCollector.CollectAll(root);

            // Should at least contain the renderer material
            Assert.IsTrue(result.Any(r => r.Material == rendererMaterial));
        }

        #endregion

        #region GetDistinctMaterials Tests

        [Test]
        public void GetDistinctMaterials_EmptyReferences_ReturnsEmpty()
        {
            var references = new List<MaterialReference>();

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetDistinctMaterials_SingleReference_ReturnsMaterial()
        {
            var material = CreateMaterial();
            var references = new List<MaterialReference>
            {
                MaterialReference.FromRenderer(material, null)
            };

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0]);
        }

        [Test]
        public void GetDistinctMaterials_DuplicateMaterials_ReturnsDistinct()
        {
            var material = CreateMaterial();
            var references = new List<MaterialReference>
            {
                MaterialReference.FromRenderer(material, null),
                MaterialReference.FromRenderer(material, null),
                MaterialReference.FromAnimation(material, null)
            };

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0]);
        }

        [Test]
        public void GetDistinctMaterials_MultipleDifferentMaterials_ReturnsAll()
        {
            var material1 = CreateMaterial();
            var material2 = CreateMaterial();
            var material3 = CreateMaterial();
            var references = new List<MaterialReference>
            {
                MaterialReference.FromRenderer(material1, null),
                MaterialReference.FromAnimation(material2, null),
                MaterialReference.FromComponent(material3, null)
            };

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.AreEqual(3, result.Count);
            Assert.Contains(material1, result);
            Assert.Contains(material2, result);
            Assert.Contains(material3, result);
        }

        [Test]
        public void GetDistinctMaterials_WithNullReferences_SkipsNulls()
        {
            var material = CreateMaterial();
            var references = new List<MaterialReference>
            {
                null,
                MaterialReference.FromRenderer(material, null),
                null
            };

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0]);
        }

        [Test]
        public void GetDistinctMaterials_WithNullMaterialInReference_SkipsNull()
        {
            var material = CreateMaterial();
            var references = new List<MaterialReference>
            {
                new MaterialReference(null, MaterialSourceType.Renderer, null),
                MaterialReference.FromRenderer(material, null)
            };

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0]);
        }

        [Test]
        public void GetDistinctMaterials_MixedSourceTypes_PreservesAll()
        {
            var rendererMat = CreateMaterial();
            var animationMat = CreateMaterial();
            var componentMat = CreateMaterial();

            var references = new List<MaterialReference>
            {
                MaterialReference.FromRenderer(rendererMat, null),
                MaterialReference.FromAnimation(animationMat, null),
                MaterialReference.FromComponent(componentMat, null)
            };

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void GetDistinctMaterials_SameMaterialDifferentSources_ReturnsOnce()
        {
            var material = CreateMaterial();
            var references = new List<MaterialReference>
            {
                MaterialReference.FromRenderer(material, null),
                MaterialReference.FromAnimation(material, null),
                MaterialReference.FromComponent(material, null)
            };

            var result = MaterialCollector.GetDistinctMaterials(references).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(material, result[0]);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CollectFromRenderers_IntegrationWithMaterialCloner_Works()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial();
            renderer.sharedMaterial = material;

            var references = MaterialCollector.CollectFromRenderers(root);
            var clonedMaterials = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, clonedMaterials.Count);
            Assert.IsTrue(clonedMaterials.ContainsKey(material));
            Assert.AreNotSame(material, renderer.sharedMaterial);

            // Clean up cloned material
            foreach (var kvp in clonedMaterials)
            {
                _createdObjects.Add(kvp.Value);
            }
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

        #endregion
    }
}
