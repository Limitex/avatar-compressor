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
    public static class ExclusionListDrawer
    {
        /// <summary>
        /// Draws a full exclusion list section with foldout, item rows, and add/remove buttons.
        /// </summary>
        /// <param name="undoTarget">The Object to record undo on.</param>
        /// <param name="list">The backing list to draw and modify.</param>
        /// <param name="showSection">Foldout state (passed by ref).</param>
        /// <param name="sectionLabel">Label for the foldout header (count is appended automatically).</param>
        /// <param name="emptyHelpText">Help text shown when the list is empty.</param>
        /// <param name="drawItemField">Draws the input field for one item and returns the new value.</param>
        /// <param name="addButtonLabel">Label for the add button.</param>
        /// <param name="onAdd">Called when the add button is clicked. If null, appends default(T).</param>
        /// <param name="validateChange">Returns true to accept a change. If null, all changes are accepted.</param>
        /// <param name="drawItemExtra">Draws optional extra UI below each item row. Can be null.</param>
        public static void Draw<T>(
            UnityEngine.Object undoTarget,
            List<T> list,
            ref bool showSection,
            string sectionLabel,
            string emptyHelpText,
            Func<T, T> drawItemField,
            string addButtonLabel = "+ Add",
            Action<UnityEngine.Object, List<T>> onAdd = null,
            Func<T, int, List<T>, bool> validateChange = null,
            Action<T, int> drawItemExtra = null
        )
        {
            int count = list.Count;
            string label = count > 0 ? $"{sectionLabel} ({count})" : sectionLabel;

            showSection = EditorGUILayout.Foldout(showSection, label, true);
            if (!showSection)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = list.Count - 1; i >= 0; i--)
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
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                if (drawItemExtra != null && i < list.Count)
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
