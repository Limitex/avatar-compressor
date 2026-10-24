using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Hosts shared preferences and sections contributed through IPreferencesSection.
    /// </summary>
    internal static class AvatarCompressorPreferences
    {
        public const string PrefsPrefix = "dev.limitex.avatar-compressor.";
        private const string BasePath = "Preferences/Avatar Compressor";
        private const string EnableLoggingKey = PrefsPrefix + "enableLogging";
        private const string ShowLanguagePickerInInspectorKey =
            PrefsPrefix + "showLanguagePickerInInspector";

        /// <summary>
        /// When true, debug log output is enabled during build and preview.
        /// </summary>
        public static bool EnableLogging
        {
            get => EditorPrefs.GetBool(EnableLoggingKey, true);
            set => EditorPrefs.SetBool(EnableLoggingKey, value);
        }

        public static bool ShowLanguagePickerInInspector
        {
            get => EditorPrefs.GetBool(ShowLanguagePickerInInspectorKey, true);
            set => EditorPrefs.SetBool(ShowLanguagePickerInInspectorKey, value);
        }

        /// <summary>
        /// Instantiates and title-sorts the preference sections. A section
        /// that fails to construct — or whose Title/Keywords getters throw —
        /// is skipped with an error, so one broken implementer cannot take
        /// down the whole settings page (SettingsService drops the entire
        /// provider when its factory throws).
        /// </summary>
        public static List<IPreferencesSection> CreateSections(IEnumerable<System.Type> types)
        {
            var sections = new List<IPreferencesSection>();
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;

                try
                {
                    var section = (IPreferencesSection)System.Activator.CreateInstance(type);
                    // Evaluate everything the provider reads inside the guard,
                    // so a broken member surfaces here and not mid-layout.
                    _ = section.Title;
                    _ = section.Keywords.Count();
                    sections.Add(section);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        $"[AvatarCompressor] Skipping preferences section '{type.FullName}': {e}"
                    );
                }
            }

            sections.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Title, b.Title));
            return sections;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProvider()
        {
            var sections = CreateSections(TypeCache.GetTypesDerivedFrom<IPreferencesSection>());

            var keywords = new HashSet<string> { "Avatar", "Compressor", "LAC", "Log", "Debug" };
            keywords.UnionWith(AvatarCompressorLocalization.Translations("Common:label:language"));
            keywords.UnionWith(
                AvatarCompressorLocalization.Translations(
                    "Common:prop:showLanguagePickerInInspector"
                )
            );
            foreach (var section in sections)
            {
                keywords.UnionWith(section.Keywords);
            }

            var provider = new SettingsProvider(BasePath, SettingsScope.User)
            {
                label = "Avatar Compressor",
                guiHandler = _ =>
                {
                    EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

                    EditorGUILayout.LabelField(
                        AvatarCompressorLocalization.Tr("Common:label:general"),
                        EditorStyles.boldLabel
                    );
                    EnableLogging = EditorGUILayout.Toggle(
                        AvatarCompressorLocalization.Content(
                            "Common:prop:enableLogging",
                            "Common:prop:enableLogging:tooltip"
                        ),
                        EnableLogging
                    );

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField(
                        AvatarCompressorLocalization.Tr("Common:label:language"),
                        EditorStyles.boldLabel
                    );
                    AvatarCompressorLocalization.DrawLanguagePicker();

                    EditorGUI.BeginChangeCheck();
                    GUI.SetNextControlName(nameof(ShowLanguagePickerInInspector));
                    var showLanguagePickerInInspector = EditorGUILayout.ToggleLeft(
                        AvatarCompressorLocalization.Content(
                            "Common:prop:showLanguagePickerInInspector",
                            "Common:prop:showLanguagePickerInInspector:tooltip"
                        ),
                        ShowLanguagePickerInInspector
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        ShowLanguagePickerInInspector = showLanguagePickerInInspector;
                        InternalEditorUtility.RepaintAllViews();
                    }

                    foreach (var section in sections)
                    {
                        EditorGUILayout.Space(10);
                        EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
                        section.Draw();
                    }

                    EditorGUILayout.EndVertical();
                },
                keywords = keywords,
            };
            provider.activateHandler = (_, __) =>
                AvatarCompressorLocalization.Changed += provider.Repaint;
            provider.deactivateHandler = () =>
                AvatarCompressorLocalization.Changed -= provider.Repaint;
            return provider;
        }
    }
}
