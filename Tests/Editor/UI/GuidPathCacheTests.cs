using System.Collections.Generic;
using System.IO;
using dev.limitex.avatar.compressor.editor.ui;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class GuidPathCacheTests
    {
        private const string TestAssetFolder = "Assets/_LAC_TMP_GuidPathCache";
        private List<string> _createdAssetPaths;

        [SetUp]
        public void SetUp()
        {
            _createdAssetPaths = new List<string>();
            GuidPathCache.Clear();

            if (!AssetDatabase.IsValidFolder(TestAssetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_LAC_TMP_GuidPathCache");
            }
        }

        [TearDown]
        public void TearDown()
        {
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

        [Test]
        public void GetPath_AfterClear_RefreshesCachedPath()
        {
            string originalPath = CreateImportedTextureAsset("GuidPathCacheTexture");
            string guid = AssetDatabase.AssetPathToGUID(originalPath);

            Assert.That(GuidPathCache.GetPath(guid), Is.EqualTo(originalPath));

            string renameError = AssetDatabase.RenameAsset(
                originalPath,
                "GuidPathCacheTextureRenamed"
            );
            Assert.That(renameError, Is.Empty);

            string renamedPath = $"{TestAssetFolder}/GuidPathCacheTextureRenamed.png";
            ReplaceTrackedPath(originalPath, renamedPath);

            GuidPathCache.Clear();

            Assert.That(GuidPathCache.GetPath(guid), Is.EqualTo(renamedPath));
        }

        private string CreateImportedTextureAsset(string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels32(
                new[]
                {
                    new Color32(255, 0, 0, 255),
                    new Color32(0, 255, 0, 255),
                    new Color32(0, 0, 255, 255),
                    new Color32(255, 255, 255, 255),
                }
            );
            texture.Apply();

            string assetPath = $"{TestAssetFolder}/{name}.png";
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            _createdAssetPaths.Add(assetPath);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return assetPath;
        }

        private void ReplaceTrackedPath(string originalPath, string renamedPath)
        {
            int index = _createdAssetPaths.IndexOf(originalPath);
            if (index >= 0)
            {
                _createdAssetPaths[index] = renamedPath;
            }
        }
    }
}
