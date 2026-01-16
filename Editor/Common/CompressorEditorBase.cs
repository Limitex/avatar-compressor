using dev.limitex.avatar.compressor.editor.ui;
using nadena.dev.ndmf.runtime;
using UnityEngine;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Base class for compressor custom editors with common utilities.
    /// Uses Template Method pattern to ensure consistent UI structure across all compressor editors.
    /// </summary>
    public abstract class CompressorEditorBase : Editor
    {
        protected bool _showAdvancedSettings;

        /// <summary>
        /// Main inspector GUI entry point. Implements common structure for all compressor editors.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawAvatarRootWarningIfNeeded();

            DrawInspectorContent();

            EditorGUILayout.Space(15);
            GitHubSection.Draw();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Override this to draw editor-specific content.
        /// Called between avatar root warning and GitHub section.
        /// </summary>
        protected abstract void DrawInspectorContent();

        /// <summary>
        /// Draws a warning if the component is not placed on an avatar root.
        /// </summary>
        private void DrawAvatarRootWarningIfNeeded()
        {
            var component = target as Component;
            if (component == null) return;

            if (!RuntimeUtil.IsAvatarRoot(component.transform))
            {
                EditorGUILayout.HelpBox(
                    "This component should be placed on the avatar root GameObject. " +
                    "While it will still work, placing it on the avatar root is recommended.",
                    MessageType.Warning);
                EditorGUILayout.Space(5);
            }
        }
    }
}
