// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using System.Linq;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(CurveSelectorAttribute))]
    public class CurveSelectorDrawer : PropertyDrawer
    {
        private void AddAnimatorNames(ref List<string> options, RuntimeAnimatorController controller)
        {
            if (controller == null)
            {
                return;
            }
            
            AnimatorController animatorController = controller as AnimatorController;
            if (animatorController != null)
            {
                var parameters = animatorController.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].type != AnimatorControllerParameterType.Float)
                    {
                        continue;
                    }
                    
                    options.Add($"{parameters[i].name} (Animator)");
                }
            }
        }

        private void AddInputNames(ref List<string> options, UserInputConfig config)
        {
            if (config == null) return;
            
            foreach (var property in config.floatProperties)
            {
                options.Add($"{property.name} (Input)");
            }
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CurveSelectorAttribute curveAttribute = attribute as CurveSelectorAttribute;
            if (curveAttribute == null)
            {
                return;
            }
            
            KRig rig = (property.serializedObject.targetObject as IRigUser)?.GetRigAsset();
            if (rig == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            SerializedProperty name = property.FindPropertyRelative("name");
            SerializedProperty source = property.FindPropertyRelative("source");
            SerializedProperty mode = property.FindPropertyRelative("mode");
            SerializedProperty clampMin = property.FindPropertyRelative("clampMin");

            List<string> options = new List<string>();
            options.Add("None");

            if (rig != null)
            {
                if (curveAttribute.useAnimator)
                {
                    // Add the Animator curves.
                    AddAnimatorNames(ref options, rig.targetAnimator);
                }

                if (curveAttribute.usePlayables)
                {
                    // Add the Playables curves.
                    foreach (var curve in rig.rigCurves)
                    {
                        options.Add($"{curve} (Playables)");
                    }
                }

                // Add the Input parameters.
                if (curveAttribute.useInput)
                {
                    AddInputNames(ref options, rig.inputConfig);
                }
            }
            
            name ??= property;
            
            int index = options.IndexOf(name.stringValue);

            if (index < 0)
            {
                index = options.ToList().IndexOf(name.stringValue + " (Animator)");
            }

            if (index < 0)
            {
                index = options.ToList().IndexOf(name.stringValue + " (Playables)");
            }
            
            if (index < 0)
            {
                index = options.ToList().IndexOf(name.stringValue + " (Input)");
            }

            Rect propertyRect = position;
            propertyRect.height = EditorGUIUtility.singleLineHeight;
            
            index = EditorGUI.Popup(propertyRect, label.text, index, options.ToArray());
            string selection = index >= 0 ? options[index] : "None";

            if (source != null)
            {
                if (selection.EndsWith("(Animator)"))
                {
                    source.intValue = 0;
                }
                
                if (selection.EndsWith("(Playables)"))
                {
                    source.intValue = 1;
                }
                
                if (selection.EndsWith("(Input)"))
                {
                    source.intValue = 2;
                }
            }
            
            selection = selection.Replace(" (Playables)", "");
            selection = selection.Replace(" (Animator)", "");
            selection = selection.Replace(" (Input)", "");
            
            name.stringValue = selection;
            
            if (mode != null)
            {
                propertyRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(propertyRect, mode);
            }

            if (clampMin != null)
            {
                propertyRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(propertyRect, clampMin);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty name = property.FindPropertyRelative("name");
            SerializedProperty mode = property.FindPropertyRelative("mode");
            SerializedProperty clampMin = property.FindPropertyRelative("clampMin");

            if (name == null || mode == null || clampMin == null)
            {
                return base.GetPropertyHeight(property, label);
            }

            return EditorGUIUtility.singleLineHeight * 3f + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}