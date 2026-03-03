using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Service for processing textures (resizing only).
    /// All output dimensions are guaranteed to be multiples of 4 for DXT/BC compression compatibility.
    /// </summary>
    public class TextureProcessor
    {
        // Lock object for thread-safe RenderTexture operations
        private static readonly object RenderTextureLock = new object();

        private readonly int _minResolution;
        private readonly int _maxResolution;
        private readonly bool _forcePowerOfTwo;

        public TextureProcessor(int minResolution, int maxResolution, bool forcePowerOfTwo)
        {
            _minResolution = minResolution;
            _maxResolution = maxResolution;
            _forcePowerOfTwo = forcePowerOfTwo;
        }

        /// <summary>
        /// Resizes a texture using pre-computed analysis result.
        /// </summary>
        /// <param name="source">Source texture to resize</param>
        /// <param name="analysis">Pre-computed analysis result with recommended settings</param>
        /// <param name="isNormalMap">Whether this texture is a normal map (uses linear color space)</param>
        /// <returns>Resized texture (uncompressed RGBA32 format)</returns>
        public Texture2D Resize(
            Texture2D source,
            TextureAnalysisResult analysis,
            bool isNormalMap = false
        )
        {
            Texture2D result;
            if (
                analysis.RecommendedDivisor <= 1
                && source.width <= _maxResolution
                && source.height <= _maxResolution
            )
            {
                // Even when copying, ensure dimensions are multiples of 4 for DXT/BC compression
                int width = EnsureMultipleOf4(source.width);
                int height = EnsureMultipleOf4(source.height);

                result = ResizeTo(source, width, height, isNormalMap);
            }
            else
            {
                result = ResizeTo(
                    source,
                    analysis.RecommendedResolution.x,
                    analysis.RecommendedResolution.y,
                    isNormalMap
                );
            }

            return result;
        }

        /// <summary>
        /// Ensures a dimension is a multiple of 4 for DXT/BC compression compatibility.
        /// </summary>
        /// <returns>The dimension rounded up to the nearest multiple of 4, minimum 4 (e.g., 150→152, 4→4, 2→4)</returns>
        private static int EnsureMultipleOf4(int dimension)
        {
            return Mathf.Max(4, ((dimension + 3) / 4) * 4);
        }

        /// <summary>
        /// Calculates new dimensions based on divisor.
        /// </summary>
        /// <returns>New dimensions clamped to min/max resolution and rounded appropriately</returns>
        public Vector2Int CalculateNewDimensions(int width, int height, int divisor)
        {
            int newWidth = Mathf.Max(width / divisor, _minResolution);
            int newHeight = Mathf.Max(height / divisor, _minResolution);

            newWidth = Mathf.Min(newWidth, _maxResolution);
            newHeight = Mathf.Min(newHeight, _maxResolution);

            if (_forcePowerOfTwo)
            {
                newWidth = Mathf.ClosestPowerOfTwo(newWidth);
                newHeight = Mathf.ClosestPowerOfTwo(newHeight);

                if (newWidth > _maxResolution)
                    newWidth = Mathf.ClosestPowerOfTwo(_maxResolution / 2) * 2;
                if (newHeight > _maxResolution)
                    newHeight = Mathf.ClosestPowerOfTwo(_maxResolution / 2) * 2;
            }
            else
            {
                // Ensure dimensions are multiples of 4 for DXT/BC compression compatibility
                newWidth = EnsureMultipleOf4(newWidth);
                newHeight = EnsureMultipleOf4(newHeight);
            }

            return new Vector2Int(newWidth, newHeight);
        }

        /// <summary>
        /// Resizes a texture to the specified dimensions.
        /// Thread-safe: uses lock to protect RenderTexture.active.
        /// </summary>
        /// <param name="source">Source texture to resize</param>
        /// <param name="newWidth">Target width</param>
        /// <param name="newHeight">Target height</param>
        /// <param name="isNormalMap">Whether this texture is a normal map (uses linear color space)</param>
        /// <returns>Resized texture (uncompressed RGBA32 format)</returns>
        public Texture2D ResizeTo(
            Texture2D source,
            int newWidth,
            int newHeight,
            bool isNormalMap = false
        )
        {
            lock (RenderTextureLock)
            {
                RenderTexture previous = RenderTexture.active;
                try
                {
                    return BlitResize(source, newWidth, newHeight, isNormalMap);
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }
        }

        /// <summary>
        /// Resizes multiple textures in a single lock scope for efficiency.
        /// </summary>
        public Dictionary<Texture2D, Texture2D> ResizeBatch(
            IEnumerable<(Texture2D Source, TextureAnalysisResult Analysis, bool IsNormalMap)> items
        )
        {
            var result = new Dictionary<Texture2D, Texture2D>();

            lock (RenderTextureLock)
            {
                RenderTexture previous = RenderTexture.active;
                try
                {
                    foreach (var item in items)
                    {
                        int newWidth,
                            newHeight;
                        if (
                            item.Analysis.RecommendedDivisor <= 1
                            && item.Source.width <= _maxResolution
                            && item.Source.height <= _maxResolution
                        )
                        {
                            newWidth = EnsureMultipleOf4(item.Source.width);
                            newHeight = EnsureMultipleOf4(item.Source.height);
                        }
                        else
                        {
                            newWidth = item.Analysis.RecommendedResolution.x;
                            newHeight = item.Analysis.RecommendedResolution.y;
                        }

                        result[item.Source] = BlitResize(
                            item.Source,
                            newWidth,
                            newHeight,
                            item.IsNormalMap
                        );
                    }
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }

            return result;
        }

        /// <summary>
        /// Core blit logic: creates a resized Texture2D from source using RenderTexture.
        /// Caller must hold RenderTextureLock and manage RenderTexture.active save/restore.
        /// </summary>
        private static Texture2D BlitResize(
            Texture2D source,
            int newWidth,
            int newHeight,
            bool isNormalMap
        )
        {
            // Normal maps store vector data, not color, so they must be processed in linear space
            // to avoid sRGB gamma correction that would corrupt the normal vectors.
            var colorSpace = isNormalMap
                ? RenderTextureReadWrite.Linear
                : RenderTextureReadWrite.Default;

            var rtFormat = SelectNormalMapRTFormat(isNormalMap);

            var rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, rtFormat, colorSpace);
            try
            {
                rt.filterMode = FilterMode.Bilinear;
                RenderTexture.active = rt;
                Graphics.Blit(source, rt);

                var resized = new Texture2D(
                    newWidth,
                    newHeight,
                    TextureFormat.RGBA32,
                    source.mipmapCount > 1,
                    isNormalMap
                );
                resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                resized.Apply(source.mipmapCount > 1);

                CopyTextureSettings(source, resized);
                return resized;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// Selects the best RenderTextureFormat for normal map resize (float > half > ARGB32).
        /// </summary>
        private static RenderTextureFormat SelectNormalMapRTFormat(bool isNormalMap)
        {
            if (!isNormalMap)
                return RenderTextureFormat.ARGB32;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                return RenderTextureFormat.ARGBFloat;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                return RenderTextureFormat.ARGBHalf;
            return RenderTextureFormat.ARGB32;
        }

        private static void CopyTextureSettings(Texture2D source, Texture2D dest)
        {
            dest.wrapModeU = source.wrapModeU;
            dest.wrapModeV = source.wrapModeV;
            dest.wrapModeW = source.wrapModeW;
            dest.filterMode = source.filterMode;
            dest.anisoLevel = source.anisoLevel;
        }

        /// <summary>
        /// Gets readable pixels from multiple textures in a single lock scope for efficiency.
        /// Readable textures are processed without locking; non-readable textures are batched
        /// under a single lock acquisition to minimize RenderTexture lock contention.
        /// </summary>
        public Dictionary<Texture2D, Color[]> GetReadablePixelsBatch(
            IEnumerable<Texture2D> textures
        )
        {
            var result = new Dictionary<Texture2D, Color[]>();
            var nonReadable = new List<Texture2D>();

            // First pass: collect readable textures without lock
            foreach (var texture in textures)
            {
                if (texture == null)
                    continue;

                if (texture.isReadable)
                {
                    try
                    {
                        result[texture] = texture.GetPixels();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning(
                            $"[TextureCompressor] Failed to read pixels from readable texture: {e.Message}"
                        );
                        result[texture] = new Color[0];
                    }
                }
                else
                {
                    nonReadable.Add(texture);
                }
            }

            // Second pass: batch all non-readable textures under a single lock
            if (nonReadable.Count > 0)
            {
                lock (RenderTextureLock)
                {
                    RenderTexture previous = RenderTexture.active;
                    try
                    {
                        foreach (var texture in nonReadable)
                        {
                            RenderTexture rt = null;
                            Texture2D readable = null;
                            try
                            {
                                rt = RenderTexture.GetTemporary(
                                    texture.width,
                                    texture.height,
                                    0,
                                    RenderTextureFormat.ARGB32
                                );

                                if (rt == null)
                                {
                                    Debug.LogWarning(
                                        "[TextureCompressor] Failed to create temporary RenderTexture"
                                    );
                                    result[texture] = new Color[0];
                                    continue;
                                }

                                Graphics.Blit(texture, rt);
                                RenderTexture.active = rt;

                                readable = new Texture2D(
                                    texture.width,
                                    texture.height,
                                    TextureFormat.RGBA32,
                                    texture.mipmapCount > 1
                                );
                                readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                                readable.Apply(texture.mipmapCount > 1);

                                result[texture] = readable.GetPixels();
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning(
                                    $"[TextureCompressor] Failed to read pixels from texture '{texture.name}': {e.Message}"
                                );
                                result[texture] = new Color[0];
                            }
                            finally
                            {
                                if (rt != null)
                                    RenderTexture.ReleaseTemporary(rt);
                                if (readable != null)
                                    Object.DestroyImmediate(readable);
                            }
                        }
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                    }
                }
            }

            return result;
        }

    }
}
