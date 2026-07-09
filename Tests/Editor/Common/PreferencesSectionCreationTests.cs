using System.Text.RegularExpressions;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.texture.ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace dev.limitex.avatar.compressor.tests
{
    // Deliberately no IPreferencesSection test doubles here: TypeCache would
    // discover them and inject them into the real preferences window whenever
    // the test assembly is loaded. The guard is exercised with types that
    // fail construction instead.
    [TestFixture]
    internal class PreferencesSectionCreationTests
    {
        [Test]
        public void CreateSections_RealSection_IsCreated()
        {
            var sections = AvatarCompressorPreferences.CreateSections(
                new[] { typeof(TextureCompressorPreferencesSection) }
            );

            Assert.AreEqual(1, sections.Count);
            Assert.AreEqual("Texture Compressor", sections[0].Title);
        }

        [Test]
        public void CreateSections_TypeWithoutParameterlessCtor_IsSkipped()
        {
            LogAssert.Expect(LogType.Error, new Regex("Skipping preferences section"));

            var sections = AvatarCompressorPreferences.CreateSections(new[] { typeof(string) });

            Assert.IsEmpty(sections);
        }

        [Test]
        public void CreateSections_TypeNotImplementingInterface_IsSkipped()
        {
            LogAssert.Expect(LogType.Error, new Regex("Skipping preferences section"));

            var sections = AvatarCompressorPreferences.CreateSections(new[] { typeof(object) });

            Assert.IsEmpty(sections);
        }

        [Test]
        public void CreateSections_BrokenTypeDoesNotKillValidSections()
        {
            LogAssert.Expect(LogType.Error, new Regex("Skipping preferences section"));

            var sections = AvatarCompressorPreferences.CreateSections(
                new[] { typeof(string), typeof(TextureCompressorPreferencesSection) }
            );

            Assert.AreEqual(1, sections.Count);
            Assert.AreEqual("Texture Compressor", sections[0].Title);
        }
    }
}
