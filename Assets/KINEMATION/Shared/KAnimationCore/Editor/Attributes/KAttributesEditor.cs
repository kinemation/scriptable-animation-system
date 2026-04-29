// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Attributes
{
    public class KAttributesEditor
    {
        public static T GetComponent<T>(SerializedProperty property) where T : class
        {
            Object targetObject = property.serializedObject.targetObject;

            Component targetComponent = targetObject as Component;
            if (targetComponent != null)
            {
                return targetComponent.GetComponentInChildren<T>();
            }

            return null;
        }
    }
}