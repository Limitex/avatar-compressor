using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using dev.limitex.avatar.compressor.common;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class ObjectRegistryUtilsTests
    {
        #region GetOriginalAssetPath Tests

        [Test]
        public void GetOriginalAssetPath_NullTexture_ReturnsEmptyString()
        {
            string result = ObjectRegistryUtils.GetOriginalAssetPath(null);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void GetOriginalAssetPath_RuntimeTexture_ReturnsEmptyPath()
        {
            // Runtime-created textures have no asset path
            var texture = new Texture2D(64, 64);

            string result = ObjectRegistryUtils.GetOriginalAssetPath(texture);

            // Runtime textures return empty string from AssetDatabase.GetAssetPath
            Assert.AreEqual(string.Empty, result);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GetOriginalAssetPath_UnregisteredTexture_ReturnsDirectPath()
        {
            // When texture is not registered in ObjectRegistry, should return its direct asset path
            var texture = new Texture2D(64, 64);

            string directPath = AssetDatabase.GetAssetPath(texture);
            string result = ObjectRegistryUtils.GetOriginalAssetPath(texture);

            Assert.AreEqual(directPath, result);

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region GetOriginalObject Tests

        [Test]
        public void GetOriginalObject_NullObject_ReturnsNull()
        {
            Texture2D result = ObjectRegistryUtils.GetOriginalObject<Texture2D>(null);

            Assert.IsNull(result);
        }

        [Test]
        public void GetOriginalObject_UnregisteredObject_ReturnsSameObject()
        {
            // When object is not registered in ObjectRegistry, should return the same object
            var texture = new Texture2D(64, 64);

            var result = ObjectRegistryUtils.GetOriginalObject(texture);

            Assert.AreSame(texture, result);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GetOriginalObject_GameObject_ReturnsSameObject()
        {
            var go = new GameObject("TestObject");

            var result = ObjectRegistryUtils.GetOriginalObject(go);

            Assert.AreSame(go, result);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GetOriginalObject_Material_ReturnsSameObject()
        {
            var material = new Material(Shader.Find("Standard"));

            var result = ObjectRegistryUtils.GetOriginalObject(material);

            Assert.AreSame(material, result);

            Object.DestroyImmediate(material);
        }

        [Test]
        public void GetOriginalObject_PreservesObjectType()
        {
            var texture = new Texture2D(128, 128);

            var result = ObjectRegistryUtils.GetOriginalObject(texture);

            Assert.IsInstanceOf<Texture2D>(result);
            Assert.AreEqual(128, result.width);
            Assert.AreEqual(128, result.height);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GetOriginalObject_MultipleCallsOnSameObject_ReturnConsistentResult()
        {
            var texture = new Texture2D(64, 64);

            var result1 = ObjectRegistryUtils.GetOriginalObject(texture);
            var result2 = ObjectRegistryUtils.GetOriginalObject(texture);

            Assert.AreSame(result1, result2);

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetOriginalAssetPath_DestroyedTexture_HandlesGracefully()
        {
            var texture = new Texture2D(64, 64);
            Object.DestroyImmediate(texture);

            // Should not throw, texture is now null
            Assert.DoesNotThrow(() => ObjectRegistryUtils.GetOriginalAssetPath(texture));
        }

        [Test]
        public void GetOriginalObject_DestroyedObject_HandlesGracefully()
        {
            var texture = new Texture2D(64, 64);
            Object.DestroyImmediate(texture);

            // Should not throw, returns null for destroyed object
            Assert.DoesNotThrow(() => ObjectRegistryUtils.GetOriginalObject(texture));
        }

        #endregion
    }
}
