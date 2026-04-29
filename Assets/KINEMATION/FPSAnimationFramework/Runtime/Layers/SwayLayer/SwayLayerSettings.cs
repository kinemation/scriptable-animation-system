// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using KINEMATION.FPSAnimationFramework.Runtime.Layers.WeaponLayer;

using UnityEngine;
using System;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.SwayLayer
{
    [Serializable]
    public struct VectorSpring
    {
        public Vector3 damping;
        public Vector3 stiffness;
        public Vector3 speed;
        public Vector3 scale;

        public static VectorSpring Identity = new VectorSpring()
        {
            damping = Vector3.one,
            stiffness = Vector3.one,
            speed = Vector3.one,
            scale = Vector3.one
        };
    }
    
    public class SwayLayerSettings : WeaponLayerSettings
    {
        [Header("Free Aiming")]
        public KRigElement headBone;
        public float freeAimClamp;
        public float freeAimInterpSpeed;
        public float freeAimInputScale;
        public ESpaceType freeAimSpace;

        [InputProperty]
        public string useFreeAimProperty = FPSANames.UseFreeAim;

        [Header("Move Sway")]
        public VectorSpring moveSwayPositionSpring = VectorSpring.Identity;
        public VectorSpring moveSwayRotationSpring = VectorSpring.Identity;
        [Min(0f)] public float moveSwayTargetDamping;
        public ESpaceType moveSwaySpace;
        
        [InputProperty]
        public string moveInputProperty = FPSANames.MoveInput;

        [Header("Aim Sway")] 
        public VectorSpring aimSwayPositionSpring = VectorSpring.Identity;
        public VectorSpring aimSwayRotationSpring = VectorSpring.Identity;
        [Min(0f)] public float aimSwayTargetDamping;
        public ESpaceType aimSwaySpace;
        
        [InputProperty]
        public string mouseDeltaInputProperty = FPSANames.MouseDeltaInput;

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new SwayLayerJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            base.OnRigUpdated();
            UpdateRigElement(ref headBone);
        }
#endif
    }
}