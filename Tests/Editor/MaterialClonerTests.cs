using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.common;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class MaterialClonerTests
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

        #region Empty/Null Input Tests

        [Test]
        public void CloneMaterials_EmptyGameObject_ReturnsEmptyDictionary()
        {
            var root = CreateGameObject("EmptyRoot");

            var result = MaterialCloner.CloneMaterials(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CloneMaterials_NoRenderers_ReturnsEmptyDictionary()
        {
            var root = CreateGameObject("Root");
            var child = CreateGameObject("Child");
            child.transform.SetParent(root.transform);

            var result = MaterialCloner.CloneMaterials(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CloneMaterials_RendererWithNoMaterial_ReturnsEmptyDictionary()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[0];

            var result = MaterialCloner.CloneMaterials(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CloneMaterials_RendererWithNullMaterial_HandlesGracefully()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[] { null };

            var result = MaterialCloner.CloneMaterials(root);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            // Renderer should still have null material
            Assert.IsNull(renderer.sharedMaterials[0]);
        }

        #endregion

        #region Single Material Tests

        [Test]
        public void CloneMaterials_SingleMaterial_CreatesClone()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(originalMaterial));
        }

        [Test]
        public void CloneMaterials_SingleMaterial_CloneIsDifferentInstance()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            var clonedMaterial = result[originalMaterial];
            Assert.AreNotSame(originalMaterial, clonedMaterial);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneMaterials_SingleMaterial_RendererUsesClone()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual(clonedMaterial, renderer.sharedMaterial);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneMaterials_SingleMaterial_CloneHasCorrectName()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual("OriginalMaterial_clone", clonedMaterial.name);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneMaterials_SingleMaterial_ClonePreservesShader()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var shader = originalMaterial.shader;
            renderer.sharedMaterial = originalMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual(shader, clonedMaterial.shader);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneMaterials_SingleMaterial_ClonePreservesColor()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            originalMaterial.color = Color.red;
            renderer.sharedMaterial = originalMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual(Color.red, clonedMaterial.color);
            _createdObjects.Add(clonedMaterial);
        }

        #endregion

        #region Multiple Materials Tests

        [Test]
        public void CloneMaterials_MultipleMaterialsOnSameRenderer_ClonesAll()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            var material2 = CreateMaterial("Material2");
            renderer.sharedMaterials = new Material[] { material1, material2 };

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(material1));
            Assert.IsTrue(result.ContainsKey(material2));

            // Clean up clones
            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CloneMaterials_MultipleMaterialsOnSameRenderer_AllClonesAreUnique()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            var material2 = CreateMaterial("Material2");
            renderer.sharedMaterials = new Material[] { material1, material2 };

            var result = MaterialCloner.CloneMaterials(root);

            var clone1 = result[material1];
            var clone2 = result[material2];
            Assert.AreNotSame(clone1, clone2);

            // Clean up clones
            _createdObjects.Add(clone1);
            _createdObjects.Add(clone2);
        }

        [Test]
        public void CloneMaterials_SameMaterialOnMultipleRenderers_ClonesOnce()
        {
            var root = CreateGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var renderer1 = child1.AddComponent<MeshRenderer>();
            var renderer2 = child2.AddComponent<MeshRenderer>();
            var sharedMaterial = CreateMaterial("SharedMaterial");
            renderer1.sharedMaterial = sharedMaterial;
            renderer2.sharedMaterial = sharedMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(1, result.Count);
            // Both renderers should use the same clone
            Assert.AreEqual(renderer1.sharedMaterial, renderer2.sharedMaterial);

            // Clean up clone
            _createdObjects.Add(result[sharedMaterial]);
        }

        [Test]
        public void CloneMaterials_DifferentMaterialsOnDifferentRenderers_ClonesAll()
        {
            var root = CreateGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            var renderer1 = child1.AddComponent<MeshRenderer>();
            var renderer2 = child2.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            var material2 = CreateMaterial("Material2");
            renderer1.sharedMaterial = material1;
            renderer2.sharedMaterial = material2;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(material1));
            Assert.IsTrue(result.ContainsKey(material2));

            // Clean up clones
            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        #endregion

        #region Hierarchy Tests

        [Test]
        public void CloneMaterials_DeepHierarchy_ProcessesAllRenderers()
        {
            var root = CreateGameObject("Root");
            var level1 = CreateGameObject("Level1");
            var level2 = CreateGameObject("Level2");
            var level3 = CreateGameObject("Level3");

            level1.transform.SetParent(root.transform);
            level2.transform.SetParent(level1.transform);
            level3.transform.SetParent(level2.transform);

            var renderer = level3.AddComponent<MeshRenderer>();
            var material = CreateMaterial("DeepMaterial");
            renderer.sharedMaterial = material;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            // Clean up clone
            _createdObjects.Add(result[material]);
        }

        [Test]
        public void CloneMaterials_InactiveChildren_ProcessesAll()
        {
            var root = CreateGameObject("Root");
            var inactiveChild = CreateGameObject("InactiveChild");
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            var renderer = inactiveChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial("InactiveMaterial");
            renderer.sharedMaterial = material;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            // Clean up clone
            _createdObjects.Add(result[material]);
        }

        [Test]
        public void CloneMaterials_MixedHierarchy_ProcessesAllMaterials()
        {
            var root = CreateGameObject("Root");
            var child1 = CreateGameObject("Child1");
            var child2 = CreateGameObject("Child2");
            var grandchild = CreateGameObject("Grandchild");

            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);
            grandchild.transform.SetParent(child1.transform);

            // Root has renderer
            var rootRenderer = root.AddComponent<MeshRenderer>();
            var rootMaterial = CreateMaterial("RootMaterial");
            rootRenderer.sharedMaterial = rootMaterial;

            // Child2 has renderer
            var child2Renderer = child2.AddComponent<MeshRenderer>();
            var child2Material = CreateMaterial("Child2Material");
            child2Renderer.sharedMaterial = child2Material;

            // Grandchild has renderer
            var grandchildRenderer = grandchild.AddComponent<MeshRenderer>();
            var grandchildMaterial = CreateMaterial("GrandchildMaterial");
            grandchildRenderer.sharedMaterial = grandchildMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(3, result.Count);

            // Clean up clones
            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        #endregion

        #region Different Renderer Types Tests

        [Test]
        public void CloneMaterials_SkinnedMeshRenderer_ProcessesMaterials()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<SkinnedMeshRenderer>();
            var material = CreateMaterial("SkinnedMaterial");
            renderer.sharedMaterial = material;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));
            Assert.AreEqual(result[material], renderer.sharedMaterial);

            // Clean up clone
            _createdObjects.Add(result[material]);
        }

        [Test]
        public void CloneMaterials_MixedRendererTypes_ProcessesAll()
        {
            var root = CreateGameObject("Root");
            var meshChild = CreateGameObject("MeshChild");
            var skinnedChild = CreateGameObject("SkinnedChild");

            meshChild.transform.SetParent(root.transform);
            skinnedChild.transform.SetParent(root.transform);

            var meshRenderer = meshChild.AddComponent<MeshRenderer>();
            var skinnedRenderer = skinnedChild.AddComponent<SkinnedMeshRenderer>();

            var meshMaterial = CreateMaterial("MeshMaterial");
            var skinnedMaterial = CreateMaterial("SkinnedMaterial");

            meshRenderer.sharedMaterial = meshMaterial;
            skinnedRenderer.sharedMaterial = skinnedMaterial;

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(2, result.Count);

            // Clean up clones
            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        #endregion

        #region Additional Materials Tests

        [Test]
        public void CloneMaterials_WithAdditionalMaterials_ClonesAll()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var rendererMaterial = CreateMaterial("RendererMaterial");
            renderer.sharedMaterial = rendererMaterial;

            var additionalMaterial = CreateMaterial("AdditionalMaterial");
            var additionalMaterials = new Material[] { additionalMaterial };

            var result = MaterialCloner.CloneMaterials(root, additionalMaterials);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(rendererMaterial));
            Assert.IsTrue(result.ContainsKey(additionalMaterial));

            // Clean up clones
            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CloneMaterials_WithNullAdditionalMaterials_HandlesGracefully()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial("Material");
            renderer.sharedMaterial = material;

            var result = MaterialCloner.CloneMaterials(root, null);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            // Clean up clone
            _createdObjects.Add(result[material]);
        }

        [Test]
        public void CloneMaterials_WithEmptyAdditionalMaterials_ClonesOnlyRendererMaterials()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial("Material");
            renderer.sharedMaterial = material;

            var result = MaterialCloner.CloneMaterials(root, new Material[0]);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            // Clean up clone
            _createdObjects.Add(result[material]);
        }

        [Test]
        public void CloneMaterials_AdditionalMaterialsWithNulls_SkipsNulls()
        {
            var root = CreateGameObject("Root");
            var validMaterial = CreateMaterial("ValidMaterial");
            var additionalMaterials = new Material[] { null, validMaterial, null };

            var result = MaterialCloner.CloneMaterials(root, additionalMaterials);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(validMaterial));

            // Clean up clone
            _createdObjects.Add(result[validMaterial]);
        }

        [Test]
        public void CloneMaterials_SharedMaterialBetweenRendererAndAdditional_ClonesOnce()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var sharedMaterial = CreateMaterial("SharedMaterial");
            renderer.sharedMaterial = sharedMaterial;

            // Same material passed as additional
            var additionalMaterials = new Material[] { sharedMaterial };

            var result = MaterialCloner.CloneMaterials(root, additionalMaterials);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(sharedMaterial));

            // Clean up clone
            _createdObjects.Add(result[sharedMaterial]);
        }

        [Test]
        public void CloneMaterials_AdditionalMaterialOnly_ClonesSuccessfully()
        {
            var root = CreateGameObject("Root"); // No renderer

            var additionalMaterial = CreateMaterial("AdditionalMaterial");
            var additionalMaterials = new Material[] { additionalMaterial };

            var result = MaterialCloner.CloneMaterials(root, additionalMaterials);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(additionalMaterial));
            Assert.AreNotSame(additionalMaterial, result[additionalMaterial]);

            // Clean up clone
            _createdObjects.Add(result[additionalMaterial]);
        }

        [Test]
        public void CloneMaterials_MultipleAdditionalMaterials_ClonesAll()
        {
            var root = CreateGameObject("Root");

            var additionalMat1 = CreateMaterial("Additional1");
            var additionalMat2 = CreateMaterial("Additional2");
            var additionalMat3 = CreateMaterial("Additional3");
            var additionalMaterials = new Material[] { additionalMat1, additionalMat2, additionalMat3 };

            var result = MaterialCloner.CloneMaterials(root, additionalMaterials);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey(additionalMat1));
            Assert.IsTrue(result.ContainsKey(additionalMat2));
            Assert.IsTrue(result.ContainsKey(additionalMat3));

            // Clean up clones
            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CloneMaterials_DuplicateAdditionalMaterials_ClonesOnce()
        {
            var root = CreateGameObject("Root");

            var material = CreateMaterial("DuplicateMaterial");
            var additionalMaterials = new Material[] { material, material, material };

            var result = MaterialCloner.CloneMaterials(root, additionalMaterials);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            // Clean up clone
            _createdObjects.Add(result[material]);
        }

        #endregion

        #region Material Array Integrity Tests

        [Test]
        public void CloneMaterials_MaterialArrayWithNulls_PreservesArrayStructure()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            renderer.sharedMaterials = new Material[] { material1, null, material1 };

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(1, result.Count);
            var materials = renderer.sharedMaterials;
            Assert.AreEqual(3, materials.Length);
            Assert.AreEqual(result[material1], materials[0]);
            Assert.IsNull(materials[1]);
            Assert.AreEqual(result[material1], materials[2]);

            // Clean up clone
            _createdObjects.Add(result[material1]);
        }

        [Test]
        public void CloneMaterials_DuplicateMaterialInArray_UsesSameClone()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial("DuplicatedMaterial");
            renderer.sharedMaterials = new Material[] { material, material, material };

            var result = MaterialCloner.CloneMaterials(root);

            Assert.AreEqual(1, result.Count);
            var materials = renderer.sharedMaterials;
            Assert.AreEqual(materials[0], materials[1]);
            Assert.AreEqual(materials[1], materials[2]);

            // Clean up clone
            _createdObjects.Add(result[material]);
        }

        #endregion

        #region Helper Methods

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        private Material CreateMaterial(string name)
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = name;
            _createdObjects.Add(material);
            return material;
        }

        #endregion
    }
}
