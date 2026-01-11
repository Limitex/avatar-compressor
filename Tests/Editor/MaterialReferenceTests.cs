using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class MaterialReferenceTests
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

        #region Constructor Tests

        [Test]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            var material = CreateMaterial();
            var sourceObject = CreateGameObject("SourceObject");

            var reference = new MaterialReference(
                material,
                MaterialSourceType.Renderer,
                sourceObject);

            Assert.AreEqual(material, reference.Material);
            Assert.AreEqual(MaterialSourceType.Renderer, reference.SourceType);
            Assert.AreEqual(sourceObject, reference.SourceObject);
        }

        [Test]
        public void Constructor_WithNullMaterial_AllowsNull()
        {
            var sourceObject = CreateGameObject("SourceObject");

            var reference = new MaterialReference(
                null,
                MaterialSourceType.Component,
                sourceObject);

            Assert.IsNull(reference.Material);
        }

        [Test]
        public void Constructor_WithNullSourceObject_AllowsNull()
        {
            var material = CreateMaterial();

            var reference = new MaterialReference(
                material,
                MaterialSourceType.Animation,
                null);

            Assert.IsNull(reference.SourceObject);
        }

        #endregion

        #region FromRenderer Factory Method Tests

        [Test]
        public void FromRenderer_CreatesCorrectReference()
        {
            var material = CreateMaterial();
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<MeshRenderer>();

            var reference = MaterialReference.FromRenderer(material, renderer);

            Assert.AreEqual(material, reference.Material);
            Assert.AreEqual(MaterialSourceType.Renderer, reference.SourceType);
            Assert.AreEqual(renderer, reference.SourceObject);
        }

        [Test]
        public void FromRenderer_WithNullRenderer_AllowsNull()
        {
            var material = CreateMaterial();

            var reference = MaterialReference.FromRenderer(material, null);

            Assert.IsNull(reference.SourceObject);
            Assert.AreEqual(MaterialSourceType.Renderer, reference.SourceType);
        }

        [Test]
        public void FromRenderer_WithSkinnedMeshRenderer_Works()
        {
            var material = CreateMaterial();
            var root = CreateGameObject("Root");
            var renderer = root.AddComponent<SkinnedMeshRenderer>();

            var reference = MaterialReference.FromRenderer(material, renderer);

            Assert.AreEqual(renderer, reference.SourceObject);
            Assert.AreEqual(MaterialSourceType.Renderer, reference.SourceType);
        }

        #endregion

        #region FromAnimation Factory Method Tests

        [Test]
        public void FromAnimation_CreatesCorrectReference()
        {
            var material = CreateMaterial();
            var animationClip = new AnimationClip();
            _createdObjects.Add(animationClip);

            var reference = MaterialReference.FromAnimation(material, animationClip);

            Assert.AreEqual(material, reference.Material);
            Assert.AreEqual(MaterialSourceType.Animation, reference.SourceType);
            Assert.AreEqual(animationClip, reference.SourceObject);
        }

        [Test]
        public void FromAnimation_WithNullAnimationSource_AllowsNull()
        {
            var material = CreateMaterial();

            var reference = MaterialReference.FromAnimation(material, null);

            Assert.AreEqual(material, reference.Material);
            Assert.AreEqual(MaterialSourceType.Animation, reference.SourceType);
            Assert.IsNull(reference.SourceObject);
        }

        #endregion

        #region FromComponent Factory Method Tests

        [Test]
        public void FromComponent_CreatesCorrectReference()
        {
            var material = CreateMaterial();
            var root = CreateGameObject("Root");
            var component = root.AddComponent<BoxCollider>();

            var reference = MaterialReference.FromComponent(material, component);

            Assert.AreEqual(material, reference.Material);
            Assert.AreEqual(MaterialSourceType.Component, reference.SourceType);
            Assert.AreEqual(component, reference.SourceObject);
        }

        [Test]
        public void FromComponent_WithNullComponent_AllowsNull()
        {
            var material = CreateMaterial();

            var reference = MaterialReference.FromComponent(material, null);

            Assert.IsNull(reference.SourceObject);
            Assert.AreEqual(MaterialSourceType.Component, reference.SourceType);
        }

        #endregion

        #region MaterialSourceType Enum Tests

        [Test]
        public void MaterialSourceType_RendererValue_IsDefined()
        {
            Assert.That(System.Enum.IsDefined(typeof(MaterialSourceType), MaterialSourceType.Renderer));
        }

        [Test]
        public void MaterialSourceType_AnimationValue_IsDefined()
        {
            Assert.That(System.Enum.IsDefined(typeof(MaterialSourceType), MaterialSourceType.Animation));
        }

        [Test]
        public void MaterialSourceType_ComponentValue_IsDefined()
        {
            Assert.That(System.Enum.IsDefined(typeof(MaterialSourceType), MaterialSourceType.Component));
        }

        [Test]
        public void MaterialSourceType_HasThreeValues()
        {
            var values = System.Enum.GetValues(typeof(MaterialSourceType));
            Assert.AreEqual(3, values.Length);
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
