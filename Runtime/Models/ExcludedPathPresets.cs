namespace dev.limitex.avatar.compressor.texture
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
        /// </summary>
        public static string[] GetDefaultPaths()
        {
            var paths = new string[Presets.Length];
            for (int i = 0; i < Presets.Length; i++)
            {
                paths[i] = Presets[i].Path;
            }
            return paths;
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
