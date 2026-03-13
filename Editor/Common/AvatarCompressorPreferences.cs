using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Backend selection for texture analysis.
    /// </summary>
    public enum AnalysisBackendPreference
    {
        /// <summary>Use GPU if available, otherwise fall back to CPU.</summary>
        Auto,

        /// <summary>Always use CPU backend.</summary>
        [InspectorName("Software")]
        CPU,
    }

    /// <summary>
    /// Shared editor preferences for Avatar Compressor.
    /// Accessible via Edit > Preferences > Avatar Compressor > Texture Compressor.
    /// </summary>
    public static class AvatarCompressorPreferences
    {
        private const string PrefsPrefix = "dev.limitex.avatar-compressor.";
        private const string BasePath = "Preferences/Avatar Compressor";
        private const string EnableLoggingKey = PrefsPrefix + "enableLogging";
        private const string AnalysisBackendKey = PrefsPrefix + "analysisBackend";

        private static readonly GUIContent EnableLoggingContent = new(
            "Enable Logging",
            "Output debug logs during build and preview"
        );

        private static readonly GUIContent AnalysisBackendContent = new(
            "Analysis Backend",
            "Select the backend used for texture complexity analysis"
        );

        /// <summary>
        /// When true, debug log output is enabled during build and preview.
        /// </summary>
        public static bool EnableLogging
        {
            get => EditorPrefs.GetBool(EnableLoggingKey, true);
            set => EditorPrefs.SetBool(EnableLoggingKey, value);
        }

        /// <summary>
        /// Which backend to use for texture analysis.
        /// Auto prefers GPU when available, CPU forces CPU-only.
        /// </summary>
        public static AnalysisBackendPreference AnalysisBackend
        {
            get =>
                (AnalysisBackendPreference)
                    EditorPrefs.GetInt(AnalysisBackendKey, (int)AnalysisBackendPreference.Auto);
            set => EditorPrefs.SetInt(AnalysisBackendKey, (int)value);
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(BasePath, SettingsScope.User)
            {
                label = "Avatar Compressor",
                guiHandler = _ =>
                {
                    EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

                    EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
                    EnableLogging = EditorGUILayout.Toggle(EnableLoggingContent, EnableLogging);

                    EditorGUILayout.Space(10);

                    EditorGUILayout.LabelField("Texture Compressor", EditorStyles.boldLabel);
                    AnalysisBackend = (AnalysisBackendPreference)
                        EditorGUILayout.EnumPopup(AnalysisBackendContent, AnalysisBackend);

                    var backendHelp = AnalysisBackend switch
                    {
                        AnalysisBackendPreference.Auto =>
                            "Uses GPU compute shaders when available, otherwise falls back to CPU.",
                        AnalysisBackendPreference.CPU =>
                            "Always uses CPU for texture analysis. Useful when GPU results are unstable or for debugging.",
                        _ => null,
                    };
                    if (backendHelp != null)
                    {
                        EditorGUILayout.HelpBox(backendHelp, MessageType.Info);
                    }

                    EditorGUILayout.EndVertical();
                },
                keywords = new HashSet<string>
                {
                    "Avatar",
                    "Compressor",
                    "LAC",
                    "Log",
                    "Debug",
                    "GPU",
                    "CPU",
                    "Software",
                    "Backend",
                    "Analysis",
                    "Texture",
                },
            };
        }
    }
}
