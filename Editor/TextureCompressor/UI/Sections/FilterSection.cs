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
    internal static class FilterSection
    {
        /// <summary>
        /// Draws the unified exclusions section containing both texture and path exclusions.
        /// </summary>
        public static void DrawExclusions(TextureCompressor config, ref bool showSection)
        {
            showSection = EditorGUILayout.Foldout(
                showSection,
                AvatarCompressorLocalization.Tr("TextureCompressor:label:exclusions"),
                true
            );
            if (!showSection)
                return;

            DrawExcludedTexturesContent(config);
            EditorGUILayout.Space(5);
            DrawExcludedPathsContent(config);
        }

        /// <summary>
        /// Draws the lilToon optimization section containing texture baking and unused-slot
        /// detection toggles under a shared foldout.
        /// </summary>
        public static void DrawLilToonOptimizations(TextureCompressor config, ref bool showSection)
        {
            showSection = EditorGUILayout.Foldout(
                showSection,
                AvatarCompressorLocalization.Tr("TextureCompressor:label:liltoonOptimizations"),
                true
            );
            if (!showSection)
                return;

            EditorGUI.BeginChangeCheck();

            bool bake = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    AvatarCompressorLocalization.Tr("TextureCompressor:prop:bakeLiltoon"),
                    AvatarCompressorLocalization.Tr("TextureCompressor:prop:bakeLiltoon:tooltip")
                ),
                config.BakeLilToonTextures
            );

            bool detect = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    AvatarCompressorLocalization.Tr("TextureCompressor:prop:removeUnused"),
                    AvatarCompressorLocalization.Tr("TextureCompressor:prop:removeUnused:tooltip")
                ),
                config.DetectUnusedTextures
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change lilToon Optimizations");
                config.BakeLilToonTextures = bake;
                config.DetectUnusedTextures = detect;
                EditorUtility.SetDirty(config);
            }
        }

        /// <summary>
        /// Draws texture type filters (Main, Normal, Emission, Other) with foldout
        /// and an inline sub-option for skipping uncompressed textures on unknown properties.
        /// </summary>
        public static void DrawTextureFilters(TextureCompressor config, ref bool showSection)
        {
            showSection = EditorGUILayout.Foldout(
                showSection,
                AvatarCompressorLocalization.Tr("TextureCompressor:label:textureFilters"),
                true
            );
            if (!showSection)
                return;

            EditorGUI.BeginChangeCheck();

            bool main = EditorGUILayout.ToggleLeft(
                AvatarCompressorLocalization.Content(
                    "TextureCompressor:prop:mainTextures",
                    "TextureCompressor:prop:mainTextures:tooltip"
                ),
                config.ProcessMainTextures
            );
            bool normal = EditorGUILayout.ToggleLeft(
                AvatarCompressorLocalization.Content(
                    "TextureCompressor:prop:normalMaps",
                    "TextureCompressor:prop:normalMaps:tooltip"
                ),
                config.ProcessNormalMaps
            );
            bool emission = EditorGUILayout.ToggleLeft(
                AvatarCompressorLocalization.Content(
                    "TextureCompressor:prop:emissionMaps",
                    "TextureCompressor:prop:emissionMaps:tooltip"
                ),
                config.ProcessEmissionMaps
            );
            bool other = EditorGUILayout.ToggleLeft(
                AvatarCompressorLocalization.Content(
                    "TextureCompressor:prop:otherTextures",
                    "TextureCompressor:prop:otherTextures:tooltip"
                ),
                config.ProcessOtherTextures
            );

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
                    AvatarCompressorLocalization.Content(
                        "TextureCompressor:prop:skipUnknownUncompressed",
                        "TextureCompressor:prop:skipUnknownUncompressed:tooltip"
                    ),
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
            string label =
                count > 0
                    ? AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:texturesCount",
                        count
                    )
                    : AvatarCompressorLocalization.Tr("TextureCompressor:label:excludedTextures");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            ExclusionListDrawer.DrawContent(
                config,
                config.ExcludedTextures,
                drawItemField: current =>
                    (Texture2D)EditorGUILayout.ObjectField(current, typeof(Texture2D), false),
                sectionLabel: "Excluded Textures",
                emptyHelpText: AvatarCompressorLocalization.Tr(
                    "TextureCompressor:message:excludedTexturesEmpty"
                ),
                addButtonLabel: AvatarCompressorLocalization.Tr(
                    "TextureCompressor:button:addTexture"
                ),
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
            string label =
                count > 0
                    ? AvatarCompressorLocalization.Tr("TextureCompressor:message:pathsCount", count)
                    : AvatarCompressorLocalization.Tr("TextureCompressor:label:excludedPaths");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            ExclusionListDrawer.DrawContent(
                config,
                config.ExcludedPaths,
                drawItemField: current => EditorGUILayout.TextField(current),
                sectionLabel: "Path Exclusions",
                emptyHelpText: AvatarCompressorLocalization.Tr(
                    "TextureCompressor:message:excludedPathsEmpty"
                ),
                addButtonLabel: AvatarCompressorLocalization.Tr("TextureCompressor:button:addPath"),
                onAdd: ShowAddPathMenu,
                drawItemExtra: (item, _) =>
                {
                    if (!string.IsNullOrWhiteSpace(item) && !IsValidAssetPath(item))
                    {
                        var savedColor = GUI.color;
                        GUI.color = new Color(1f, 0.7f, 0.3f);
                        EditorGUILayout.LabelField(
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:message:pathNotFound"
                            ),
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
                new GUIContent(AvatarCompressorLocalization.Tr("TextureCompressor:menu:emptyPath")),
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
                        menu.AddDisabledItem(
                            new GUIContent(
                                AvatarCompressorLocalization.Tr(
                                    "TextureCompressor:menu:pathPresetAdded",
                                    preset.Label
                                )
                            )
                        );
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
