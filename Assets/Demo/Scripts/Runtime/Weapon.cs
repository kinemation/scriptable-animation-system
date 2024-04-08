// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;

using System.Collections.Generic;
using UnityEngine;

namespace Demo.Scripts.Runtime
{
    public enum OverlayType
    {
        Default,
        Pistol,
        Rifle
    }
    
    public class Weapon : FPSItem
    {
        [Header("Animations")]
        [SerializeField] private FPSAnimationAsset reloadClip;
        [SerializeField] private FPSAnimationAsset grenadeClip;
        [SerializeField] private OverlayType overlayType;
        
        [Header("Recoil")] 
        [SerializeField] private FPSCameraShake cameraShake;
        [SerializeField] private RecoilAnimData recoilData;
        [Min(0f)] [SerializeField] private float fireRate;

        [SerializeField] private bool supportsAuto;
        [SerializeField] private bool supportsBurst;
        [SerializeField] private int burstLength;
        
        [Header("AimPoints")]
        [SerializeField] private List<Transform> aimPoints;
        
        //~ Controller references

        private Animator _controllerAnimator;
        private UserInputController _userInputController;
        private IPlayablesController _playablesController;
        private FPSCameraController _fpsCameraController;
        private RecoilAnimation _recoilAnimation;
        private FPSAnimator _fpsAnimator;
        private FPSAnimatorEntity _fpsAnimatorEntity;
        
        //~ Controller references
        
        private Animator _weaponAnimator;
        private int _scopeIndex;
        
        private float _lastRecoilTime;
        private int _bursts;
        private FireMode _fireMode = FireMode.Semi;
        
        private static readonly int OverlayType = Animator.StringToHash("OverlayType");
        private static readonly int CurveEquip = Animator.StringToHash("CurveEquip");
        private static readonly int CurveUnequip = Animator.StringToHash("CurveUnequip");
        
        public override void OnEquip()
        {
            _weaponAnimator = GetComponentInChildren<Animator>();
            FPSController controller = transform.parent.root.GetComponentInChildren<FPSController>();
            if (controller == null)
            {
                return;
            }

            _fpsAnimatorEntity = GetComponent<FPSAnimatorEntity>();
            
            _controllerAnimator = controller.GetComponent<Animator>();
            _userInputController = controller.GetComponent<UserInputController>();
            _playablesController = controller.GetComponent<IPlayablesController>();
            _fpsCameraController = controller.GetComponentInChildren<FPSCameraController>();
            _fpsAnimator = controller.GetComponent<FPSAnimator>();
            _recoilAnimation = controller.GetComponent<RecoilAnimation>();
            
            _controllerAnimator.SetFloat(OverlayType, (float) overlayType);
            _fpsAnimator.LinkAnimatorProfile(gameObject);
            
            _recoilAnimation.Init(recoilData, fireRate, _fireMode);
            
            _controllerAnimator.CrossFade(CurveEquip, 0.15f);
        }

        public override void OnUnEquip()
        {
            _controllerAnimator.CrossFade(CurveUnequip, 0.15f);
        }

        public override void OnUnarmedEnabled()
        {
            _controllerAnimator.SetFloat(OverlayType, 0);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 0f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 0f);
        }

        public override void OnUnarmedDisabled()
        {
            _controllerAnimator.SetFloat(OverlayType, (int) overlayType);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 1f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 1f);
            _fpsAnimator.LinkAnimatorProfile(gameObject);
        }

        public override bool OnAimPressed()
        {
            _userInputController.SetValue(FPSANames.IsAiming, true);
            _fpsCameraController.UpdateTargetFOV(60f);
            _recoilAnimation.isAiming = true;
            
            return true;
        }

        public override bool OnAimReleased()
        {
            _userInputController.SetValue(FPSANames.IsAiming, false);
            _fpsCameraController.UpdateTargetFOV(90f);
            _recoilAnimation.isAiming = false;
            
            return true;
        }

        public override bool OnFirePressed()
        {
            // Do not allow firing faster than the allowed fire rate.
            if (Time.unscaledTime - _lastRecoilTime < 60f / fireRate)
            {
                return false;
            }
            
            _lastRecoilTime = Time.unscaledTime;
            _bursts = burstLength;
            OnFire();
            
            return true;
        }

        public override bool OnFireReleased()
        {
            if (_recoilAnimation != null)
            {
                _recoilAnimation.Stop();
            }
            
            CancelInvoke(nameof(OnFire));
            
            return true;
        }

        public override bool OnReload()
        {
            if (reloadClip == null)
            {
                return false;
            }
            
            _playablesController.PlayAnimation(reloadClip, 0f);
            
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Rebind();
                _weaponAnimator.Play("Reload", 0);
            }
            
            return true;
        }

        public override bool OnGrenadeThrow()
        {
            if (grenadeClip == null)
            {
                return false;
            }

            _playablesController.PlayAnimation(grenadeClip, 0f);
            return true;
        }
        
        private void OnFire()
        {
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Play("Fire", 0, 0f);
            }
            
            _fpsCameraController.PlayCameraShake(cameraShake);

            if (_recoilAnimation != null && recoilData != null)
            {
                _recoilAnimation.Play();
            }

            if (_recoilAnimation.fireMode == FireMode.Semi)
            {
                return;
            }
            
            if (_recoilAnimation.fireMode == FireMode.Burst)
            {
                _bursts--;
                
                if (_bursts == 0)
                {
                    OnFireReleased();
                    return;
                }
            }
            
            Invoke(nameof(OnFire), 60f / fireRate);
        }

        public override void OnCycleScope()
        {
            _scopeIndex++;
            _scopeIndex = _scopeIndex > aimPoints.Count - 1 ? 0 : _scopeIndex;
            _fpsAnimatorEntity.defaultAimPoint = aimPoints[_scopeIndex];
        }

        private void CycleFireMode()
        {
            if (_fireMode == FireMode.Semi && supportsBurst)
            {
                _fireMode = FireMode.Burst;
                _bursts = burstLength;
                return;
            }

            if (_fireMode != FireMode.Auto && supportsAuto)
            {
                _fireMode = FireMode.Auto;
                return;
            }

            _fireMode = FireMode.Semi;
        }
        
        public override void OnChangeFireMode()
        {
            CycleFireMode();
            _recoilAnimation.fireMode = _fireMode;
        }
    }
}