using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Common interface for all compressors.
    /// </summary>
    public interface ICompressor
    {
        /// <summary>
        /// Display name of the compressor.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Compresses the avatar.
        /// </summary>
        /// <param name="root">Avatar root GameObject</param>
        /// <param name="enableLogging">Whether to log progress</param>
        void Compress(GameObject root, bool enableLogging);
    }
}
