using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the frozen textures section for manual texture overrides.
    /// </summary>
    public static class FrozenTexturesSection
    {
        private static readonly System.Collections.Generic.Dictionary<
            string,
            string
        > _guidPathCache = new();

        /// <summary>
        /// Gets the asset path for a GUID, using cache to avoid repeated lookups.
        /// </summary>
        private static string GetAssetPathCached(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return "";

            if (!_guidPathCache.TryGetValue(guid, out var path))
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                _guidPathCache[guid] = path;
            }
            return path;
        }

        /// <summary>
        /// Clears the GUID-to-path cache. Call when assets may have changed.
        /// </summary>
        public static void InvalidateCache()
        {
            _guidPathCache.Clear();
        }

        /// <summary>
        /// Draws the frozen textures section with search filtering.
        /// </summary>
        public static void Draw(
            TextureCompressor config,
            SearchBoxControl search,
            ref bool showSection,
            ref Vector2 scrollPos
        )
        {
            int frozenCount = config.FrozenTextures.Count;
            bool isSearching = search.IsSearching;
            int filteredCount = isSearching ? CountMatches(config, search) : frozenCount;

            // Show filtered count in header when searching
            string headerText =
                isSearching && filteredCount != frozenCount
                    ? $"Frozen Textures ({filteredCount}/{frozenCount})"
                    : $"Frozen Textures ({frozenCount})";

            showSection = EditorGUILayout.Foldout(showSection, headerText, true);

            if (!showSection)
                return;

            if (frozenCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "No frozen textures. Click 'Freeze' on textures in Preview to add manual overrides.",
                    MessageType.Info
                );
                return;
            }

            // Show "no results" message when searching with no matches
            if (isSearching && filteredCount == 0)
            {
                EditorGUILayout.HelpBox("No frozen textures match the search.", MessageType.Info);
                return;
            }

            // TODO: Remove this call after users have migrated from TexturePath to TextureGuid
            DrawLegacyPathMigrationUI(config);

            // Only use ScrollView when there are many visible items (3+)
            bool useScrollView = filteredCount >= 3;

            if (useScrollView)
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(250));
            }

            for (int i = config.FrozenTextures.Count - 1; i >= 0; i--)
            {
                var frozen = config.FrozenTextures[i];

                // Skip items that don't match search
                if (isSearching && !MatchesFrozenSearch(frozen, search))
                    continue;

                DrawFrozenTextureEntry(config, frozen, i);
            }

            if (useScrollView)
            {
                EditorGUILayout.EndScrollView();
            }

            // Show hidden count when searching
            if (isSearching && filteredCount < frozenCount)
            {
                SearchBoxControl.DrawHiddenCount(frozenCount - filteredCount);
            }
        }

        private static void DrawFrozenTextureEntry(
            TextureCompressor config,
            FrozenTextureSettings frozen,
            int index
        )
        {
            bool shouldRemove = false;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Resolve path from GUID for display and loading (cached)
            string assetPath = GetAssetPathCached(frozen.TextureGuid);
            var texture = !string.IsNullOrEmpty(assetPath)
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)
                : null;

            // Thumbnail
            ThumbnailControl.DrawClickable(texture);

            EditorGUILayout.BeginVertical();

            // Header row with texture name and unfreeze button
            EditorGUILayout.BeginHorizontal();

            string textureName = !string.IsNullOrEmpty(assetPath)
                ? System.IO.Path.GetFileName(assetPath)
                : frozen.TextureGuid;
            EditorGUILayout.LabelField(textureName, EditorStyles.boldLabel);

            if (GUILayout.Button("Unfreeze", GUILayout.Width(70)))
            {
                shouldRemove = true;
            }

            EditorGUILayout.EndHorizontal();

            // Warning if texture asset is missing
            if (texture == null)
            {
                var savedColor = GUI.color;
                GUI.color = new Color(1f, 0.7f, 0.3f);
                EditorGUILayout.LabelField("Texture not found", EditorStyles.miniLabel);
                GUI.color = savedColor;
            }

            // Skip checkbox
            EditorGUI.BeginChangeCheck();
            bool skip = EditorGUILayout.Toggle("Skip compression", frozen.Skip);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change Frozen Skip");
                frozen.Skip = skip;
                EditorUtility.SetDirty(config);
            }

            // Disable divisor and format controls when skip is enabled
            EditorGUI.BeginDisabledGroup(frozen.Skip);

            // Divisor selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Divisor:", GUILayout.Width(60));

            int[] divisors = { 1, 2, 4, 8, 16 };
            foreach (int div in divisors)
            {
                bool isSelected = frozen.Divisor == div;
                var style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;

                EditorGUI.BeginChangeCheck();
                if (GUILayout.Toggle(isSelected, div.ToString(), style, GUILayout.Width(35)))
                {
                    if (!isSelected)
                    {
                        Undo.RecordObject(config, "Change Frozen Divisor");
                        frozen.Divisor = div;
                        EditorUtility.SetDirty(config);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // Format selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format:", GUILayout.Width(60));

            EditorGUI.BeginChangeCheck();
            var newFormat = (FrozenTextureFormat)EditorGUILayout.EnumPopup(frozen.Format);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change Frozen Format");
                frozen.Format = newFormat;
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // Handle removal after layout is complete
            if (shouldRemove)
            {
                Undo.RecordObject(config, "Unfreeze Texture");
                config.FrozenTextures.RemoveAt(index);
                EditorUtility.SetDirty(config);
            }
        }

        // TODO: Remove this method after users have migrated from TexturePath to TextureGuid
        private static void DrawLegacyPathMigrationUI(TextureCompressor config)
        {
            bool hasLegacyPaths = false;
            foreach (var frozen in config.FrozenTextures)
            {
                if (
                    !string.IsNullOrEmpty(frozen.TextureGuid)
                    && (
                        frozen.TextureGuid.StartsWith("Assets/")
                        || frozen.TextureGuid.StartsWith("Packages/")
                    )
                )
                {
                    hasLegacyPaths = true;
                    break;
                }
            }

            if (!hasLegacyPaths)
                return;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "Legacy path-based entries detected. Convert to GUID for better stability.",
                MessageType.Warning
            );
            if (GUILayout.Button("Convert to GUID", GUILayout.Width(120), GUILayout.Height(38)))
            {
                Undo.RecordObject(config, "Convert Frozen Textures to GUID");
                int converted = 0;
                foreach (var frozen in config.FrozenTextures)
                {
                    if (
                        !string.IsNullOrEmpty(frozen.TextureGuid)
                        && (
                            frozen.TextureGuid.StartsWith("Assets/")
                            || frozen.TextureGuid.StartsWith("Packages/")
                        )
                    )
                    {
                        string guid = AssetDatabase.AssetPathToGUID(frozen.TextureGuid);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            frozen.TextureGuid = guid;
                            converted++;
                        }
                    }
                }
                EditorUtility.SetDirty(config);
                Debug.Log(
                    $"[TextureCompressor] Converted {converted} frozen texture entries from path to GUID."
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        private static int CountMatches(TextureCompressor config, SearchBoxControl search)
        {
            int count = 0;
            foreach (var frozen in config.FrozenTextures)
            {
                if (MatchesFrozenSearch(frozen, search))
                    count++;
            }
            return count;
        }

        private static bool MatchesFrozenSearch(
            FrozenTextureSettings frozen,
            SearchBoxControl search
        )
        {
            string assetPath = GetAssetPathCached(frozen.TextureGuid);
            string textureName = !string.IsNullOrEmpty(assetPath)
                ? assetPath.Substring(assetPath.LastIndexOf('/') + 1)
                : "";

            return search.MatchesSearchAny(textureName, assetPath);
        }
    }
}
