// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.WeaponLayer;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using System;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.AdsLayer
{
    [Serializable]
    public struct AdsBlend
    {
        [Range(0f, 1f)] public float x;
        [Range(0f, 1f)] public float y;
        [Range(0f, 1f)] public float z;
    }
    
    public class AdsLayerSettings : WeaponLayerSettings
    {
        public KRigElement aimTargetBone;
        public EaseMode aimingEaseMode;

        public AdsBlend positionBlend;
        public AdsBlend rotationBlend;

        [Min(0.01f)] public float aimingSpeed = 1f;

        public EaseMode aimPointEaseMode;
        [Min(0.01f)] public float aimPointSpeed = 1f;
        
        [Range(0f, 1f)] public float cameraBlend = 0f;
        
        [InputProperty] public string isAimingProperty = FPSANames.IsAiming; 
        
        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new AdsLayerJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            base.OnRigUpdated();
            UpdateRigElement(ref aimTargetBone);
        }
#endif
    }
}