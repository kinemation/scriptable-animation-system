// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using UnityEditor;

namespace KINEMATION.FPSAnimationFramework.Editor.Core
{
    [CustomEditor(typeof(FPSBoneController), true)]
    public class FPSBoneControllerEditor : UnityEditor.Editor
    {
        private FPSBoneController _boneController;
        
        private void OnEnable()
        {
            _boneController = target as FPSBoneController;
        }
    }
}