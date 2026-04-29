// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.Shared.KAnimationCore.Editor.Widgets;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Editor.Playables
{
    [CustomEditor(typeof(FPSPlayablesController), true)]
    public class FPSPlayablesControllerEditor : PlayablesControllerBaseEditor
    {
        private AssetObjectWidget<AvatarMask> _maskWidget;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _maskWidget = new AssetObjectWidget<AvatarMask>(serializedObject, "upperBodyMask", 
                "Upper Body Mask");
        }

        public override void OnInspectorGUI()
        {
            _maskWidget.OnInspectorGUI();
            base.OnInspectorGUI();
        }
    }
}