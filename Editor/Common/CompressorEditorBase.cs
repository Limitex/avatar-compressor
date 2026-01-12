using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Base class for compressor custom editors with common utilities.
    /// </summary>
    public abstract class CompressorEditorBase : Editor
    {
        /// <summary>
        /// Draws a warning if the component is not placed on the avatar root.
        /// </summary>
        protected void DrawAvatarRootWarning(Transform transform)
        {
            if (!RuntimeUtil.IsAvatarRoot(transform))
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
