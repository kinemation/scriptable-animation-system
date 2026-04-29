// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.ScriptableWidget.Editor;
using UnityEditor;

namespace KINEMATION.FPSAnimationFramework.Editor.Core
{
    [CustomEditor(typeof(FPSAnimatorProfile), true)]
    public class FPSAnimatorProfileEditor : UnityEditor.Editor
    {
        private SerializedProperty _rigAsset;
        private SerializedProperty _blendInTime;
        private SerializedProperty _blendOutTime;
        private SerializedProperty _easeMode;

        private FPSAnimatorProfile _animatorProfile;
        private ScriptableComponentListWidget _listWidget;

        private void MarkLayersStandalone()
        {
            if (_animatorProfile == null || _animatorProfile.settings == null) return;
            foreach (var layer in _animatorProfile.settings) layer.isStandalone = false;
        }
        
        private void OnEnable()
        {
            _animatorProfile = target as FPSAnimatorProfile;

            _rigAsset = serializedObject.FindProperty("rigAsset");
            _blendInTime = serializedObject.FindProperty("blendInTime");
            _blendOutTime = serializedObject.FindProperty("blendOutTime");
            _easeMode = serializedObject.FindProperty("easeMode");
            
            _listWidget = new ScriptableComponentListWidget("Animator Layer");
            _listWidget.Init(serializedObject, typeof(FPSAnimatorLayerSettings), "settings");

            // Update the Rig Asset for all layers.
            _listWidget.onComponentAdded = (int i) => _animatorProfile.OnRigUpdated();
            _listWidget.onComponentPasted = (int i) => _animatorProfile.OnRigUpdated();
            MarkLayersStandalone();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.BeginVertical();

            EditorGUILayout.PropertyField(_rigAsset);
            EditorGUILayout.PropertyField(_blendInTime);
            EditorGUILayout.PropertyField(_blendOutTime);
            EditorGUILayout.PropertyField(_easeMode);
            
            EditorGUILayout.EndVertical();
            
            _listWidget.OnGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}