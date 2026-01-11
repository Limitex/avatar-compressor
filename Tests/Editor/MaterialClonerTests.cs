using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

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
        public void CloneAndReplace_EmptyReferences_ReturnsEmptyDictionary()
        {
            var references = new List<MaterialReference>();

            var result = MaterialCloner.CloneAndReplace(references);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void CloneAndReplace_NullMaterialInReference_SkipsNull()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new Material[] { null };

            var references = MaterialCollector.CollectFromRenderers(root);

            var result = MaterialCloner.CloneAndReplace(references);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region Single Material Tests

        [Test]
        public void CloneAndReplace_SingleMaterial_CreatesClone()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(originalMaterial));
            _createdObjects.Add(result[originalMaterial]);
        }

        [Test]
        public void CloneAndReplace_SingleMaterial_CloneIsDifferentInstance()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            var clonedMaterial = result[originalMaterial];
            Assert.AreNotSame(originalMaterial, clonedMaterial);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneAndReplace_SingleMaterial_RendererUsesClone()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual(clonedMaterial, renderer.sharedMaterial);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneAndReplace_SingleMaterial_CloneHasCorrectName()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual("OriginalMaterial_clone", clonedMaterial.name);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneAndReplace_SingleMaterial_ClonePreservesShader()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            var shader = originalMaterial.shader;
            renderer.sharedMaterial = originalMaterial;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual(shader, clonedMaterial.shader);
            _createdObjects.Add(clonedMaterial);
        }

        [Test]
        public void CloneAndReplace_SingleMaterial_ClonePreservesColor()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            originalMaterial.color = Color.red;
            renderer.sharedMaterial = originalMaterial;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            var clonedMaterial = result[originalMaterial];
            Assert.AreEqual(Color.red, clonedMaterial.color);
            _createdObjects.Add(clonedMaterial);
        }

        #endregion

        #region Multiple Materials Tests

        [Test]
        public void CloneAndReplace_MultipleMaterialsOnSameRenderer_ClonesAll()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            var material2 = CreateMaterial("Material2");
            renderer.sharedMaterials = new Material[] { material1, material2 };

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(material1));
            Assert.IsTrue(result.ContainsKey(material2));

            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CloneAndReplace_MultipleMaterialsOnSameRenderer_AllClonesAreUnique()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            var material2 = CreateMaterial("Material2");
            renderer.sharedMaterials = new Material[] { material1, material2 };

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            var clone1 = result[material1];
            var clone2 = result[material2];
            Assert.AreNotSame(clone1, clone2);

            _createdObjects.Add(clone1);
            _createdObjects.Add(clone2);
        }

        [Test]
        public void CloneAndReplace_SameMaterialOnMultipleRenderers_ClonesOnce()
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

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(renderer1.sharedMaterial, renderer2.sharedMaterial);

            _createdObjects.Add(result[sharedMaterial]);
        }

        [Test]
        public void CloneAndReplace_DifferentMaterialsOnDifferentRenderers_ClonesAll()
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

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(material1));
            Assert.IsTrue(result.ContainsKey(material2));

            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        #endregion

        #region Hierarchy Tests

        [Test]
        public void CloneAndReplace_DeepHierarchy_ProcessesAllRenderers()
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

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            _createdObjects.Add(result[material]);
        }

        [Test]
        public void CloneAndReplace_InactiveChildren_ProcessesAll()
        {
            var root = CreateGameObject("Root");
            var inactiveChild = CreateGameObject("InactiveChild");
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);

            var renderer = inactiveChild.AddComponent<MeshRenderer>();
            var material = CreateMaterial("InactiveMaterial");
            renderer.sharedMaterial = material;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            _createdObjects.Add(result[material]);
        }

        #endregion

        #region Different Renderer Types Tests

        [Test]
        public void CloneAndReplace_SkinnedMeshRenderer_ProcessesMaterials()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<SkinnedMeshRenderer>();
            var material = CreateMaterial("SkinnedMaterial");
            renderer.sharedMaterial = material;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));
            Assert.AreEqual(result[material], renderer.sharedMaterial);

            _createdObjects.Add(result[material]);
        }

        [Test]
        public void CloneAndReplace_MixedRendererTypes_ProcessesAll()
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

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(2, result.Count);

            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        #endregion

        #region Mixed Source Tests

        [Test]
        public void CloneAndReplace_MixedSources_ClonesAll()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var rendererMaterial = CreateMaterial("RendererMaterial");
            renderer.sharedMaterial = rendererMaterial;

            var animationMaterial = CreateMaterial("AnimationMaterial");

            var references = new List<MaterialReference>();
            references.AddRange(MaterialCollector.CollectFromRenderers(root));
            references.Add(MaterialReference.FromAnimation(animationMaterial, null));

            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey(rendererMaterial));
            Assert.IsTrue(result.ContainsKey(animationMaterial));

            foreach (var kvp in result)
            {
                _createdObjects.Add(kvp.Value);
            }
        }

        [Test]
        public void CloneAndReplace_AnimationOnlyMaterials_ClonesWithoutRendererUpdate()
        {
            var root = CreateGameObject("Root");

            var animationMaterial = CreateMaterial("AnimationMaterial");
            var references = new List<MaterialReference>
            {
                MaterialReference.FromAnimation(animationMaterial, null)
            };

            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(animationMaterial));
            Assert.AreNotSame(animationMaterial, result[animationMaterial]);

            _createdObjects.Add(result[animationMaterial]);
        }

        [Test]
        public void CloneAndReplace_SharedMaterialBetweenRendererAndAnimation_ClonesOnce()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var sharedMaterial = CreateMaterial("SharedMaterial");
            renderer.sharedMaterial = sharedMaterial;

            var references = new List<MaterialReference>();
            references.AddRange(MaterialCollector.CollectFromRenderers(root));
            references.Add(MaterialReference.FromAnimation(sharedMaterial, null));

            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(sharedMaterial));

            _createdObjects.Add(result[sharedMaterial]);
        }

        [Test]
        public void CloneAndReplace_DuplicateReferences_ClonesOnce()
        {
            var material = CreateMaterial("DuplicateMaterial");

            var references = new List<MaterialReference>
            {
                MaterialReference.FromAnimation(material, null),
                MaterialReference.FromAnimation(material, null),
                MaterialReference.FromComponent(material, null)
            };

            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(material));

            _createdObjects.Add(result[material]);
        }

        #endregion

        #region CloneOnly Tests

        [Test]
        public void CloneOnly_SingleMaterial_ClonesWithoutUpdatingRenderer()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var originalMaterial = CreateMaterial("OriginalMaterial");
            renderer.sharedMaterial = originalMaterial;

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneOnly(references);

            Assert.AreEqual(1, result.Count);
            // Renderer should still have original material
            Assert.AreEqual(originalMaterial, renderer.sharedMaterial);

            _createdObjects.Add(result[originalMaterial]);
        }

        #endregion

        #region Material Array Integrity Tests

        [Test]
        public void CloneAndReplace_MaterialArrayWithNulls_PreservesArrayStructure()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material1 = CreateMaterial("Material1");
            renderer.sharedMaterials = new Material[] { material1, null, material1 };

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            var materials = renderer.sharedMaterials;
            Assert.AreEqual(3, materials.Length);
            Assert.AreEqual(result[material1], materials[0]);
            Assert.IsNull(materials[1]);
            Assert.AreEqual(result[material1], materials[2]);

            _createdObjects.Add(result[material1]);
        }

        [Test]
        public void CloneAndReplace_DuplicateMaterialInArray_UsesSameClone()
        {
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();
            var material = CreateMaterial("DuplicatedMaterial");
            renderer.sharedMaterials = new Material[] { material, material, material };

            var references = MaterialCollector.CollectFromRenderers(root);
            var result = MaterialCloner.CloneAndReplace(references);

            Assert.AreEqual(1, result.Count);
            var materials = renderer.sharedMaterials;
            Assert.AreEqual(materials[0], materials[1]);
            Assert.AreEqual(materials[1], materials[2]);

            _createdObjects.Add(result[material]);
        }

        #endregion

        #region GetClonedMaterialsBySource Tests

        [Test]
        public void GetClonedMaterialsBySource_FiltersBySourceType()
        {
            var rendererMaterial = CreateMaterial("RendererMaterial");
            var animationMaterial = CreateMaterial("AnimationMaterial");
            var componentMaterial = CreateMaterial("ComponentMaterial");

            var references = new List<MaterialReference>
            {
                MaterialReference.FromRenderer(rendererMaterial, null),
                MaterialReference.FromAnimation(animationMaterial, null),
                MaterialReference.FromComponent(componentMaterial, null)
            };

            var clonedMaterials = MaterialCloner.CloneOnly(references);

            var rendererClones = MaterialCloner.GetClonedMaterialsBySource(
                references, clonedMaterials, MaterialSourceType.Renderer);
            var animationClones = MaterialCloner.GetClonedMaterialsBySource(
                references, clonedMaterials, MaterialSourceType.Animation);
            var componentClones = MaterialCloner.GetClonedMaterialsBySource(
                references, clonedMaterials, MaterialSourceType.Component);

            Assert.AreEqual(1, rendererClones.Count);
            Assert.AreEqual(1, animationClones.Count);
            Assert.AreEqual(1, componentClones.Count);

            Assert.AreEqual(clonedMaterials[rendererMaterial], rendererClones[0]);
            Assert.AreEqual(clonedMaterials[animationMaterial], animationClones[0]);
            Assert.AreEqual(clonedMaterials[componentMaterial], componentClones[0]);

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
