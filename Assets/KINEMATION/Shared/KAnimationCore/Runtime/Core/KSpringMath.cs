// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    public struct FloatSpringState
    {
        public float velocity;
        public float error;
        
        public void Reset()
        {
            error = velocity = 0f;
        }
    }
    
    public struct VectorSpringState
    {
        public FloatSpringState x;
        public FloatSpringState y;
        public FloatSpringState z;

        public void Reset()
        {
            x.Reset();
            y.Reset();
            z.Reset();
        }
    }
    
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Core")]
    [Serializable]
    public struct VectorSpring
    {
        public Vector3 damping;
        public Vector3 stiffness;
        public Vector3 speed;
        public Vector3 scale;

        public static VectorSpring identity = new VectorSpring()
        {
            damping = Vector3.zero,
            stiffness = Vector3.zero,
            speed = Vector3.zero,
            scale = Vector3.zero
        };
    }
    
    public class KSpringMath
    {
        public static float FloatSpringInterp(float current, float target, float speed, float criticalDamping,
            float stiffness, float scale, ref FloatSpringState state, float deltaTime)
        {
            float interpSpeed = Mathf.Min(deltaTime * speed, 1f);

            if (!Mathf.Approximately(interpSpeed, 0f))
            {
                float damping = 2 * Mathf.Sqrt(stiffness) * criticalDamping;
                float error = target * scale - current;
                float errorDeriv = error - state.error;
                state.velocity += error * stiffness * interpSpeed + errorDeriv * damping;
                state.error = error;

                float value = current + state.velocity * interpSpeed;
                return value;
            }

            return current;
        }
        
        public static Vector3 VectorSpringInterp(Vector3 current, in Vector3 target, in VectorSpring spring, 
            ref VectorSpringState state, float deltaTime)
        {
            current.x = FloatSpringInterp(current.x, target.x, spring.speed.x, 
                spring.damping.x, spring.stiffness.x, spring.scale.x, ref state.x, deltaTime);
            
            current.y = FloatSpringInterp(current.y, target.y, spring.speed.y, 
                spring.damping.y, spring.stiffness.y, spring.scale.y, ref state.y, deltaTime);
            
            current.z = FloatSpringInterp(current.z, target.z, spring.speed.z, 
                spring.damping.z, spring.stiffness.z, spring.scale.z, ref state.z, deltaTime);

            return current;
        }
    }
}