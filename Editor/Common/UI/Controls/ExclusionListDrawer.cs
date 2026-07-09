using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Reusable static drawer for exclusion list sections (foldout + list + add/remove).
    /// Specific item drawing and behavior are provided via delegate callbacks.
    /// </summary>
    internal static class ExclusionListDrawer
    {
        /// <summary>
        /// Draws the list content without a foldout wrapper.
        /// Use this when the caller manages the foldout externally.
        /// </summary>
        public static void DrawContent<T>(
            UnityEngine.Object undoTarget,
            List<T> list,
            Func<T, T> drawItemField,
            string sectionLabel = "Item",
            string emptyHelpText = "No items.",
            string addButtonLabel = "+ Add",
            Action<UnityEngine.Object, List<T>> onAdd = null,
            Func<T, int, List<T>, bool> validateChange = null,
            Action<T, int> drawItemExtra = null
        )
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                T newValue = drawItemField(list[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    if (validateChange == null || validateChange(newValue, i, list))
                    {
                        Undo.RecordObject(undoTarget, $"Change {sectionLabel} Item");
                        list[i] = newValue;
                        EditorUtility.SetDirty(undoTarget);
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(undoTarget, $"Remove {sectionLabel} Item");
                    list.RemoveAt(i);
                    EditorUtility.SetDirty(undoTarget);
                    EditorGUILayout.EndHorizontal();
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                if (drawItemExtra != null)
                {
                    drawItemExtra(list[i], i);
                }
            }

            if (GUILayout.Button(addButtonLabel))
            {
                if (onAdd != null)
                {
                    onAdd(undoTarget, list);
                }
                else
                {
                    Undo.RecordObject(undoTarget, $"Add {sectionLabel} Item");
                    list.Add(default);
                    EditorUtility.SetDirty(undoTarget);
                }
            }

            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox(emptyHelpText, MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
