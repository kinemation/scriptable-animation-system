// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Rig;

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Demo.Scripts.Runtime
{
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
        PlayingAnimation,
        WeaponChange
    }

    [RequireComponent(typeof(CharacterController), typeof(FPSMovement))]
    public class FPSController : MonoBehaviour
    {
        //~ Legacy Controller Interface

        [SerializeField] private FPSControllerSettings settings;

        private FPSMovement _movementComponent;

        private Transform _weaponBone;
        private Vector2 _playerInput;

        private int _activeWeaponIndex;
        private int _previousWeaponIndex;

        private FPSAimState _aimState;
        private FPSActionState _actionState;

        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int TurnRight = Animator.StringToHash("TurnRight");
        private static readonly int TurnLeft = Animator.StringToHash("TurnLeft");

        private Quaternion _moveRotation;
        private float _turnProgress = 1f;
        private bool _isTurning;

        private bool _isUnarmed;
        private Animator _animator;

        //~ Legacy Controller Interface

        // ~Scriptable Animation System Integration
        private FPSAnimator _fpsAnimator;
        private UserInputController _userInput;
        // ~Scriptable Animation System Integration

        private List<FPSItem> _instantiatedWeapons;
        private Vector2 _lookDeltaInput;
        private bool _cursorLocked;

        private bool IsSprinting()
        {
            return _movementComponent.MovementState == FPSMovementState.Sprinting;
        }
        
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
            _movementComponent = GetComponent<FPSMovement>();

            _movementComponent.onCrouch.AddListener(OnCrouch);
            _movementComponent.onUncrouch.AddListener(OnUncrouch);

            _movementComponent.onSprintStarted.AddListener(OnSprintStarted);
            _movementComponent.onSprintEnded.AddListener(OnSprintEnded);

            _movementComponent.onSlideStarted.AddListener(OnSlideStarted);

            _movementComponent.slideCondition += () => !HasActiveAction();
            _movementComponent.sprintCondition += () => !HasActiveAction();
            _movementComponent.proneCondition += () => !HasActiveAction();
        }

        private void InitializeWeapons()
        {
            _instantiatedWeapons = new List<FPSItem>();

            foreach (var prefab in settings.weaponPrefabs)
            {
                var weapon = Instantiate(prefab, transform.position, Quaternion.identity);

                var weaponTransform = weapon.transform;

                weaponTransform.parent = _weaponBone;
                weaponTransform.localPosition = Vector3.zero;
                weaponTransform.localRotation = Quaternion.identity;

                _instantiatedWeapons.Add(weapon.GetComponent<FPSItem>());
                weapon.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _weaponBone = GetComponentInChildren<KRigComponent>().GetRigTransform(settings.weaponBone);
            _fpsAnimator = GetComponent<FPSAnimator>();
            _userInput = GetComponent<UserInputController>();
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
            GetActiveItem().OnUnEquip();
        }

        public void ResetActionState()
        {
            _actionState = FPSActionState.None;
        }

        private void EquipWeapon()
        {
            if (_instantiatedWeapons.Count == 0) return;

            _instantiatedWeapons[_previousWeaponIndex].gameObject.SetActive(false);
            GetActiveItem().gameObject.SetActive(true);
            GetActiveItem().OnEquip();
        }

        private void DisableAim()
        {
            if (GetActiveItem().OnAimReleased()) _aimState = FPSAimState.None;
        }
        
        private void OnFirePressed()
        {
            if (_instantiatedWeapons.Count == 0 || HasActiveAction()) return;
            GetActiveItem().OnFirePressed();
        }

        private void OnFireReleased()
        {
            if (_instantiatedWeapons.Count == 0) return;
            GetActiveItem().OnFireReleased();
        }

        private FPSItem GetActiveItem()
        {
            if (_instantiatedWeapons.Count == 0) return null;
            return _instantiatedWeapons[_activeWeaponIndex];
        }
        
        private void OnSlideStarted()
        {
            _animator.CrossFade("Sliding", 0.1f);
        }

        private void OnSprintStarted()
        {
            OnFireReleased();
            DisableAim();

            _aimState = FPSAimState.None;

            _userInput.SetValue(FPSANames.StabilizationWeight, 0f);
            _userInput.SetValue(FPSANames.PlayablesWeight, 0f);
            _userInput.SetValue("LookLayerWeight", 0.3f);
        }

        private void OnSprintEnded()
        {
            _userInput.SetValue(FPSANames.StabilizationWeight, 1f);
            _userInput.SetValue(FPSANames.PlayablesWeight, 1f);
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
        
        private bool _isLeaning;

        private void StartWeaponChange(int newIndex)
        {
            if (newIndex == _activeWeaponIndex || newIndex > _instantiatedWeapons.Count - 1)
            {
                return;
            }

            UnequipWeapon();

            OnFireReleased();
            Invoke(nameof(EquipWeapon), settings.equipDelay);

            _previousWeaponIndex = _activeWeaponIndex;
            _activeWeaponIndex = newIndex;
        }
        
        private void TurnInPlace()
        {
            float turnInput = _playerInput.x;
            _playerInput.x = Mathf.Clamp(_playerInput.x, -90f, 90f);
            turnInput -= _playerInput.x;

            float sign = Mathf.Sign(_playerInput.x);
            if (Mathf.Abs(_playerInput.x) > settings.turnInPlaceAngle)
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

            float lastProgress = settings.turnCurve.Evaluate(_turnProgress);
            _turnProgress += Time.deltaTime * settings.turnSpeed;
            _turnProgress = Mathf.Min(_turnProgress, 1f);

            float deltaProgress = settings.turnCurve.Evaluate(_turnProgress) - lastProgress;

            _playerInput.x -= sign * settings.turnInPlaceAngle * deltaProgress;

            transform.rotation *= Quaternion.Slerp(Quaternion.identity,
                Quaternion.Euler(0f, sign * settings.turnInPlaceAngle, 0f), deltaProgress);

            if (Mathf.Approximately(_turnProgress, 1f) && _isTurning)
            {
                _isTurning = false;
            }
        }

        private float _jumpState = 0f;

        private void UpdateLookInput()
        {
            float deltaMouseX = _lookDeltaInput.x * settings.sensitivity;
            float deltaMouseY = -_lookDeltaInput.y * settings.sensitivity;

            _playerInput.x += deltaMouseX;
            _playerInput.y += deltaMouseY;

            float proneWeight = _animator.GetFloat("ProneWeight");
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            _moveRotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
            TurnInPlace();

            _jumpState = Mathf.Lerp(_jumpState, _movementComponent.IsInAir() ? 1f : 0f,
                KMath.ExpDecayAlpha(10f, Time.deltaTime));

            float moveWeight = Mathf.Clamp01(_movementComponent.AnimatorVelocity.magnitude);
            transform.rotation = Quaternion.Slerp(transform.rotation, _moveRotation, moveWeight);
            _playerInput.x *= 1f - moveWeight;
            _playerInput.x *= 1f - _jumpState;
            
            _userInput.SetValue(FPSANames.MouseDeltaInput, new Vector4(deltaMouseX, deltaMouseY));
            _userInput.SetValue(FPSANames.MouseInput, new Vector4(_playerInput.x, _playerInput.y));
        }

        private void Update()
        {
            Time.timeScale = settings.timeScale;
            UpdateLookInput();
        }

        public void SetActionActive(int isActive)
        {
            if (isActive == 0) ResetActionState();
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            _cursorLocked = hasFocus;
            Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
        }

#if ENABLE_INPUT_SYSTEM
        public void OnReload()
        {
            if (IsSprinting() || HasActiveAction() || !GetActiveItem().OnReload()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }

        public void OnThrowGrenade()
        {
            if (IsSprinting()|| HasActiveAction() || !GetActiveItem().OnGrenadeThrow()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }

        public void OnToggleUnarmed()
        {
            _isUnarmed = !_isUnarmed;

            if (_isUnarmed)
            {
                GetActiveItem().gameObject.SetActive(false);
                GetActiveItem().OnUnarmedEnabled();
                _fpsAnimator.LinkAnimatorProfile(settings.unarmedProfile);
            }
            else
            {
                GetActiveItem().gameObject.SetActive(true);
                GetActiveItem().OnUnarmedDisabled();
            }
        }

        public void OnFire(InputValue value)
        {
            if (IsSprinting()) return;
            
            if (value.isPressed)
            {
                OnFirePressed();
                return;
            }
            
            OnFireReleased();
        }

        public void OnAim()
        {
            if (IsSprinting()) return;
            
            if (!IsAiming())
            {
                if (GetActiveItem().OnAimPressed()) _aimState = FPSAimState.Aiming;
                return;
            }

            DisableAim();
        }

        public void OnChangeWeapon()
        {
            if (_movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || _instantiatedWeapons.Count == 0) return;
            
            StartWeaponChange(_activeWeaponIndex + 1 > _instantiatedWeapons.Count - 1 ? 0 : _activeWeaponIndex + 1);
        }

        public void OnLook(InputValue value)
        {
            if (!_cursorLocked) return;
            
            _lookDeltaInput = value.Get<Vector2>();
        }

        public void OnLean(InputValue value)
        {
            _userInput.SetValue(FPSANames.LeanInput, value.Get<float>() * settings.leanAngle);
        }
#endif
    }
}