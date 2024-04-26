// Designed by KINEMATION, 2024.

using Demo.Scripts.Runtime.Character;
using KINEMATION.KAnimationCore.Editor.Misc;
using UnityEditor;

namespace Demo.Scripts.Editor
{
    [CustomEditor(typeof(FPSControllerSettings), true)]
    public class FPSControllerSettingsEditor : UnityEditor.Editor
    {
        private TabInspectorWidget _tabInspectorWidget;
        
        private void OnEnable()
        {
            _tabInspectorWidget = new TabInspectorWidget(serializedObject);
            _tabInspectorWidget.Init();
        }
        
        public override void OnInspectorGUI()
        {
            _tabInspectorWidget.Render();
        }
    }
}
