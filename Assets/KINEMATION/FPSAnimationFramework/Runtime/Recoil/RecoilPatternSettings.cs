// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Recoil
{
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/recoil-system/recoil-pattern")]
    [CreateAssetMenu(fileName = "NewRecoilPattern", menuName = FPSANames.FileMenuGeneral + "Recoil Pattern")]
    public class RecoilPatternSettings : ScriptableObject
    {
        public Vector2 horizontalRecoil;
        public Vector2 verticalRecoil;
        
        [Min(0f)] public float horizontalSmoothing;
        [Min(0f)] public float verticalSmoothing;

        [Min(0f)] public float damping = 0f;
    }
}