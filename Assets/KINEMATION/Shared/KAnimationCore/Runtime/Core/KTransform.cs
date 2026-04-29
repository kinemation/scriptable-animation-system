// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    // Struct alternative for Transform.
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Core")]
    [Serializable]
    public struct KTransform
    {
        public static KTransform Identity = new(Vector3.zero, Quaternion.identity, Vector3.one);
        public Vector3 forward => rotation * Vector3.forward;
        public Vector3 up => rotation * Vector3.up;
        public Vector3 right => rotation * Vector3.right;
        
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        
        public KTransform(Vector3 newPos, Quaternion newRot, Vector3 newScale)
        {
            position = newPos;
            rotation = newRot;
            scale = newScale;
        }
        
        public KTransform(Vector3 newPos, Quaternion newRot)
        {
            position = newPos;
            rotation = newRot;
            scale = Vector3.one;
        }

        public KTransform(Transform t, bool worldSpace = true)
        {
            if (worldSpace)
            {
                position = t.position;
                rotation = t.rotation;
            }
            else
            {
                position = t.localPosition;
                rotation = t.localRotation;
            }
            
            scale = t.localScale;
        }
        
        // Linearly interpolates translation and scale. Spherically interpolates rotation.
        public static KTransform Lerp(KTransform a, KTransform b, float alpha)
        {
            Vector3 outPos = Vector3.Lerp(a.position, b.position, alpha);
            Quaternion outRot = Quaternion.Slerp(a.rotation, b.rotation, alpha);
            Vector3 outScale = Vector3.Lerp(a.scale, b.scale, alpha);

            return new KTransform(outPos, outRot, outScale);
        }

        public static KTransform EaseLerp(KTransform a, KTransform b, float alpha, EaseMode easeMode)
        {
            return Lerp(a, b, KCurves.Ease(0f, 1f, alpha, easeMode));
        }

        // Frame-rate independent interpolation.
        public static KTransform ExpDecay(KTransform a, KTransform b, float speed, float deltaTime)
        {
            return Lerp(a, b, KMath.ExpDecayAlpha(speed, deltaTime));
        }

        public bool Equals(KTransform other, bool useScale)
        {
            bool result = position.Equals(other.position) && rotation.Equals(other.rotation);

            if (useScale)
            {
                result = result && scale.Equals(other.scale);
            }

            return result;
        }

        // Returns a point relative to this transform.
        public Vector3 InverseTransformPoint(Vector3 worldPosition, bool useScale)
        {
            Vector3 result = Quaternion.Inverse(rotation) * (worldPosition - position);

            if (useScale)
            {
                result = Vector3.Scale(scale, result);
            }

            return result;
        }

        // Returns a vector relative to this transform.
        public Vector3 InverseTransformVector(Vector3 worldDirection, bool useScale)
        {
            Vector3 result = Quaternion.Inverse(rotation) * worldDirection;
            
            if (useScale)
            {
                result = Vector3.Scale(scale, result);
            }
            
            return result;
        }

        // Converts a local position from this transform to world.
        public Vector3 TransformPoint(Vector3 localPosition, bool useScale)
        {
            if (useScale)
            {
                localPosition = Vector3.Scale(scale, localPosition);
            }

            return position + rotation * localPosition;
        }

        // Converts a local vector from this transform to world.
        public Vector3 TransformVector(Vector3 localDirection, bool useScale)
        {
            if (useScale)
            {
                localDirection = Vector3.Scale(scale, localDirection);
            }

            return rotation * localDirection;
        }

        // Returns a transform relative to this transform.
        public KTransform GetRelativeTransform(KTransform worldTransform, bool useScale)
        {
            return new KTransform()
            {
                position = InverseTransformPoint(worldTransform.position, useScale),
                rotation = Quaternion.Inverse(rotation) * worldTransform.rotation,
                scale = Vector3.Scale(scale, worldTransform.scale)
            };
        }

        // Converts a local transform to world.
        public KTransform GetWorldTransform(KTransform localTransform, bool useScale)
        {
            return new KTransform()
            {
                position = TransformPoint(localTransform.position, useScale),
                rotation = rotation * localTransform.rotation,
                scale = Vector3.Scale(scale, localTransform.scale)
            };
        }
    }
}