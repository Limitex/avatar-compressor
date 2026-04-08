using System.Collections.Generic;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the texture filter and exclusion list sections.
    /// </summary>
    public static class FilterSection
    {
        /// <summary>
        /// Draws the unified exclusions section containing both texture and path exclusions.
        /// </summary>
        public static void DrawExclusions(TextureCompressor config, ref bool showSection)
        {
            showSection = EditorGUILayout.Foldout(showSection, "Exclusions", true);
            if (!showSection)
                return;

            DrawExcludedTexturesContent(config);
            EditorGUILayout.Space(5);
            DrawExcludedPathsContent(config);
        }

        /// <summary>
        /// Draws texture type filters (Main, Normal, Emission, Other) with foldout
        /// and an inline sub-option for skipping uncompressed textures on unknown properties.
        /// </summary>
        public static void DrawTextureFilters(TextureCompressor config, ref bool showSection)
        {
            showSection = EditorGUILayout.Foldout(showSection, "Texture Filters", true);
            if (!showSection)
                return;

            EditorGUI.BeginChangeCheck();

            bool main = EditorGUILayout.ToggleLeft("Main Textures", config.ProcessMainTextures);
            bool normal = EditorGUILayout.ToggleLeft("Normal Maps", config.ProcessNormalMaps);
            bool emission = EditorGUILayout.ToggleLeft("Emission Maps", config.ProcessEmissionMaps);
            bool other = EditorGUILayout.ToggleLeft("Other Textures", config.ProcessOtherTextures);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change Texture Filters");
                config.ProcessMainTextures = main;
                config.ProcessNormalMaps = normal;
                config.ProcessEmissionMaps = emission;
                config.ProcessOtherTextures = other;
                EditorUtility.SetDirty(config);
            }

            // Sub-option for Other: skip uncompressed textures on unknown properties.
            // Only meaningful when Other is enabled; disabled state shows the toggle grayed out.
            using (new EditorGUI.DisabledScope(!config.ProcessOtherTextures))
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                bool skipUnknownUncompressed = EditorGUILayout.ToggleLeft(
                    "Skip uncompressed textures on unknown properties",
                    config.SkipUnknownUncompressedTextures
                );
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Change Skip Unknown Uncompressed Textures");
                    config.SkipUnknownUncompressedTextures = skipUnknownUncompressed;
                    EditorUtility.SetDirty(config);
                }

                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// Draws the excluded textures content (label + list, no foldout).
        /// </summary>
        private static void DrawExcludedTexturesContent(TextureCompressor config)
        {
            int count = config.ExcludedTextures.Count;
            string label = count > 0 ? $"Textures ({count})" : "Textures";
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            ExclusionListDrawer.DrawContent(
                config,
                config.ExcludedTextures,
                drawItemField: current =>
                    (Texture2D)EditorGUILayout.ObjectField(current, typeof(Texture2D), false),
                sectionLabel: "Excluded Textures",
                emptyHelpText: "Textures added here will be excluded from compression.",
                addButtonLabel: "+ Add Texture",
                validateChange: (newValue, index, list) =>
                {
                    if (
                        newValue != null
                        && list.IndexOf(newValue) is int existing
                        && existing >= 0
                        && existing != index
                    )
                    {
                        Debug.LogWarning(
                            $"[LAC] Texture '{newValue.name}' is already in the excluded list."
                        );
                        return false;
                    }
                    return true;
                }
            );
        }

        /// <summary>
        /// Draws the excluded paths content (label + list, no foldout).
        /// </summary>
        private static void DrawExcludedPathsContent(TextureCompressor config)
        {
            int count = config.ExcludedPaths.Count;
            string label = count > 0 ? $"Paths ({count})" : "Paths";
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            ExclusionListDrawer.DrawContent(
                config,
                config.ExcludedPaths,
                drawItemField: current => EditorGUILayout.TextField(current),
                sectionLabel: "Path Exclusions",
                emptyHelpText: "Textures with paths starting with listed prefixes will be skipped.",
                addButtonLabel: "+ Add Path...",
                onAdd: ShowAddPathMenu,
                drawItemExtra: (item, _) =>
                {
                    if (!string.IsNullOrWhiteSpace(item) && !IsValidAssetPath(item))
                    {
                        var savedColor = GUI.color;
                        GUI.color = new Color(1f, 0.7f, 0.3f);
                        EditorGUILayout.LabelField(
                            "  \u26a0 Path not found",
                            EditorStyles.miniLabel
                        );
                        GUI.color = savedColor;
                    }
                }
            );
        }

        private static void ShowAddPathMenu(UnityEngine.Object undoTarget, List<string> list)
        {
            var menu = new GenericMenu();

            menu.AddItem(
                new GUIContent("Empty"),
                false,
                () =>
                {
                    Undo.RecordObject(undoTarget, "Add Excluded Path");
                    list.Add("");
                    EditorUtility.SetDirty(undoTarget);
                }
            );

            if (ExcludedPathPresets.Presets.Length > 0)
            {
                menu.AddSeparator("");

                foreach (var preset in ExcludedPathPresets.Presets)
                {
                    bool alreadyAdded = list.Contains(preset.Path);
                    if (alreadyAdded)
                    {
                        menu.AddDisabledItem(new GUIContent($"{preset.Label} (added)"));
                    }
                    else
                    {
                        var presetCopy = preset;
                        menu.AddItem(
                            new GUIContent(preset.Label),
                            false,
                            () =>
                            {
                                Undo.RecordObject(
                                    undoTarget,
                                    $"Add {presetCopy.Label} Excluded Path"
                                );
                                list.Add(presetCopy.Path);
                                EditorUtility.SetDirty(undoTarget);
                            }
                        );
                    }
                }
            }

            menu.ShowAsContext();
        }

        private static bool IsValidAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                string folderPath = path.TrimEnd('/', '\\');

                if (AssetDatabase.IsValidFolder(folderPath))
                    return true;

                int lastSlash = folderPath.LastIndexOfAny(new[] { '/', '\\' });
                if (lastSlash > 0)
                {
                    string parentPath = folderPath.Substring(0, lastSlash);
                    if (AssetDatabase.IsValidFolder(parentPath))
                        return true;
                }

                if (path.StartsWith("Packages/"))
                {
                    string[] parts = path.Split('/');
                    if (parts.Length >= 2)
                    {
                        string packagePath = $"Packages/{parts[1]}";
                        string packageJsonPath = $"{packagePath}/package.json";
                        return System.IO.File.Exists(packageJsonPath);
                    }
                }

                return false;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
