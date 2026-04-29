// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    public class KMath
    {
        public const float FloatMin = 1e-10f;
        public const float SqrEpsilon = 1e-8f;

        public static float Square(float value)
        {
            return value * value;
        }
        
        public static float SqrDistance(Vector3 a, Vector3 b)
        {
            return (b - a).sqrMagnitude;
        }
        
        public static float NormalizeEulerAngle(float angle)
        {
            while (angle < -180f) angle += 360f;
            while (angle >= 180f) angle -= 360f;
            return angle;
        }
        
        public static float TriangleAngle(float aLen, float aLen1, float aLen2)
        {
            float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
            return Mathf.Acos(c);
        }

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            float theta = Vector3.Dot(from.normalized, to.normalized);
            if (theta >= 1f) return Quaternion.identity;

            if (theta <= -1f)
            {
                Vector3 axis = Vector3.Cross(from, Vector3.right);
                if (axis.sqrMagnitude == 0f) axis = Vector3.Cross(from, Vector3.up);

                return Quaternion.AngleAxis(180f, axis);
            }

            return Quaternion.AngleAxis(Mathf.Acos(theta) * Mathf.Rad2Deg, Vector3.Cross(from, to).normalized);
        }

        public static Quaternion NormalizeSafe(Quaternion q)
        {
            float dot = Quaternion.Dot(q, q);
            if (dot > FloatMin)
            {
                float rsqrt = 1.0f / Mathf.Sqrt(dot);
                return new Quaternion(q.x * rsqrt, q.y * rsqrt, q.z * rsqrt, q.w * rsqrt);
            }

            return Quaternion.identity;
        }

        public static float InvLerp(float value, float a, float b)
        {
            float alpha = 0f;

            if (!Mathf.Approximately(a, b))
            {
                alpha = (value - a) / (b - a);
            }

            return Mathf.Clamp01(alpha);
        }
        
        public static float ExpDecayAlpha(float speed, float deltaTime)
        {
            return 1 - Mathf.Exp(-speed * deltaTime);
        }

        public static float FloatInterp(float a, float b, float speed, float deltaTime)
        {
            return speed > 0f ? Mathf.Lerp(a, b, ExpDecayAlpha(speed, deltaTime)) : b;
        }

        public static Quaternion SmoothSlerp(Quaternion a, Quaternion b, float speed, float deltaTime)
        {
            return speed > 0f ? Quaternion.Slerp(a, b, ExpDecayAlpha(speed, deltaTime)) : b;
        }

        public static Vector2 ComputeLookAtInput(Transform root, Transform from, Transform to)
        {
            Vector2 result = Vector2.zero;
            
            Quaternion rot = Quaternion.LookRotation(to.position - from.position);
            rot = Quaternion.Inverse(root.rotation) * rot;

            Vector3 euler = rot.eulerAngles;
            result.x = NormalizeEulerAngle(euler.x);
            result.y = NormalizeEulerAngle(euler.y);

            return result;
        }
    }
}