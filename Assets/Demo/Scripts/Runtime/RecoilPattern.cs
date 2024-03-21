// Designed by KINEMATION, 2023

using UnityEngine;

namespace Demo.Scripts.Runtime
{
    [CreateAssetMenu(fileName = "NewRecoilPattern", menuName = "FPS Animator Demo/Recoil Pattern")]
    public class RecoilPattern : ScriptableObject
    {
        [Min(0f)] public float smoothing = 0f;
        public float acceleration = 0f;
        public float step = 0f;
        public Vector2 horizontalVariation = Vector3.zero;
        [Range(0f, 1f)] public float aimRatio = 0f;
        [Range(0f, 1f)] public float cameraWeight = 0f;
        [Min(0f)] public float cameraRestoreSpeed;
    }
}