// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Input;

using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.Scripts.Runtime.Character
{
    public enum FPSMovementState
    {
        Idle,
        Walking,
        Sprinting,
        InAir,
        Sliding
    }

    public enum FPSPoseState
    {
        Standing,
        Crouching,
        Prone
    }
    
    public class FPSMovement : MonoBehaviour
    {
        public delegate bool ActionConditionDelegate();
        public delegate void OnActionCallback();
        
        [SerializeField] private FPSMovementSettings movementSettings;
        
        public OnActionCallback onStartMoving;
        public OnActionCallback onStopMoving;
        
        public OnActionCallback onSprintStarted;
        public OnActionCallback onSprintEnded;
        
        public OnActionCallback onCrouch;
        public OnActionCallback onUncrouch;
        
        public OnActionCallback onProneStarted;
        public OnActionCallback onProneEnded;
        
        public OnActionCallback onJump;
        public OnActionCallback onLanded;
        
        public OnActionCallback onSlideStarted;
        public OnActionCallback onSlideEnded;

        public ActionConditionDelegate _slideActionCondition;
        public ActionConditionDelegate _proneActionCondition;
        public ActionConditionDelegate _sprintActionCondition;
        
        public FPSMovementState MovementState { get; private set; }
        public FPSPoseState PoseState { get; private set; }

        public Vector2 AnimatorVelocity { get; private set; }
        
        private CharacterController _controller;
        private Vector2 _inputDirection;

        private FPSMovementState _cachedMovementState;

        public Vector3 MoveVector { get; private set; }
        
        private Vector3 _velocity;

        private float _originalHeight;
        private Vector3 _originalCenter;
        
        private GaitSettings _desiredGait;
        private GaitSettings _cachedGait;
        private float _slideProgress = 0f;

        private Vector3 _prevPosition;
        private Vector3 _slideVector;

        private static readonly int InAir = Animator.StringToHash("InAir");
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Moving = Animator.StringToHash("Moving");
        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int Sprinting = Animator.StringToHash("Sprinting");
        private static readonly int Proning = Animator.StringToHash("Proning");
        
        private bool _wasMoving = false;

        private UserInputController _inputController;
        private Animator _animator;
        private bool _consumeMoveInput = true;

        private float _gaitProgress;
        
        public bool IsInAir()
        {
            return !_controller.isGrounded;
        }
        
        public bool IsMoving()
        {
            return !Mathf.Approximately(_inputDirection.normalized.magnitude, 0f);
        }

        private void AllowConsumingInput()
        {
            _consumeMoveInput = true;
        }

        public float GetSpeed()
        {
            return new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
        }

        private bool CanSlide()
        {
            return MovementState == FPSMovementState.Sprinting && PoseState == FPSPoseState.Standing
                                                               && (_slideActionCondition == null || _slideActionCondition.Invoke());
        }

        private bool CanSprint()
        {
            bool conditionCheck = false;
            if (_sprintActionCondition != null)
            {
                conditionCheck = _sprintActionCondition.Invoke();
            }
            
            return PoseState == FPSPoseState.Standing && conditionCheck;
        }

        private bool CanProne()
        {
            return _proneActionCondition == null || _proneActionCondition.Invoke(); 
        }
        
        private bool CanUnCrouch()
        {
            float height = _originalHeight - _controller.radius * 2f;
            Vector3 position = transform.TransformPoint(_originalCenter + Vector3.up * height / 2f);
            return !Physics.CheckSphere(position, _controller.radius);
        }

        private void OnProneEnabled()
        {
            Crouch();
            PoseState = FPSPoseState.Prone;
            
            _animator.SetBool(Crouching, false);
            _animator.SetBool(Proning, true);
            
            onProneStarted?.Invoke();
            _desiredGait = movementSettings.prone;

            _consumeMoveInput = false;
            _inputDirection = Vector2.zero;
            Invoke(nameof(AllowConsumingInput), movementSettings.proneTransitionDuration);
        }

        private void OnProneDisabled()
        {
            if (!CanUnCrouch())
            {
                return;
            }
            
            UnCrouch();
            PoseState = FPSPoseState.Standing;
            _animator.SetBool(Proning, false);
            
            onProneEnded?.Invoke();
            _desiredGait = movementSettings.idle;

            _consumeMoveInput = false;
            _inputDirection = Vector2.zero;
            Invoke(nameof(AllowConsumingInput), movementSettings.proneTransitionDuration);
        }

        private void Crouch()
        {
            float crouchedHeight = _originalHeight * movementSettings.crouchRatio;
            float heightDifference = _originalHeight - crouchedHeight;

            _controller.height = crouchedHeight;

            // Adjust the center position so the bottom of the capsule remains at the same position
            Vector3 crouchedCenter = _originalCenter;
            crouchedCenter.y -= heightDifference / 2;
            _controller.center = crouchedCenter;

            PoseState = FPSPoseState.Crouching;
            
            _animator.SetBool(Crouching, true);
            onCrouch?.Invoke();
        }

        private void UnCrouch()
        {
            _controller.height = _originalHeight;
            _controller.center = _originalCenter;
            
            PoseState = FPSPoseState.Standing;
            
            _animator.SetBool(Crouching, false);
            onUncrouch?.Invoke();
        }
        
        private void UpdateMovementState()
        {
            if (MovementState == FPSMovementState.Sliding && !Mathf.Approximately(_slideProgress, 1f))
            {
                // Consume input, but do not allow cancelling sliding.
                return;
            }

            if (MovementState == FPSMovementState.InAir)
            {
                return;
            }

            // If still can sprint, keep the sprinting state.
            if (MovementState == FPSMovementState.Sprinting 
                && _inputDirection.y > 0f && Mathf.Approximately(_inputDirection.x, 0f))
            {
                return;
            }

            if (!IsMoving())
            {
                MovementState = FPSMovementState.Idle;
                return;
            }
            
            MovementState = FPSMovementState.Walking;
        }

        private void OnMovementStateChanged()
        {
            if (_cachedMovementState == FPSMovementState.InAir)
            {
                onLanded?.Invoke();
            }

            if (_cachedMovementState == FPSMovementState.Sprinting)
            {
                onSprintEnded?.Invoke();
            }

            if (_cachedMovementState == FPSMovementState.Sliding)
            {
                onSlideEnded?.Invoke();

                if (CanUnCrouch())
                {
                    UnCrouch();
                }
            }
            
            if (MovementState == FPSMovementState.Idle)
            {
                _desiredGait = movementSettings.idle;
                return;
            }

            if (MovementState == FPSMovementState.InAir)
            {
                onJump?.Invoke();
                return;
            }

            if (MovementState == FPSMovementState.Sprinting)
            {
                _gaitProgress = 0f;
                _cachedGait = _desiredGait;
                
                onSprintStarted?.Invoke();
                _desiredGait = movementSettings.sprinting;
                return;
            }

            if (MovementState == FPSMovementState.Sliding)
            {
                _desiredGait.velocitySmoothing = movementSettings.slideDirectionSmoothing;
                _slideVector = _velocity;
                _slideProgress = 0f;
                onSlideStarted?.Invoke();
                Crouch();
                return;
            }

            if (PoseState == FPSPoseState.Crouching)
            {
                _desiredGait = movementSettings.crouching;
                return;
            }

            if (PoseState == FPSPoseState.Prone)
            {
                _gaitProgress = 0f;
                _cachedGait = _desiredGait;
                _desiredGait = movementSettings.prone;
                return;
            }

            if (_cachedMovementState == FPSMovementState.Idle)
            {
                _gaitProgress = 0f;
                _cachedGait = _desiredGait;
            }
            
            // Walking state
            _desiredGait = movementSettings.walking;
        }

        private void UpdateSliding()
        {
            float slideAmount = movementSettings.slideCurve.Evaluate(_slideProgress) * movementSettings.slideSpeed;
            _velocity = _slideVector.normalized * slideAmount;

            Vector3 desiredVelocity = _velocity;
            desiredVelocity.y = -2f;
            MoveVector = desiredVelocity;
            
            _slideProgress = Mathf.Clamp01(_slideProgress + Time.deltaTime);
        }
        
        private void UpdateGrounded()
        {
            var normInput = _inputDirection.normalized;
            var targetDirection = transform.right * normInput.x + transform.forward * normInput.y;

            float maxAccelTime = movementSettings.accelerationCurve.keys[^1].time;
            _gaitProgress = Mathf.Min(_gaitProgress + Time.deltaTime, maxAccelTime);

            float t = movementSettings.accelerationCurve.Evaluate(_gaitProgress);
            t = Mathf.Lerp(_cachedGait.velocity, _desiredGait.velocity, t);
           
            targetDirection *= Mathf.Lerp(_cachedGait.velocity, _desiredGait.velocity, t);

            targetDirection = Vector3.Lerp(_velocity, targetDirection, 
                KMath.ExpDecayAlpha(_desiredGait.velocitySmoothing, Time.deltaTime));
            
            _velocity = targetDirection;

            targetDirection.y = -2f;
            MoveVector = targetDirection;
        }
        
        private void UpdateInAir()
        {
            var normInput = _inputDirection.normalized;
            _velocity.y -= movementSettings.gravity * Time.deltaTime;
            _velocity.y = Mathf.Max(-movementSettings.maxFallVelocity, _velocity.y);
            
            var desiredVelocity = transform.right * normInput.x + transform.forward * normInput.y;
            desiredVelocity *= _desiredGait.velocity;

            desiredVelocity = Vector3.Lerp(_velocity, desiredVelocity * movementSettings.airFriction, 
                KMath.ExpDecayAlpha(movementSettings.airVelocity, Time.deltaTime));

            desiredVelocity.y = _velocity.y;
            _velocity = desiredVelocity;
            
            MoveVector = desiredVelocity;
        }
        
        private void UpdateMovement()
        {
            _controller.Move(MoveVector * Time.deltaTime);
        }
        

        private void UpdateAnimatorParams()
        {
            var animatorVelocity = _inputDirection;
            animatorVelocity *= MovementState == FPSMovementState.InAir ? 0f : 1f;

            AnimatorVelocity = Vector2.Lerp(AnimatorVelocity, animatorVelocity, 
                KMath.ExpDecayAlpha(_desiredGait.velocitySmoothing, Time.deltaTime));

            _animator.SetFloat(MoveX, AnimatorVelocity.x);
            _animator.SetFloat(MoveY, AnimatorVelocity.y);
            _animator.SetFloat(Velocity, AnimatorVelocity.magnitude);
            _animator.SetBool(InAir, IsInAir());
            _animator.SetBool(Moving, IsMoving());

            float sprintWeight = _animator.GetFloat(Sprinting);
            float t = KMath.ExpDecayAlpha(_desiredGait.velocitySmoothing, Time.deltaTime);
            sprintWeight = Mathf.Lerp(sprintWeight, MovementState == FPSMovementState.Sprinting ? 1f : 0f, t);
            _animator.SetFloat(Sprinting, sprintWeight);
            
            _inputController.SetValue(FPSANames.MoveInput, 
                new Vector4(AnimatorVelocity.x, AnimatorVelocity.y));
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _inputController = GetComponent<UserInputController>();
            _animator = GetComponent<Animator>();
            
            _originalHeight = _controller.height;
            _originalCenter = _controller.center;
            
            _cachedMovementState = MovementState = FPSMovementState.Idle;
            PoseState = FPSPoseState.Standing;

            _desiredGait = _cachedGait = movementSettings.idle;
        }
        
        private void Update()
        {
            UpdateMovementState();
            
            if (_cachedMovementState != MovementState)
            {
                OnMovementStateChanged();
            }

            bool isMoving = IsMoving();
            
            if (_wasMoving != isMoving)
            {
                if (isMoving)
                {
                    onStartMoving?.Invoke();
                }
                else
                {
                    onStopMoving?.Invoke();
                }
            }
            
            _wasMoving = isMoving;

            if (MovementState == FPSMovementState.InAir)
            {
                UpdateInAir();
            }
            else if (MovementState == FPSMovementState.Sliding)
            {
                UpdateSliding();
            }
            else
            {
                UpdateGrounded();
            }

            UpdateMovement();
            UpdateAnimatorParams();

            _cachedMovementState = MovementState;
        }

        private void LateUpdate()
        {
            if (MovementState != FPSMovementState.InAir && IsInAir())
            {
                MovementState = FPSMovementState.InAir;
            }
            
            if (MovementState == FPSMovementState.InAir && !IsInAir())
            {
                MovementState = FPSMovementState.Idle;
            }
        }

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
        {
            if (!_consumeMoveInput) return;
            _inputDirection = value.Get<Vector2>();
        }

        public void OnCrouch()
        {
            if (!_consumeMoveInput) return;
            
            if (MovementState is not (FPSMovementState.Idle or FPSMovementState.Walking))
            {
                return;
            }
            
            if (PoseState == FPSPoseState.Standing)
            {
                Crouch();
                _desiredGait = movementSettings.crouching;
                return;
            }

            if (!CanUnCrouch())
            {
                return;
            }

            if (PoseState == FPSPoseState.Prone)
            {
                OnProneDisabled();
                return;
            }
            
            UnCrouch();
            _desiredGait = movementSettings.walking;
        }

        public void OnProne()
        {
            if (!_consumeMoveInput) return;
            
            if (MovementState is FPSMovementState.Sprinting or FPSMovementState.InAir)
            {
                return;
            }

            if (!CanProne())
            {
                return;
            }
            
            if (PoseState == FPSPoseState.Prone)
            {
                OnProneDisabled();
                return;
            }
            
            OnProneEnabled();
        }

        public void OnJump()
        {
            if (!_consumeMoveInput || MovementState == FPSMovementState.InAir || PoseState == FPSPoseState.Crouching)
            {
                return;
            }

            if (PoseState == FPSPoseState.Prone)
            {
                OnProneDisabled();
                return;
            }
            
            MovementState = FPSMovementState.InAir;
            _velocity.y = movementSettings.jumpHeight;
        }

        public void OnSprint(InputValue value)
        {
            if (!_consumeMoveInput || MovementState is FPSMovementState.InAir or FPSMovementState.Sliding)
            {
                return;
            }
            
            bool enableSprint = value.isPressed && CanSprint();

            if (enableSprint)
            {
                MovementState = FPSMovementState.Sprinting;
                return;
            }
            
            MovementState = FPSMovementState.Walking;
        }

        public void OnSlide()
        {
            if (!_consumeMoveInput || !CanSlide())
            {
                return;
            }

            _slideProgress = 0f;
            MovementState = FPSMovementState.Sliding;
        }
#endif
    }
}