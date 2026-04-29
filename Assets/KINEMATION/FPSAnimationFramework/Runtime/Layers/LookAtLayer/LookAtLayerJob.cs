using System.Collections.Generic;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.LookAtLayer
{
    public struct LookAtLayerAtom
    {
        public TransformStreamHandle handle;
        public Vector2 clampedAngle;
    }

    public struct LookAtLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private LookAtLayerSettings _settings;
        private LayerJobData _jobData;

        private NativeArray<LookAtLayerAtom> _pitchElements;
        private NativeArray<LookAtLayerAtom> _yawElements;

        private TransformStreamHandle _boneToAlignHandle;
        private TransformSceneHandle _lookTargetHandle;

        private LookAt _lookAtSource;
        private Transform _cachedBoneToAlign;
        private Transform _cachedLookTarget;
        private bool _isSetupValid;

        private float _yawPositiveLimit;
        private float _yawNegativeLimit;
        private float _pitchPositiveLimit;
        private float _pitchNegativeLimit;

        private Vector3 _forwardAxis;
        private Quaternion _forwardAxisToZ;

        private void SetupChain(out NativeArray<LookAtLayerAtom> chain, List<LookAtLayerElement> elements)
        {
            int count = elements.Count;
            chain = new NativeArray<LookAtLayerAtom>(count, Allocator.Persistent);

            for (int i = 0; i < count; i++)
            {
                var transform = _jobData.rigComponent.GetRigTransform(elements[i].rigElement);
                chain[i] = new LookAtLayerAtom()
                {
                    handle = _jobData.animator.BindStreamTransform(transform),
                    clampedAngle = new Vector2(Mathf.Abs(elements[i].clampedAngle.x), Mathf.Abs(elements[i].clampedAngle.y))
                };
            }
        }

        private static float SumLimit(NativeArray<LookAtLayerAtom> elements, bool positive)
        {
            float sum = 0f;
            for (int i = 0; i < elements.Length; i++)
            {
                sum += Mathf.Abs(positive ? elements[i].clampedAngle.x : elements[i].clampedAngle.y);
            }

            return sum;
        }

        private void CacheForwardAxis()
        {
            _forwardAxis = _settings.boneForwardAxis.sqrMagnitude > 0.000001f
                ? _settings.boneForwardAxis.normalized
                : Vector3.forward;

            _forwardAxisToZ = Quaternion.FromToRotation(_forwardAxis, Vector3.forward);
        }

        private void CacheAngleLimits()
        {
            _yawPositiveLimit = SumLimit(_yawElements, true);
            _yawNegativeLimit = SumLimit(_yawElements, false);

            if (_settings.rotateBoneToAlignYaw)
            {
                _yawPositiveLimit += Mathf.Abs(_settings.boneToAlignYawClamp.x);
                _yawNegativeLimit += Mathf.Abs(_settings.boneToAlignYawClamp.y);
            }

            _pitchPositiveLimit = SumLimit(_pitchElements, true);
            _pitchNegativeLimit = SumLimit(_pitchElements, false);

            if (_settings.rotateBoneToAlignPitch)
            {
                _pitchPositiveLimit += Mathf.Abs(_settings.boneToAlignPitchClamp.x);
                _pitchNegativeLimit += Mathf.Abs(_settings.boneToAlignPitchClamp.y);
            }
        }

        private void RefreshLookSource(bool forceRebind = false)
        {
            if (_lookAtSource == null)
            {
                _lookAtSource = _jobData.Owner.GetComponent<LookAt>();
                if (_lookAtSource == null)
                {
                    _lookAtSource = _jobData.Owner.GetComponentInChildren<LookAt>();
                }
            }

            if (_lookAtSource == null)
            {
                _isSetupValid = false;
                return;
            }

            Transform newBoneToAlign = ResolveBoneToAlign();
            if (newBoneToAlign == null)
            {
                newBoneToAlign = _lookAtSource.BoneToAlign;
            }

            Transform newLookTarget = _lookAtSource.LookTarget;

            if (forceRebind || newBoneToAlign != _cachedBoneToAlign)
            {
                _cachedBoneToAlign = newBoneToAlign;
                _boneToAlignHandle = newBoneToAlign != null
                    ? _jobData.animator.BindStreamTransform(newBoneToAlign)
                    : default;
            }

            if (forceRebind || newLookTarget != _cachedLookTarget)
            {
                _cachedLookTarget = newLookTarget;
                _lookTargetHandle = newLookTarget != null
                    ? _jobData.animator.BindSceneTransform(newLookTarget)
                    : default;
            }

            _isSetupValid = _cachedBoneToAlign != null && _cachedLookTarget != null;
        }

        private Transform ResolveBoneToAlign()
        {
            Transform resolved = null;

            if (_settings.boneToAlign.index >= 0)
            {
                resolved = _jobData.rigComponent.GetRigTransform(_settings.boneToAlign.index);
            }

            if (resolved == null && !string.IsNullOrEmpty(_settings.boneToAlign.name))
            {
                resolved = _jobData.rigComponent.GetRigTransform(_settings.boneToAlign.name);
            }

            return resolved;
        }

        private Vector2 ComputeLookAngles(AnimationStream stream)
        {
            Vector3 direction = _lookTargetHandle.GetPosition(stream) - _boneToAlignHandle.GetPosition(stream);
            if (direction.sqrMagnitude < 0.000001f)
            {
                return Vector2.zero;
            }

            direction.Normalize();

            Quaternion boneRotation = _boneToAlignHandle.GetRotation(stream);
            Vector3 localDirection = Quaternion.Inverse(boneRotation) * direction;
            localDirection = _forwardAxisToZ * localDirection;

            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(localDirection.y,
                Mathf.Sqrt(localDirection.x * localDirection.x + localDirection.z * localDirection.z)) * Mathf.Rad2Deg;

            return new Vector2(yaw, pitch);
        }

        private static float ComputeFraction(float angle, float positiveLimit, float negativeLimit)
        {
            float limit = angle >= 0f ? positiveLimit : negativeLimit;
            if (Mathf.Approximately(limit, 0f))
            {
                return 0f;
            }

            return angle / limit;
        }

        private void RotatePitch(AnimationStream stream, TransformStreamHandle handle, Quaternion spaceRotation, float angle)
        {
            Quaternion rotation = handle.GetRotation(stream);
            rotation = KAnimationMath.RotateInSpace(spaceRotation, rotation, Quaternion.Euler(angle, 0f, 0f), _jobData.weight);
            handle.SetRotation(stream, rotation);
        }

        private void ApplyYaw(AnimationStream stream, float angle)
        {
            float fraction = ComputeFraction(angle, _yawPositiveLimit, _yawNegativeLimit);
            if (Mathf.Approximately(fraction, 0f))
            {
                return;
            }

            bool isPositive = angle >= 0f;

            for (int i = 0; i < _yawElements.Length; i++)
            {
                float maxAngle = isPositive ? _yawElements[i].clampedAngle.x : _yawElements[i].clampedAngle.y;
                AnimLayerJobUtility.RotateInSpace(stream, _jobData.rootHandle, _yawElements[i].handle,
                    Quaternion.Euler(0f, maxAngle * fraction, 0f), _jobData.weight);
            }

            if (!_settings.rotateBoneToAlignYaw)
            {
                return;
            }

            float alignAngle = isPositive ? Mathf.Abs(_settings.boneToAlignYawClamp.x) : Mathf.Abs(_settings.boneToAlignYawClamp.y);
            AnimLayerJobUtility.RotateInSpace(stream, _jobData.rootHandle, _boneToAlignHandle,
                Quaternion.Euler(0f, alignAngle * fraction, 0f), _jobData.weight);
        }

        private void ApplyPitch(AnimationStream stream, float angle, float yawAngle)
        {
            float fraction = ComputeFraction(angle, _pitchPositiveLimit, _pitchNegativeLimit);
            if (Mathf.Approximately(fraction, 0f))
            {
                return;
            }

            bool isPositive = angle >= 0f;
            Quaternion spaceRotation = _jobData.rootHandle.GetRotation(stream) * Quaternion.Euler(0f, yawAngle, 0f);

            for (int i = 0; i < _pitchElements.Length; i++)
            {
                float maxAngle = isPositive ? _pitchElements[i].clampedAngle.x : _pitchElements[i].clampedAngle.y;
                RotatePitch(stream, _pitchElements[i].handle, spaceRotation, maxAngle * fraction);
            }

            if (!_settings.rotateBoneToAlignPitch)
            {
                return;
            }

            float alignAngle = isPositive ? Mathf.Abs(_settings.boneToAlignPitchClamp.x) : Mathf.Abs(_settings.boneToAlignPitchClamp.y);
            RotatePitch(stream, _boneToAlignHandle, spaceRotation, alignAngle * fraction);
        }

        private void ApplyFinalBoneAlignment(AnimationStream stream)
        {
            if (!_settings.useFinalBoneAlignment || Mathf.Approximately(_settings.finalBoneAlignmentWeight, 0f))
            {
                return;
            }

            Vector3 direction = _lookTargetHandle.GetPosition(stream) - _boneToAlignHandle.GetPosition(stream);
            if (direction.sqrMagnitude < 0.000001f)
            {
                return;
            }

            direction.Normalize();

            Quaternion currentRotation = _boneToAlignHandle.GetRotation(stream);
            Vector3 currentForward = currentRotation * _forwardAxis;
            Quaternion deltaRotation = Quaternion.FromToRotation(currentForward, direction);

            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            if (Mathf.Approximately(angle, 0f) || axis.sqrMagnitude < 0.000001f)
            {
                return;
            }

            if (angle > 180f)
            {
                angle -= 360f;
            }

            float clampedAngle = Mathf.Clamp(angle, -_settings.finalBoneAlignmentMaxAngle, _settings.finalBoneAlignmentMaxAngle);
            Quaternion clampedDelta = Quaternion.AngleAxis(clampedAngle, axis.normalized);

            Quaternion targetRotation = clampedDelta * currentRotation;
            float blendWeight = Mathf.Clamp01(_settings.finalBoneAlignmentWeight * _jobData.weight);
            _boneToAlignHandle.SetRotation(stream, Quaternion.Slerp(currentRotation, targetRotation, blendWeight));
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!_isSetupValid || Mathf.Approximately(_jobData.weight, 0f))
            {
                return;
            }

            if (!_boneToAlignHandle.IsValid(stream) || !_lookTargetHandle.IsValid(stream))
            {
                return;
            }

            Vector2 lookAngles = ComputeLookAngles(stream);

            float clampedYaw = Mathf.Clamp(lookAngles.x, -_yawNegativeLimit, _yawPositiveLimit);
            float clampedPitch = Mathf.Clamp(lookAngles.y, -_pitchNegativeLimit, _pitchPositiveLimit);

            ApplyYaw(stream, clampedYaw);
            ApplyPitch(stream, clampedPitch, clampedYaw);
            ApplyFinalBoneAlignment(stream);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (LookAtLayerSettings)settings;
            _jobData = jobData;

            SetupChain(out _pitchElements, _settings.pitchSpineElements);
            SetupChain(out _yawElements, _settings.yawSpineElements);

            CacheAngleLimits();
            CacheForwardAxis();
            RefreshLookSource(true);
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
            if (!(newSettings is LookAtLayerSettings lookAtSettings))
            {
                return;
            }

            _settings = lookAtSettings;
            CacheAngleLimits();
            CacheForwardAxis();
            RefreshLookSource(true);
        }

        public void UpdateEntity(FPSAnimatorEntity newEntity)
        {
        }

        public void OnPreGameThreadUpdate()
        {
            RefreshLookSource();
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
            if (_pitchElements.IsCreated) _pitchElements.Dispose();
            if (_yawElements.IsCreated) _yawElements.Dispose();
        }
    }
}
