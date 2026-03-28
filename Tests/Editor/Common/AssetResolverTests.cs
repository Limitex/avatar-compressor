using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor;
using nadena.dev.ndmf;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AssetResolverTests
    {
        private const string TestAssetFolder = "Assets/_LAC_TMP";
        private List<Object> _createdObjects;
        private List<string> _createdAssetPaths;

        [SetUp]
        public void SetUp()
        {
            _createdObjects = new List<Object>();
            _createdAssetPaths = new List<string>();

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

            if (AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                var remaining = AssetDatabase.FindAssets("", new[] { TestAssetFolder });
                if (remaining.Length == 0)
                {
                    AssetDatabase.DeleteAsset(TestAssetFolder);
                }
            }
        }

        #region ResolveAssetPath Tests

        [Test]
        public void ResolveAssetPath_Null_ReturnsEmpty()
        {
            var result = AssetResolver.ResolveAssetPath(null);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ResolveAssetPath_AssetTexture_NoActiveRegistry_ReturnsAssetPath()
        {
            var texture = CreateAssetTexture(128, 128);
            string expectedPath = AssetDatabase.GetAssetPath(texture);

            // Explicitly verify no ActiveRegistry — ensures the fallback path is tested
            Assert.IsNull(
                ObjectRegistry.ActiveRegistry,
                "ActiveRegistry should be null outside of build context"
            );

            var result = AssetResolver.ResolveAssetPath(texture);

            Assert.AreEqual(expectedPath, result);
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        [Test]
        public void ResolveAssetPath_RuntimeTexture_NoActiveRegistry_ReturnsEmpty()
        {
            var texture = CreateRuntimeTexture(128, 128);

            // Explicitly verify no ActiveRegistry — ensures the fallback path is tested
            Assert.IsNull(
                ObjectRegistry.ActiveRegistry,
                "ActiveRegistry should be null outside of build context"
            );

            var result = AssetResolver.ResolveAssetPath(texture);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ResolveAssetPath_ReplacedTexture_ResolvesOriginalPath()
        {
            var originalTexture = CreateAssetTexture(128, 128);
            string expectedPath = AssetDatabase.GetAssetPath(originalTexture);

            var replacementTexture = CreateRuntimeTexture(64, 64);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalTexture, replacementTexture);

                var result = AssetResolver.ResolveAssetPath(replacementTexture);

                Assert.AreEqual(expectedPath, result);
            }
        }

        [Test]
        public void ResolveAssetPath_UnregisteredRuntimeTexture_WithRegistry_ReturnsEmpty()
        {
            var runtimeTexture = CreateRuntimeTexture(128, 128);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                var result = AssetResolver.ResolveAssetPath(runtimeTexture);

                Assert.AreEqual(string.Empty, result);
            }
        }

        [Test]
        public void ResolveAssetPath_AssetTexture_WithRegistry_ReturnsDirectPath()
        {
            var texture = CreateAssetTexture(128, 128);
            string expectedPath = AssetDatabase.GetAssetPath(texture);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                var result = AssetResolver.ResolveAssetPath(texture);

                Assert.AreEqual(expectedPath, result);
            }
        }

        [Test]
        public void ResolveAssetPath_ChainedReplacement_ResolvesOriginalPath()
        {
            var originalTexture = CreateAssetTexture(256, 256);
            string expectedPath = AssetDatabase.GetAssetPath(originalTexture);

            var firstReplacement = CreateRuntimeTexture(128, 128);
            var secondReplacement = CreateRuntimeTexture(64, 64);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalTexture, firstReplacement);
                ObjectRegistry.RegisterReplacedObject(firstReplacement, secondReplacement);

                var result = AssetResolver.ResolveAssetPath(secondReplacement);

                Assert.AreEqual(expectedPath, result);
            }
        }

        [Test]
        public void ResolveAssetPath_ReplacementWithOwnAssetPath_ResolvesOriginalPath()
        {
            // Simulates a plugin that generates a new texture saved as its own asset,
            // but registers the replacement in ObjectRegistry to track the original.
            var originalTexture = CreateAssetTexture(256, 256);
            string originalPath = AssetDatabase.GetAssetPath(originalTexture);

            var replacementTexture = CreateAssetTexture(128, 128);
            string replacementPath = AssetDatabase.GetAssetPath(replacementTexture);

            // Verify both have different asset paths
            Assert.AreNotEqual(originalPath, replacementPath);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalTexture, replacementTexture);

                var result = AssetResolver.ResolveAssetPath(replacementTexture);

                // Should return the ORIGINAL path, not the replacement's own path
                Assert.AreEqual(originalPath, result);
            }
        }

        [Test]
        public void ResolveAssetGuid_ReplacementWithOwnAssetPath_ResolvesOriginalGuid()
        {
            var originalTexture = CreateAssetTexture(256, 256);
            string originalPath = AssetDatabase.GetAssetPath(originalTexture);
            string originalGuid = AssetDatabase.AssetPathToGUID(originalPath);

            var replacementTexture = CreateAssetTexture(128, 128);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalTexture, replacementTexture);

                var result = AssetResolver.ResolveAssetGuid(replacementTexture);

                Assert.AreEqual(originalGuid, result);
            }
        }

        #endregion

        #region ResolveAssetGuid Tests

        [Test]
        public void ResolveAssetGuid_Null_ReturnsEmpty()
        {
            var result = AssetResolver.ResolveAssetGuid(null);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ResolveAssetGuid_AssetTexture_NoActiveRegistry_ReturnsGuid()
        {
            var texture = CreateAssetTexture(128, 128);
            string assetPath = AssetDatabase.GetAssetPath(texture);
            string expectedGuid = AssetDatabase.AssetPathToGUID(assetPath);

            Assert.IsNull(
                ObjectRegistry.ActiveRegistry,
                "ActiveRegistry should be null outside of build context"
            );

            var result = AssetResolver.ResolveAssetGuid(texture);

            Assert.AreEqual(expectedGuid, result);
            Assert.IsFalse(string.IsNullOrEmpty(result));
        }

        [Test]
        public void ResolveAssetGuid_RuntimeTexture_NoActiveRegistry_ReturnsEmpty()
        {
            var texture = CreateRuntimeTexture(128, 128);

            Assert.IsNull(
                ObjectRegistry.ActiveRegistry,
                "ActiveRegistry should be null outside of build context"
            );

            var result = AssetResolver.ResolveAssetGuid(texture);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ResolveAssetGuid_ReplacedTexture_ResolvesOriginalGuid()
        {
            var originalTexture = CreateAssetTexture(128, 128);
            string assetPath = AssetDatabase.GetAssetPath(originalTexture);
            string expectedGuid = AssetDatabase.AssetPathToGUID(assetPath);

            var replacementTexture = CreateRuntimeTexture(64, 64);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalTexture, replacementTexture);

                var result = AssetResolver.ResolveAssetGuid(replacementTexture);

                Assert.AreEqual(expectedGuid, result);
            }
        }

        [Test]
        public void ResolveAssetGuid_ChainedReplacement_ResolvesOriginalGuid()
        {
            var originalTexture = CreateAssetTexture(256, 256);
            string assetPath = AssetDatabase.GetAssetPath(originalTexture);
            string expectedGuid = AssetDatabase.AssetPathToGUID(assetPath);

            var firstReplacement = CreateRuntimeTexture(128, 128);
            var secondReplacement = CreateRuntimeTexture(64, 64);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalTexture, firstReplacement);
                ObjectRegistry.RegisterReplacedObject(firstReplacement, secondReplacement);

                var result = AssetResolver.ResolveAssetGuid(secondReplacement);

                Assert.AreEqual(expectedGuid, result);
            }
        }

        [Test]
        public void ResolveAssetGuid_UnregisteredRuntimeTexture_WithRegistry_ReturnsEmpty()
        {
            var runtimeTexture = CreateRuntimeTexture(128, 128);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                var result = AssetResolver.ResolveAssetGuid(runtimeTexture);

                Assert.AreEqual(string.Empty, result);
            }
        }

        #endregion

        #region Material Tests

        [Test]
        public void ResolveAssetPath_ReplacedMaterial_ResolvesOriginalPath()
        {
            var originalMaterial = CreateAssetMaterial();
            string expectedPath = AssetDatabase.GetAssetPath(originalMaterial);

            var clonedMaterial = Object.Instantiate(originalMaterial);
            _createdObjects.Add(clonedMaterial);

            var root = new GameObject("Root");
            _createdObjects.Add(root);
            var registry = new ObjectRegistry(root.transform);
            using (new ObjectRegistryScope(registry))
            {
                ObjectRegistry.RegisterReplacedObject(originalMaterial, clonedMaterial);

                var result = AssetResolver.ResolveAssetPath(clonedMaterial);

                Assert.AreEqual(expectedPath, result);
            }
        }

        #endregion

        #region Helper Methods

        private Texture2D CreateAssetTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string assetPath =
                $"{TestAssetFolder}/TestTexture_{width}x{height}_{System.Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

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

        private Material CreateAssetMaterial()
        {
            var material = new Material(Shader.Find("Standard"));

            string assetPath = $"{TestAssetFolder}/TestMaterial_{System.Guid.NewGuid():N}.mat";
            AssetDatabase.CreateAsset(material, assetPath);
            _createdAssetPaths.Add(assetPath);

            return AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        }

        #endregion
    }
}
