// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Collections.Generic;
using UnityEngine;

namespace Demo.Scripts.Runtime
{
    [CreateAssetMenu(fileName = "NewFPSControllerSettings", menuName = MenuName)]
    public class FPSControllerSettings : ScriptableObject, IRigUser
    {
        private const string MenuName = "FPS Animator Demo/FPS Controller Settings";
        
        public KRig rigAsset;
        
        [Tab("Animation")] 
        
        [Header("Unarmed State")]
        [SerializeField] public FPSAnimatorProfile unarmedProfile;
        
        [Header("Turn In Place")]
        public float turnInPlaceAngle;
        public AnimationCurve turnCurve = new AnimationCurve(new Keyframe(0f, 0f));
        [Min(0f)] public float turnSpeed = 1f;
        
        [Tab("Controller")] 
        
        [Header("General")] 
        [Min(0f)] public float timeScale = 1f;
        [Min(0f)] public float equipDelay;
        [Range(0f, 90f)] public float leanAngle = 25f;

        [Header("Camera")]
        [Min(0f)] public float sensitivity;
        
        [Tab("Weapon")] 
        public KRigElement weaponBone = new KRigElement(-1, FPSANames.IkWeaponBone);
        public List<GameObject> weaponPrefabs;
        
        public KRig GetRigAsset()
        {
            return rigAsset;
        }
    }
}