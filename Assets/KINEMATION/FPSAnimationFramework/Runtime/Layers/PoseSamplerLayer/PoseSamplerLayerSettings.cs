// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.PoseSamplerLayer
{
    public class PoseSamplerLayerSettings : FPSAnimatorLayerSettings
    {
        [Header("General")]
        public FPSAnimationAsset poseToSample;
        public KTransform defaultWeaponPose = KTransform.Identity;
        public KTransform weaponBoneOffset = KTransform.Identity;
        public bool overwriteRoot = false;
        public bool overwriteWeaponBone = false;
        
        [Header("IK Targets")]
        public KRigElement ikWeaponBone = new KRigElement(-1, FPSANames.IkWeaponBone);
        public KRigElement ikHandRight = new KRigElement(-1, FPSANames.IkRightHand);
        public KRigElement ikHandLeft = new KRigElement(-1, FPSANames.IkLeftHand);
        
        public KRigElement ikHandRightHint = new KRigElement(-1, FPSANames.IkRightElbow);
        public KRigElement ikHandLeftHint = new KRigElement(-1, FPSANames.IkLeftElbow);
        
        [Header("Weapon Bone")]
        public KRigElement weaponBoneRight = new KRigElement(-1, FPSANames.IkWeaponBoneRight);
        public KRigElement weaponBoneLeft = new KRigElement(-1, FPSANames.IkWeaponBoneLeft);
        public KRigElement weaponBone = new KRigElement(-1, FPSANames.WeaponBone);
        
        [Header("Spine")]
        public KRigElement pelvis;
        public KRigElement spineRoot;
        
        [Header("Input Properties")]
        [CurveSelector(false, false)]
        public string stabilizationWeight = FPSANames.StabilizationWeight;
        [CurveSelector(false, true, false)]
        public string weaponBoneWeight = FPSANames.Curve_WeaponBoneWeight;

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new PoseSamplerJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            UpdateRigElement(ref ikWeaponBone);
            UpdateRigElement(ref ikHandRight);
            UpdateRigElement(ref ikHandLeft);
            
            UpdateRigElement(ref ikHandRightHint);
            UpdateRigElement(ref ikHandLeftHint);
            
            UpdateRigElement(ref weaponBoneRight);
            UpdateRigElement(ref weaponBoneLeft);
            UpdateRigElement(ref weaponBone);
            
            UpdateRigElement(ref pelvis);
            UpdateRigElement(ref spineRoot);
        }
#endif
    }
}