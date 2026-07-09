using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEditor;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal class TextureCompressorPreferencesTests
    {
        private const string AnalysisBackendKey = "dev.limitex.avatar-compressor.analysisBackend";
        private const string ResizeBackendKey = "dev.limitex.avatar-compressor.resizeBackend";

        private int _originalAnalysisBackend;
        private int _originalResizeBackend;
        private bool _hadAnalysisBackend;
        private bool _hadResizeBackend;

        [SetUp]
        public void SetUp()
        {
            _hadAnalysisBackend = EditorPrefs.HasKey(AnalysisBackendKey);
            _hadResizeBackend = EditorPrefs.HasKey(ResizeBackendKey);
            _originalAnalysisBackend = EditorPrefs.GetInt(AnalysisBackendKey, 0);
            _originalResizeBackend = EditorPrefs.GetInt(ResizeBackendKey, 0);

            EditorPrefs.DeleteKey(AnalysisBackendKey);
            EditorPrefs.DeleteKey(ResizeBackendKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadAnalysisBackend)
                EditorPrefs.SetInt(AnalysisBackendKey, _originalAnalysisBackend);
            else
                EditorPrefs.DeleteKey(AnalysisBackendKey);

            if (_hadResizeBackend)
                EditorPrefs.SetInt(ResizeBackendKey, _originalResizeBackend);
            else
                EditorPrefs.DeleteKey(ResizeBackendKey);
        }

        #region Default Values

        [Test]
        public void AnalysisBackend_DefaultValue_IsAuto()
        {
            Assert.AreEqual(
                AnalysisBackendPreference.Auto,
                TextureCompressorPreferences.AnalysisBackend
            );
        }

        [Test]
        public void ResizeBackend_DefaultValue_IsAuto()
        {
            Assert.AreEqual(
                ResizeBackendPreference.Auto,
                TextureCompressorPreferences.ResizeBackend
            );
        }

        #endregion

        #region Get/Set Round-Trip

        [Test]
        public void AnalysisBackend_SetCpu_ReturnsCpu()
        {
            TextureCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.CPU;

            Assert.AreEqual(
                AnalysisBackendPreference.CPU,
                TextureCompressorPreferences.AnalysisBackend
            );
        }

        [Test]
        public void AnalysisBackend_SetAuto_ReturnsAuto()
        {
            TextureCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.CPU;
            TextureCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.Auto;

            Assert.AreEqual(
                AnalysisBackendPreference.Auto,
                TextureCompressorPreferences.AnalysisBackend
            );
        }

        [Test]
        public void ResizeBackend_SetCpu_ReturnsCpu()
        {
            TextureCompressorPreferences.ResizeBackend = ResizeBackendPreference.CPU;

            Assert.AreEqual(
                ResizeBackendPreference.CPU,
                TextureCompressorPreferences.ResizeBackend
            );
        }

        [Test]
        public void ResizeBackend_SetAuto_ReturnsAuto()
        {
            TextureCompressorPreferences.ResizeBackend = ResizeBackendPreference.CPU;
            TextureCompressorPreferences.ResizeBackend = ResizeBackendPreference.Auto;

            Assert.AreEqual(
                ResizeBackendPreference.Auto,
                TextureCompressorPreferences.ResizeBackend
            );
        }

        #endregion

        #region EditorPrefs Persistence

        [Test]
        public void AnalysisBackend_PersistsToEditorPrefs()
        {
            TextureCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.CPU;

            Assert.AreEqual(
                (int)AnalysisBackendPreference.CPU,
                EditorPrefs.GetInt(AnalysisBackendKey)
            );
        }

        [Test]
        public void ResizeBackend_PersistsToEditorPrefs()
        {
            TextureCompressorPreferences.ResizeBackend = ResizeBackendPreference.CPU;

            Assert.AreEqual((int)ResizeBackendPreference.CPU, EditorPrefs.GetInt(ResizeBackendKey));
        }

        #endregion

        #region Enum Values

        [Test]
        public void AnalysisBackendPreference_Auto_IsZero()
        {
            Assert.AreEqual(0, (int)AnalysisBackendPreference.Auto);
        }

        [Test]
        public void AnalysisBackendPreference_CPU_IsOne()
        {
            Assert.AreEqual(1, (int)AnalysisBackendPreference.CPU);
        }

        [Test]
        public void ResizeBackendPreference_Auto_IsZero()
        {
            Assert.AreEqual(0, (int)ResizeBackendPreference.Auto);
        }

        [Test]
        public void ResizeBackendPreference_CPU_IsOne()
        {
            Assert.AreEqual(1, (int)ResizeBackendPreference.CPU);
        }

        #endregion
    }
}
