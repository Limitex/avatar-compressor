using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Texture Compressor section of the shared Avatar Compressor
    /// preferences window.
    /// </summary>
    public class TextureCompressorPreferencesSection : IPreferencesSection
    {
        private static readonly GUIContent AnalysisBackendContent = new(
            "Analysis Backend",
            "Select the backend used for texture complexity analysis"
        );

        private static readonly GUIContent ResizeBackendContent = new(
            "Resize Backend",
            "Select the backend used for Area Averaging texture resize"
        );

        public string Title => "Texture Compressor";

        public IEnumerable<string> Keywords =>
            new[] { "Texture", "Analysis", "Resize", "Backend", "GPU", "CPU", "Software" };

        public void Draw()
        {
            var analysisBackend = (AnalysisBackendPreference)
                EditorGUILayout.EnumPopup(
                    AnalysisBackendContent,
                    TextureCompressorPreferences.AnalysisBackend
                );
            TextureCompressorPreferences.AnalysisBackend = analysisBackend;
            DrawBackendHelpBox(
                analysisBackend == AnalysisBackendPreference.CPU,
                AnalysisBackendFactory.ResolveBackendName(analysisBackend),
                "texture analysis"
            );

            EditorGUILayout.Space(4);

            var resizeBackend = (ResizeBackendPreference)
                EditorGUILayout.EnumPopup(
                    ResizeBackendContent,
                    TextureCompressorPreferences.ResizeBackend
                );
            TextureCompressorPreferences.ResizeBackend = resizeBackend;
            DrawBackendHelpBox(
                resizeBackend == ResizeBackendPreference.CPU,
                AreaAverageResizerFactory.ResolveBackendName(resizeBackend),
                "Area Averaging resize"
            );
        }

        private static void DrawBackendHelpBox(
            bool isCpuForced,
            string resolvedName,
            string subject
        )
        {
            var help = isCpuForced
                ? $"Always uses CPU for {subject}. Useful when GPU results are unstable or for debugging."
                : $"Currently using {resolvedName}. Uses GPU compute shaders for {subject} when available, otherwise falls back to CPU.";
            EditorGUILayout.HelpBox(help, MessageType.Info);
        }
    }
}
