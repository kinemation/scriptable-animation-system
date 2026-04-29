// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Editor.Rig;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(KRigElement))]
    public class RigElementDrawer : PropertyDrawer
    {
        private void DrawRigElement(Rect position, SerializedProperty property, GUIContent label)
        {
            IRigProvider rig = RigEditorUtility.TryGetRigProvider(fieldInfo, property);
            
            SerializedProperty name = property.FindPropertyRelative("name");
            SerializedProperty index = property.FindPropertyRelative("index");

            if (rig == null)
            {
                EditorGUI.PropertyField(position, name, label, true);
                return;
            }
            
            // Calculate label width
            float labelWidth = EditorGUIUtility.labelWidth;
            float indentLevel = EditorGUI.indentLevel;

            // Calculate button width and property field width
            float totalWidth = position.width - indentLevel - labelWidth;

            // Display the default property field
            Rect propertyFieldRect = new Rect(position.x + indentLevel, position.y,
                labelWidth, position.height);
            
            EditorGUI.LabelField(propertyFieldRect, label.text);

            // Display the bone selection button
            Rect buttonRect = new Rect(position.x + indentLevel + labelWidth, position.y,
                totalWidth, EditorGUIUtility.singleLineHeight);

            string currentName = string.IsNullOrEmpty(name.stringValue) ? "None" : name.stringValue;

            if (GUI.Button(buttonRect, currentName))
            {
                var hierarchy = rig.GetHierarchy();
                if (hierarchy == null) return;

                List<int> selection = null;
                if (index.intValue > -1 || !string.IsNullOrEmpty(name.stringValue))
                {
                    int foundIndex = ArrayUtility.FindIndex(hierarchy,
                        element => element.name.Equals(name.stringValue));
                    if(foundIndex >= 0) selection = new List<int>() { foundIndex + 1 };
                }

                RigWindow.ShowWindow(hierarchy, (selectedElement) =>
                    {
                        name.stringValue = selectedElement.name;
                        index.intValue = selectedElement.index;
                        name.serializedObject.ApplyModifiedProperties();
                    },
                    items => { },
                    false, selection, "Rig Element Selection"
                );
            }
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            DrawRigElement(position, property, label);

            EditorGUI.EndProperty();
        }
    }
}