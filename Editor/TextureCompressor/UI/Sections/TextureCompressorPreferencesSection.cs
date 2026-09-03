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
        public string Title => AvatarCompressorLocalization.Tr("preferences.texture_compressor");

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
            var analysisBackend = AvatarCompressorLocalization.EnumPopup(
                AvatarCompressorLocalization.Content(
                    "preferences.analysis_backend",
                    "preferences.analysis_backend.tooltip"
                ),
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
                "preferences.analysis_backend.cpu_help",
                "preferences.analysis_backend.auto_help"
            );

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            var resizeBackend = AvatarCompressorLocalization.EnumPopup(
                AvatarCompressorLocalization.Content(
                    "preferences.resize_backend",
                    "preferences.resize_backend.tooltip"
                ),
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
                "preferences.resize_backend.cpu_help",
                "preferences.resize_backend.auto_help"
            );
        }

        private static void DrawBackendHelpBox(
            bool isCpuForced,
            string resolvedName,
            string cpuHelpKey,
            string autoHelpKey
        )
        {
            var help = isCpuForced
                ? AvatarCompressorLocalization.Tr(cpuHelpKey)
                : AvatarCompressorLocalization.Tr(autoHelpKey, resolvedName);
            EditorGUILayout.HelpBox(help, MessageType.Info);
        }
    }
}
