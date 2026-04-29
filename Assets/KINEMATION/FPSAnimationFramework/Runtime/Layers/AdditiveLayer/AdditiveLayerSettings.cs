// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.WeaponLayer;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.AdditiveLayer
{
    public class AdditiveLayerSettings : WeaponLayerSettings
    {
        [Header("IK Animation")]
        public KRigElement additiveBone = new KRigElement(-1, FPSANames.WeaponBoneAdditive);
        [Min(0f)] public float interpSpeed = 0f;

        [Header("Aiming")] 
        [CurveSelector(false, false)]
        public string aimingInputProperty = FPSANames.AimingWeight;
        [Range(0f, 1f)] public float adsScalar = 1f;

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new AdditiveLayerJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            base.OnRigUpdated();
            UpdateRigElement(ref additiveBone);
        }
#endif
    }
}