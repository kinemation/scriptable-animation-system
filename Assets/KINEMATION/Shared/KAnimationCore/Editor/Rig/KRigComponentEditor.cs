// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Rig
{
    [CustomEditor(typeof(KRigComponent), true)]
    public class KRigComponentEditor : UnityEditor.Editor
    {
        private KRigComponent _rigComponent;
        private int _boneCount = 0;

        private void OnEnable()
        {
            _rigComponent = (KRigComponent) target;
            _boneCount = _rigComponent.GetRigTransforms().Length;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Total bones: " + _boneCount);
            if (GUILayout.Button("Refresh Hierarchy"))
            {
                _rigComponent.RefreshHierarchy();
                _boneCount = _rigComponent.GetRigTransforms().Length;
            }
        }
    }
}