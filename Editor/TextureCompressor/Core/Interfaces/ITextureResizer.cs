using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    public interface ITextureResizer
    {
        Texture2D Resize(Texture2D source, int targetWidth, int targetHeight);
    }
}
