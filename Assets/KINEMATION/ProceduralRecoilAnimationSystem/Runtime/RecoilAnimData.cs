// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using UnityEngine;

namespace KINEMATION.ProceduralRecoilAnimationSystem.Runtime
{
    [Serializable]
    public struct RecoilProgression
    {
        public float acceleration;
        public float damping;
        public float amount;
    }

    [Serializable]
    public struct RecoilSway
    {
        public Vector2 pitchSway;
        public Vector2 yawSway;
        public float rollMultiplier;
        public float damping;
        public float acceleration;
        [Range(0f, 1f)] public float adsScale;
        public Vector3 pivotOffset;
    }
    
    [CreateAssetMenu(fileName = "NewRecoilAnimData", menuName = "KINEMATION/Procedural Recoil/Recoil Data")]
    public class RecoilAnimData : ScriptableObject
    {
        [Tab("Recoil Targets")]
        
        [Header("Rotation Targets")]
        public Vector2 pitch;
        public Vector4 roll = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 yaw = new Vector4(0f, 0f, 0f, 0f);

        [Header("Translation Targets")] 
        public Vector2 kickback;
        public Vector2 kickUp;
        public Vector2 kickRight;
    
        [Header("Aiming Multipliers")]
        public Vector3 aimRot;
        public Vector3 aimLoc;
        
        [Tab("Smoothing")]
    
        [Header("Auto/Burst Settings")]
        public Vector3 smoothRot;
        public Vector3 smoothLoc;
        
        public Vector3 extraRot;
        public Vector3 extraLoc;
        
        [Tab("Layers")]
    
        [Header("Noise Layer")]
        public Vector2 noiseX;
        public Vector2 noiseY;

        public Vector2 noiseAccel;
        public Vector2 noiseDamp;
    
        [Range(0f, 1f)] public float noiseScalar = 1f;
    
        [Header("Pushback Layer")]
        public float pushAmount = 0f;
        public float pushAccel;
        public float pushDamp;

        [Header("Recoil Sway")]
        public RecoilSway recoilSway;

        [Header("Progression")]
        public RecoilProgression pitchProgress;
        public RecoilProgression upProgress;
        [Range(0f, 1f)] public float adsProgressAlpha = 1f;
        
        [Tab("Misc")]
        
        [Header("Controller Recoil")]
        public Vector2 horizontalRecoil = Vector2.zero;
        public Vector2 verticalRecoil = Vector2.zero;
        
        [Min(0f)] public float horizontalSmoothing = 0f;
        [Min(0f)] public float verticalSmoothing = 0f;

        [Min(0f)] public float damping = 0f;
        
        [Header("Recoil Motion")]
        public Vector3 hipPivotOffset;
        public Vector3 aimPivotOffset;
        public bool smoothRoll;
        [Min(0f)] public float playRate;
        
        [Header("Curves")]
        public RecoilCurves recoilCurves = new RecoilCurves(
            new[] { new Keyframe(0f, 0f), new Keyframe(1f, 0f) });
    }
}