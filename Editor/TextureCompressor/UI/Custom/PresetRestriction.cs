namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Preset editing restriction levels.
    /// </summary>
    public enum PresetRestriction
    {
        /// <summary>In Assets/ folder and Lock=false. Freely editable.</summary>
        None,

        /// <summary>In Assets/ folder but Lock=true. Can edit after unlinking.</summary>
        Locked,

        /// <summary>In another package. Can edit after unlinking.</summary>
        ExternalPackage,

        /// <summary>In this package (built-in). Can edit after unlinking.</summary>
        BuiltIn,
    }

    /// <summary>
    /// Extension methods for PresetRestriction enum.
    /// </summary>
    public static class PresetRestrictionExtensions
    {
        /// <summary>
        /// Returns true if the preset can be edited directly without unlinking.
        /// </summary>
        public static bool CanDirectEdit(this PresetRestriction restriction) =>
            restriction == PresetRestriction.None;

        /// <summary>
        /// Returns true if the preset requires unlinking to edit.
        /// </summary>
        public static bool RequiresUnlink(this PresetRestriction restriction) =>
            restriction != PresetRestriction.None;
    }
}
