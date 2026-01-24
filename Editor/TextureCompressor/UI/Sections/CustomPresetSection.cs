using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the custom preset management section.
    /// Provides UI for loading, saving, and creating custom presets.
    /// </summary>
    public static class CustomPresetSection
    {
        private const string DefaultPresetFolder = "Assets";
        private const string PresetFileExtension = "asset";

        private enum PendingAction
        {
            None,
            Save,
            Discard,
            CreateNew,
            Unlink,
            ChangePreset,
        }

        /// <summary>
        /// Draws the custom preset section when Custom preset is selected.
        /// </summary>
        public static void Draw(TextureCompressor config)
        {
            if (config.Preset != CompressorPreset.Custom)
                return;

            // Use-only mode: preset is assigned and not in edit mode
            if (CustomPresetEditorState.IsInUseOnlyMode(config))
            {
                DrawUseOnlyMode(config);
            }
            else
            {
                DrawEditMode(config);
            }
        }

        /// <summary>
        /// Draws the use-only mode UI (read-only display of selected preset).
        /// </summary>
        private static void DrawUseOnlyMode(TextureCompressor config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorDrawUtils.DrawSectionHeader("Custom Preset (Use Only)");
            EditorGUILayout.Space(4);

            var restriction = CustomPresetEditorState.GetEditRestriction(config);

            EditorGUILayout.BeginHorizontal();
            if (restriction.IsBuiltIn)
            {
                EditorGUILayout.LabelField(
                    $"{config.CustomPresetAsset.name} (Built-in)",
                    EditorStyles.boldLabel
                );
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(
                    config.CustomPresetAsset,
                    typeof(CustomTextureCompressorPreset),
                    false
                );
                EditorGUI.EndDisabledGroup();
            }

            if (restriction.RequiresUnlink)
            {
                var lockIcon = EditorGUIUtility.IconContent("IN LockButton on");
                lockIcon.tooltip = restriction.IsBuiltIn
                    ? "This preset is built-in"
                    : "This preset is locked";
                GUILayout.Label(lockIcon, GUILayout.Width(18), GUILayout.Height(18));
            }

            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                CustomPresetEditTransition.TryEnterEditMode(config);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawPresetSummary(config);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the edit mode UI (full editing capabilities).
        /// </summary>
        private static void DrawEditMode(TextureCompressor config)
        {
            var pendingAction = PendingAction.None;
            CustomTextureCompressorPreset pendingNewPreset = null;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorDrawUtils.DrawSectionHeader("Custom Preset");
            EditorGUILayout.Space(4);
            DrawPresetField(config, ref pendingAction, ref pendingNewPreset);
            EditorGUILayout.Space(4);
            DrawStatusAndActions(config);

            EditorGUILayout.EndVertical();

            // Execute pending action after GUI layout is complete
            if (pendingAction != PendingAction.None)
            {
                ExecuteAction(pendingAction, config, pendingNewPreset);
                GUIUtility.ExitGUI();
            }
        }

        private static void DrawPresetField(
            TextureCompressor config,
            ref PendingAction pendingAction,
            ref CustomTextureCompressorPreset pendingNewPreset
        )
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var newPreset = (CustomTextureCompressorPreset)
                EditorGUILayout.ObjectField(
                    config.CustomPresetAsset,
                    typeof(CustomTextureCompressorPreset),
                    false
                );

            if (EditorGUI.EndChangeCheck() && newPreset != config.CustomPresetAsset)
            {
                pendingAction = PendingAction.ChangePreset;
                pendingNewPreset = newPreset;
            }

            DrawActionButtons(config, ref pendingAction);

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawActionButtons(
            TextureCompressor config,
            ref PendingAction pendingAction
        )
        {
            bool hasPreset = config.CustomPresetAsset != null;
            bool isModified = hasPreset && !config.CustomPresetAsset.MatchesSettings(config);

            // Save button (only when modified)
            if (hasPreset && isModified)
            {
                if (
                    GUILayout.Button(
                        new GUIContent("\u2193", "Save current settings to preset"),
                        GUILayout.Width(24),
                        GUILayout.Height(18)
                    )
                )
                {
                    pendingAction = PendingAction.Save;
                }

                // Discard button
                if (
                    GUILayout.Button(
                        new GUIContent("\u21A9", "Discard changes and reload from preset"),
                        GUILayout.Width(24),
                        GUILayout.Height(18)
                    )
                )
                {
                    pendingAction = PendingAction.Discard;
                }
            }

            // New preset button
            if (
                GUILayout.Button(
                    new GUIContent("+", "Create new preset from current settings"),
                    GUILayout.Width(24),
                    GUILayout.Height(18)
                )
            )
            {
                pendingAction = PendingAction.CreateNew;
            }

            // Unlink button (only when preset is assigned)
            if (hasPreset)
            {
                if (
                    GUILayout.Button(
                        new GUIContent("\u2715", "Unlink preset (keep current settings)"),
                        GUILayout.Width(24),
                        GUILayout.Height(18)
                    )
                )
                {
                    pendingAction = PendingAction.Unlink;
                }
            }
        }

        private static void DrawStatusAndActions(TextureCompressor config)
        {
            bool hasPreset = config.CustomPresetAsset != null;

            if (!hasPreset)
            {
                EditorGUILayout.HelpBox(
                    "Settings are stored in this component only.\n"
                        + "Create a preset to reuse across avatars.",
                    MessageType.Info
                );
                return;
            }

            bool isModified = !config.CustomPresetAsset.MatchesSettings(config);

            if (isModified)
            {
                EditorGUILayout.LabelField(
                    "\u25CF Modified",
                    EditorStylesCache.ModifiedStatusStyle
                );
            }
            else
            {
                EditorGUILayout.LabelField("\u2713 Synced", EditorStylesCache.SyncedStatusStyle);
            }
        }

        private static void ExecuteAction(
            PendingAction action,
            TextureCompressor config,
            CustomTextureCompressorPreset newPreset
        )
        {
            switch (action)
            {
                case PendingAction.Save:
                    SaveToPreset(config);
                    break;
                case PendingAction.Discard:
                    DiscardChanges(config);
                    break;
                case PendingAction.CreateNew:
                    CreateNewPreset(config);
                    break;
                case PendingAction.Unlink:
                    UnlinkPreset(config);
                    break;
                case PendingAction.ChangePreset:
                    ChangePreset(config, newPreset);
                    break;
            }
        }

        private static void ChangePreset(
            TextureCompressor config,
            CustomTextureCompressorPreset newPreset
        )
        {
            Undo.RecordObject(config, "Change Custom Preset");
            config.CustomPresetAsset = newPreset;

            if (newPreset != null)
            {
                newPreset.ApplyTo(config);
            }

            EditorUtility.SetDirty(config);
        }

        private static void SaveToPreset(TextureCompressor config)
        {
            if (config.CustomPresetAsset == null)
                return;

            Undo.RecordObject(config.CustomPresetAsset, "Save to Custom Preset");
            config.CustomPresetAsset.CopyFrom(config);
            EditorUtility.SetDirty(config.CustomPresetAsset);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Avatar Compressor] Saved settings to preset: {config.CustomPresetAsset.name}"
            );
        }

        private static void DiscardChanges(TextureCompressor config)
        {
            if (config.CustomPresetAsset == null)
                return;

            Undo.RecordObject(config, "Discard Preset Changes");
            config.CustomPresetAsset.ApplyTo(config);
            EditorUtility.SetDirty(config);
        }

        private static void CreateNewPreset(TextureCompressor config)
        {
            string defaultName = "NewTextureCompressorPreset";
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Texture Compressor Preset",
                defaultName,
                PresetFileExtension,
                "Choose a location to save the new preset",
                DefaultPresetFolder
            );

            if (string.IsNullOrEmpty(path))
                return;

            var newPreset = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            newPreset.CopyFrom(config);

            // Default MenuPath to filename for immediate menu visibility
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            newPreset.MenuPath = fileName;

            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(config, "Create and Assign Custom Preset");
            config.CustomPresetAsset = newPreset;
            EditorUtility.SetDirty(config);

            EditorGUIUtility.PingObject(newPreset);
            Debug.Log($"[Avatar Compressor] Created new preset: {path}");
        }

        private static void UnlinkPreset(TextureCompressor config)
        {
            Undo.RecordObject(config, "Unlink Custom Preset");
            config.CustomPresetAsset = null;
            EditorUtility.SetDirty(config);
        }

        /// <summary>
        /// Draws a summary of the preset including description and settings.
        /// </summary>
        private static void DrawPresetSummary(TextureCompressor config)
        {
            // Description
            if (
                config.CustomPresetAsset != null
                && !string.IsNullOrEmpty(config.CustomPresetAsset.Description)
            )
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    config.CustomPresetAsset.Description,
                    EditorStyles.wordWrappedLabel
                );
                EditorGUILayout.EndVertical();
            }

            // Settings Summary
            PresetSection.DrawSettingsSummary(config, "Settings Summary");
        }
    }
}
