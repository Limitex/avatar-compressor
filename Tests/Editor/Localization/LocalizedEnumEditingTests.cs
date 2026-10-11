using System;
using System.Collections;
using System.Linq;
using dev.limitex.avatar.compressor.editor.texture.ui;
using dev.limitex.avatar.compressor.editor.ui;
using nadena.dev.ndmf.localization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using static dev.limitex.avatar.compressor.tests.LocalizationTestCatalog;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    internal sealed class LocalizedEnumEditingTests : LocalizationTestBase
    {
        private CustomTextureCompressorPreset _first;
        private CustomTextureCompressorPreset _second;
        private LocalizedEnumTestWindow _window;
        private string _prefabPath;
        private GameObject _prefabInstance;

        [SetUp]
        public void SetUp()
        {
            InspectorUiProbe.RequireGraphicsDevice();

            _first = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            _second = ScriptableObject.CreateInstance<CustomTextureCompressorPreset>();
            _first.Strategy = AnalysisStrategyType.Fast;
            _second.Strategy = AnalysisStrategyType.Combined;
            _window = ScriptableObject.CreateInstance<LocalizedEnumTestWindow>();
            _window.position = new Rect(50, 50, 450, 100);
            _window.ShowUtility();
            _window.SetTargets(_first, _second);
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
                _window.Close();
            if (_prefabInstance != null)
                Undo.ClearUndo(_prefabInstance.GetComponent<TextureCompressor>());
            foreach (var target in new UnityEngine.Object[] { _first, _second, _prefabInstance })
            {
                if (target == null)
                    continue;
                Undo.ClearUndo(target);
                UnityEngine.Object.DestroyImmediate(target);
            }
            if (_prefabPath != null)
                AssetDatabase.DeleteAsset(_prefabPath);
        }

        [Test]
        public void MixedValues_DrawingWithoutInput_PreservesEachValue()
        {
            _window.Draw();
            Assert.IsTrue(_window.HasMixedValues);
            Assert.AreEqual(AnalysisStrategyType.Fast, _first.Strategy);
            Assert.AreEqual(AnalysisStrategyType.Combined, _second.Strategy);
            Assert.IsFalse(_window.AppliedChanges);
        }

        [UnityTest]
        public IEnumerator Popup_UsesLabelsFromCurrentLanguage()
        {
            const string key = "TextureCompressor:enum:analysisStrategyTypeFast";
            const string expected = "Test fast strategy";
            var catalog = Track(new TemporaryCatalog());
            catalog.Import(expected, key);
            LanguagePrefs.Language = catalog.Language;
            yield return WaitForTranslation(key, expected);
            _window.Draw();

            CollectionAssert.Contains(_window.Options, expected);
        }

        [Test]
        public void UnknownSerializedValue_DrawingWithoutInput_PreservesValue()
        {
            _first.Strategy = (AnalysisStrategyType)int.MaxValue;
            _window.SetTargets(_first);

            _window.Draw();

            Assert.AreEqual((AnalysisStrategyType)int.MaxValue, _first.Strategy);
            Assert.IsFalse(_window.AppliedChanges);
        }

        [TestCase(AnalysisStrategyType.Fast)]
        [TestCase(AnalysisStrategyType.Perceptual)]
        public void MixedValues_SelectingValue_SupportsUndoAndRedo(AnalysisStrategyType selection)
        {
            _window.Draw();
            Assert.IsTrue(_window.HasMixedValues);
            Undo.IncrementCurrentGroup();

            _window.Select(selection);

            Assert.IsTrue(_window.AppliedChanges);
            Assert.AreEqual(selection, _first.Strategy);
            Assert.AreEqual(selection, _second.Strategy);
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            Undo.PerformUndo();
            Assert.AreEqual(AnalysisStrategyType.Fast, _first.Strategy);
            Assert.AreEqual(AnalysisStrategyType.Combined, _second.Strategy);
            Undo.PerformRedo();
            Assert.AreEqual(selection, _first.Strategy);
            Assert.AreEqual(selection, _second.Strategy);
        }

        [Test]
        public void Prefab_SelectingValue_CreatesOverrideThatCanBeReverted()
        {
            _prefabPath = "Assets/_LAC_EnumEditing_" + Guid.NewGuid().ToString("N") + ".prefab";
            var source = new GameObject("Localized enum prefab");
            GameObject prefab;
            try
            {
                source.AddComponent<TextureCompressor>().Strategy = AnalysisStrategyType.Fast;
                prefab = PrefabUtility.SaveAsPrefabAsset(source, _prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }

            _prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var component = _prefabInstance.GetComponent<TextureCompressor>();
            _window.SetTargets(component);
            _window.Draw();
            Assert.IsFalse(_window.Property.prefabOverride);

            _window.Select(AnalysisStrategyType.Perceptual);

            Assert.AreEqual(AnalysisStrategyType.Perceptual, component.Strategy);
            Undo.FlushUndoRecordObjects();
            _window.Draw();
            Assert.IsTrue(_window.Property.prefabOverride);
            Assert.AreEqual(
                AnalysisStrategyType.Fast,
                prefab.GetComponent<TextureCompressor>().Strategy
            );
            PrefabUtility.RevertPropertyOverride(_window.Property, InteractionMode.AutomatedAction);
            _window.Draw();
            Assert.AreEqual(AnalysisStrategyType.Fast, component.Strategy);
            Assert.IsFalse(_window.Property.prefabOverride);
        }
    }

    internal sealed class LocalizedEnumTestWindow : EditorWindow
    {
        private SerializedObject _serialized;
        private AnalysisStrategyType? _selection;
        private int _drawCount;
        private Exception _guiException;
        internal bool HasMixedValues { get; private set; }
        internal string[] Options { get; private set; }
        internal bool AppliedChanges { get; private set; }
        internal SerializedProperty Property =>
            _serialized.FindProperty(nameof(CustomTextureCompressorPreset.Strategy));

        internal void SetTargets(params UnityEngine.Object[] targets)
        {
            _serialized?.Dispose();
            _serialized = new SerializedObject(targets);
        }

        private void OnGUI()
        {
            if (_serialized == null)
                return;
            try
            {
                _serialized.Update();
                LocalizedEnumPopup.DrawProperty<AnalysisStrategyType>(
                    Property,
                    TextureCompressorPropertyLabels.Content(
                        nameof(CustomTextureCompressorPreset.Strategy)
                    ),
                    TextureCompressorEnumLabels.Text,
                    DrawPopup
                );
                AppliedChanges = _serialized.ApplyModifiedProperties();
                _drawCount++;
            }
            catch (Exception exception)
            {
                _guiException = exception;
            }
        }

        internal void Draw()
        {
            int before = _drawCount;
            SendEvent(new Event { type = EventType.Layout });
            SendEvent(new Event { type = EventType.Repaint });
            Assert.IsNull(_guiException);
            Assert.Greater(_drawCount, before, "The test must execute the actual IMGUI control.");
        }

        private int DrawPopup(Rect position, GUIContent label, int index, GUIContent[] options)
        {
            HasMixedValues = EditorGUI.showMixedValue;
            Options = options.Select(option => option.text).ToArray();
            int drawn = EditorGUI.Popup(position, label, index, options);
            // Native popup menus have no public API for batchmode interaction.
            if (
                _selection.HasValue
                && Event.current.type == EventType.ExecuteCommand
                && Event.current.commandName == "LAC_SelectEnum"
            )
            {
                int selected = Array.IndexOf(
                    Enum.GetValues(typeof(AnalysisStrategyType)),
                    _selection.Value
                );
                _selection = null;
                GUI.changed = true;
                Event.current.Use();
                return selected;
            }
            return drawn;
        }

        internal void Select(AnalysisStrategyType value)
        {
            _selection = value;
            AppliedChanges = false;
            SendEvent(EditorGUIUtility.CommandEvent("LAC_SelectEnum"));
            Assert.IsNull(_guiException);
            Assert.IsNull(_selection, "The popup renderer must consume the selection.");
        }

        private void OnDisable()
        {
            _serialized?.Dispose();
            _serialized = null;
        }
    }
}
