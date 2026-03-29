namespace dev.limitex.avatar.compressor
{
    /// <summary>
    /// Predefined path exclusion presets for common packages.
    /// </summary>
    public static class ExcludedPathPresets
    {
        public static readonly ExcludedPathPreset[] Presets = new[]
        {
            new ExcludedPathPreset("VRCFury Temp", "Packages/com.vrcfury.temp/"),
        };

        /// <summary>
        /// Gets the default excluded paths for new TextureCompressor components.
        /// Returns an empty array — presets are available via the UI "Add Path" menu
        /// but not applied by default.
        /// </summary>
        public static string[] GetDefaultPaths()
        {
            return System.Array.Empty<string>();
        }
    }

    /// <summary>
    /// Represents a predefined path exclusion preset.
    /// </summary>
    public readonly struct ExcludedPathPreset
    {
        public readonly string Label;
        public readonly string Path;

        public ExcludedPathPreset(string label, string path)
        {
            Label = label;
            Path = path;
        }
    }
}
