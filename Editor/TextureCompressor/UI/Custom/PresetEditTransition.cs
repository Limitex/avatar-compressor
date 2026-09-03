using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Handles edit mode transitions with user confirmation dialogs.
    /// Coordinates between PresetEditorState and the UI layer.
    /// </summary>
    internal static class PresetEditTransition
    {
        /// <summary>
        /// Validates edit state consistency and auto-exits edit mode if the preset became locked.
        /// Call this at the start of UI drawing to ensure state is valid.
        /// </summary>
        public static void EnsureValidEditState(TextureCompressor config)
        {
            if (config == null)
                return;

            if (
                !PresetEditorState.IsCustomEditable(config)
                && PresetEditorState.IsInEditMode(config)
            )
            {
                if (config.CustomPresetAsset != null)
                {
                    config.CustomPresetAsset.ApplyTo(config);
                    EditorUtility.SetDirty(config);
                }
                PresetEditorState.SetEditMode(config, false);
            }
        }

        /// <summary>
        /// Attempts to transition to edit mode, showing a confirmation dialog if unlinking is required.
        /// Uses appropriate dialog messages based on the preset's restriction type.
        /// Handles Undo registration and SetDirty automatically when unlinking.
        /// </summary>
        /// <param name="config">The TextureCompressor configuration.</param>
        public static void TryEnterEditMode(TextureCompressor config)
        {
            if (config == null)
                return;

            var restriction = PresetEditorState.GetRestriction(config);

            if (restriction.CanDirectEdit())
            {
                PresetEditorState.SwitchToEditMode(config);
                InternalEditorUtility.RepaintAllViews();
                return;
            }

            string title = AvatarCompressorLocalization.Tr("unlink_dialog.title");
            string reason = restriction switch
            {
                PresetRestriction.BuiltIn => AvatarCompressorLocalization.Tr(
                    "unlink_dialog.reason.built_in"
                ),
                PresetRestriction.ExternalPackage => AvatarCompressorLocalization.Tr(
                    "unlink_dialog.reason.package"
                ),
                _ => AvatarCompressorLocalization.Tr("unlink_dialog.reason.locked"),
            };
            string message = AvatarCompressorLocalization.Tr("unlink_dialog.message", reason);

            bool confirmed = EditorUtility.DisplayDialog(
                title,
                message,
                AvatarCompressorLocalization.Tr("unlink_dialog.confirm"),
                AvatarCompressorLocalization.Tr("common.cancel")
            );

            if (!confirmed)
                return;

            Undo.RecordObject(config, "Unlink Preset and Edit");
            PresetEditorState.UnlinkPresetAndSwitchToEditMode(config);
            EditorUtility.SetDirty(config);
            GUIUtility.ExitGUI();
        }
    }
}
