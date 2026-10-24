using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor.ui;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    internal sealed class PreviewSearch
    {
        private readonly SearchBoxControl _searchBox;

        internal PreviewSearch(SearchBoxControl searchBox)
        {
            _searchBox = searchBox;
        }

        internal int CountMatches(IReadOnlyCollection<TexturePreviewData> items)
        {
            return _searchBox.CountMatches(
                items,
                Matches,
                (
                    AvatarCompressorLocalization.CurrentLanguage,
                    AvatarCompressorLocalization.CatalogRevision
                )
            );
        }

        internal bool Matches(TexturePreviewData data)
        {
            return _searchBox.MatchesSearchAny(
                data.Texture != null ? data.Texture.name : "",
                GuidPathCache.GetPath(data.Guid),
                data.TextureType.ToString(),
                TextureCompressorEnumLabels.Text(data.TextureType)
            );
        }
    }
}
