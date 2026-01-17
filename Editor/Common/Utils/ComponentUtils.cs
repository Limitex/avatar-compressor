using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Utility methods for component operations.
    /// </summary>
    public static class ComponentUtils
    {
        /// <summary>
        /// Safely destroys a component.
        /// Uses DestroyImmediate in editor, Destroy at runtime.
        /// </summary>
        public static void SafeDestroy(Object obj)
        {
            if (obj == null)
                return;
            Object.DestroyImmediate(obj);
        }

        /// <summary>
        /// Checks if a GameObject or any of its parents has the EditorOnly tag.
        /// Objects with EditorOnly tag are stripped from build.
        /// </summary>
        public static bool IsEditorOnly(GameObject obj)
        {
            if (obj == null)
                return false;

            var current = obj.transform;
            while (current != null)
            {
                if (current.CompareTag("EditorOnly"))
                    return true;
                current = current.parent;
            }
            return false;
        }
    }
}
