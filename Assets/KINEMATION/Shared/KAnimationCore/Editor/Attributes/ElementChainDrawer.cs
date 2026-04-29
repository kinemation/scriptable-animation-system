// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Editor.Rig;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(KRigElementChain))]
    public class ElementChainDrawer : PropertyDrawer
    {
        private CustomElementChainDrawerAttribute GetCustomChainAttribute()
        {
            CustomElementChainDrawerAttribute attr = null;

            var attributes = fieldInfo.GetCustomAttributes(true);
            foreach (var customAttribute in attributes)
            {
                attr = customAttribute as CustomElementChainDrawerAttribute;
                if (attr != null) break;
            }
            
            return attr;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            IRigProvider rig = RigEditorUtility.TryGetRigProvider(fieldInfo, property);
            
            SerializedProperty elementChain = property.FindPropertyRelative("elementChain");
            SerializedProperty chainName = property.FindPropertyRelative("chainName");
            
            if (rig != null)
            {
                float labelWidth = EditorGUIUtility.labelWidth;
                var customChain = GetCustomChainAttribute();
                
                Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
                Rect buttonRect = position;
                
                string buttonText = $"Edit {chainName.stringValue}";

                if (customChain is {drawLabel: true})
                {
                    EditorGUI.PrefixLabel(labelRect, label);
                    labelRect.x += labelRect.width;
                    labelRect.width = (position.width - labelWidth) / 2f;

                    buttonRect.x = labelRect.x;
                    buttonRect.width = position.width - labelWidth;
                    
                    buttonText = $"Edit {label.text}";
                }

                if (customChain is {drawTextField: true})
                {
                    chainName.stringValue = EditorGUI.TextField(labelRect, chainName.stringValue);
                    
                    buttonRect.width = position.width  - labelRect.width - (labelRect.x - position.x);
                    buttonRect.x = labelRect.x + labelRect.width;
                    
                    buttonText = "Edit";
                }
                
                if (GUI.Button(buttonRect, buttonText))
                {
                    var hierarchy = rig.GetHierarchy();
                    if (hierarchy != null)
                    {
                        List<int> selectedIds = null;

                        // Get the active element indexes.
                        int arraySize = elementChain.arraySize;

                        if (arraySize > 0)
                        {
                            selectedIds = new List<int>();

                            for (int i = 0; i < arraySize; i++)
                            {
                                var boneName
                                    = elementChain.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                                selectedIds.Add(Array.FindIndex(hierarchy,
                                    element => element.name.Equals(boneName)) + 1);
                            }
                        }

                        RigWindow.ShowWindow(hierarchy,
                            (selectedElement) => { },
                            items =>
                            {
                                elementChain.ClearArray();

                                foreach (var selection in items)
                                {
                                    elementChain.arraySize++;
                                    int lastIndex = elementChain.arraySize - 1;

                                    var element = elementChain.GetArrayElementAtIndex(lastIndex);
                                    var name = element.FindPropertyRelative("name");
                                    var index = element.FindPropertyRelative("index");

                                    name.stringValue = selection.name;
                                    index.intValue = selection.index;
                                }

                                property.serializedObject.ApplyModifiedProperties();
                            },
                            true, selectedIds, "Element Chain Selection"
                        );
                    }
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = true;
            }

            EditorGUI.EndProperty();
        }
    }
}