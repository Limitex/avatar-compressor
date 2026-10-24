using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dev.limitex.avatar.compressor.editor;
using nadena.dev.ndmf.localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace dev.limitex.avatar.compressor.tests
{
    internal abstract class LocalizationTestBase
    {
        private string _tempFolder;
        private string _language;
        private readonly List<IDisposable> _resources = new List<IDisposable>();

        [SetUp]
        public void SaveEditorState()
        {
            _ = AvatarCompressorLocalization.Localizer;
            _language = LanguagePrefs.Language;
            var preferences = Track(new SavedEditorPreferences());
            foreach (string key in new[] { "enableLogging", "showLanguagePickerInInspector" })
                preferences.Remember(
                    AvatarCompressorPreferences.PrefsPrefix + key,
                    EditorPrefs.GetBool,
                    EditorPrefs.SetBool
                );
            _tempFolder = "Assets/_LAC_Localization_" + Guid.NewGuid().ToString("N");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorState()
        {
            var cleanup = new List<Action>();
            cleanup.AddRange(
                _resources
                    .AsEnumerable()
                    .Reverse()
                    .Select<IDisposable, Action>(resource => resource.Dispose)
            );
            cleanup.Add(() =>
            {
                if (AssetDatabase.IsValidFolder(_tempFolder))
                    AssetDatabase.DeleteAsset(_tempFolder);
            });
            cleanup.Add(() => LanguagePrefs.Language = _language);
            Exception failure = null;
            try
            {
                RunCleanup(cleanup);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                _resources.Clear();
            }

            yield return WaitForEditorCallbacks();
            if (failure != null)
                throw failure;
        }

        internal static void RunCleanup(IEnumerable<Action> actions)
        {
            var failures = new List<Exception>();
            foreach (var action in actions)
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (failures.Count != 0)
                throw new AggregateException("Localization test cleanup failed.", failures);
        }

        protected T Track<T>(T resource)
            where T : IDisposable
        {
            _resources.Add(resource);
            return resource;
        }

        protected string CreateTempPo(
            string fileName,
            string entries,
            string header = "\"Language: xx-XX\\n\"\n"
        )
        {
            if (!AssetDatabase.IsValidFolder(_tempFolder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(_tempFolder));

            string path = _tempFolder + "/" + fileName;
            File.WriteAllText(path, "msgid \"\"\nmsgstr \"\"\n" + header + "\n" + entries);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        protected static void DeleteTempAsset(string path) => AssetDatabase.DeleteAsset(path);

        protected static IEnumerator WaitForTranslation(string key, string expected)
        {
            yield return null;
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (
                AvatarCompressorLocalization.Tr(key) != expected
                && EditorApplication.timeSinceStartup < deadline
            )
            {
                yield return null;
            }

            Assert.AreEqual(expected, AvatarCompressorLocalization.Tr(key));
        }

        protected static IEnumerator WaitForEditorCallbacks()
        {
            // Catalog reloads can enqueue a notification for the following editor tick.
            for (int i = 0; i < 2; i++)
            {
                bool called = false;
                void Callback() => called = true;
                EditorApplication.delayCall += Callback;
                try
                {
                    double deadline = EditorApplication.timeSinceStartup + 5;
                    while (!called && EditorApplication.timeSinceStartup < deadline)
                        yield return null;
                    Assert.IsTrue(called, "Editor delayCall did not run.");
                }
                finally
                {
                    EditorApplication.delayCall -= Callback;
                }
            }
        }
    }
}
