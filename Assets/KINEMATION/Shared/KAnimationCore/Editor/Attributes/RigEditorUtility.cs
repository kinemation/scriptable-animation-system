// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Reflection;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Attributes
{
    public class RigEditorUtility
    {
        public static IRigProvider TryGetRigProvider(FieldInfo fieldInfo, SerializedProperty property)
        {
            IRigProvider provider = null;

            RigAssetSelectorAttribute assetAttribute = null;
            foreach (var customAttribute in fieldInfo.GetCustomAttributes(false))
            {
                if (customAttribute is RigAssetSelectorAttribute)
                {
                    assetAttribute = customAttribute as RigAssetSelectorAttribute;
                }
            }
            
            if (assetAttribute != null && !string.IsNullOrEmpty(assetAttribute.assetName))
            {
                if (property.serializedObject.FindProperty(assetAttribute.assetName) is var prop)
                {
                    provider = prop.objectReferenceValue as IRigProvider;
                }
            }

            if (provider == null)
            {
                provider = property.serializedObject.targetObject as IRigProvider;
            }

            if (provider == null && property.serializedObject.targetObject is MonoBehaviour component)
            {
                provider = component.GetComponentInChildren<IRigProvider>();
            }

            return provider;
        }
    }
}