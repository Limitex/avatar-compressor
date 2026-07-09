using System.Collections.Generic;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// A feature-contributed section of the Avatar Compressor preferences
    /// window. Implementations are discovered via TypeCache, must have a
    /// parameterless constructor, and are drawn in Title order after the
    /// General section.
    /// </summary>
    internal interface IPreferencesSection
    {
        /// <summary>Bold section header shown in the preferences window.</summary>
        string Title { get; }

        /// <summary>Search keywords merged into the preferences index.</summary>
        IEnumerable<string> Keywords { get; }

        void Draw();
    }
}
