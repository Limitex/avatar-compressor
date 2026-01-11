using UnityEngine;

namespace dev.limitex.avatar.compressor.common
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
            if (obj == null) return;
            Object.DestroyImmediate(obj);
        }

        /// <summary>
        /// Destroys all components of type T in the hierarchy.
        /// </summary>
        public static void DestroyAllComponents<T>(GameObject root) where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            foreach (var component in components)
            {
                SafeDestroy(component);
            }
        }

        /// <summary>
        /// Gets or adds a component to a GameObject.
        /// </summary>
        public static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            var component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Checks if a GameObject or any of its parents has the EditorOnly tag.
        /// Objects with EditorOnly tag are stripped from build.
        /// </summary>
        public static bool IsEditorOnly(GameObject obj)
        {
            if (obj == null) return false;

            var current = obj.transform;
            while (current != null)
            {
                if (current.CompareTag("EditorOnly")) return true;
                current = current.parent;
            }
            return false;
        }
    }
}
