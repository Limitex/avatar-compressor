using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Categories for texture properties, used to control per-type processing toggles.
    /// </summary>
    internal enum TexturePropertyCategory
    {
        Main,
        Normal,
        Emission,
        Other,
    }

    /// <summary>
    /// Reason why a texture is skipped from compression.
    /// </summary>
    internal enum SkipReason
    {
        None,
        TooSmall,
        FilteredByType,
        FrozenSkip,
        RuntimeGenerated,
        ExcludedPath,
        ExcludedTexture,
        UnknownUncompressedProperty,
    }

    /// <summary>
    /// Information about a texture and its usage context.
    /// </summary>
    internal sealed class TextureInfo
    {
        public TexturePropertyCategory TextureType { get; set; }
        public bool IsNormalMap { get; set; }
        public bool IsEmission { get; set; }
        public bool IsProcessed { get; set; } = true;
        public SkipReason SkipReason { get; set; } = SkipReason.None;
        public string AssetGuid { get; set; } = string.Empty;
        public List<MaterialTextureReference> References { get; } =
            new List<MaterialTextureReference>();
    }

    /// <summary>
    /// Reference to a texture in a material.
    /// </summary>
    internal sealed class MaterialTextureReference
    {
        public Material Material { get; set; }
        public string PropertyName { get; set; }
        public Renderer Renderer { get; set; }
    }
}
