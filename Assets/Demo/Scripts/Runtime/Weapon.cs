// Designed by Kinemation, 2023

using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using UnityEngine;

namespace Demo.Scripts.Runtime
{
    public enum OverlayType
    {
        Default,
        Pistol,
        Rifle
    }
    
    public class Weapon : MonoBehaviour
    {
        [Header("Animations")]
        public FPSAnimationAsset reloadClip;
        public FPSAnimationAsset grenadeClip;
        public FPSAnimationAsset fireClip;
        public OverlayType overlayType;
        
        [Header("Aiming")]
        public bool canAim = true;

        [Header("Recoil")] 
        public FPSCameraShake cameraShake;
        public RecoilAnimData recoilData;
        [Min(0f)] public float fireRate;
        public bool isAuto;
        
        private Animator _animator;
        private int _scopeIndex;

        protected void Start()
        {
            _animator = GetComponentInChildren<Animator>();
        }
        
        public void OnFire()
        {
            if (_animator == null)
            {
                return;
            }
            
            _animator.Play("Fire", 0, 0f);
        }

        public void Reload()
        {
            if (_animator == null)
            {
                return;
            }
            
            _animator.Rebind();
            _animator.Play("Reload", 0);
        }
    }
}