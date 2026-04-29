// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Editor.Tools;
using KINEMATION.Shared.KAnimationCore.Editor.Widgets;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using UnityEditor;

namespace KINEMATION.ProceduralRecoilAnimationSystem.Editor
{
    [CustomEditor(typeof(RecoilAnimData))]
    public class RecoilAnimDataEditor : UnityEditor.Editor
    {
        private TabInspectorWidget _tabInspectorWidget;

        private void OnEnable()
        {
            _tabInspectorWidget = new TabInspectorWidget(serializedObject);
            _tabInspectorWidget.Init();
        }

        public override void OnInspectorGUI()
        {
            _tabInspectorWidget.OnGUI();
        }
    }
}