// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using UnityEngine;
using UnityEngine.Animations;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.WeaponLayer
{
    public struct WeaponLayerJobData
    {
        public TransformStreamHandle weaponHandle;
        public TransformStreamHandle rightElbowHandle;
        public TransformStreamHandle leftElbowHandle;
        public WeaponLayerSettings weaponSettings;

        public KTransform cachedWeapon;
        public KTransform cachedRightElbow;
        public KTransform cachedLeftElbow;

        public void Setup(LayerJobData jobData, WeaponLayerSettings settings)
        {
            var transform = jobData.rigComponent.GetRigTransform(settings.weaponIkBone);
            weaponHandle = jobData.animator.BindStreamTransform(transform);

            transform = jobData.rigComponent.GetRigTransform(settings.rightHandElbow);
            rightElbowHandle = jobData.animator.BindStreamTransform(transform);

            transform = jobData.rigComponent.GetRigTransform(settings.leftHandElbow);
            leftElbowHandle = jobData.animator.BindStreamTransform(transform);

            weaponSettings = settings;
        }

        public void Cache(AnimationStream stream)
        {
            cachedWeapon = AnimLayerJobUtility.GetTransformFromHandle(stream, weaponHandle);
            cachedRightElbow = AnimLayerJobUtility.GetTransformFromHandle(stream, rightElbowHandle);
            cachedLeftElbow = AnimLayerJobUtility.GetTransformFromHandle(stream, leftElbowHandle);
        }

        public void PostProcessPose(AnimationStream stream, float weight)
        {
            var weapon = AnimLayerJobUtility.GetTransformFromHandle(stream, weaponHandle);
            var rightElbow = AnimLayerJobUtility.GetTransformFromHandle(stream, rightElbowHandle);
            var leftElbow = AnimLayerJobUtility.GetTransformFromHandle(stream, leftElbowHandle);

            rightElbow = KTransform.Lerp(cachedRightElbow, rightElbow, weaponSettings.hintTargetWeight);
            leftElbow = KTransform.Lerp(cachedLeftElbow, leftElbow, weaponSettings.hintTargetWeight);
            
            rightElbowHandle.SetPosition(stream, rightElbow.position);
            rightElbowHandle.SetRotation(stream, rightElbow.rotation);
            
            leftElbowHandle.SetPosition(stream, leftElbow.position);
            leftElbowHandle.SetRotation(stream, leftElbow.rotation);

            var delta = cachedWeapon.GetRelativeTransform(weapon, false);
            Vector3 offset = weaponSettings.animatedPivotOffset;
            offset = delta.rotation * offset - offset;
            
            AnimLayerJobUtility.MoveInSpace(stream, weaponHandle, weaponHandle, offset, weight);
        }
    }
    
    public abstract class WeaponLayerSettings : FPSAnimatorLayerSettings
    {
        [Header("Weapon Layer General")]
        public KRigElement weaponIkBone = new KRigElement(-1, FPSANames.IkWeaponBone);
        public KRigElement rightHandElbow = new KRigElement(-1, FPSANames.IkRightElbow);
        public KRigElement leftHandElbow = new KRigElement(-1, FPSANames.IkLeftElbow);
        
        public Vector3 animatedPivotOffset = Vector3.zero;

        [Tooltip("How much we want to affect the elbows: 1 - fully affected, 0 - no effect.")]
        [Range(0f, 1f)] public float hintTargetWeight = 1f;

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            UpdateRigElement(ref weaponIkBone);
            UpdateRigElement(ref rightHandElbow);
            UpdateRigElement(ref leftHandElbow);
        }
#endif
    }
}