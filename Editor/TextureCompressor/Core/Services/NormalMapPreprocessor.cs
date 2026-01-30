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
    /// - RGBA/RGB: XYZ stored in RGB channels
    ///
    /// Reference: UnityCG.cginc UnpackNormalDXT5nm uses packednormal.wy (AG)
    /// </summary>
    public class NormalMapPreprocessor
    {
        /// <summary>
        /// Minimum vector length threshold for normalization.
        /// Vectors with length below this are considered degenerate (e.g., corrupted data,
        /// interpolation artifacts) and will be reset to the default flat normal (0, 0, 1).
        /// </summary>
        private const float MinVectorLength = 0.0001f;

        /// <summary>
        /// Prepares a normal map texture for compression.
        /// Reads XY from appropriate channels based on source format,
        /// normalizes vectors, and writes to RGB for subsequent compression.
        /// </summary>
        public void PrepareForCompression(Texture2D texture, TextureFormat sourceFormat)
        {
            if (texture == null || !texture.isReadable)
            {
                return;
            }

            var channelLayout = GetChannelLayout(sourceFormat);
            var pixels = texture.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
            {
                // Read XY from appropriate channels based on source format
                float x,
                    y,
                    originalZ;
                ReadNormalChannels(pixels[i], channelLayout, out x, out y, out originalZ);

                // Recalculate Z magnitude from unit sphere constraint
                float zSquared = 1f - x * x - y * y;
                float zMagnitude = zSquared > 0f ? Mathf.Sqrt(zSquared) : 0f;

                // Determine Z sign based on source format
                // 2-channel formats (BC5, DXTnm) don't store Z, assume positive (Tangent Space)
                // 3-channel formats preserve original Z sign (Object Space support)
                float z =
                    channelLayout == NormalChannelLayout.RGB && originalZ < 0f
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

                // Always write to RGB for subsequent compression
                // EditorUtility.CompressTexture will handle channel packing for target format
                pixels[i].r = (byte)Mathf.Clamp((x * 0.5f + 0.5f) * 255f, 0f, 255f);
                pixels[i].g = (byte)Mathf.Clamp((y * 0.5f + 0.5f) * 255f, 0f, 255f);
                pixels[i].b = (byte)Mathf.Clamp((z * 0.5f + 0.5f) * 255f, 0f, 255f);
                pixels[i].a = 255;
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
        private static NormalChannelLayout GetChannelLayout(TextureFormat format)
        {
            switch (format)
            {
                // BC5: 2-channel format, XY stored in RG
                case TextureFormat.BC5:
                    return NormalChannelLayout.RG;

                // DXTnm formats: XY stored in AG (shader reads as .wy)
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC7:
                    return NormalChannelLayout.AG;

                // All other formats: assume standard RGB layout
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
    }
}
