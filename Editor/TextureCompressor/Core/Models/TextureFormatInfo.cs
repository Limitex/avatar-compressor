using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Utility for querying texture format properties.
    /// </summary>
    public static class TextureFormatInfo
    {
        /// <summary>
        /// Returns true if the given texture format is a block-compressed format
        /// (DXT, BC, ASTC, ETC, EAC, PVRTC).
        /// </summary>
        public static bool IsCompressed(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC4:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_12x12:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC_RGB4Crunched:
                case TextureFormat.ETC2_RGB:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.EAC_R:
                case TextureFormat.EAC_RG:
                case TextureFormat.EAC_R_SIGNED:
                case TextureFormat.EAC_RG_SIGNED:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.PVRTC_RGBA4:
                    return true;
                default:
                    return false;
            }
        }
    }
}
