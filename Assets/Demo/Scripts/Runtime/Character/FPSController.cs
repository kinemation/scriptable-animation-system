// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Rig;

using Demo.Scripts.Runtime.Item;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.Scripts.Runtime.Character
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
        WeaponChange,
        AttachmentEditing
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

        private Animator _animator;
        
        //~ Legacy Controller Interface

        // ~Scriptable Animation System Integration
        private FPSAnimator _fpsAnimator;
        private UserInputController _userInput;
        // ~Scriptable Animation System Integration

        private List<FPSItem> _instantiatedWeapons;
        private Vector2 _lookDeltaInput;

        private RecoilPattern _recoilPattern;
        private int _sensitivityMultiplierPropertyIndex;

        private static int _fullBodyWeightHash = Animator.StringToHash("FullBodyWeight");
        private static int _proneWeightHash = Animator.StringToHash("ProneWeight");
        private static int _inspectStartHash = Animator.StringToHash("InspectStart");
        private static int _inspectEndHash = Animator.StringToHash("InspectEnd");
        private static int _slideHash = Animator.StringToHash("Sliding");

        private void PlayTransitionMotion(FPSAnimatorLayerSettings layerSettings)
        {
            if (layerSettings == null)
            {
                return;
            }
            
            _fpsAnimator.LinkAnimatorLayer(layerSettings);
        }

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
            _movementComponent = GetComponent<FPSMovement>();
            
            _movementComponent.onJump = () => { PlayTransitionMotion(settings.jumpingMotion); };
            _movementComponent.onLanded = () => { PlayTransitionMotion(settings.jumpingMotion); };

            _movementComponent.onCrouch = OnCrouch;
            _movementComponent.onUncrouch = OnUncrouch;

            _movementComponent.onSprintStarted = OnSprintStarted;
            _movementComponent.onSprintEnded = OnSprintEnded;

            _movementComponent.onSlideStarted = OnSlideStarted;

            _movementComponent._slideActionCondition += () => !HasActiveAction();
            _movementComponent._sprintActionCondition += () => !HasActiveAction();
            _movementComponent._proneActionCondition += () => !HasActiveAction();
            
            _movementComponent.onStopMoving = () =>
            {
                PlayTransitionMotion(settings.stopMotion);
            };
            
            _movementComponent.onProneEnded = () =>
            {
                _userInput.SetValue(FPSANames.PlayablesWeight, 1f);
            };
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
            _animator = GetComponent<Animator>();
            
            _userInput = GetComponent<UserInputController>();
            _recoilPattern = GetComponent<RecoilPattern>();

            InitializeMovement();
            InitializeWeapons();

            _actionState = FPSActionState.None;
            EquipWeapon();

            _sensitivityMultiplierPropertyIndex = _userInput.GetPropertyIndex("SensitivityMultiplier");
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
            GetActiveItem().OnEquip(gameObject);

            _actionState = FPSActionState.None;
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
            _animator.CrossFade(_slideHash, 0.2f);
        }
        
        private void OnSprintStarted()
        {
            OnFireReleased();
            DisableAim();

            _aimState = FPSAimState.None;

            _userInput.SetValue(FPSANames.StabilizationWeight, 0f);
            _userInput.SetValue("LookLayerWeight", 0.3f);
        }

        private void OnSprintEnded()
        {
            _userInput.SetValue(FPSANames.StabilizationWeight, 1f);
            _userInput.SetValue("LookLayerWeight", 1f);
        }

        private void OnCrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion);
        }

        private void OnUncrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion);
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
        
        private void UpdateLookInput()
        {
            float scale = _userInput.GetValue<float>(_sensitivityMultiplierPropertyIndex);
            
            float deltaMouseX = _lookDeltaInput.x * settings.sensitivity * scale;
            float deltaMouseY = -_lookDeltaInput.y * settings.sensitivity * scale;
            
            _playerInput.y += deltaMouseY;
            _playerInput.x += deltaMouseX;
            
            if (_recoilPattern != null)
            {
                _playerInput += _recoilPattern.GetRecoilDelta();
                deltaMouseX += _recoilPattern.GetRecoilDelta().x;
            }
            
            float proneWeight = _animator.GetFloat(_proneWeightHash);
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            
            transform.rotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
            
            _userInput.SetValue(FPSANames.MouseDeltaInput, new Vector4(deltaMouseX, deltaMouseY));
            _userInput.SetValue(FPSANames.MouseInput, new Vector4(_playerInput.x, _playerInput.y));
        }

        private void OnMovementUpdated()
        {
            float playablesWeight = 1f - _animator.GetFloat(_fullBodyWeightHash);
            _userInput.SetValue(FPSANames.PlayablesWeight, playablesWeight);
        }

        private void Update()
        {
            Time.timeScale = settings.timeScale;
            UpdateLookInput();
            OnMovementUpdated();
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

        public void OnAim(InputValue value)
        {
            if (IsSprinting()) return;

            if (value.isPressed && !IsAiming())
            {
                if (GetActiveItem().OnAimPressed()) _aimState = FPSAimState.Aiming;
                PlayTransitionMotion(settings.aimingMotion);
                return;
            }

            if (!value.isPressed && IsAiming())
            {
                DisableAim();
                PlayTransitionMotion(settings.aimingMotion);
            }
        }

        public void OnChangeWeapon()
        {
            if (_movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || _instantiatedWeapons.Count == 0) return;
            
            StartWeaponChange(_activeWeaponIndex + 1 > _instantiatedWeapons.Count - 1 ? 0 : _activeWeaponIndex + 1);
        }

        public void OnLook(InputValue value)
        {
            _lookDeltaInput = value.Get<Vector2>();
        }

        public void OnLean(InputValue value)
        {
            _userInput.SetValue(FPSANames.LeanInput, value.Get<float>() * settings.leanAngle);
            PlayTransitionMotion(settings.leanMotion);
        }

        public void OnCycleScope()
        {
            if (!IsAiming()) return;
            
            GetActiveItem().OnCycleScope();
            PlayTransitionMotion(settings.aimingMotion);
        }

        public void OnChangeFireMode()
        {
            GetActiveItem().OnChangeFireMode();
        }

        public void OnToggleAttachmentEditing()
        {
            if (HasActiveAction() && _actionState != FPSActionState.AttachmentEditing) return;
            
            _actionState = _actionState == FPSActionState.AttachmentEditing 
                ? FPSActionState.None : FPSActionState.AttachmentEditing;

            if (_actionState == FPSActionState.AttachmentEditing)
            {
                _animator.CrossFade(_inspectStartHash, 0.2f);
                return;
            }
            
            _animator.CrossFade(_inspectEndHash, 0.3f);
        }

        public void OnDigitAxis(InputValue value)
        {
            if (!value.isPressed || _actionState != FPSActionState.AttachmentEditing) return;
            GetActiveItem().OnAttachmentChanged((int) value.Get<float>());
        }
#endif
    }
}