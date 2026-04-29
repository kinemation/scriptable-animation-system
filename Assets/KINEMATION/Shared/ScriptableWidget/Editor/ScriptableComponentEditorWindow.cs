// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace KINEMATION.Shared.ScriptableWidget.Editor
{
    public class ScriptableComponentEditorWindow : EditorWindow
    {
        private UnityEditor.Editor _targetEditor;
        private Vector2 _scrollPosition;

        public static ScriptableComponentEditorWindow CreateWindow()
        {
            var newWindow = GetWindow<ScriptableComponentEditorWindow>(false, "Window", true);
            return newWindow;
        }

        public UnityEditor.Editor GetEditor()
        {
            return _targetEditor;
        }

        public void RefreshEditor(UnityEditor.Editor newEditor, string windowTitle)
        {
            titleContent.text = windowTitle;
            _targetEditor = newEditor;
            Assert.IsNotNull(_targetEditor);
        }

        public void OnGUI()
        {
            if (_targetEditor == null)
            {
                Close();
                return;
            }
            
            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
            }
            
            GUIStyle paddedStyle = new GUIStyle()
            {
                // Set the padding you want (left, right, top, bottom)
                padding = new RectOffset(15, 5, 5, 5)
            };
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, paddedStyle);
            _targetEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }
    }
}