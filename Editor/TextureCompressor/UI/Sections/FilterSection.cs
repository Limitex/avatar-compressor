using dev.limitex.avatar.compressor.texture;
using UnityEngine;
using UnityEditor;

namespace dev.limitex.avatar.compressor.texture.ui
{
    /// <summary>
    /// Draws the texture filter and excluded paths sections.
    /// </summary>
    public static class FilterSection
    {
        /// <summary>
        /// Draws texture type filters (Main, Normal, Emission, Other).
        /// </summary>
        public static void DrawTextureFilters(TextureCompressor config)
        {
            EditorGUILayout.LabelField("Texture Filters", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            bool main = GUILayout.Toggle(config.ProcessMainTextures, "Main", GUILayout.Width(70));
            bool normal = GUILayout.Toggle(config.ProcessNormalMaps, "Normal", GUILayout.Width(70));
            bool emission = GUILayout.Toggle(config.ProcessEmissionMaps, "Emission", GUILayout.Width(80));
            bool other = GUILayout.Toggle(config.ProcessOtherTextures, "Other", GUILayout.Width(70));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change Texture Filters");
                config.ProcessMainTextures = main;
                config.ProcessNormalMaps = normal;
                config.ProcessEmissionMaps = emission;
                config.ProcessOtherTextures = other;
                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the excluded paths section with foldout.
        /// </summary>
        public static void DrawExcludedPaths(TextureCompressor config, ref bool showSection)
        {
            int excludedCount = config.ExcludedPaths.Count;
            string excludedLabel = excludedCount > 0
                ? $"Path Exclusions ({excludedCount})"
                : "Path Exclusions";

            showSection = EditorGUILayout.Foldout(showSection, excludedLabel, true);
            if (!showSection) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Display existing paths
            for (int i = config.ExcludedPaths.Count - 1; i >= 0; i--)
            {
                string currentPath = config.ExcludedPaths[i];

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                string newPath = EditorGUILayout.TextField(currentPath);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Edit Excluded Path");
                    config.ExcludedPaths[i] = newPath;
                    EditorUtility.SetDirty(config);
                    currentPath = newPath;
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(config, "Remove Excluded Path");
                    config.ExcludedPaths.RemoveAt(i);
                    EditorUtility.SetDirty(config);
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                // Show warning if path doesn't exist
                if (!string.IsNullOrWhiteSpace(currentPath) && !IsValidAssetPath(currentPath))
                {
                    var savedColor = GUI.color;
                    GUI.color = new Color(1f, 0.7f, 0.3f);
                    EditorGUILayout.LabelField("  \u26a0 Path not found", EditorStyles.miniLabel);
                    GUI.color = savedColor;
                }
            }

            // Add new path button with dropdown for presets
            if (GUILayout.Button("+ Add Path..."))
            {
                ShowAddPathMenu(config);
            }

            if (config.ExcludedPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("Textures with paths starting with listed prefixes will be skipped.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private static void ShowAddPathMenu(TextureCompressor config)
        {
            var menu = new GenericMenu();

            // Empty path option
            menu.AddItem(new GUIContent("Empty"), false, () =>
            {
                Undo.RecordObject(config, "Add Excluded Path");
                config.ExcludedPaths.Add("");
                EditorUtility.SetDirty(config);
            });

            // Separator and presets
            if (ExcludedPathPresets.Presets.Length > 0)
            {
                menu.AddSeparator("");

                foreach (var preset in ExcludedPathPresets.Presets)
                {
                    bool alreadyAdded = config.ExcludedPaths.Contains(preset.Path);
                    if (alreadyAdded)
                    {
                        menu.AddDisabledItem(new GUIContent($"{preset.Label} (added)"));
                    }
                    else
                    {
                        var presetCopy = preset;
                        menu.AddItem(new GUIContent(preset.Label), false, () =>
                        {
                            Undo.RecordObject(config, $"Add {presetCopy.Label} Excluded Path");
                            config.ExcludedPaths.Add(presetCopy.Path);
                            EditorUtility.SetDirty(config);
                        });
                    }
                }
            }

            menu.ShowAsContext();
        }

        private static bool IsValidAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Remove trailing slash for folder check
            string folderPath = path.TrimEnd('/', '\\');

            // Check if it's a valid folder
            if (AssetDatabase.IsValidFolder(folderPath))
                return true;

            // Check if parent folder exists (for partial paths)
            int lastSlash = folderPath.LastIndexOfAny(new[] { '/', '\\' });
            if (lastSlash > 0)
            {
                string parentPath = folderPath.Substring(0, lastSlash);
                if (AssetDatabase.IsValidFolder(parentPath))
                    return true;
            }

            // Check Packages folder specially
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
    }
}
