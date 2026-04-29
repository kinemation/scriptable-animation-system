using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.IkLayer
{
    public class IkLayerSettings : FPSAnimatorLayerSettings
    {
        [Header("Hands IK")]
        public KRigElement rightHand;
        public KRigElement leftHand;
        
        public KRigElement rightHandIk = new KRigElement(-1, FPSANames.IkRightHand);
        public KRigElement leftHandIk = new KRigElement(-1, FPSANames.IkLeftHand);
        
        public KRigElement rightHandHint = new KRigElement(-1, FPSANames.IkRightElbow);
        public KRigElement leftHandHint = new KRigElement(-1, FPSANames.IkLeftElbow);
        
        [Header("Foot IK")]
        public KRigElement rightFoot;
        public KRigElement leftFoot;
        
        public KRigElement rightFootIk = new KRigElement(-1, FPSANames.IkRightFoot);
        public KRigElement leftFootIk = new KRigElement(-1, FPSANames.IkLeftFoot);
        
        public KRigElement rightFootHint = new KRigElement(-1, FPSANames.IkRightKnee);
        public KRigElement leftFootHint = new KRigElement(-1, FPSANames.IkLeftKnee);

        [Header("Humanoid IK")]
        [Tooltip("If true, the layer will rotate feet around the root to compensate the turn rotation.")]
        public bool offsetFeetTargets = true;

        [Header("Control Weight")]
        [Range(0f, 1f)] public float rightHandWeight = 1f;
        [Range(0f, 1f)] public float leftHandWeight = 1f;
        [Range(0f, 1f)] public float rightFootWeight = 1f;
        [Range(0f, 1f)] public float leftFootWeight = 1f;

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new IkLayerJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            UpdateRigElement(ref rightHand);
            UpdateRigElement(ref leftHand);

            UpdateRigElement(ref rightHandIk);
            UpdateRigElement(ref leftHandIk);

            UpdateRigElement(ref rightHandHint);
            UpdateRigElement(ref leftHandHint);

            UpdateRigElement(ref rightFoot);
            UpdateRigElement(ref leftFoot);

            UpdateRigElement(ref rightFootIk);
            UpdateRigElement(ref leftFootIk);
            
            UpdateRigElement(ref rightFootHint);
            UpdateRigElement(ref leftFootHint);
        }
#endif
    }
}