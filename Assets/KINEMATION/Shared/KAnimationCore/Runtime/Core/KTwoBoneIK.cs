// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    public struct KTwoBoneIkData
    {
        public KTransform root;
        public KTransform mid;
        public KTransform tip;
        public KTransform target;
        public KTransform hint;

        public float posWeight;
        public float rotWeight;
        public float hintWeight;

        public bool hasValidHint;
    }
    
    public class KTwoBoneIK
    {
        public static void Solve(ref KTwoBoneIkData ikData)
        {
            Vector3 aPosition = ikData.root.position;
            Vector3 bPosition = ikData.mid.position;
            Vector3 cPosition = ikData.tip.position;
            
            Vector3 tPosition = Vector3.Lerp(cPosition, ikData.target.position, ikData.posWeight);
            Quaternion tRotation = Quaternion.Slerp(ikData.tip.rotation, ikData.target.rotation, ikData.rotWeight);
            bool hasHint = ikData.hasValidHint && ikData.hintWeight > 0f;

            Vector3 ab = bPosition - aPosition;
            Vector3 bc = cPosition - bPosition;
            Vector3 ac = cPosition - aPosition;
            Vector3 at = tPosition - aPosition;

            float abLen = ab.magnitude;
            float bcLen = bc.magnitude;
            float acLen = ac.magnitude;
            float atLen = at.magnitude;

            float oldAbcAngle = KMath.TriangleAngle(acLen, abLen, bcLen);
            float newAbcAngle = KMath.TriangleAngle(atLen, abLen, bcLen);

            // Bend normal strategy is to take whatever has been provided in the animation
            // stream to minimize configuration changes, however if this is collinear
            // try computing a bend normal given the desired target position.
            // If this also fails, try resolving axis using hint if provided.
            Vector3 axis = Vector3.Cross(ab, bc);
            if (axis.sqrMagnitude < KMath.SqrEpsilon)
            {
                axis = hasHint ? Vector3.Cross(ikData.hint.position - aPosition, bc) : Vector3.zero;

                if (axis.sqrMagnitude < KMath.SqrEpsilon)
                    axis = Vector3.Cross(at, bc);

                if (axis.sqrMagnitude < KMath.SqrEpsilon)
                    axis = Vector3.up;
            }

            axis = Vector3.Normalize(axis);

            float a = 0.5f * (oldAbcAngle - newAbcAngle);
            float sin = Mathf.Sin(a);
            float cos = Mathf.Cos(a);
            Quaternion deltaR = new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos);

            KTransform localTip = ikData.mid.GetRelativeTransform(ikData.tip, false);
            ikData.mid.rotation = deltaR * ikData.mid.rotation;
            
            // Update child transform.
            ikData.tip = ikData.mid.GetWorldTransform(localTip, false);
            
            cPosition = ikData.tip.position;
            ac = cPosition - aPosition;

            KTransform localMid = ikData.root.GetRelativeTransform(ikData.mid, false);
            localTip = ikData.mid.GetRelativeTransform(ikData.tip, false);
            ikData.root.rotation = KMath.FromToRotation(ac, at) * ikData.root.rotation;

            // Update child transforms.
            ikData.mid = ikData.root.GetWorldTransform(localMid, false);
            ikData.tip = ikData.mid.GetWorldTransform(localTip, false);

            if (hasHint)
            {
                float acSqrMag = ac.sqrMagnitude;
                if (acSqrMag > 0f)
                {
                    bPosition = ikData.mid.position;
                    cPosition = ikData.tip.position;
                    ab = bPosition - aPosition;
                    ac = cPosition - aPosition;

                    Vector3 acNorm = ac / Mathf.Sqrt(acSqrMag);
                    Vector3 ah = ikData.hint.position - aPosition;
                    Vector3 abProj = ab - acNorm * Vector3.Dot(ab, acNorm);
                    Vector3 ahProj = ah - acNorm * Vector3.Dot(ah, acNorm);

                    float maxReach = abLen + bcLen;
                    if (abProj.sqrMagnitude > (maxReach * maxReach * 0.001f) && ahProj.sqrMagnitude > 0f)
                    {
                        Quaternion hintR = KMath.FromToRotation(abProj, ahProj);
                        hintR.x *= ikData.hintWeight;
                        hintR.y *= ikData.hintWeight;
                        hintR.z *= ikData.hintWeight;
                        hintR = KMath.NormalizeSafe(hintR);
                        ikData.root.rotation = hintR * ikData.root.rotation;
                        
                        ikData.mid = ikData.root.GetWorldTransform(localMid, false);
                        ikData.tip = ikData.mid.GetWorldTransform(localTip, false);
                    }
                }
            }
            
            ikData.tip.rotation = tRotation;
        }
    }
}