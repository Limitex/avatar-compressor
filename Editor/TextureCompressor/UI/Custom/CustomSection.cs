using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Main UI for Custom preset mode.
    /// Handles mode selection, preset management, and detail panels.
    /// </summary>
    public static class CustomSection
    {
        private const float ButtonSpacing = 2f;
        private const int MaxCachedRects = 32;
        private static readonly LruCache<int, Rect> _buttonRectCache = new(MaxCachedRects);

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

        #region Public Entry Point

        /// <summary>
        /// Draws the complete Custom preset UI including mode selector and detail panels.
        /// </summary>
        public static void Draw(TextureCompressor config)
        {
            if (config.Preset != CompressorPreset.Custom)
                return;

            bool isEditable = PresetEditorState.IsCustomEditable(config);
            var restriction = PresetEditorState.GetRestriction(config);

            DrawModeSelector(config, isEditable, restriction);
            EditorGUILayout.Space(10);
            DrawDetailPanel(config, isEditable, restriction);
        }

        #endregion

        #region Mode Selector

        private static void DrawModeSelector(
            TextureCompressor config,
            bool isEditable,
            PresetRestriction restriction
        )
        {
            EditorGUILayout.BeginHorizontal();

            float availableWidth = EditorGUIUtility.currentViewWidth - 20f;
            float buttonWidth = (availableWidth - ButtonSpacing) / 2f;

            DrawEditModeButton(config, isEditable, restriction, buttonWidth);
            GUILayout.Space(ButtonSpacing);
            DrawCustomPresetButton(config, isEditable, buttonWidth);

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawEditModeButton(
            TextureCompressor config,
            bool isEditable,
            PresetRestriction restriction,
            float buttonWidth
        )
        {
            string tooltip = restriction.RequiresUnlink()
                ? "Unlink preset and edit settings manually"
                : "Manually configure compression settings";

            if (
                EditorDrawUtils.DrawColoredButton(
                    "Edit Mode",
                    tooltip,
                    PresetColors.EditMode,
                    isEditable,
                    height: 24f,
                    width: buttonWidth
                )
            )
            {
                PresetEditTransition.TryEnterEditMode(config);
            }
        }

        private static void DrawCustomPresetButton(
            TextureCompressor config,
            bool isEditable,
            float buttonWidth
        )
        {
            string presetLabel = "Custom Preset \u25BE";

            bool clicked = EditorDrawUtils.DrawColoredButton(
                presetLabel,
                "Select a custom preset from the menu",
                PresetColors.CustomPreset,
                !isEditable,
                height: 24f,
                width: buttonWidth
            );

            if (Event.current.type == EventType.Repaint)
            {
                _buttonRectCache.Set(config.GetInstanceID(), GUILayoutUtility.GetLastRect());
            }

            if (clicked)
            {
                Rect buttonRect = _buttonRectCache.TryGetValue(config.GetInstanceID(), out var rect)
                    ? rect
                    : GUILayoutUtility.GetLastRect();
                ShowCustomPresetMenu(config, buttonRect);
            }
        }

        private static void ShowCustomPresetMenu(TextureCompressor config, Rect buttonRect)
        {
            var menu = PresetScanner.BuildPresetMenu(
                currentPreset: config.CustomPresetAsset,
                onPresetSelected: (preset) =>
                {
                    Undo.RecordObject(config, "Apply Custom Preset");
                    PresetEditorState.ApplyPresetAndSwitchToUseOnly(config, preset);
                    EditorUtility.SetDirty(config);
                }
            );

            menu.DropDown(buttonRect);
        }

        #endregion

        #region Detail Panels

        private static void DrawDetailPanel(
            TextureCompressor config,
            bool isEditable,
            PresetRestriction restriction
        )
        {
            if (isEditable)
            {
                DrawEditPanel(config);
            }
            else
            {
                DrawUseOnlyPanel(config, restriction);
            }
        }

        private static void DrawUseOnlyPanel(
            TextureCompressor config,
            PresetRestriction restriction
        )
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorDrawUtils.DrawSectionHeader("Custom Preset (Use Only)");
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (restriction == PresetRestriction.BuiltIn)
            {
                EditorGUILayout.LabelField(
                    $"{config.CustomPresetAsset.name} (Built-in)",
                    EditorStyles.boldLabel
                );
            }
            else if (restriction == PresetRestriction.ExternalPackage)
            {
                EditorGUILayout.LabelField(
                    $"{config.CustomPresetAsset.name} (Package)",
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

            if (restriction.RequiresUnlink())
            {
                var lockIcon = EditorGUIUtility.IconContent("IN LockButton on");
                lockIcon.tooltip = restriction switch
                {
                    PresetRestriction.BuiltIn => "This preset is built-in",
                    PresetRestriction.ExternalPackage => "This preset is in a package",
                    PresetRestriction.Locked => "This preset is locked",
                    _ => "This preset cannot be edited",
                };
                GUILayout.Label(lockIcon, GUILayout.Width(18), GUILayout.Height(18));
            }

            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                PresetEditTransition.TryEnterEditMode(config);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawPresetSummary(config);

            EditorGUILayout.EndVertical();
        }

        private static void DrawEditPanel(TextureCompressor config)
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

        private static void DrawPresetSummary(TextureCompressor config)
        {
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

            SettingsSummaryDrawer.Draw(config, "Settings Summary");
        }

        #endregion

        #region Preset Actions

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

                var restriction = PresetEditorState.GetRestriction(config);
                if (restriction.RequiresUnlink())
                {
                    PresetEditorState.SetEditMode(config, false);
                }
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

            string presetName = System.IO.Path.GetFileNameWithoutExtension(path);
            newPreset.MenuPath = presetName;

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

        #endregion
    }
}
