// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Input
{
    [CustomEditor(typeof(UserInputController), true)]
    public class UserInputControllerInspector : UnityEditor.Editor
    {
        private UserInputController _controller;
        
        private void OnEnable()
        {
            _controller = (UserInputController) target;
        }
        
        private static bool IsInspectorFocused() 
        {
            var focusedWindow = EditorWindow.focusedWindow;
            
            if (focusedWindow != null && focusedWindow.GetType().Name == "InspectorWindow") 
            {
                return true;
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var properties = _controller.GetPropertyBindings();

            if (properties == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foreach (var property in properties)
            {
                string label = property.Item1;
                object value = property.Item2;
                
                if (value is bool)
                {
                    bool toggle = EditorGUILayout.Toggle(label, (bool) value);
                    if(toggle != (bool) value) _controller.SetValue(label, toggle);
                }
                else if (value is int)
                {
                    int integer = EditorGUILayout.IntField(label, (int) value);
                    if(integer != (int) value)  _controller.SetValue(label, integer);
                }
                else if (value is float)
                {
                    float floatVal = EditorGUILayout.FloatField(label, (float) value);
                    if(!Mathf.Approximately(floatVal, (float) value)) _controller.SetValue(label, floatVal);
                }
                else if (value is Vector4)
                {
                    Vector4 vector4 = EditorGUILayout.Vector4Field(label, (Vector4) value);
                    if(!vector4.Equals(((Vector4) value))) _controller.SetValue(label, vector4);
                }
            }
            
            EditorGUILayout.EndVertical();
            if(!IsInspectorFocused()) Repaint();
        }
    }
}