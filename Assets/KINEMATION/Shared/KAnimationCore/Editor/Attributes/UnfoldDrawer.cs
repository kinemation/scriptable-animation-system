// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(UnfoldAttribute))]
    public class UnfoldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Generic && !property.isArray)
            {
                SerializedProperty iterator = property.Copy();
                bool enterChildren = true;

                // Calculate the initial position rect for the first property
                Rect propertyPosition = position;
                propertyPosition.height = EditorGUIUtility.singleLineHeight;

                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, property.GetEndProperty()))
                    {
                        break;
                    }

                    enterChildren = false;

                    EditorGUI.PropertyField(propertyPosition, iterator, new GUIContent(iterator.displayName), true);

                    // Update the position for the next property
                    propertyPosition.y +=
                        EditorGUI.GetPropertyHeight(iterator, new GUIContent(iterator.displayName), true) +
                        EditorGUIUtility.standardVerticalSpacing;
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, GUIContent.none, false);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Generic && !property.isArray)
            {
                float totalHeight = 0;
                SerializedProperty iterator = property.Copy();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, property.GetEndProperty()))
                    {
                        break;
                    }

                    enterChildren = false;
                    totalHeight +=
                        EditorGUI.GetPropertyHeight(iterator, new GUIContent(iterator.displayName), true) +
                        EditorGUIUtility.standardVerticalSpacing;
                }

                return totalHeight;
            }

            return EditorGUI.GetPropertyHeight(property, GUIContent.none, false);
        }
    }
}