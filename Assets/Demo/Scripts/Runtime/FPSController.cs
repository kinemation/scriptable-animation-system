// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;

using System;
using UnityEngine;
using System.Collections.Generic;
using KINEMATION.KAnimationCore.Runtime.Core;

namespace Demo.Scripts.Runtime
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TabAttribute : PropertyAttribute
    {
        public readonly string tabName;

        public TabAttribute(string tabName)
        {
            this.tabName = tabName;
        }
    }
    
    public enum FPSAimState
    {
        None,
        Ready,
        Aiming,
        PointAiming
    }

    public enum FPSActionState
    {
        None,
        Reloading,
        WeaponChange
    }

    // An example-controller class
    public class FPSController : MonoBehaviour
    {
        //~ Legacy Controller Interface

        [Tab("Animation")] [Header("Unarmed State")] [SerializeField]
        private FPSAnimatorProfile unarmedProfile;
        
        [Header("Turn In Place")]
        [SerializeField] private float turnInPlaceAngle;
        [SerializeField] private AnimationCurve turnCurve = new AnimationCurve(new Keyframe(0f, 0f));
        [SerializeField] private float turnSpeed = 1f;
        
        [Header("General")] 
        [Tab("Controller")]
        [SerializeField] private float timeScale = 1f;
        [SerializeField, Min(0f)] private float equipDelay = 0f;

        [Header("Camera")]
        [SerializeField] [Min(0f)] private float sensitivity;

        [Header("Movement")] 
        [SerializeField] private FPSMovement movementComponent;

        [Tab("Weapon")] 
        [SerializeField] private Transform weaponBone;
        [SerializeField] private List<GameObject> weaponPrefabs;
        private Vector2 _playerInput;

        // Used for free-look
        private Vector2 _freeLookInput;

        private int _activeWeaponIndex;
        private int _previousWeaponIndex;
        
        private int _bursts;
        
        private FPSAimState _aimState;
        private FPSActionState _actionState;
        
        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int OverlayType = Animator.StringToHash("OverlayType");
        private static readonly int TurnRight = Animator.StringToHash("TurnRight");
        private static readonly int TurnLeft = Animator.StringToHash("TurnLeft");
        private static readonly int UnEquip = Animator.StringToHash("UnEquip");
        
        private Quaternion _moveRotation;
        private float _turnProgress = 1f;
        private bool _isTurning = false;
        
        private bool _isUnarmed;
        private float _lastRecoilTime;
        private Animator _animator;
        
        //~ Legacy Controller Interface
        
        // ~Scriptable Animation System Integration
        private FPSAnimator _fpsAnimator;
        private UserInputController _userInput;
        private FPSCameraController _fpsCamera;
        private IPlayablesController _playablesController;
        private RecoilAnimation _recoilAnimation;
        // ~Scriptable Animation System Integration
        
        private List<Weapon> _instantiatedWeapons;
        
        private bool HasActiveAction()
        {
            return _actionState != FPSActionState.None;
        }

        private bool IsAiming()
        {
            return _aimState is FPSAimState.Aiming or FPSAimState.PointAiming;
        }

        private void InitializeMovement()
        {
            _moveRotation = transform.rotation;
            movementComponent = GetComponent<FPSMovement>();

            movementComponent.onStartMoving.AddListener(OnMoveStarted);
            movementComponent.onStopMoving.AddListener(OnMoveEnded);

            movementComponent.onCrouch.AddListener(OnCrouch);
            movementComponent.onUncrouch.AddListener(OnUncrouch);

            movementComponent.onJump.AddListener(OnJump);
            movementComponent.onLanded.AddListener(OnLand);

            movementComponent.onSprintStarted.AddListener(OnSprintStarted);
            movementComponent.onSprintEnded.AddListener(OnSprintEnded);

            movementComponent.onSlideStarted.AddListener(OnSlideStarted);
            movementComponent.onSlideEnded.AddListener(OnSlideEnded);

            movementComponent.slideCondition += () => !HasActiveAction();
            movementComponent.sprintCondition += () => !HasActiveAction();
            movementComponent.proneCondition += () => !HasActiveAction();

            //movementComponent.onProneStarted.AddListener(() => collisionLayer.SetLayerAlpha(0f));
            //movementComponent.onProneEnded.AddListener(() => collisionLayer.SetLayerAlpha(1f));
        }

        private void InitializeWeapons()
        {
            _instantiatedWeapons = new List<Weapon>();

            foreach (var prefab in weaponPrefabs)
            {
                GameObject weapon = Instantiate(prefab, transform.position, Quaternion.identity);

                var weaponTransform = weapon.transform;
                
                weaponTransform.parent = weaponBone;
                weaponTransform.localPosition = Vector3.zero;
                weaponTransform.localRotation = Quaternion.identity;

                weapon.SetActive(false);
                _instantiatedWeapons.Add(weapon.GetComponent<Weapon>());
            }
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            _fpsAnimator = GetComponent<FPSAnimator>();
            _userInput = GetComponent<UserInputController>();
            _fpsCamera = GetComponentInChildren<FPSCameraController>();
            _playablesController = GetComponent<IPlayablesController>();
            _recoilAnimation = GetComponent<RecoilAnimation>();
            _animator = GetComponent<Animator>();

            InitializeMovement();
            InitializeWeapons();
            
            _actionState = FPSActionState.None;
            EquipWeapon();
        }

        private void UnequipWeapon()
        {
            DisableAim();
            _actionState = FPSActionState.WeaponChange;
            _animator.CrossFade("CurveUnequip", 0.15f);
        }

        public void ResetActionState()
        {
            _actionState = FPSActionState.None;
        }

        public void RefreshStagedState()
        {
        }
        
        public void ResetStagedState()
        {
        }

        private void EquipWeapon()
        {
            if (_instantiatedWeapons.Count == 0) return;

            _instantiatedWeapons[_previousWeaponIndex].gameObject.SetActive(false);
            var gun = _instantiatedWeapons[_activeWeaponIndex];
            
            gun.gameObject.SetActive(true);

            _animator.SetFloat(OverlayType, (float) gun.overlayType);
            _actionState = FPSActionState.None;
            
            _fpsAnimator.LinkAnimatorProfile(gun.gameObject);
            _recoilAnimation.Init(gun.recoilData, gun.fireRate, gun.isAuto ? FireMode.Auto : FireMode.Semi);
            
            _animator.CrossFade("CurveEquip", 0.15f);
        }
        
        private void DisableAim()
        {
            if (!GetGun().canAim) return;
            
            _aimState = FPSAimState.None;
            _userInput.SetValue("IsAiming", false);
        }

        private void ToggleAim()
        {
            if (!GetGun().canAim)
            {
                return;
            }
            
            if (!IsAiming())
            {
                _aimState = FPSAimState.Aiming;
                _userInput.SetValue("IsAiming", true);
                _fpsCamera.UpdateTargetFOV(60f);
            }
            else
            {
                DisableAim();
                _fpsCamera.UpdateTargetFOV(90f);
            }

            _recoilAnimation.isAiming = IsAiming();
        }
        
        private void Fire()
        {
            if (HasActiveAction()) return;

            GetGun().OnFire();
            _fpsCamera.PlayCameraShake(GetGun().cameraShake);

            if (_recoilAnimation != null && GetGun().recoilData != null)
            {
                _recoilAnimation.Play();
            }

            if (_recoilAnimation.fireMode == FireMode.Semi)
            {
                return;
            }
            
            if (_recoilAnimation.fireMode == FireMode.Burst)
            {
                if (_bursts == 0)
                {
                    OnFireReleased();
                    return;
                }

                _bursts--;
            }
            
            Invoke(nameof(Fire), 60f / GetGun().fireRate);
        }

        private void OnFirePressed()
        {
            if (_instantiatedWeapons.Count == 0 || HasActiveAction()) return;

            // Do not allow firing faster than the allowed fire rate.
            if (Time.unscaledTime - _lastRecoilTime < 60f / GetGun().fireRate)
            {
                return;
            }
            
            _lastRecoilTime = Time.unscaledTime;
            Fire();
        }

        private Weapon GetGun()
        {
            if (_instantiatedWeapons.Count == 0) return null;
            
            return _instantiatedWeapons[_activeWeaponIndex];
        }

        private void OnFireReleased()
        {
            if (_instantiatedWeapons.Count == 0) return;
            
            if (_recoilAnimation != null)
            {
                _recoilAnimation.Stop();
            }
            
            CancelInvoke(nameof(Fire));
        }

        private void OnMoveStarted()
        {
        }

        private void OnMoveEnded()
        {
        }

        private void OnSlideStarted()
        {
            _animator.CrossFade("Sliding", 0.1f);
        }

        private void OnSlideEnded()
        {
        }

        private void OnSprintStarted()
        {
            OnFireReleased();
            DisableAim();
            
            _aimState = FPSAimState.None;
            
            _userInput.SetValue("StabilizationWeight", 0f);
            _userInput.SetValue("PlayablesWeight", 0f);
            _userInput.SetValue("LookLayerWeight", 0.3f);
        }

        private void OnSprintEnded()
        {
            _userInput.SetValue("StabilizationWeight", 1f);
            _userInput.SetValue("PlayablesWeight", 1f);
            _userInput.SetValue("LookLayerWeight", 1f);
        }

        private void OnCrouch()
        {
            _animator.SetBool(Crouching, true);
        }

        private void OnUncrouch()
        {
            _animator.SetBool(Crouching, false);
        }

        private void OnJump()
        {
        }

        private void OnLand()
        {
        }

        private void TryReload()
        {
            if (HasActiveAction()) return;

            var reloadClip = GetGun().reloadClip;

            if (reloadClip == null) return;
            
            OnFireReleased();

            _playablesController.PlayAnimation(reloadClip, 0f);
            GetGun().Reload();
            _actionState = FPSActionState.Reloading;
        }

        private void TryGrenadeThrow()
        {
            if (HasActiveAction()) return;
            if (GetGun().grenadeClip == null) return;
            
            OnFireReleased();
            DisableAim();
            _actionState = FPSActionState.Reloading;
        }

        private bool _isLeaning;
        
        private void ChangeWeapon_Internal(int newIndex)
        {
            if (newIndex == _activeWeaponIndex || newIndex > _instantiatedWeapons.Count - 1)
            {
                return;
            }
            
            _previousWeaponIndex = _activeWeaponIndex;
            _activeWeaponIndex = newIndex;
            
            OnFireReleased();

            UnequipWeapon();
            Invoke(nameof(EquipWeapon), equipDelay);
        }

        private void HandleWeaponChangeInput()
        {
            if (movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || _instantiatedWeapons.Count == 0) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                ChangeWeapon_Internal(_activeWeaponIndex + 1 > _instantiatedWeapons.Count - 1
                    ? 0
                    : _activeWeaponIndex + 1);
                return;
            }
            
            for (int i = (int) KeyCode.Alpha1; i <= (int) KeyCode.Alpha9; i++)
            {
                if (Input.GetKeyDown((KeyCode) i))
                {
                    ChangeWeapon_Internal(i - (int) KeyCode.Alpha1);
                }
            }
        }

        private void UpdateActionInput()
        {
            if (movementComponent.MovementState == FPSMovementState.Sprinting)
            {
                return;
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                TryReload();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                TryGrenadeThrow();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                _isUnarmed = !_isUnarmed;

                if (_isUnarmed)
                {
                    GetGun().gameObject.SetActive(false);
                    
                    _animator.SetFloat(OverlayType, 0);
                    _userInput.SetValue(FPSANames.PlayablesWeight, 0f);
                    _userInput.SetValue("StabilizationWeight", 0f);
                    _fpsAnimator.LinkAnimatorProfile(unarmedProfile);
                }
                else
                {
                    GetGun().gameObject.SetActive(true);
                    
                    _animator.SetFloat(OverlayType, (int) GetGun().overlayType);
                    _userInput.SetValue("PlayablesWeight", 1f);
                    _userInput.SetValue("StabilizationWeight", 1f);
                    _fpsAnimator.LinkAnimatorProfile(GetGun().gameObject);
                }
            }

            HandleWeaponChangeInput();

            if (_aimState == FPSAimState.Ready) return;
            
            if (Input.GetKeyDown(KeyCode.Mouse0)) OnFirePressed();
            if (Input.GetKeyUp(KeyCode.Mouse0)) OnFireReleased();
            if (Input.GetKeyDown(KeyCode.Mouse1)) ToggleAim();
            if (Input.GetKeyDown(KeyCode.B) && IsAiming())
                _aimState = _aimState == FPSAimState.PointAiming ? FPSAimState.Aiming : FPSAimState.PointAiming;
        }
        
        private void TurnInPlace()
        {
            float turnInput = _playerInput.x;
            _playerInput.x = Mathf.Clamp(_playerInput.x, -90f, 90f);
            turnInput -= _playerInput.x;

            float sign = Mathf.Sign(_playerInput.x);
            if (Mathf.Abs(_playerInput.x) > turnInPlaceAngle)
            {
                if (!_isTurning)
                {
                    _turnProgress = 0f;
                    
                    _animator.ResetTrigger(TurnRight);
                    _animator.ResetTrigger(TurnLeft);
                    
                    _animator.SetTrigger(sign > 0f ? TurnRight : TurnLeft);
                }
                
                _isTurning = true;
            }

            transform.rotation *= Quaternion.Euler(0f, turnInput, 0f);
            
            float lastProgress = turnCurve.Evaluate(_turnProgress);
            _turnProgress += Time.deltaTime * turnSpeed;
            _turnProgress = Mathf.Min(_turnProgress, 1f);
            
            float deltaProgress = turnCurve.Evaluate(_turnProgress) - lastProgress;

            _playerInput.x -= sign * turnInPlaceAngle * deltaProgress;
            
            transform.rotation *= Quaternion.Slerp(Quaternion.identity,
                Quaternion.Euler(0f, sign * turnInPlaceAngle, 0f), deltaProgress);
            
            if (Mathf.Approximately(_turnProgress, 1f) && _isTurning)
            {
                _isTurning = false;
            }
        }

        private float _jumpState = 0f;

        private void UpdateLookInput()
        {
            float deltaMouseX = Input.GetAxis("Mouse X") * sensitivity;
            float deltaMouseY = -Input.GetAxis("Mouse Y") * sensitivity;
            
            _freeLookInput = Vector2.Lerp(_freeLookInput, Vector2.zero, 
                KMath.ExpDecayAlpha(15f, Time.deltaTime));
            
            _playerInput.x += deltaMouseX;
            _playerInput.y += deltaMouseY;
            
            float proneWeight = _animator.GetFloat("ProneWeight");
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);
            
            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            _moveRotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
            TurnInPlace();

            _jumpState = Mathf.Lerp(_jumpState, movementComponent.IsInAir() ? 1f : 0f,
                KMath.ExpDecayAlpha(10f, Time.deltaTime));

            float moveWeight = Mathf.Clamp01(movementComponent.AnimatorVelocity.magnitude);
            transform.rotation = Quaternion.Slerp(transform.rotation, _moveRotation, moveWeight);
            _playerInput.x *= 1f - moveWeight;
            _playerInput.x *= 1f - _jumpState;
            
            float leanAmount = 25f * (Input.GetKey(KeyCode.Q) ? 1f : Input.GetKey(KeyCode.E) ? -1f : 0f);
            
            _userInput.SetValue("MouseDeltaInput", new Vector4(deltaMouseX, deltaMouseY));
            _userInput.SetValue("MouseInput", new Vector4(_playerInput.x, _playerInput.y));
            _userInput.SetValue("LeanInput", leanAmount);
        }
        
        private void Update()
        {
            Time.timeScale = timeScale;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit(0);
            }
            
            UpdateActionInput();
            UpdateLookInput();
        }
    }
}