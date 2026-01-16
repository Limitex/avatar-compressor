using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.common;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ComponentUtilsTests
    {
        #region SafeDestroy Tests

        [Test]
        public void SafeDestroy_NullObject_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ComponentUtils.SafeDestroy(null));
        }

        [Test]
        public void SafeDestroy_GameObject_DestroysObject()
        {
            var go = new GameObject("TestObject");

            ComponentUtils.SafeDestroy(go);

            Assert.IsTrue(go == null);
        }

        [Test]
        public void SafeDestroy_Component_DestroysComponent()
        {
            var go = new GameObject("TestObject");
            var component = go.AddComponent<BoxCollider>();

            ComponentUtils.SafeDestroy(component);

            Assert.IsTrue(component == null);
            Assert.IsFalse(go == null);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafeDestroy_Material_DestroysMaterial()
        {
            var material = new Material(Shader.Find("Standard"));

            ComponentUtils.SafeDestroy(material);

            Assert.IsTrue(material == null);
        }

        [Test]
        public void SafeDestroy_Texture_DestroysTexture()
        {
            var texture = new Texture2D(64, 64);

            ComponentUtils.SafeDestroy(texture);

            Assert.IsTrue(texture == null);
        }

        #endregion

        #region DestroyAllComponents Tests

        [Test]
        public void DestroyAllComponents_NoComponents_DoesNotThrow()
        {
            var root = new GameObject("Root");

            Assert.DoesNotThrow(() => ComponentUtils.DestroyAllComponents<BoxCollider>(root));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DestroyAllComponents_SingleComponent_DestroysIt()
        {
            var root = new GameObject("Root");
            root.AddComponent<BoxCollider>();

            ComponentUtils.DestroyAllComponents<BoxCollider>(root);

            Assert.IsNull(root.GetComponent<BoxCollider>());

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DestroyAllComponents_MultipleComponents_DestroysAll()
        {
            var root = new GameObject("Root");
            var child1 = new GameObject("Child1");
            var child2 = new GameObject("Child2");
            child1.transform.SetParent(root.transform);
            child2.transform.SetParent(root.transform);

            root.AddComponent<BoxCollider>();
            child1.AddComponent<BoxCollider>();
            child2.AddComponent<BoxCollider>();

            ComponentUtils.DestroyAllComponents<BoxCollider>(root);

            Assert.IsNull(root.GetComponent<BoxCollider>());
            Assert.IsNull(child1.GetComponent<BoxCollider>());
            Assert.IsNull(child2.GetComponent<BoxCollider>());

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DestroyAllComponents_InactiveChildren_AlsoDestroyed()
        {
            var root = new GameObject("Root");
            var inactiveChild = new GameObject("InactiveChild");
            inactiveChild.transform.SetParent(root.transform);
            inactiveChild.SetActive(false);
            inactiveChild.AddComponent<BoxCollider>();

            ComponentUtils.DestroyAllComponents<BoxCollider>(root);

            Assert.IsNull(inactiveChild.GetComponent<BoxCollider>());

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DestroyAllComponents_DeepHierarchy_DestroysAll()
        {
            var root = new GameObject("Root");
            var level1 = new GameObject("Level1");
            var level2 = new GameObject("Level2");
            var level3 = new GameObject("Level3");

            level1.transform.SetParent(root.transform);
            level2.transform.SetParent(level1.transform);
            level3.transform.SetParent(level2.transform);

            level3.AddComponent<BoxCollider>();

            ComponentUtils.DestroyAllComponents<BoxCollider>(root);

            Assert.IsNull(level3.GetComponent<BoxCollider>());

            Object.DestroyImmediate(root);
        }

        [Test]
        public void DestroyAllComponents_DifferentComponentType_NotAffected()
        {
            var root = new GameObject("Root");
            root.AddComponent<BoxCollider>();
            root.AddComponent<SphereCollider>();

            ComponentUtils.DestroyAllComponents<BoxCollider>(root);

            Assert.IsNull(root.GetComponent<BoxCollider>());
            Assert.IsNotNull(root.GetComponent<SphereCollider>());

            Object.DestroyImmediate(root);
        }

        #endregion

        #region GetOrAddComponent Tests

        [Test]
        public void GetOrAddComponent_ComponentNotExists_AddsComponent()
        {
            var go = new GameObject("TestObject");

            var component = ComponentUtils.GetOrAddComponent<BoxCollider>(go);

            Assert.IsNotNull(component);
            Assert.IsNotNull(go.GetComponent<BoxCollider>());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetOrAddComponent_ComponentExists_ReturnsExisting()
        {
            var go = new GameObject("TestObject");
            var existingComponent = go.AddComponent<BoxCollider>();
            existingComponent.size = new Vector3(5, 5, 5);

            var component = ComponentUtils.GetOrAddComponent<BoxCollider>(go);

            Assert.AreSame(existingComponent, component);
            Assert.AreEqual(new Vector3(5, 5, 5), component.size);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetOrAddComponent_CalledTwice_ReturnsSameInstance()
        {
            var go = new GameObject("TestObject");

            var component1 = ComponentUtils.GetOrAddComponent<BoxCollider>(go);
            var component2 = ComponentUtils.GetOrAddComponent<BoxCollider>(go);

            Assert.AreSame(component1, component2);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetOrAddComponent_DifferentTypes_AddsBoth()
        {
            var go = new GameObject("TestObject");

            var boxCollider = ComponentUtils.GetOrAddComponent<BoxCollider>(go);
            var sphereCollider = ComponentUtils.GetOrAddComponent<SphereCollider>(go);

            Assert.IsNotNull(boxCollider);
            Assert.IsNotNull(sphereCollider);
            Assert.AreNotSame(boxCollider, sphereCollider);

            Object.DestroyImmediate(go);
        }

        #endregion

        #region IsEditorOnly Tests

        [Test]
        public void IsEditorOnly_NullObject_ReturnsFalse()
        {
            Assert.IsFalse(ComponentUtils.IsEditorOnly(null));
        }

        [Test]
        public void IsEditorOnly_NoTag_ReturnsFalse()
        {
            var go = new GameObject("TestObject");

            Assert.IsFalse(ComponentUtils.IsEditorOnly(go));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void IsEditorOnly_DirectEditorOnlyTag_ReturnsTrue()
        {
            var go = new GameObject("TestObject");
            go.tag = "EditorOnly";

            Assert.IsTrue(ComponentUtils.IsEditorOnly(go));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void IsEditorOnly_ParentHasEditorOnlyTag_ReturnsTrue()
        {
            var parent = new GameObject("Parent");
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            parent.tag = "EditorOnly";

            Assert.IsTrue(ComponentUtils.IsEditorOnly(child));

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void IsEditorOnly_GrandparentHasEditorOnlyTag_ReturnsTrue()
        {
            var grandparent = new GameObject("Grandparent");
            var parent = new GameObject("Parent");
            var child = new GameObject("Child");
            parent.transform.SetParent(grandparent.transform);
            child.transform.SetParent(parent.transform);
            grandparent.tag = "EditorOnly";

            Assert.IsTrue(ComponentUtils.IsEditorOnly(child));

            Object.DestroyImmediate(grandparent);
        }

        [Test]
        public void IsEditorOnly_DeepHierarchy_ParentHasTag_ReturnsTrue()
        {
            var root = new GameObject("Root");
            var level1 = new GameObject("Level1");
            var level2 = new GameObject("Level2");
            var level3 = new GameObject("Level3");
            var level4 = new GameObject("Level4");

            level1.transform.SetParent(root.transform);
            level2.transform.SetParent(level1.transform);
            level3.transform.SetParent(level2.transform);
            level4.transform.SetParent(level3.transform);

            level2.tag = "EditorOnly";

            Assert.IsTrue(ComponentUtils.IsEditorOnly(level4));
            Assert.IsTrue(ComponentUtils.IsEditorOnly(level3));
            Assert.IsTrue(ComponentUtils.IsEditorOnly(level2));
            Assert.IsFalse(ComponentUtils.IsEditorOnly(level1));
            Assert.IsFalse(ComponentUtils.IsEditorOnly(root));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void IsEditorOnly_SiblingHasEditorOnlyTag_ReturnsFalse()
        {
            var parent = new GameObject("Parent");
            var child1 = new GameObject("Child1");
            var child2 = new GameObject("Child2");
            child1.transform.SetParent(parent.transform);
            child2.transform.SetParent(parent.transform);
            child1.tag = "EditorOnly";

            Assert.IsTrue(ComponentUtils.IsEditorOnly(child1));
            Assert.IsFalse(ComponentUtils.IsEditorOnly(child2));

            Object.DestroyImmediate(parent);
        }

        [Test]
        public void IsEditorOnly_InactiveObject_StillChecksTag()
        {
            var go = new GameObject("TestObject");
            go.SetActive(false);
            go.tag = "EditorOnly";

            Assert.IsTrue(ComponentUtils.IsEditorOnly(go));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void IsEditorOnly_InactiveParentWithTag_ReturnsTrue()
        {
            var parent = new GameObject("Parent");
            var child = new GameObject("Child");
            child.transform.SetParent(parent.transform);
            parent.tag = "EditorOnly";
            parent.SetActive(false);

            Assert.IsTrue(ComponentUtils.IsEditorOnly(child));

            Object.DestroyImmediate(parent);
        }

        #endregion
    }
}
