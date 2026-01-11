using System;
using dev.limitex.avatar.compressor.common;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Draws the frozen textures management section.
    /// </summary>
    public class FrozenTexturesDrawer
    {
        private bool _showFrozenSection = true;
        private Vector2 _frozenScrollPosition;

        public void Draw(TextureCompressor compressor, string searchText, Func<FrozenTextureSettings, bool> matchesSearch)
        {
            int frozenCount = compressor.FrozenTextures.Count;
            bool isSearching = !string.IsNullOrEmpty(searchText);
            int filteredCount = isSearching ? CountMatches(compressor, matchesSearch) : frozenCount;

            string headerText = isSearching && filteredCount != frozenCount
                ? $"Frozen Textures ({filteredCount}/{frozenCount})"
                : $"Frozen Textures ({frozenCount})";

            _showFrozenSection = EditorGUILayout.Foldout(_showFrozenSection, headerText, true);

            if (!_showFrozenSection) return;

            if (frozenCount == 0)
            {
                GUIDrawing.DrawHelpBox("No frozen textures. Click 'Freeze' on textures in Preview to add manual overrides.", MessageType.Info);
                return;
            }

            if (isSearching && filteredCount == 0)
            {
                GUIDrawing.DrawHelpBox("No frozen textures match the search.", MessageType.Info);
                return;
            }

            // TODO: Remove this call after users have migrated from TexturePath to TextureGuid 
            DrawLegacyPathMigrationUI(compressor);

            bool useScrollView = filteredCount >= 3;

            if (useScrollView)
            {
                _frozenScrollPosition = EditorGUILayout.BeginScrollView(_frozenScrollPosition, GUILayout.MaxHeight(250));
            }

            for (int i = compressor.FrozenTextures.Count - 1; i >= 0; i--)
            {
                var frozen = compressor.FrozenTextures[i];

                if (isSearching && !matchesSearch(frozen))
                    continue;

                DrawFrozenTextureEntry(compressor, frozen, i);
            }

            if (useScrollView)
            {
                EditorGUILayout.EndScrollView();
            }

            if (isSearching && filteredCount < frozenCount)
            {
                int hiddenCount = frozenCount - filteredCount;
                string hiddenText = hiddenCount == 1 ? "1 hidden" : $"{hiddenCount} hidden";
                EditorGUILayout.LabelField(hiddenText, GUIDrawing.HiddenCountStyle);
            }
        }

        private int CountMatches(TextureCompressor compressor, Func<FrozenTextureSettings, bool> matchesSearch)
        {
            int count = 0;
            foreach (var frozen in compressor.FrozenTextures)
            {
                if (matchesSearch(frozen))
                    count++;
            }
            return count;
        }

        private void DrawFrozenTextureEntry(TextureCompressor compressor, FrozenTextureSettings frozen, int index)
        {
            bool shouldRemove = false;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            string assetPath = AssetDatabase.GUIDToAssetPath(frozen.TextureGuid);
            var texture = !string.IsNullOrEmpty(assetPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) : null;
            GUIDrawing.DrawClickableThumbnail(texture);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            string textureName = !string.IsNullOrEmpty(assetPath) ? System.IO.Path.GetFileName(assetPath) : frozen.TextureGuid;
            EditorGUILayout.LabelField(textureName, EditorStyles.boldLabel);

            if (GUILayout.Button("Unfreeze", GUILayout.Width(70)))
            {
                shouldRemove = true;
            }

            EditorGUILayout.EndHorizontal();

            if (texture == null)
            {
                var savedColor = GUI.color;
                GUI.color = new Color(1f, 0.7f, 0.3f);
                EditorGUILayout.LabelField("Texture not found", EditorStyles.miniLabel);
                GUI.color = savedColor;
            }

            EditorGUI.BeginChangeCheck();
            bool skip = EditorGUILayout.Toggle("Skip compression", frozen.Skip);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(compressor, "Change Frozen Skip");
                frozen.Skip = skip;
                EditorUtility.SetDirty(compressor);
            }

            EditorGUI.BeginDisabledGroup(frozen.Skip);

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
                        Undo.RecordObject(compressor, "Change Frozen Divisor");
                        frozen.Divisor = div;
                        EditorUtility.SetDirty(compressor);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format:", GUILayout.Width(60));

            EditorGUI.BeginChangeCheck();
            var newFormat = (FrozenTextureFormat)EditorGUILayout.EnumPopup(frozen.Format);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(compressor, "Change Frozen Format");
                frozen.Format = newFormat;
                EditorUtility.SetDirty(compressor);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (shouldRemove)
            {
                Undo.RecordObject(compressor, "Unfreeze Texture");
                compressor.FrozenTextures.RemoveAt(index);
                EditorUtility.SetDirty(compressor);
            }
        }

        // TODO: Remove this method after users have migrated from TexturePath to TextureGuid
        private void DrawLegacyPathMigrationUI(TextureCompressor compressor)
        {
            bool hasLegacyPaths = false;
            foreach (var frozen in compressor.FrozenTextures)
            {
                if (!string.IsNullOrEmpty(frozen.TextureGuid) &&
                    (frozen.TextureGuid.StartsWith("Assets/") || frozen.TextureGuid.StartsWith("Packages/")))
                {
                    hasLegacyPaths = true;
                    break;
                }
            }

            if (!hasLegacyPaths) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Legacy path-based entries detected. Convert to GUID for better stability.", MessageType.Warning);
            if (GUILayout.Button("Convert to GUID", GUILayout.Width(120), GUILayout.Height(38)))
            {
                Undo.RecordObject(compressor, "Convert Frozen Textures to GUID");
                int converted = 0;
                foreach (var frozen in compressor.FrozenTextures)
                {
                    if (!string.IsNullOrEmpty(frozen.TextureGuid) &&
                        (frozen.TextureGuid.StartsWith("Assets/") || frozen.TextureGuid.StartsWith("Packages/")))
                    {
                        string guid = AssetDatabase.AssetPathToGUID(frozen.TextureGuid);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            frozen.TextureGuid = guid;
                            converted++;
                        }
                    }
                }
                EditorUtility.SetDirty(compressor);
                Debug.Log($"[TextureCompressor] Converted {converted} frozen texture entries from path to GUID.");
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
