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
    /// Backend selection for texture resize (Area Averaging).
    /// </summary>
    public enum ResizeBackendPreference
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
        private const string ResizeBackendKey = PrefsPrefix + "resizeBackend";

        private static readonly GUIContent EnableLoggingContent = new(
            "Enable Logging",
            "Output debug logs during build and preview"
        );

        private static readonly GUIContent AnalysisBackendContent = new(
            "Analysis Backend",
            "Select the backend used for texture complexity analysis"
        );

        private static readonly GUIContent ResizeBackendContent = new(
            "Resize Backend",
            "Select the backend used for Area Averaging texture resize"
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

        /// <summary>
        /// Which backend to use for Area Averaging texture resize.
        /// Auto prefers GPU when available, CPU forces CPU-only.
        /// </summary>
        public static ResizeBackendPreference ResizeBackend
        {
            get =>
                (ResizeBackendPreference)
                    EditorPrefs.GetInt(ResizeBackendKey, (int)ResizeBackendPreference.Auto);
            set => EditorPrefs.SetInt(ResizeBackendKey, (int)value);
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

                    var analysisHelp = AnalysisBackend switch
                    {
                        AnalysisBackendPreference.Auto =>
                            "Uses GPU compute shaders when available, otherwise falls back to CPU.",
                        AnalysisBackendPreference.CPU =>
                            "Always uses CPU for texture analysis. Useful when GPU results are unstable or for debugging.",
                        _ => null,
                    };
                    if (analysisHelp != null)
                    {
                        EditorGUILayout.HelpBox(analysisHelp, MessageType.Info);
                    }

                    EditorGUILayout.Space(4);

                    ResizeBackend = (ResizeBackendPreference)
                        EditorGUILayout.EnumPopup(ResizeBackendContent, ResizeBackend);

                    var resizeHelp = ResizeBackend switch
                    {
                        ResizeBackendPreference.Auto =>
                            "Uses GPU compute shaders for Area Averaging resize when available, otherwise falls back to CPU.",
                        ResizeBackendPreference.CPU =>
                            "Always uses CPU for texture resize. Useful when GPU results are unstable or for debugging.",
                        _ => null,
                    };
                    if (resizeHelp != null)
                    {
                        EditorGUILayout.HelpBox(resizeHelp, MessageType.Info);
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
                    "Resize",
                    "Texture",
                },
            };
        }
    }
}
