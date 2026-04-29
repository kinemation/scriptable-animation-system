// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Editor.Widgets;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Rig
{
    [CustomEditor(typeof(KRig), true)]
    public class KRigEditor : UnityEditor.Editor
    {
        private KRig _rigAsset;
        private KRigComponent _rigComponent;

        private SerializedProperty _rigElementChains;
        private SerializedProperty _rigCurves;

        private KToolbarWidget _kToolbarWidget;
        private RigTreeWidget _rigTreeWidget;
        
        private void RenderHierarchy()
        {
            _rigTreeWidget.Render();
        }

        private void RenderElementChains()
        {
            EditorGUILayout.PropertyField(_rigElementChains);
        }

        private void RenderCurves()
        {
            EditorGUILayout.PropertyField(_rigCurves);
        }
        
        private void OnEnable()
        {
            _rigAsset = (KRig) target;
            
            _rigElementChains = serializedObject.FindProperty("rigElementChains");
            _rigCurves = serializedObject.FindProperty("rigCurves");

            _kToolbarWidget = new KToolbarWidget(new KToolbarTab[]
            {
                new KToolbarTab()
                {
                    name = "Hierarchy",
                    onTabRendered = RenderHierarchy
                },
                new KToolbarTab()
                {
                    name = "Element Chains",
                    onTabRendered = RenderElementChains
                },
                new KToolbarTab()
                {
                    name = "Curves",
                    onTabRendered = RenderCurves
                }
            });
            
            _rigTreeWidget = new RigTreeWidget();
            _rigTreeWidget.Refresh(_rigAsset.GetHierarchy());
        }

        private void ImportRig()
        {
            _rigAsset.ImportRig(_rigComponent);
        }

        public override void OnInspectorGUI()
        {
            _rigComponent = (KRigComponent) EditorGUILayout.ObjectField("Rig Component", 
                _rigComponent, typeof(KRigComponent), true);
            
            _rigAsset.targetAnimator = (RuntimeAnimatorController) EditorGUILayout.ObjectField("Animator", 
                _rigAsset.targetAnimator, typeof(RuntimeAnimatorController), true);
            
            _rigAsset.inputConfig = (UserInputConfig) EditorGUILayout.ObjectField("Input Config", 
                _rigAsset.inputConfig, typeof(UserInputConfig), true);

            if (_rigComponent == null)
            {
                EditorGUILayout.HelpBox("Rig Component not specified", MessageType.Warning);
            }
            else if (GUILayout.Button("Import Rig"))
            {
                ImportRig();
                _rigTreeWidget.Refresh(_rigAsset.GetHierarchy());
            }
            
            _kToolbarWidget.Render();
            serializedObject.ApplyModifiedProperties();
        }
    }
}