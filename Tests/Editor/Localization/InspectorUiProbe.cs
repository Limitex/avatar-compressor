using System;
using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor.editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace dev.limitex.avatar.compressor.tests
{
    internal sealed class InspectorUiProbe : EditorWindow, IDisposable
    {
        private Editor _editor;
        private SettingsProvider _provider;
        private Exception _guiException;
        private int _drawCount;
        private string _focusedControl;
        private bool _checkFocus;
        private bool _hasControl;

        internal static void RequireGraphicsDevice()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore(
                    "IMGUI interaction tests require a graphics device; omit -nographics."
                );
        }

        internal static InspectorUiProbe OpenInspector(params UnityEngine.Object[] targets)
        {
            return Open(window => window._editor = Editor.CreateEditor(targets));
        }

        internal static InspectorUiProbe OpenPreferences()
        {
            return Open(window =>
            {
                window._provider = AvatarCompressorPreferences.CreateProvider();
                window._provider.OnActivate("", window.rootVisualElement);
            });
        }

        private static InspectorUiProbe Open(Action<InspectorUiProbe> initialize)
        {
            var window = CreateInstance<InspectorUiProbe>();
            try
            {
                window.position = new Rect(50, 50, 650, 900);
                initialize(window);
                // Popup windows don't use the user's saved Inspector/Preferences layout.
                window.ShowPopup();
                return window;
            }
            catch
            {
                window.Dispose();
                throw;
            }
        }

        private void OnGUI()
        {
            try
            {
                if (_editor != null)
                    _editor.OnInspectorGUI();
                else if (_provider != null)
                    _provider.OnGUI("");
                else
                    return;
                if (_checkFocus)
                    GUI.FocusControl(null);
                if (_focusedControl != null)
                    GUI.FocusControl(_focusedControl);
                if (_checkFocus)
                    _hasControl = GUI.GetNameOfFocusedControl() == _focusedControl;
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
            Assert.Greater(_drawCount, before, "The actual inspector/provider must draw.");
        }

        internal bool HasControl(string controlName)
        {
            _focusedControl = controlName;
            _checkFocus = true;
            try
            {
                Draw();
                return _hasControl;
            }
            finally
            {
                _focusedControl = null;
                _checkFocus = false;
            }
        }

        internal void Toggle(string propertyName)
        {
            _focusedControl = propertyName;
            Draw();
            foreach (var type in new[] { EventType.KeyDown, EventType.KeyUp })
                SendEvent(
                    new Event
                    {
                        type = type,
                        keyCode = KeyCode.Space,
                        character = ' ',
                    }
                );
            Draw();
        }

        private void OnDisable()
        {
            try
            {
                if (_editor != null)
                    DestroyImmediate(_editor);
                _editor = null;
            }
            finally
            {
                _provider?.OnDeactivate();
                _provider = null;
            }
        }

        public void Dispose()
        {
            if (this != null)
                Close();
        }
    }

    internal sealed class SavedEditorPreferences : IDisposable
    {
        private readonly List<Action> _restore = new List<Action>();

        internal void Remember<T>(string key, Func<string, T> read, Action<string, T> write)
        {
            bool existed = EditorPrefs.HasKey(key);
            T value = read(key);
            _restore.Add(() =>
            {
                if (existed)
                    write(key, value);
                else
                    EditorPrefs.DeleteKey(key);
            });
        }

        public void Dispose()
        {
            var actions = _restore.AsEnumerable().Reverse().ToArray();
            _restore.Clear();
            LocalizationTestBase.RunCleanup(actions);
        }
    }
}
