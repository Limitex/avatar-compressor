using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Preprocessor for normal map compression.
    /// Handles channel layout differences between BC5 and DXTnm formats.
    ///
    /// Unity Normal Map Channel Layouts (as observed via GetPixels):
    /// - BC5: XY stored in RG channels
    /// - DXT5/BC7 (DXTnm): XY stored in AG channels (sampled as wy in shaders)
    ///   (BC7 can optionally use RGB layout when preserving source alpha)
    /// - RGBA/RGB: XYZ stored in RGB channels
    ///
    /// Reference: UnityCG.cginc UnpackNormalDXT5nm uses packednormal.wy (AG)
    ///
    /// IMPORTANT: EditorUtility.CompressTexture does NOT perform DXTnm conversion.
    /// It compresses the texture as-is, preserving channel layout.
    /// Therefore, we must manually pack data into the correct channels before compression:
    /// - For BC5 target: Write XY to RG channels
    /// - For DXT5 target: Write XY to AG channels (DXTnm format)
    /// - For BC7 target:
    ///   - Default: Write XY to AG channels (DXTnm format)
    ///   - When preserving alpha: Write XYZ to RGB and keep original A channel
    /// </summary>
    public class NormalMapPreprocessor
    {
        /// <summary>
        /// Optional source layout override for cases where format alone is ambiguous (e.g. BC7).
        /// </summary>
        public enum SourceLayout
        {
            Auto,
            RG,
            AG,
            RGB,
        }

        /// <summary>
        /// Minimum vector length threshold for normalization.
        /// Vectors with length below this are considered degenerate (e.g., corrupted data,
        /// interpolation artifacts) and will be reset to the default flat normal (0, 0, 1).
        /// </summary>
        private const float MinVectorLength = 0.0001f;

        /// <summary>
        /// Prepares a normal map texture for compression.
        /// Reads XY from appropriate channels based on source format,
        /// normalizes vectors, and writes to the appropriate channels based on target format.
        /// </summary>
        /// <param name="texture">The texture to preprocess (must be readable)</param>
        /// <param name="sourceFormat">The original texture format (determines input channel layout)</param>
        /// <param name="targetFormat">The target compression format (determines output channel layout)</param>
        /// <param name="preserveAlpha">
        /// When true and target is BC7, preserves source alpha by writing normals to RGB instead of AG.
        /// DXT5/DXT5Crunched targets cannot preserve alpha because A is required for X in DXTnm layout.
        /// </param>
        /// <param name="sourceLayout">
        /// Optional source channel layout override. Use Auto for format-based detection.
        /// </param>
        public void PrepareForCompression(
            Texture2D texture,
            TextureFormat sourceFormat,
            TextureFormat targetFormat,
            bool preserveAlpha = false,
            SourceLayout sourceLayout = SourceLayout.Auto
        )
        {
            if (texture == null || !texture.isReadable)
            {
                return;
            }

            var sourceChannelLayout =
                sourceLayout == SourceLayout.Auto
                    ? GetChannelLayout(sourceFormat)
                    : ToChannelLayout(sourceLayout);
            var targetLayout = GetChannelLayout(targetFormat, preserveAlpha);
            var pixels = texture.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
            {
                byte sourceAlpha = pixels[i].a;

                // Read XY from appropriate channels based on source format
                float x,
                    y,
                    originalZ;
                ReadNormalChannels(pixels[i], sourceChannelLayout, out x, out y, out originalZ);

                // Recalculate Z magnitude from unit sphere constraint
                float zSquared = 1f - x * x - y * y;
                float zMagnitude = zSquared > 0f ? Mathf.Sqrt(zSquared) : 0f;

                // Determine Z sign based on source format
                // 2-channel formats (BC5, DXTnm) don't store Z, assume positive (Tangent Space)
                // 3-channel formats preserve original Z sign (Object Space support)
                float z =
                    sourceChannelLayout == NormalChannelLayout.RGB && originalZ < 0f
                        ? -zMagnitude
                        : zMagnitude;

                // Normalize the vector
                float length = Mathf.Sqrt(x * x + y * y + z * z);
                if (length > MinVectorLength)
                {
                    x /= length;
                    y /= length;
                    z /= length;
                }
                else
                {
                    // Degenerate vector, reset to flat normal
                    x = 0f;
                    y = 0f;
                    z = 1f;
                }

                // Write to appropriate channels based on target format
                WriteNormalChannels(ref pixels[i], targetLayout, x, y, z, sourceAlpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        /// <summary>
        /// Channel layout for normal map data.
        /// </summary>
        private enum NormalChannelLayout
        {
            /// <summary>XY in RG channels (BC5)</summary>
            RG,

            /// <summary>XY in AG channels (DXT5/BC7 DXTnm format)</summary>
            AG,

            /// <summary>XYZ in RGB channels (uncompressed)</summary>
            RGB,
        }

        /// <summary>
        /// Determines the channel layout based on texture format.
        /// </summary>
        private static NormalChannelLayout GetChannelLayout(
            TextureFormat format,
            bool preserveAlpha = false
        )
        {
            switch (format)
            {
                // BC5: 2-channel format, XY stored in RG
                case TextureFormat.BC5:
                    return NormalChannelLayout.RG;

                // DXTnm formats: XY stored in AG (shader reads as .wy)
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                    return NormalChannelLayout.AG;

                case TextureFormat.BC7:
                    return preserveAlpha ? NormalChannelLayout.RGB : NormalChannelLayout.AG;

                // All other formats: assume standard RGB layout
                default:
                    return NormalChannelLayout.RGB;
            }
        }

        private static NormalChannelLayout ToChannelLayout(SourceLayout sourceLayout)
        {
            switch (sourceLayout)
            {
                case SourceLayout.RG:
                    return NormalChannelLayout.RG;
                case SourceLayout.AG:
                    return NormalChannelLayout.AG;
                case SourceLayout.RGB:
                default:
                    return NormalChannelLayout.RGB;
            }
        }

        /// <summary>
        /// Reads normal XY(Z) from appropriate channels based on layout.
        /// </summary>
        private static void ReadNormalChannels(
            Color32 pixel,
            NormalChannelLayout layout,
            out float x,
            out float y,
            out float originalZ
        )
        {
            switch (layout)
            {
                case NormalChannelLayout.RG:
                    x = (pixel.r / 255f) * 2f - 1f;
                    y = (pixel.g / 255f) * 2f - 1f;
                    originalZ = 0f;
                    break;

                case NormalChannelLayout.AG:
                    x = (pixel.a / 255f) * 2f - 1f;
                    y = (pixel.g / 255f) * 2f - 1f;
                    originalZ = 0f;
                    break;

                case NormalChannelLayout.RGB:
                default:
                    // Standard RGB: XYZ in RGB
                    x = (pixel.r / 255f) * 2f - 1f;
                    y = (pixel.g / 255f) * 2f - 1f;
                    originalZ = (pixel.b / 255f) * 2f - 1f;
                    break;
            }
        }

        /// <summary>
        /// Writes normal XYZ to appropriate channels based on target layout.
        /// </summary>
        /// <remarks>
        /// Channel packing for each format:
        /// - RG (BC5): X in R, Y in G, Z in B (B is ignored during BC5 compression but useful for testing)
        /// - AG (DXTnm for DXT5/BC7): X in A, Y in G, R/B are constants for better DXT1 block precision
        /// - RGB: X in R, Y in G, Z in B, A copied from source
        ///
        /// Note: Z is always written to B channel for consistency, even though BC5 and DXTnm
        /// formats only use 2 channels. This allows pre-compression validation and debugging.
        /// The compression step will ignore unused channels appropriately.
        /// </remarks>
        private static void WriteNormalChannels(
            ref Color32 pixel,
            NormalChannelLayout layout,
            float x,
            float y,
            float z,
            byte sourceAlpha
        )
        {
            byte encodedX = (byte)Mathf.Clamp((x * 0.5f + 0.5f) * 255f, 0f, 255f);
            byte encodedY = (byte)Mathf.Clamp((y * 0.5f + 0.5f) * 255f, 0f, 255f);
            byte encodedZ = (byte)Mathf.Clamp((z * 0.5f + 0.5f) * 255f, 0f, 255f);

            switch (layout)
            {
                case NormalChannelLayout.RG:
                    // BC5: XY in RG, Z in B (B ignored during compression)
                    pixel.r = encodedX;
                    pixel.g = encodedY;
                    pixel.b = encodedZ;
                    pixel.a = 255;
                    break;

                case NormalChannelLayout.AG:
                    // DXTnm (DXT5/BC7): X in A, Y in G.
                    // Keep R/B at constants so DXT1 RGB block precision focuses on G.
                    pixel.r = 255;
                    pixel.g = encodedY;
                    pixel.b = 255;
                    pixel.a = encodedX;
                    break;

                case NormalChannelLayout.RGB:
                default:
                    // Standard RGB: XYZ in RGB
                    pixel.r = encodedX;
                    pixel.g = encodedY;
                    pixel.b = encodedZ;
                    pixel.a = sourceAlpha;
                    break;
            }
        }
    }
}
