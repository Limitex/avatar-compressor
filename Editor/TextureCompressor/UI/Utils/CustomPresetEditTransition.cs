using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Handles edit mode transitions with user confirmation dialogs.
    /// Coordinates between CustomPresetEditorState and the UI layer.
    /// </summary>
    public static class CustomPresetEditTransition
    {
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

            var restriction = CustomPresetEditorState.GetEditRestriction(config);

            if (restriction.CanDirectEdit)
            {
                CustomPresetEditorState.SwitchToEditMode(config);
                return;
            }

            string title = "Unlink Preset";
            string reason = restriction switch
            {
                { IsBuiltIn: true } => "This preset is a built-in preset and cannot be edited.",
                { IsInPackage: true } => "This preset is in a package and cannot be edited.",
                _ => "This preset is locked and cannot be edited directly.",
            };
            string message =
                $"{reason}\n\n"
                + "Do you want to unlink and edit the settings manually?\n"
                + "(Current settings will be preserved)";

            bool confirmed = EditorUtility.DisplayDialog(
                title,
                message,
                "Unlink and Edit",
                "Cancel"
            );

            if (!confirmed)
                return;

            Undo.RecordObject(config, "Unlink Preset and Edit");
            CustomPresetEditorState.UnlinkPresetAndSwitchToEditMode(config);
            EditorUtility.SetDirty(config);
        }
    }
}
