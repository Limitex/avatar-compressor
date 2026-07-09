using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Texture Compressor section of the shared Avatar Compressor
    /// preferences window.
    /// </summary>
    internal sealed class TextureCompressorPreferencesSection : IPreferencesSection
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

        // GPU availability is static per editor session (statics reset on
        // domain reload), so resolved names are cached per preference value
        // to keep the shader-asset probes off the repaint path.
        private AnalysisBackendPreference? _analysisNameFor;
        private string _analysisName;
        private ResizeBackendPreference? _resizeNameFor;
        private string _resizeName;

        public void Draw()
        {
            // Change checks keep EditorPrefs writes off the repaint path.
            EditorGUI.BeginChangeCheck();
            var analysisBackend = (AnalysisBackendPreference)
                EditorGUILayout.EnumPopup(
                    AnalysisBackendContent,
                    TextureCompressorPreferences.AnalysisBackend
                );
            if (EditorGUI.EndChangeCheck())
                TextureCompressorPreferences.AnalysisBackend = analysisBackend;
            if (_analysisNameFor != analysisBackend)
            {
                _analysisName = AnalysisBackendFactory.ResolveBackendName(analysisBackend);
                _analysisNameFor = analysisBackend;
            }
            DrawBackendHelpBox(
                analysisBackend == AnalysisBackendPreference.CPU,
                _analysisName,
                "texture analysis"
            );

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            var resizeBackend = (ResizeBackendPreference)
                EditorGUILayout.EnumPopup(
                    ResizeBackendContent,
                    TextureCompressorPreferences.ResizeBackend
                );
            if (EditorGUI.EndChangeCheck())
                TextureCompressorPreferences.ResizeBackend = resizeBackend;
            if (_resizeNameFor != resizeBackend)
            {
                _resizeName = AreaAverageResizerFactory.ResolveBackendName(resizeBackend);
                _resizeNameFor = resizeBackend;
            }
            DrawBackendHelpBox(
                resizeBackend == ResizeBackendPreference.CPU,
                _resizeName,
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
