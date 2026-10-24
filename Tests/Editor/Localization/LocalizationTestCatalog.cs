using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace dev.limitex.avatar.compressor.tests
{
    internal static class LocalizationTestCatalog
    {
        internal static string LocalizationFolder =>
            AvatarCompressorLocalization.LocalizationFolder;

        internal static readonly string[] BundledLocales =
        {
            "en-US",
            "zh-Hans",
            "zh-Hant",
            "ja-JP",
            "ko-KR",
        };

        internal static string PhysicalPath(string path)
        {
            if (!path.StartsWith("Packages/", StringComparison.Ordinal))
                return path;

            var package = PackageInfo.FindForAssetPath(path);
            Assert.IsNotNull(package, $"No package found for '{path}'.");
            return Path.Combine(package.resolvedPath, path.Substring(package.assetPath.Length + 1));
        }

        internal static string[] PoPaths()
        {
            return Directory
                .GetFiles(PhysicalPath(LocalizationFolder), "*.po")
                .Select(path => LocalizationFolder + "/" + Path.GetFileName(path))
                .ToArray();
        }

        internal static Dictionary<string, string> ReadMessages(string path)
        {
            var messages = new Dictionary<string, string>(StringComparer.Ordinal);
            var id = new StringBuilder();
            var value = new StringBuilder();
            StringBuilder current = null;

            void Commit()
            {
                if (current != value || id.Length == 0)
                    return;

                string key = id.ToString();
                Assert.IsFalse(messages.ContainsKey(key), $"Duplicate key '{key}'.");
                messages.Add(key, value.ToString());
            }

            foreach (string line in File.ReadAllLines(PhysicalPath(path)))
            {
                if (line.StartsWith("msgid \"", StringComparison.Ordinal))
                {
                    Commit();
                    id.Clear().Append(ReadQuotedValue(line));
                    value.Clear();
                    current = id;
                }
                else if (line.StartsWith("msgstr \"", StringComparison.Ordinal))
                {
                    value.Clear().Append(ReadQuotedValue(line));
                    current = value;
                }
                else if (current != null && line.StartsWith("\"", StringComparison.Ordinal))
                {
                    current.Append(ReadQuotedValue(line));
                }
            }

            Commit();
            return messages;
        }

        private static string ReadQuotedValue(string line)
        {
            int firstQuote = line.IndexOf('"');
            return line.Substring(firstQuote + 1, line.Length - firstQuote - 2);
        }

        internal sealed class TemporaryCatalog : IDisposable
        {
            internal const string Key = "Tests:catalogLifecycle";

            // Reuse a bundled locale: NDMF has no public API to unregister a new language.
            internal readonly string Language;
            private readonly string _assetPath;
            private readonly string _physicalPath;
            private readonly string _outsidePath;
            private readonly string _originalPath;
            private bool _disposed;

            internal TemporaryCatalog(string language = "ko-KR")
            {
                CollectionAssert.Contains(BundledLocales, language);
                Language = language;
                _assetPath = LocalizationFolder + "/" + Language + ".po";
                _physicalPath = PhysicalPath(_assetPath);
                _outsidePath = Path.Combine(
                    Application.dataPath,
                    "_LAC_Catalog_" + Guid.NewGuid().ToString("N") + ".po"
                );
                _originalPath = Path.Combine(
                    Application.dataPath,
                    "_LAC_OriginalCatalog_" + Guid.NewGuid().ToString("N") + ".po"
                );
                Assert.IsTrue(File.Exists(_physicalPath));
                Assert.IsTrue(File.Exists(_physicalPath + ".meta"));
                // Keep the original asset and GUID recoverable if the test aborts.
                try
                {
                    Move(_physicalPath, _originalPath);
                }
                catch
                {
                    RestoreOriginal();
                    throw;
                }
            }

            internal void Import(string translation = "translated", string key = Key)
            {
                File.WriteAllText(
                    _physicalPath,
                    $"msgid \"\"\nmsgstr \"\"\n\"Language: {Language}\\n\"\n\n"
                        + $"msgid \"{key}\"\nmsgstr \"{translation}\"\n"
                );
                AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceSynchronousImport);
            }

            internal void MoveOutside() => Move(_physicalPath, _outsidePath);

            internal void MoveBack() => Move(_outsidePath, _physicalPath);

            private static void Move(string from, string to)
            {
                File.Move(from, to);
                File.Move(from + ".meta", to + ".meta");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            internal void Delete()
            {
                LocalizationTestBase.RunCleanup(
                    new Action[]
                    {
                        () => File.Delete(_physicalPath),
                        () => File.Delete(_physicalPath + ".meta"),
                        () => File.Delete(_outsidePath),
                        () => File.Delete(_outsidePath + ".meta"),
                        () => AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport),
                    }
                );
            }

            private void RestoreOriginal()
            {
                LocalizationTestBase.RunCleanup(
                    new Action[]
                    {
                        () =>
                        {
                            if (File.Exists(_originalPath))
                                File.Move(_originalPath, _physicalPath);
                        },
                        () =>
                        {
                            if (File.Exists(_originalPath + ".meta"))
                                File.Move(_originalPath + ".meta", _physicalPath + ".meta");
                        },
                        () => AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport),
                    }
                );
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                LocalizationTestBase.RunCleanup(new Action[] { Delete, RestoreOriginal });
            }
        }

        internal static string[] GetPlaceholders(string value)
        {
            var collector = new PlaceholderCollector();
            // Let the runtime distinguish format items from escaped braces, including
            // alignment and format specifiers, just as it does when rendering the UI.
            string.Format(collector, value, Enumerable.Range(0, 10).Cast<object>().ToArray());
            return collector
                .Items.OrderBy(placeholder => placeholder, StringComparer.Ordinal)
                .ToArray();
        }

        private sealed class PlaceholderCollector : IFormatProvider, ICustomFormatter
        {
            internal readonly List<string> Items = new List<string>();

            public object GetFormat(Type formatType) =>
                formatType == typeof(ICustomFormatter) ? this : null;

            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                string index = ((int)arg).ToString(CultureInfo.InvariantCulture);
                Items.Add("{" + index + (string.IsNullOrEmpty(format) ? "" : ":" + format) + "}");
                return "";
            }
        }
    }
}
