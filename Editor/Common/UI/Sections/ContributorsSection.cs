using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Contributors grid of the preferences window. Nothing highlights on hover: the
    /// Settings window gets no MouseMove, so it would linger.
    /// </summary>
    internal sealed class ContributorsSection
    {
        internal const float AvatarSize = 32f;
        internal const float Spacing = 6f;
        private const string ContributorsUrl = GitHubSection.GitHubUrl + "/graphs/contributors";

        private IReadOnlyList<GitHubContributors.Contributor> _contributors =
            Array.Empty<GitHubContributors.Contributor>();
        private bool _isLoading;
        private int _columns = 1;

        // Measured on Repaint; currentViewWidth would include the Settings tree pane.
        private float _availableWidth;

        private static Color PlaceholderColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.35f, 0.35f, 0.35f)
                : new Color(0.72f, 0.72f, 0.72f);

        public void Draw()
        {
            // Snapshot at Layout so the following Repaint draws the same controls.
            if (Event.current.type == EventType.Layout)
            {
                _contributors = GitHubContributors.Contributors;
                _isLoading = GitHubContributors.IsLoading;
                _columns = ColumnCount(_availableWidth, _contributors.Count);
            }

            if (_contributors.Count > 0)
                DrawGrid();
            else
                DrawStatus();

            EditorGUILayout.Space(4);
            var linkContent = AvatarCompressorLocalization.Content(
                "Common:button:viewContributors"
            );
            var linkRect = GUILayoutUtility.GetRect(
                linkContent,
                EditorStylesCache.LinkStyle,
                GUILayout.ExpandWidth(false)
            );
            if (EditorDrawUtils.DrawLink(linkRect, linkContent, highlightOnHover: false))
                Application.OpenURL(ContributorsUrl);
        }

        internal static int ColumnCount(float availableWidth, int count)
        {
            // Unknown before the first Repaint: one clipped row beats a one-frame tall column.
            if (availableWidth <= 0f)
                return Mathf.Max(1, count);
            return Mathf.Max(
                1,
                Mathf.FloorToInt((availableWidth + Spacing) / (AvatarSize + Spacing))
            );
        }

        private void DrawStatus()
        {
            // One avatar row tall, so the link below stays put when the grid replaces it.
            var row = GUILayoutUtility.GetRect(0f, AvatarSize, GUILayout.ExpandWidth(true));
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var line = new Rect(
                row.x,
                row.y + (AvatarSize - lineHeight) / 2f,
                row.width,
                lineHeight
            );

            var savedColor = GUI.color;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            EditorGUI.LabelField(
                line,
                AvatarCompressorLocalization.Tr(
                    _isLoading
                        ? "Common:message:contributorsLoading"
                        : "Common:message:contributorsUnavailable"
                ),
                EditorStyles.miniLabel
            );
            GUI.color = savedColor;
        }

        private void DrawGrid()
        {
            int count = _contributors.Count;
            int rows = (count + _columns - 1) / _columns;
            float height = rows * AvatarSize + (rows - 1) * Spacing;
            // Zero min width: a stale column count clips for a frame instead of scrolling.
            var grid = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint && grid.width != _availableWidth)
            {
                _availableWidth = grid.width;
                HandleUtility.Repaint();
            }

            for (int index = 0; index < count; index++)
            {
                var cell = new Rect(
                    grid.x + index % _columns * (AvatarSize + Spacing),
                    grid.y + index / _columns * (AvatarSize + Spacing),
                    AvatarSize,
                    AvatarSize
                );
                DrawAvatar(cell, _contributors[index]);
            }
        }

        private static void DrawAvatar(Rect rect, GitHubContributors.Contributor contributor)
        {
            if (GUI.Button(rect, new GUIContent(string.Empty, contributor.login), GUIStyle.none))
                Application.OpenURL(GitHubContributors.ProfileUrl(contributor.login));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (Event.current.type != EventType.Repaint)
                return;

            float radius = rect.width / 2f;
            var avatar = GitHubContributors.GetAvatar(contributor.id);
            if (avatar != null)
            {
                GUI.DrawTexture(
                    rect,
                    avatar,
                    ScaleMode.ScaleAndCrop,
                    true,
                    0f,
                    Color.white,
                    0f,
                    radius
                );
                return;
            }

            GUI.DrawTexture(
                rect,
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                true,
                0f,
                PlaceholderColor,
                0f,
                radius
            );
            EditorStylesCache.CenteredBoldLabel.Draw(
                rect,
                new GUIContent(contributor.login.Substring(0, 1).ToUpperInvariant()),
                false,
                false,
                false,
                false
            );
        }
    }
}
