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
        public string Title =>
            AvatarCompressorLocalization.Tr("TextureCompressor:label:preferences");

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
                "TextureCompressor",
                AvatarCompressorLocalization.Content(
                    "TextureCompressor:prop:analysisBackend",
                    "TextureCompressor:prop:analysisBackend:tooltip"
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
                "TextureCompressor:message:analysisBackendCpuHelp",
                "TextureCompressor:message:analysisBackendAutoHelp"
            );

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            var resizeBackend = AvatarCompressorLocalization.EnumPopup(
                "TextureCompressor",
                AvatarCompressorLocalization.Content(
                    "TextureCompressor:prop:resizeBackend",
                    "TextureCompressor:prop:resizeBackend:tooltip"
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
                "TextureCompressor:message:resizeBackendCpuHelp",
                "TextureCompressor:message:resizeBackendAutoHelp"
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
