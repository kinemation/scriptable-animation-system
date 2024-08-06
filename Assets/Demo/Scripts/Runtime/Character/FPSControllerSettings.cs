// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.IkMotionLayer;
using KINEMATION.KAnimationCore.Runtime.Attributes;
using KINEMATION.KAnimationCore.Runtime.Rig;

using System.Collections.Generic;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using UnityEngine;

namespace Demo.Scripts.Runtime.Character
{
    [CreateAssetMenu(fileName = "NewFPSControllerSettings", menuName = MenuName)]
    public class FPSControllerSettings : ScriptableObject, IRigUser
    {
        private const string MenuName = "FPS Animator Demo/FPS Controller Settings";
        
        public KRig rigAsset;
        
        [Tab("Animation")] 
        
        [Header("Unarmed State")]
        [SerializeField] public RuntimeAnimatorController unarmedController;

        [Header("IK Motions")]
        public IkMotionLayerSettings aimingMotion;
        public IkMotionLayerSettings crouchingMotion;
        public IkMotionLayerSettings jumpingMotion;
        public IkMotionLayerSettings stopMotion;
        public IkMotionLayerSettings leanMotion;
        
        [Tab("Controller")] 
        
        [Header("General")] 
        [Min(0f)] public float timeScale = 1f;
        [Min(0f)] public float equipDelay;
        [Range(0f, 90f)] public float leanAngle = 25f;

        [Header("Camera")]
        [Min(0f)] public float sensitivity = 1f;
        
        [Tab("Weapon")] 
        public KRigElement weaponBone = new KRigElement(-1, FPSANames.IkWeaponBone);
        public List<GameObject> weaponPrefabs;
        
        public KRig GetRigAsset()
        {
            return rigAsset;
        }
    }
}