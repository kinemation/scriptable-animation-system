// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Widgets
{
    public enum SplitOrientation
    {
        Horizontal,
        Vertical
    }

    public class KSplitterWidget
    {
        public Action onDrawFirstGUI;
        public Action onDrawSecondGUI;
        public SplitOrientation orientation = SplitOrientation.Horizontal;
        public float splitRatio = 0.35f;
        public float splitterSize = 4f;
        public bool drawSplitterLine = true;

        private bool _resizing;
        private float _dragMinRatio;
        private float _dragMaxRatio;
        private Vector2 _firstScroll;
        private Vector2 _secondScroll;

        public void OnGUI()
        {
            var host = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, 
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            OnGUI(host);
        }

        public void OnGUI(Rect rect)
        {
            var e = Event.current;

            float axis = orientation == SplitOrientation.Horizontal ? rect.width : rect.height;
            float avail = Mathf.Max(0f, axis - splitterSize);

            float firstSize = avail * Mathf.Clamp01(splitRatio);
            float secondSize = Mathf.Max(0f, avail - firstSize);

            Rect firstRect, splitterRect, secondRect;
            if (orientation == SplitOrientation.Horizontal)
            {
                firstRect = new Rect(0f, 0f, firstSize, rect.height);
                splitterRect = new Rect(firstRect.xMax, 0f, splitterSize, rect.height);
                secondRect = new Rect(splitterRect.xMax, 0f, secondSize, rect.height);
            }
            else
            {
                firstRect = new Rect(0f, 0f, rect.width, firstSize);
                splitterRect = new Rect(0f, firstRect.yMax, rect.width, splitterSize);
                secondRect = new Rect(0f, splitterRect.yMax, rect.width, secondSize);
            }
            
            // First pane
            GUILayout.BeginArea(firstRect);
            _firstScroll = EditorGUILayout.BeginScrollView(_firstScroll);
            onDrawFirstGUI?.Invoke();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            // Splitter
            var cursor = orientation == SplitOrientation.Horizontal
                ? MouseCursor.ResizeHorizontal
                : MouseCursor.ResizeVertical;
            EditorGUIUtility.AddCursorRect(splitterRect, cursor);

            if (e.type == EventType.MouseDown && splitterRect.Contains(e.mousePosition))
            {
                _resizing = true;

                // Capture allowed range based on current ratio
                float r = Mathf.Clamp01(splitRatio);
                float minR = Mathf.Min(r, 1f - r);
                _dragMinRatio = minR;
                _dragMaxRatio = 1f - minR;

                e.Use();
            }

            if (_resizing && e.type == EventType.MouseDrag)
            {
                float rel = orientation == SplitOrientation.Horizontal ? e.mousePosition.x : e.mousePosition.y;

                float desiredFirst = Mathf.Clamp(rel, 0f, avail);
                float rawRatio = avail > 0f ? desiredFirst / avail : splitRatio;

                // Clamp within the drag-bounded range
                splitRatio = Mathf.Clamp(rawRatio, _dragMinRatio, _dragMaxRatio);
                GUI.changed = true;
            }

            if (e.type == EventType.MouseUp)
            {
                _resizing = false;
            }

            if (drawSplitterLine)
            {
                if (orientation == SplitOrientation.Horizontal)
                    EditorGUI.DrawRect(new Rect(splitterRect.x, splitterRect.y, 1f, splitterRect.height),
                        new Color(0f, 0f, 0f, 0.25f));
                else
                    EditorGUI.DrawRect(new Rect(splitterRect.x, splitterRect.y, splitterRect.width, 1f),
                        new Color(0f, 0f, 0f, 0.25f));
            }

            // Second pane
            GUILayout.BeginArea(secondRect);
            _secondScroll = EditorGUILayout.BeginScrollView(_secondScroll);
            onDrawSecondGUI?.Invoke();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}