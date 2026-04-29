// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Attributes;
using KINEMATION.FPSAnimationFramework.Runtime.Core;

using UnityEditor;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(ShowStandaloneAttribute))]
    public class ShowStandaloneDrawer : PropertyDrawer
    {
        private bool _showProperty;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _showProperty = true;

            if (property.serializedObject.targetObject is FPSAnimatorLayerSettings {isStandalone: false})
            {
                _showProperty = false;
                return 0f;
            }
            
            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!_showProperty) return;
            
            EditorGUI.PropertyField(position, property, label);
        }
    }
}