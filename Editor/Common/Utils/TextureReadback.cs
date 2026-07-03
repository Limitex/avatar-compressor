using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// GPU→CPU texture readback helpers shared by the texture pipeline.
    /// Global graphics state changes (active RenderTexture, GL.sRGBWrite) are
    /// saved and restored; multi-step RenderTexture work must hold
    /// <see cref="RenderTextureLock"/>.
    /// </summary>
    internal static class TextureReadback
    {
        // Lock object for thread-safe RenderTexture operations
        internal static readonly object RenderTextureLock = new object();

        /// <summary>
        /// Blits a texture into a temporary RenderTexture with the given color-space
        /// policy and returns a readable RGBA32 copy (no mip chain, settings not
        /// copied), or null on failure. Owns the RenderTexture lock, the active-RT
        /// save/restore, and the GL.sRGBWrite guard. Caller destroys the result.
        /// </summary>
        internal static Texture2D BlitToReadable(
            Texture2D source,
            RenderTextureReadWrite colorSpace,
            bool linearFlag
        )
        {
            lock (RenderTextureLock)
            {
                var previousSRGBWrite = GL.sRGBWrite;
                RenderTexture rt = null;
                try
                {
                    // Use explicit RenderTexture lifecycle instead of GetTemporary/ReleaseTemporary
                    // so that native GPU memory is freed immediately by DestroyImmediate,
                    // rather than being held in Unity's RT pool across calls.
                    rt = new RenderTexture(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.ARGB32,
                        colorSpace
                    );
                    rt.Create();

                    // The write-side linear->sRGB encode into an sRGB RT is gated by
                    // GL.sRGBWrite (editor IMGUI leaves it false), not by the RT's
                    // readWrite flag. Harmless for linear RTs (no conversion).
                    GL.sRGBWrite = true;
                    Graphics.Blit(source, rt);

                    return ReadbackToTexture2D(
                        rt,
                        source.width,
                        source.height,
                        mipChain: false,
                        linear: linearFlag
                    );
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] Failed to read back texture '{source.name}': {e.Message}"
                    );
                    return null;
                }
                finally
                {
                    GL.sRGBWrite = previousSRGBWrite;
                    if (rt != null)
                    {
                        rt.Release();
                        Object.DestroyImmediate(rt);
                    }
                }
            }
        }

        /// <summary>
        /// Reads a RenderTexture back into a new RGBA32 Texture2D via
        /// ReadPixels. Saves and restores the active RenderTexture and never
        /// leaks the texture on failure. Caller destroys the result.
        /// </summary>
        internal static Texture2D ReadbackToTexture2D(
            RenderTexture rt,
            int width,
            int height,
            bool mipChain,
            bool linear
        )
        {
            var previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                RenderTexture.active = rt;
                readable = new Texture2D(width, height, TextureFormat.RGBA32, mipChain, linear);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply(mipChain);

                var result = readable;
                readable = null;
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                if (readable != null)
                    Object.DestroyImmediate(readable);
            }
        }

        internal static void CopyTextureSettings(Texture2D source, Texture2D dest)
        {
            dest.wrapModeU = source.wrapModeU;
            dest.wrapModeV = source.wrapModeV;
            dest.wrapModeW = source.wrapModeW;
            dest.filterMode = source.filterMode;
            dest.anisoLevel = source.anisoLevel;
        }
    }
}
