using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;
using UnityEditor;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AvatarCompressorPreferencesTests
    {
        private const string EnableLoggingKey = "dev.limitex.avatar-compressor.enableLogging";
        private const string AnalysisBackendKey = "dev.limitex.avatar-compressor.analysisBackend";

        private bool _originalEnableLogging;
        private int _originalAnalysisBackend;
        private bool _hadEnableLogging;
        private bool _hadAnalysisBackend;

        [SetUp]
        public void SetUp()
        {
            _hadEnableLogging = EditorPrefs.HasKey(EnableLoggingKey);
            _hadAnalysisBackend = EditorPrefs.HasKey(AnalysisBackendKey);
            _originalEnableLogging = EditorPrefs.GetBool(EnableLoggingKey, true);
            _originalAnalysisBackend = EditorPrefs.GetInt(AnalysisBackendKey, 0);

            EditorPrefs.DeleteKey(EnableLoggingKey);
            EditorPrefs.DeleteKey(AnalysisBackendKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadEnableLogging)
                EditorPrefs.SetBool(EnableLoggingKey, _originalEnableLogging);
            else
                EditorPrefs.DeleteKey(EnableLoggingKey);

            if (_hadAnalysisBackend)
                EditorPrefs.SetInt(AnalysisBackendKey, _originalAnalysisBackend);
            else
                EditorPrefs.DeleteKey(AnalysisBackendKey);
        }

        #region Default Values

        [Test]
        public void EnableLogging_DefaultValue_IsTrue()
        {
            Assert.IsTrue(AvatarCompressorPreferences.EnableLogging);
        }

        [Test]
        public void AnalysisBackend_DefaultValue_IsAuto()
        {
            Assert.AreEqual(
                AnalysisBackendPreference.Auto,
                AvatarCompressorPreferences.AnalysisBackend
            );
        }

        #endregion

        #region Get/Set Round-Trip

        [Test]
        public void EnableLogging_SetFalse_ReturnsFalse()
        {
            AvatarCompressorPreferences.EnableLogging = false;

            Assert.IsFalse(AvatarCompressorPreferences.EnableLogging);
        }

        [Test]
        public void EnableLogging_SetTrue_ReturnsTrue()
        {
            AvatarCompressorPreferences.EnableLogging = false;
            AvatarCompressorPreferences.EnableLogging = true;

            Assert.IsTrue(AvatarCompressorPreferences.EnableLogging);
        }

        [Test]
        public void AnalysisBackend_SetCpu_ReturnsCpu()
        {
            AvatarCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.CPU;

            Assert.AreEqual(
                AnalysisBackendPreference.CPU,
                AvatarCompressorPreferences.AnalysisBackend
            );
        }

        [Test]
        public void AnalysisBackend_SetAuto_ReturnsAuto()
        {
            AvatarCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.CPU;
            AvatarCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.Auto;

            Assert.AreEqual(
                AnalysisBackendPreference.Auto,
                AvatarCompressorPreferences.AnalysisBackend
            );
        }

        #endregion

        #region EditorPrefs Persistence

        [Test]
        public void EnableLogging_PersistsToEditorPrefs()
        {
            AvatarCompressorPreferences.EnableLogging = false;

            Assert.AreEqual(false, EditorPrefs.GetBool(EnableLoggingKey));
        }

        [Test]
        public void AnalysisBackend_PersistsToEditorPrefs()
        {
            AvatarCompressorPreferences.AnalysisBackend = AnalysisBackendPreference.CPU;

            Assert.AreEqual(
                (int)AnalysisBackendPreference.CPU,
                EditorPrefs.GetInt(AnalysisBackendKey)
            );
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

        #endregion
    }
}
