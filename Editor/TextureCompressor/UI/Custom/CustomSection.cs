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
    internal static class CustomSection
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

            // Validate edit state - may exit edit mode and apply preset settings if locked
            PresetEditTransition.EnsureValidEditState(config);

            bool isEditable = PresetEditorState.IsCustomEditable(config);
            bool hasPresetAsset = config.CustomPresetAsset != null;
            var restriction = PresetEditorState.GetRestriction(config);

            DrawModeSelector(config, isEditable, restriction);
            EditorGUILayout.Space(10);
            DrawDetailPanel(config, isEditable, hasPresetAsset, restriction);
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
                ? AvatarCompressorLocalization.Tr(
                    "TextureCompressor:prop:customEditModeUnlink:tooltip"
                )
                : AvatarCompressorLocalization.Tr("TextureCompressor:prop:customEditMode:tooltip");

            if (
                EditorDrawUtils.DrawColoredButton(
                    AvatarCompressorLocalization.Tr("TextureCompressor:prop:customEditMode"),
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
            string presetLabel = AvatarCompressorLocalization.Tr(
                "TextureCompressor:prop:customPreset"
            );

            bool clicked = EditorDrawUtils.DrawColoredButton(
                presetLabel,
                AvatarCompressorLocalization.Tr("TextureCompressor:prop:customPreset:tooltip"),
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
            bool hasPresetAsset,
            PresetRestriction restriction
        )
        {
            // UseOnly panel: preset is assigned but not editable (locked/package/built-in)
            // Edit panel: no preset assigned, or preset is editable
            if (!isEditable && hasPresetAsset)
            {
                DrawUseOnlyPanel(config, restriction);
            }
            else
            {
                DrawEditPanel(config, hasPresetAsset);
            }
        }

        private static void DrawUseOnlyPanel(
            TextureCompressor config,
            PresetRestriction restriction
        )
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorDrawUtils.DrawSectionHeader(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:customUseOnly")
            );
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (restriction == PresetRestriction.BuiltIn)
            {
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:customNameBuiltIn",
                        config.CustomPresetAsset.name
                    ),
                    EditorStyles.boldLabel
                );
            }
            else if (restriction == PresetRestriction.ExternalPackage)
            {
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:customNamePackage",
                        config.CustomPresetAsset.name
                    ),
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
                    PresetRestriction.BuiltIn => AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:customLockBuiltIn"
                    ),
                    PresetRestriction.ExternalPackage => AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:customLockPackage"
                    ),
                    PresetRestriction.Locked => AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:customLockLocked"
                    ),
                    _ => AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:customLockUneditable"
                    ),
                };
                GUILayout.Label(lockIcon, GUILayout.Width(18), GUILayout.Height(18));
            }

            if (
                GUILayout.Button(
                    AvatarCompressorLocalization.Tr("TextureCompressor:button:editCustomPreset"),
                    GUILayout.Width(60)
                )
            )
            {
                PresetEditTransition.TryEnterEditMode(config);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawPresetSummary(config);

            EditorGUILayout.EndVertical();
        }

        private static void DrawEditPanel(TextureCompressor config, bool hasPresetAsset)
        {
            var pendingAction = PendingAction.None;
            CustomTextureCompressorPreset pendingNewPreset = null;
            bool isModified = hasPresetAsset && !config.CustomPresetAsset.MatchesSettings(config);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorDrawUtils.DrawSectionHeader(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:customPreset")
            );
            EditorGUILayout.Space(4);
            DrawPresetField(
                config,
                hasPresetAsset,
                isModified,
                ref pendingAction,
                ref pendingNewPreset
            );
            EditorGUILayout.Space(4);
            DrawStatusAndActions(hasPresetAsset, isModified);

            EditorGUILayout.EndVertical();

            if (pendingAction != PendingAction.None)
            {
                ExecuteAction(pendingAction, config, pendingNewPreset);
                GUIUtility.ExitGUI();
            }
        }

        private static void DrawPresetField(
            TextureCompressor config,
            bool hasPresetAsset,
            bool isModified,
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

            DrawActionButtons(hasPresetAsset, isModified, ref pendingAction);

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawActionButtons(
            bool hasPresetAsset,
            bool isModified,
            ref PendingAction pendingAction
        )
        {
            if (hasPresetAsset && isModified)
            {
                if (
                    GUILayout.Button(
                        new GUIContent(
                            "\u2193",
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:button:saveCustomPreset:tooltip"
                            )
                        ),
                        GUILayout.Width(24),
                        GUILayout.Height(18)
                    )
                )
                {
                    pendingAction = PendingAction.Save;
                }

                if (
                    GUILayout.Button(
                        new GUIContent(
                            "\u21A9",
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:button:discardCustomPreset:tooltip"
                            )
                        ),
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
                    new GUIContent(
                        "+",
                        AvatarCompressorLocalization.Tr(
                            "TextureCompressor:button:createCustomPreset:tooltip"
                        )
                    ),
                    GUILayout.Width(24),
                    GUILayout.Height(18)
                )
            )
            {
                pendingAction = PendingAction.CreateNew;
            }

            if (hasPresetAsset)
            {
                if (
                    GUILayout.Button(
                        new GUIContent(
                            "\u2715",
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:button:unlinkCustomPreset:tooltip"
                            )
                        ),
                        GUILayout.Width(24),
                        GUILayout.Height(18)
                    )
                )
                {
                    pendingAction = PendingAction.Unlink;
                }
            }
        }

        private static void DrawStatusAndActions(bool hasPresetAsset, bool isModified)
        {
            if (!hasPresetAsset)
            {
                EditorGUILayout.HelpBox(
                    AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:customComponentOnly"
                    ),
                    MessageType.Info
                );
                return;
            }

            if (isModified)
            {
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr("TextureCompressor:message:customModified"),
                    EditorStylesCache.ModifiedStatusStyle
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr("TextureCompressor:message:customSynced"),
                    EditorStylesCache.SyncedStatusStyle
                );
            }
        }

        private static void DrawPresetSummary(TextureCompressor config)
        {
            // Called only from DrawUseOnlyPanel where CustomPresetAsset is guaranteed non-null
            string description = config.CustomPresetAsset.Description;
            if (!string.IsNullOrEmpty(description))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr("TextureCompressor:prop:customDescription"),
                    EditorStyles.boldLabel
                );
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }

            SettingsSummaryDrawer.Draw(
                config,
                AvatarCompressorLocalization.Tr("TextureCompressor:label:settings")
            );
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
                AvatarCompressorLocalization.Tr("TextureCompressor:dialog:createPresetTitle"),
                defaultName,
                PresetFileExtension,
                AvatarCompressorLocalization.Tr("TextureCompressor:dialog:createPresetMessage"),
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
