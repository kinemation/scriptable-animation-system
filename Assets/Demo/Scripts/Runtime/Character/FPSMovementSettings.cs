// Designed by KINEMATION, 2024.

using System;
using UnityEngine;

namespace Demo.Scripts.Runtime.Character
{
    [Serializable]
    public struct GaitSettings
    {
        // Player movement velocity.
        public float velocity;
        // Velocity vector interpolation speed.
        public float velocitySmoothing;
    }
    
    [CreateAssetMenu(fileName = "NewMovementSettings", menuName = "FPS Animator Demo/FPS Movement Settings", order = 0)]
    public class FPSMovementSettings : ScriptableObject
    {
        [Header("Settings")]
        public GaitSettings idle;
        public GaitSettings prone;
        public GaitSettings crouching;
        public GaitSettings walking;
        public GaitSettings sprinting;
        public AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Range(0f, 1f)] public float crouchRatio = 0.5f;
        [Min(0f)] public float proneTransitionDuration = 0f;
        
        public float jumpHeight = 9f;
        [Range(0f, 1f)] public float airFriction = 0f;
        public float airVelocity = 0f;
        public float maxFallVelocity = 0f;
        public float gravity = 9.81f;

        [Header("Sliding")]
        public AnimationCurve slideCurve = AnimationCurve.Constant(0f, 1f, 0f);
        [Min(0f)] public float slideSpeed = 1f;
        public float slideDirectionSmoothing = 0f;
    }
}