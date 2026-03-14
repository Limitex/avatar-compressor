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
        /// Calculates the target resize dimensions for a texture based on its analysis result.
        /// When no downscaling is needed and the source fits within max resolution, dimensions are
        /// kept at the source size (rounded to multiples of 4). Otherwise, uses the recommended resolution.
        /// </summary>
        private (int Width, int Height) CalculateResizeDimensions(
            Texture2D source,
            TextureAnalysisResult analysis
        )
        {
            if (
                analysis.RecommendedDivisor <= 1
                && source.width <= _maxResolution
                && source.height <= _maxResolution
            )
            {
                return (EnsureMultipleOf4(source.width), EnsureMultipleOf4(source.height));
            }

            return (analysis.RecommendedResolution.x, analysis.RecommendedResolution.y);
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
        /// Resizes a single texture, acquiring and releasing the RenderTexture lock per call.
        /// Preferred over ResizeBatch when textures are processed one at a time to reduce peak memory.
        /// </summary>
        /// <returns>A new readable RGBA32 Texture2D, or null if resize failed.</returns>
        public Texture2D ResizeSingle(
            Texture2D source,
            TextureAnalysisResult analysis,
            bool isNormalMap
        )
        {
            lock (RenderTextureLock)
            {
                try
                {
                    var (newWidth, newHeight) = CalculateResizeDimensions(source, analysis);
                    return BlitResize(source, newWidth, newHeight, isNormalMap);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] Failed to resize texture '{source.name}': {e.Message}"
                    );
                    return null;
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
                foreach (var item in items)
                {
                    try
                    {
                        var (newWidth, newHeight) = CalculateResizeDimensions(
                            item.Source,
                            item.Analysis
                        );

                        result[item.Source] = BlitResize(
                            item.Source,
                            newWidth,
                            newHeight,
                            item.IsNormalMap
                        );
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning(
                            $"[TextureCompressor] Failed to resize texture '{item.Source.name}': {e.Message}"
                        );
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Core blit logic: creates a resized Texture2D from source using RenderTexture.
        /// Caller must hold RenderTextureLock. This method manages RenderTexture.active save/restore internally.
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

            var rt = new RenderTexture(newWidth, newHeight, 0, rtFormat, colorSpace);
            RenderTexture previous = RenderTexture.active;
            Texture2D resized = null;
            try
            {
                rt.Create();
                rt.filterMode = FilterMode.Bilinear;
                RenderTexture.active = rt;
                Graphics.Blit(source, rt);

                resized = new Texture2D(
                    newWidth,
                    newHeight,
                    TextureFormat.RGBA32,
                    source.mipmapCount > 1,
                    isNormalMap
                );
                resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                resized.Apply(source.mipmapCount > 1);

                CopyTextureSettings(source, resized);
                var result = resized;
                resized = null;
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                rt.Release();
                Object.DestroyImmediate(rt);
                if (resized != null)
                    Object.DestroyImmediate(resized);
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
        /// Gets readable pixels from a single texture.
        /// For non-readable or sRGB textures, performs GPU→CPU readback via RenderTexture.
        /// sRGB textures always go through the blit path with an explicit linear RT
        /// so that hardware sRGB-to-linear decode is applied, matching the GPU analysis
        /// backend which also blits sRGB textures to a linear RenderTexture.
        /// The temporary resources are released immediately, keeping peak memory low.
        /// </summary>
        public Color[] GetReadablePixelsSingle(Texture2D texture)
        {
            if (texture == null)
                return new Color[0];

            // sRGB textures must go through the blit path for consistent linear decode.
            if (texture.isReadable && !texture.isDataSRGB)
            {
                try
                {
                    return texture.GetPixels();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] Failed to read pixels from readable texture: {e.Message}"
                    );
                    return new Color[0];
                }
            }

            lock (RenderTextureLock)
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture rt = null;
                Texture2D readable = null;
                try
                {
                    // Use explicit RenderTexture lifecycle instead of GetTemporary/ReleaseTemporary
                    // so that native GPU memory is freed immediately by DestroyImmediate,
                    // rather than being held in Unity's RT pool across calls.
                    // Force Linear color space so that sRGB textures are decoded to linear
                    // by the hardware during blit, matching the GPU analysis backend.
                    rt = new RenderTexture(
                        texture.width,
                        texture.height,
                        0,
                        RenderTextureFormat.ARGB32,
                        RenderTextureReadWrite.Linear
                    );
                    rt.Create();

                    Graphics.Blit(texture, rt);
                    RenderTexture.active = rt;

                    readable = new Texture2D(
                        texture.width,
                        texture.height,
                        TextureFormat.RGBA32,
                        texture.mipmapCount > 1,
                        linear: true
                    );
                    readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readable.Apply(texture.mipmapCount > 1);

                    return readable.GetPixels();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] Failed to read pixels from texture '{texture.name}': {e.Message}"
                    );
                    return new Color[0];
                }
                finally
                {
                    RenderTexture.active = previous;
                    if (readable != null)
                        Object.DestroyImmediate(readable);
                    if (rt != null)
                    {
                        rt.Release();
                        Object.DestroyImmediate(rt);
                    }
                }
            }
        }
    }
}
