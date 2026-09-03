using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Shared editor preferences for Avatar Compressor.
    /// Hosts the General and Language sections and draws sections contributed by
    /// features through IPreferencesSection.
    /// Accessible via Edit > Preferences > Avatar Compressor.
    /// </summary>
    internal static class AvatarCompressorPreferences
    {
        public const string PrefsPrefix = "dev.limitex.avatar-compressor.";
        private const string BasePath = "Preferences/Avatar Compressor";
        private const string EnableLoggingKey = PrefsPrefix + "enableLogging";

        /// <summary>
        /// When true, debug log output is enabled during build and preview.
        /// </summary>
        public static bool EnableLogging
        {
            get => EditorPrefs.GetBool(EnableLoggingKey, true);
            set => EditorPrefs.SetBool(EnableLoggingKey, value);
        }

        /// <summary>
        /// Instantiates and title-sorts the preference sections. A section
        /// that fails to construct — or whose Title/Keywords getters throw —
        /// is skipped with an error, so one broken implementer cannot take
        /// down the whole settings page (SettingsService drops the entire
        /// provider when its factory throws).
        /// </summary>
        public static List<IPreferencesSection> CreateSections(IEnumerable<System.Type> types)
        {
            var sections = new List<IPreferencesSection>();
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;

                try
                {
                    var section = (IPreferencesSection)System.Activator.CreateInstance(type);
                    // Evaluate everything the provider reads inside the guard,
                    // so a broken member surfaces here and not mid-layout.
                    _ = section.Title;
                    _ = section.Keywords.Count();
                    sections.Add(section);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[AvatarCompressor] Skipping preferences section '{type.FullName}': {e}"
                    );
                }
            }

            sections.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Title, b.Title));
            return sections;
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            var sections = CreateSections(TypeCache.GetTypesDerivedFrom<IPreferencesSection>());

            var keywords = new HashSet<string>
            {
                "Avatar",
                "Compressor",
                "LAC",
                "Log",
                "Debug",
                "Language",
                "语言",
                "語言",
                "言語",
                "언어",
            };
            foreach (var section in sections)
            {
                keywords.UnionWith(section.Keywords);
            }

            return new SettingsProvider(BasePath, SettingsScope.User)
            {
                label = "Avatar Compressor",
                guiHandler = _ =>
                {
                    EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

                    EditorGUILayout.LabelField(
                        AvatarCompressorLocalization.Tr("Common:label:general"),
                        EditorStyles.boldLabel
                    );
                    EnableLogging = EditorGUILayout.Toggle(
                        AvatarCompressorLocalization.Content(
                            "Common:prop:enableLogging",
                            "Common:prop:enableLogging:tooltip"
                        ),
                        EnableLogging
                    );

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField(
                        AvatarCompressorLocalization.Tr("Common:label:language"),
                        EditorStyles.boldLabel
                    );
                    AvatarCompressorLocalization.DrawLanguagePicker();

                    foreach (var section in sections)
                    {
                        EditorGUILayout.Space(10);
                        EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
                        section.Draw();
                    }

                    EditorGUILayout.EndVertical();
                },
                keywords = keywords,
            };
        }
    }
}
