using KINEMATION.FPSAnimationFramework.Runtime.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.TurnLayer
{
    public struct TurnLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private TurnLayerSettings _settings;

        private Transform _modelRoot;
        private TransformStreamHandle _modelRootHandle;
        
        private int _turnInputProperty;
        private int _mouseDeltaProperty;

        private float _playBack;
        private float _turnAngle;
        private float _cachedTurnAngle;
        
        private bool _isTurning;

        private int _turnRightHash;
        private int _turnLeftHash;
        
        // Animation Job
        private TransformStreamHandle _rootBoneHandle;
        private LayerJobData _jobData;

        private bool _offsetRootBone;
        private bool _wasAnimatorEnabled;

        private Quaternion _baseRootRotation;

        public void ProcessAnimation(AnimationStream stream)
        {
            Quaternion offset = Quaternion.Euler(0f, _turnAngle, 0f);
            AnimLayerJobUtility.RotateInSpace(stream, _jobData.rootHandle, _modelRootHandle, offset, 1f);

            if (!_offsetRootBone) return;
            
            Vector3 localPosition = _modelRootHandle.GetLocalPosition(stream);
            localPosition = offset * localPosition - localPosition;
            
            AnimLayerJobUtility.MoveInSpace(stream, _jobData.rootHandle, _modelRootHandle, localPosition, 1f);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (TurnLayerSettings) settings;
            _jobData = jobData;
            
            _turnInputProperty = jobData.inputController.GetPropertyIndex(_settings.turnInputProperty);
            _mouseDeltaProperty = jobData.inputController.GetPropertyIndex(_settings.mouseDeltaInputProperty);
            
            _modelRoot = jobData.rigComponent.GetRigTransform(_settings.characterRootBone);
            _baseRootRotation = _modelRoot.localRotation;
            var pelvis = jobData.rigComponent.GetRigTransform(_settings.characterHipBone);

            _modelRootHandle = jobData.animator.BindStreamTransform(_modelRoot);
            _offsetRootBone = _modelRoot != pelvis;
            
            _turnRightHash = Animator.StringToHash(_settings.animatorTurnRightTrigger);
            _turnLeftHash = Animator.StringToHash(_settings.animatorTurnLeftTrigger);
            
            _playBack = 1f;
            _turnAngle = _cachedTurnAngle = 0f;
            
            _wasAnimatorEnabled = _jobData.animator.isActiveAndEnabled;
        }

        public AnimationScriptPlayable CreatePlayable(PlayableGraph graph)
        {
            return AnimationScriptPlayable.Create(graph, this);
        }

        public FPSAnimatorLayerSettings GetSettingAsset()
        {
            return _settings;
        }

        public void OnLayerLinked(FPSAnimatorLayerSettings newSettings)
        {
        }

        public void UpdateEntity(FPSAnimatorEntity newEntity)
        {
        }
        
        public void OnPreGameThreadUpdate()
        {
            // Reset the turn angle manually when Animator is re-enabled.
            if (_jobData.animator.isActiveAndEnabled && !_wasAnimatorEnabled)
            {
                _modelRoot.localRotation = _baseRootRotation;
            }

            _wasAnimatorEnabled = _jobData.animator.isActiveAndEnabled;
            
            float mouseDelta = _jobData.inputController.GetValue<Vector4>(_mouseDeltaProperty).x;
            _turnAngle -= mouseDelta;

            _turnAngle *= _jobData.weight;

            if (!_isTurning && Mathf.Abs(_turnAngle) > _settings.angleThreshold)
            {
                _cachedTurnAngle = _turnAngle;
                _isTurning = true;
                _playBack = 0f;
                
                _jobData.animator.SetTrigger(_turnAngle < 0f ? _turnRightHash : _turnLeftHash);
            }
            
            if (_isTurning)
            {
                _playBack = Mathf.Clamp01(_playBack + Time.deltaTime * _settings.turnSpeed);
                float alpha = _settings.turnCurve.Evaluate(_playBack);

                _turnAngle = Mathf.Lerp(_cachedTurnAngle, 0f, alpha);

                if (Mathf.Approximately(_playBack, 1f)) _isTurning = false;
            }
            
            _jobData.inputController.SetValue(_turnInputProperty, -_turnAngle);
        }

        public void UpdatePlayableJobData(AnimationScriptPlayable playable, float weight)
        {
            _jobData.weight = weight;
            playable.SetJobData(this);
        }
        
        public void LateUpdate()
        {
        }

        public void Destroy()
        {
        }
    }
}