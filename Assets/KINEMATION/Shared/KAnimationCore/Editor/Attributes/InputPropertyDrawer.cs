// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(InputProperty))]
    public class InputPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MonoBehaviour component = property.serializedObject.targetObject as MonoBehaviour;
            UserInputConfig config = null;

            if (component != null)
            {
                var root = component.transform.root;
                UserInputController controller = root.gameObject.GetComponentInChildren<UserInputController>();
                config = controller == null ? null : controller.inputConfig;
            }
            
            if (config == null)
            {
                config = (property.serializedObject.targetObject as IRigUser)?.GetRigAsset().inputConfig;
            }

            if (config == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            int selectedIndex = -1;
            int indexOffset = 0;
          
            List<string> properties = new List<string>();
            List<string> options = new List<string>();
            
            int count = config.boolProperties.Count;
            for (int i = 0; i < count; i++)
            {
                var inputProperty = config.boolProperties[i];
                properties.Add(inputProperty.name);
                options.Add($"{inputProperty.name} (bool)");
                if (property.stringValue.Equals(inputProperty.name))
                {
                    selectedIndex = i + indexOffset;
                }
            }
            indexOffset += count;
            
            count = config.intProperties.Count;
            for (int i = 0; i < count; i++)
            {
                var inputProperty = config.intProperties[i];
                properties.Add(inputProperty.name);
                options.Add($"{inputProperty.name} (int)");
                if (property.stringValue.Equals(inputProperty.name))
                {
                    selectedIndex = i + indexOffset;
                }
            }
            indexOffset += count;
            
            count = config.floatProperties.Count;
            for (int i = 0; i < count; i++)
            {
                var inputProperty = config.floatProperties[i];
                properties.Add(inputProperty.name);
                options.Add($"{inputProperty.name} (float)");
                if (property.stringValue.Equals(inputProperty.name))
                {
                    selectedIndex = i + indexOffset;
                }
            }
            indexOffset += count;
            
            count = config.vectorProperties.Count;
            for (int i = 0; i < count; i++)
            {
                var inputProperty = config.vectorProperties[i];
                properties.Add(inputProperty.name);
                options.Add($"{inputProperty.name} (Vector4)");
                if (property.stringValue.Equals(inputProperty.name))
                {
                    selectedIndex = i + indexOffset;
                }
            }
            
            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, 
                options.ToArray());
            
            property.stringValue = selectedIndex == -1 ? "None" : properties[selectedIndex];
        }
    }
}