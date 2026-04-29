// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.AttachHandLayer
{
    [CreateAssetMenu(fileName = "NewAttachHandLayer", menuName = FPSANames.FileMenuLayers + "Attach Hand")]
    public class AttachHandLayerSettings : FPSAnimatorLayerSettings
    {
        [Tooltip("Use this for attachments, e.g. grips.")]
        public AnimationClip customHandPose;
        public KRigElement handBone;
        public KRigElement ikHandBone;
        public KRigElement ikWeaponBone = new KRigElement(-1, FPSANames.IkWeaponBone);
        public KRigElement weaponBone = new KRigElement(-1, FPSANames.WeaponBone);
        [ElementChainSelector] public string elementChainName;
        
        public KTransform handPoseOffset = KTransform.Identity;
        [Range(0f, 1f)] public float overridePoseWeight = 0f;

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new AttachHandLayerJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            UpdateRigElement(ref handBone);
            UpdateRigElement(ref ikHandBone);
            UpdateRigElement(ref ikWeaponBone);
            UpdateRigElement(ref weaponBone);
        }
#endif
    }
}