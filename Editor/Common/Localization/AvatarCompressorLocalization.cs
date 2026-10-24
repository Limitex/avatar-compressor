using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    internal static class AvatarCompressorLocalization
    {
        internal const string LocalizationFolder =
            "Packages/dev.limitex.avatar-compressor/Editor/Common/Localization";

        internal static int CatalogRevision { get; private set; }

        internal static event Action Changed;

        private static readonly object LanguageChangeHandle = new object();
        private static bool _notificationScheduled;

        internal static readonly Localizer Localizer = new Localizer("en-US", LoadCatalog);

        static AvatarCompressorLocalization()
        {
            LanguagePrefs.RegisterLanguageChangeCallback(
                LanguageChangeHandle,
                _ => ScheduleNotification()
            );
        }

        internal static string Tr(string key)
        {
            return Localizer.GetLocalizedString(key);
        }

        internal static string Tr(string key, params object[] args)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, Tr(key), args);
            }
            catch (FormatException)
            {
                return $"<{key}>";
            }
        }

        internal static GUIContent Content(string labelKey, string tooltipKey = null)
        {
            return new GUIContent(Tr(labelKey), tooltipKey == null ? null : Tr(tooltipKey));
        }

        /// <summary>
        /// Returns translations across bundled locales, independent of the selected language.
        /// </summary>
        internal static IEnumerable<string> Translations(string key)
        {
            return LoadLocalizations()
                .Select(entry => entry.Lookup(key))
                .Where(text => text != null);
        }

        internal static string CurrentLanguage => LanguagePrefs.Language;

        internal const string LanguagePickerControlName = "AvatarCompressor.LanguagePicker";

        internal static void DrawLanguagePicker()
        {
            GUI.SetNextControlName(LanguagePickerControlName);
            LanguageSwitcher.DrawImmediate();
        }

        // Preference keyword lookups must not invalidate search caches.
        private static List<(string Language, Func<string, string> Lookup)> LoadCatalog()
        {
            var localizations = LoadLocalizations();
            unchecked
            {
                CatalogRevision++;
            }
            // Defer notification until NDMF has installed the new catalog.
            ScheduleNotification();
            return localizations;
        }

        private static void ScheduleNotification()
        {
            if (_notificationScheduled)
                return;

            _notificationScheduled = true;
            EditorApplication.delayCall += NotifyChanged;
        }

        private static void NotifyChanged()
        {
            _notificationScheduled = false;
            Changed?.Invoke();
        }

        internal static List<(string Language, Func<string, string> Lookup)> LoadLocalizations()
        {
            if (!AssetDatabase.IsValidFolder(LocalizationFolder))
            {
                Debug.LogWarning(
                    "[AvatarCompressor] Localization folder not found; UI will show raw keys."
                );
                return new List<(string, Func<string, string>)>();
            }

            return LoadLocalizations(
                AssetDatabase
                    .FindAssets("t:LocalizationAsset", new[] { LocalizationFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => path.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.Ordinal)
            );
        }

        // Crowdin rewrites Language headers; NDMF locale codes come from file names.
        internal static List<(string Language, Func<string, string> Lookup)> LoadLocalizations(
            IEnumerable<string> paths
        )
        {
            var localizations = new List<(string, Func<string, string>)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in paths)
            {
                string language = Path.GetFileNameWithoutExtension(path);
                var lookup = CreateLookup(AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path));
                string problem =
                    !IsCultureName(language) ? "the file name is not a culture name"
                    : !seen.Add(language) ? "another file already provides this locale"
                    : lookup == null ? "the asset could not be loaded"
                    : null;

                if (problem != null)
                {
                    Debug.LogWarning(
                        $"[AvatarCompressor] Skipping localization file '{path}': {problem}."
                    );
                    continue;
                }

                localizations.Add((language, lookup));
            }

            return localizations;
        }

        // NDMF needs null to fall back for empty or missing translations.
        internal static Func<string, string> CreateLookup(LocalizationAsset asset)
        {
            if (asset == null)
                return null;

            return key =>
            {
                // Deleting a .po can destroy the asset without reloading the domain.
                if (asset == null)
                    return null;

                string localized = asset.GetLocalizedString(key);
                return string.IsNullOrEmpty(localized) || localized == key ? null : localized;
            };
        }

        private sealed class LocalizationPostprocessor : AssetPostprocessor
        {
            private static bool _reloadScheduled;

            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths
            )
            {
                bool catalogChanged = importedAssets
                    .Concat(deletedAssets)
                    .Concat(movedAssets)
                    .Concat(movedFromAssetPaths)
                    .Any(path =>
                        path.StartsWith(LocalizationFolder + "/", StringComparison.Ordinal)
                        && path.EndsWith(".po", StringComparison.OrdinalIgnoreCase)
                    );
                if (catalogChanged)
                {
                    // Existing assets can update in place before the catalog is rebuilt.
                    unchecked
                    {
                        CatalogRevision++;
                    }

                    if (!_reloadScheduled)
                    {
                        _reloadScheduled = true;
                        EditorApplication.delayCall += ReloadCatalog;
                    }
                }
            }

            private static void ReloadCatalog()
            {
                try
                {
                    // NDMF calls AssetDatabase.Refresh; wait until importing has finished.
                    // Keep the guard set during Refresh so its notifications are coalesced.
                    nadena.dev.ndmf.localization.Localizer.ReloadLocalizations();
                }
                finally
                {
                    _reloadScheduled = false;
                }
            }
        }

        private static bool IsCultureName(string name)
        {
            try
            {
                CultureInfo.GetCultureInfo(name);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}
