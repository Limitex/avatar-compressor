using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Shared editor preferences for Avatar Compressor.
    /// Hosts the General section and draws the sections contributed by
    /// features through IPreferencesSection.
    /// Accessible via Edit > Preferences > Avatar Compressor.
    /// </summary>
    public static class AvatarCompressorPreferences
    {
        public const string PrefsPrefix = "dev.limitex.avatar-compressor.";
        private const string BasePath = "Preferences/Avatar Compressor";
        private const string EnableLoggingKey = PrefsPrefix + "enableLogging";

        private static readonly GUIContent EnableLoggingContent = new(
            "Enable Logging",
            "Output debug logs during build and preview"
        );

        /// <summary>
        /// When true, debug log output is enabled during build and preview.
        /// </summary>
        public static bool EnableLogging
        {
            get => EditorPrefs.GetBool(EnableLoggingKey, true);
            set => EditorPrefs.SetBool(EnableLoggingKey, value);
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            var sections = TypeCache
                .GetTypesDerivedFrom<IPreferencesSection>()
                .Where(type => !type.IsAbstract)
                .Select(type => (IPreferencesSection)System.Activator.CreateInstance(type))
                .OrderBy(section => section.Title, System.StringComparer.Ordinal)
                .ToList();

            var keywords = new HashSet<string> { "Avatar", "Compressor", "LAC", "Log", "Debug" };
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

                    EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                    EnableLogging = EditorGUILayout.Toggle(EnableLoggingContent, EnableLogging);

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
