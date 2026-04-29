// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Camera
{
    public class FPSCameraController : MonoBehaviour
    {
        [SerializeField] private Transform cameraBone;
        [SerializeField, InputProperty] protected string mouseInputProperty = FPSANames.MouseInput;
        [SerializeField] protected EaseMode fovEaseMode;
        [SerializeField] [Min(0f)] protected float fovSpeed = 5f;
        protected UserInputController _inputController;
        protected FPSCameraShake _activeShake;

        protected FPSCameraAnimation _cameraAnimationAsset;
        protected KTransform _cachedCameraAnimation;
        protected KTransform _activeCameraAnimation;
        protected float _animationPlayback;
        protected float _animationLength;
        
        protected Vector3 _cameraShake;
        protected Vector3 _cameraShakeTarget;
        protected float _cameraShakePlayback;

        protected UnityEngine.Camera _camera;
        protected float _fovPlayback;
        protected float _cachedFov;
        protected float _targetFov;

        protected Animator _animator;
        protected AnimationClipPlayable _cameraAnimation;
        protected AnimationPlayableOutput _cameraOutput;
        protected PlayableGraph _cameraGraph;

        protected int _mouseInputPropertyIndex;

        protected Vector3 _defaultPosition;
        
        protected virtual void UpdateCameraShake()
        {
            if (_activeShake == null) return;

            float length = _activeShake.shakeCurve.GetCurveLength();
            _cameraShakePlayback += Time.deltaTime * _activeShake.playRate;
            _cameraShakePlayback = Mathf.Clamp(_cameraShakePlayback, 0f, length);

            float alpha = KMath.ExpDecayAlpha(_activeShake.smoothSpeed, Time.deltaTime);
            if (!KAnimationMath.IsWeightRelevant(alpha))
            {
                alpha = 1f;
            }

            Vector3 target = _activeShake.shakeCurve.GetValue(_cameraShakePlayback);
            target.x *= _cameraShakeTarget.x;
            target.y *= _cameraShakeTarget.y;
            target.z *= _cameraShakeTarget.z;
            
            _cameraShake = Vector3.Lerp(_cameraShake, target, alpha);
            transform.rotation *= Quaternion.Euler(_cameraShake);
        }

        protected virtual void UpdateCameraAnimation()
        {
            if (_animator != null && cameraBone != null)
            {
                // Resharper Disable All
                transform.localRotation *= cameraBone.localRotation;
            }
            
            if (_cameraAnimationAsset == null) return;

            Transform currentT = transform;
            
            var blendTime = _cameraAnimationAsset.blendTime;
            _animationPlayback += Time.deltaTime * blendTime.rateScale;
            _animationPlayback = Mathf.Clamp(_animationPlayback, 0f, _animationLength);

            float scale = _cameraAnimationAsset.scale;
            KTransform targetAnimation = new KTransform();
            targetAnimation.position = _cameraAnimationAsset.translation.GetValue(_animationPlayback) * scale;
            targetAnimation.rotation = Quaternion.Euler(_cameraAnimationAsset.rotation.GetValue(_animationPlayback) * scale);

            float alpha = 1f;
            if (!Mathf.Approximately(0f, blendTime.blendInTime))
            {
                alpha = Mathf.Clamp01(_animationPlayback / blendTime.blendInTime);
            }

            _activeCameraAnimation = KTransform.Lerp(_cachedCameraAnimation, targetAnimation, alpha);

            currentT.rotation *= _activeCameraAnimation.rotation;
            KAnimationMath.MoveInSpace(currentT.root, currentT, targetAnimation.position, 1f);
        }

        protected virtual void UpdateFOV()
        {
            _fovPlayback = Mathf.Clamp01(_fovPlayback + Time.deltaTime * fovSpeed);
            _camera.fieldOfView = KCurves.Ease(_cachedFov, _targetFov, _fovPlayback, fovEaseMode);
        }
        
        public virtual void Initialize()
        {
            _defaultPosition = transform.localPosition;
            
            _camera = GetComponent<UnityEngine.Camera>();
            _inputController = transform.root.gameObject.GetComponentInChildren<UserInputController>();
            _cachedFov = _targetFov = _camera.fieldOfView;

            if (cameraBone != null)
            {
                _animator = cameraBone.parent.gameObject.GetComponent<Animator>();
                if (_animator != null)
                {
                    _cameraGraph = PlayableGraph.Create("CameraGraph");
                    _cameraOutput = AnimationPlayableOutput.Create(_cameraGraph, "CameraAnimator", _animator);
                    _cameraOutput.SetSourcePlayable(_cameraAnimation);

                    _cameraGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                    _cameraGraph.Play();
                }
            }
            
            if (_inputController == null) return;
            _mouseInputPropertyIndex = _inputController.GetPropertyIndex(mouseInputProperty);
        }

        public virtual void UpdateCamera()
        {
            Vector4 input = _inputController.GetValue<Vector4>(_mouseInputPropertyIndex);
            
            // Stabilize the camera by overriding the rotation.
            Transform root = transform.root;
            transform.rotation = root.rotation * Quaternion.Euler(input.y, 0f, 0f);

            UpdateCameraShake();
            UpdateCameraAnimation();
            UpdateFOV();
        }

        public virtual void PlayCameraShake(FPSCameraShake newShake)
        {
            if (newShake == null) return;

            _activeShake = newShake;
            _cameraShakePlayback = 0f;

            _cameraShakeTarget.x = FPSCameraShake.GetTarget(_activeShake.pitch);
            _cameraShakeTarget.y = FPSCameraShake.GetTarget(_activeShake.yaw);
            _cameraShakeTarget.z = FPSCameraShake.GetTarget(_activeShake.roll);
        }

        public virtual void PlayCameraAnimation(FPSCameraAnimation newAnimation)
        {
            if (newAnimation == null) return;

            _cameraAnimationAsset = newAnimation;
            _cachedCameraAnimation = _activeCameraAnimation;
            _animationPlayback = 0f;

            _animationLength = Mathf.Max(newAnimation.translation.GetCurveLength(),
                newAnimation.rotation.GetCurveLength());
        }

        public virtual void PlayCameraAnimation(AnimationClip newAnimation)
        {
            if (_animator == null)
            {
                return;
            }
            
            _cameraAnimation = AnimationClipPlayable.Create(_cameraGraph, newAnimation);
            _cameraAnimation.SetDuration(newAnimation.length);
            _cameraOutput.SetSourcePlayable(_cameraAnimation);
        }

        public virtual void UpdateTargetFOV(float newFov)
        {
            _cachedFov = _camera.fieldOfView;
            _targetFov = newFov;
            _fovPlayback = 0f;
        }

        private void Update()
        {
            transform.localPosition = _defaultPosition;
        }

        private void OnDestroy()
        {
            if (_cameraGraph.IsValid())
            {
                _cameraGraph.Stop();
                _cameraGraph.Destroy();
            }
        }
    }
}