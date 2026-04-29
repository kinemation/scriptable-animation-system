// Designed by KINEMATION, 2024.

using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Camera
{
    [CreateAssetMenu(fileName = "NewCameraShake", menuName = "KINEMATION/FPS Animator General/Camera Shake")]
    public class FPSCameraShake : ScriptableObject
    {
        [Unfold] public VectorCurve shakeCurve = VectorCurve.Constant(0f, 1f, 0f);
        public Vector4 pitch;
        public Vector4 yaw;
        public Vector4 roll;
        [Min(0f)] public float smoothSpeed;
        [Min(0f)] public float playRate = 1f;

        public static float GetTarget(Vector4 value)
        {
            float a = Random.Range(value.x, value.y);
            float b = Random.Range(value.z, value.w);
            return Random.Range(0, 2) == 0 ? a : b;
        }
    }
}