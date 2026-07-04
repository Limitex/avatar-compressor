using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;
using UnityEditor;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AvatarCompressorPreferencesTests
    {
        private const string EnableLoggingKey = "dev.limitex.avatar-compressor.enableLogging";

        private bool _originalEnableLogging;
        private bool _hadEnableLogging;

        [SetUp]
        public void SetUp()
        {
            _hadEnableLogging = EditorPrefs.HasKey(EnableLoggingKey);
            _originalEnableLogging = EditorPrefs.GetBool(EnableLoggingKey, true);

            EditorPrefs.DeleteKey(EnableLoggingKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadEnableLogging)
                EditorPrefs.SetBool(EnableLoggingKey, _originalEnableLogging);
            else
                EditorPrefs.DeleteKey(EnableLoggingKey);
        }

        #region Default Values

        [Test]
        public void EnableLogging_DefaultValue_IsTrue()
        {
            Assert.IsTrue(AvatarCompressorPreferences.EnableLogging);
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

        #endregion

        #region EditorPrefs Persistence

        [Test]
        public void EnableLogging_PersistsToEditorPrefs()
        {
            AvatarCompressorPreferences.EnableLogging = false;

            Assert.AreEqual(false, EditorPrefs.GetBool(EnableLoggingKey));
        }

        #endregion
    }
}
