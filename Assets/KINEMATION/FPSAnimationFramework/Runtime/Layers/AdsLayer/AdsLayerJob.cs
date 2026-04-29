using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.WeaponLayer;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.AdsLayer
{
    public struct AdsLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private FPSAnimatorEntity _entity;
        private AdsLayerSettings _settings;

        private Transform _weaponIkBone;
        private Transform _cachedAimPoint;

        private float _aimingWeight;
        private KTransform _additivePose;
        
        private KTransform _aimPoint;
        private KTransform _prevAimPoint;
        private float _aimPointPlayback;

        private int _isAimingPropertyIndex;
        private Vector3 _targetDefaultPose;
        
        // Animation Job
        private LayerJobData _jobData;
        private WeaponLayerJobData _weaponJobData;
        private TransformStreamHandle _aimBoneHandle;
        private TransformStreamHandle _weaponBoneHandle;
        
        private Vector3 GetEuler(Quaternion rotation)
        {
            Vector3 result = rotation.eulerAngles;

            result.x = KMath.NormalizeEulerAngle(result.x);
            result.y = KMath.NormalizeEulerAngle(result.y);
            result.z = KMath.NormalizeEulerAngle(result.z);

            return result;
        }
        
        private KTransform GetLocalAimPoint(Transform aimPoint)
        {
            KTransform result = KTransform.Identity;
            
            result.rotation = Quaternion.Inverse(_weaponIkBone.rotation) * aimPoint.rotation;
            result.position = -_weaponIkBone.InverseTransformPoint(aimPoint.position);

            return result;
        }
        
        private KTransform GetComponentAdsPose(Transform weaponBoneT, Transform aimTargetT)
        {
            KTransform weaponBone =
                new KTransform(_jobData.Owner).GetRelativeTransform(new KTransform(weaponBoneT), false);
            
            KTransform aimTarget =
                new KTransform(_jobData.Owner).GetRelativeTransform(new KTransform(aimTargetT), false);
            
            KTransform result = new KTransform()
            {
                position = aimTarget.position - weaponBone.position,
                rotation = Quaternion.Inverse(weaponBone.rotation)
            };
            
            return result;
        }
        
        private KTransform GetComponentAdsPose(AnimationStream stream)
        {
            KTransform rootTransform = AnimLayerJobUtility.GetTransformFromHandle(stream, _jobData.rootHandle);
            KTransform aimTarget = AnimLayerJobUtility.GetTransformFromHandle(stream, _aimBoneHandle);
            KTransform weaponBone = AnimLayerJobUtility.GetTransformFromHandle(stream, _weaponBoneHandle);

            aimTarget = rootTransform.GetRelativeTransform(aimTarget, false);
            weaponBone = rootTransform.GetRelativeTransform(weaponBone, false);
            
            KTransform result = new KTransform()
            {
                position = aimTarget.position - weaponBone.position,
                rotation = Quaternion.Inverse(weaponBone.rotation)
            };
            
            return result;
        }
        
        public void ProcessAnimation(AnimationStream stream)
        {
            float weight = KCurves.Ease(0f, 1f, _aimingWeight, _settings.aimingEaseMode) * _jobData.weight;
            if (!KAnimationMath.IsWeightRelevant(weight))
            {
                return;
            }

            _weaponJobData.Cache(stream);
            
            if (_settings.cameraBlend > 0f)
            {
                _aimBoneHandle.SetLocalPosition(stream, _targetDefaultPose);
            }
            
            KTransform pose = GetComponentAdsPose(stream);

            pose.position.x = Mathf.Lerp(pose.position.x, _additivePose.position.x, _settings.positionBlend.x);
            pose.position.y = Mathf.Lerp(pose.position.y, _additivePose.position.y, _settings.positionBlend.y);
            pose.position.z = Mathf.Lerp(pose.position.z, _additivePose.position.z, _settings.positionBlend.z);

            pose.position += _aimPoint.rotation * _aimPoint.position;
            
            Vector3 absQ = GetEuler(pose.rotation);
            Vector3 addQ = GetEuler(_additivePose.rotation);

            absQ.x = Mathf.Lerp(absQ.x, addQ.x, _settings.rotationBlend.x);
            absQ.y = Mathf.Lerp(absQ.y, addQ.y, _settings.rotationBlend.y);
            absQ.z = Mathf.Lerp(absQ.z, addQ.z, _settings.rotationBlend.z);

            pose.rotation = Quaternion.Euler(absQ);
            pose.rotation *= _aimPoint.rotation;
            
            AnimLayerJobUtility.MoveInSpace(stream, _jobData.rootHandle, _weaponBoneHandle, pose.position, 
                weight * (1f - _settings.cameraBlend));
            AnimLayerJobUtility.RotateInSpace(stream, _jobData.rootHandle, _weaponBoneHandle, pose.rotation, 
                weight);
            
            if (_settings.cameraBlend > 0f)
            {
                AnimLayerJobUtility.MoveInSpace(stream, _jobData.rootHandle, _aimBoneHandle, -pose.position, 
                    weight * _settings.cameraBlend);
            }
            
            _weaponJobData.PostProcessPose(stream, weight);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (AdsLayerSettings) settings;
            _jobData = jobData;

            _weaponJobData = new WeaponLayerJobData();
            _weaponJobData.Setup(jobData, _settings);

            _weaponIkBone = jobData.rigComponent.GetRigTransform(_settings.weaponIkBone);
            var aimTargetBone = jobData.rigComponent.GetRigTransform(_settings.aimTargetBone);
            
            _aimBoneHandle = jobData.animator.BindStreamTransform(aimTargetBone);
            _weaponBoneHandle = jobData.animator.BindStreamTransform(_weaponIkBone);
            
            _additivePose = GetComponentAdsPose(_weaponIkBone, aimTargetBone);
            
            _isAimingPropertyIndex = jobData.inputController.GetPropertyIndex(_settings.isAimingProperty);
            _targetDefaultPose = aimTargetBone.localPosition;
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
            _entity = newEntity;
            if (_entity == null) return;
            
            _aimPointPlayback = 1f; 
            _cachedAimPoint = _entity.defaultAimPoint;
            _aimPoint = _prevAimPoint = GetLocalAimPoint(_cachedAimPoint);
        }
        
        public void OnPreGameThreadUpdate()
        {
        }

        public void UpdatePlayableJobData(AnimationScriptPlayable playable, float weight)
        {
            if (_entity == null) return;
            
            _jobData.weight = weight;
            
            bool isAiming = _jobData.inputController.GetValue<bool>(_isAimingPropertyIndex);

            _aimingWeight += _settings.aimingSpeed * (isAiming ? Time.deltaTime : -Time.deltaTime);
            _aimingWeight = Mathf.Clamp01(_aimingWeight);
            
            _jobData.inputController.SetValue(FPSANames.AimingWeight, _aimingWeight);
            _aimPointPlayback = Mathf.Clamp01(_aimPointPlayback + Time.deltaTime * _settings.aimPointSpeed);
            
            if (_entity.defaultAimPoint != _cachedAimPoint)
            {
                _aimPointPlayback = 0f;
                _prevAimPoint = _aimPoint;
            }
            
            _cachedAimPoint = _entity.defaultAimPoint;
            
            KTransform aimPoint = GetLocalAimPoint(_entity.defaultAimPoint);
            _aimPoint = KTransform.EaseLerp(_prevAimPoint, aimPoint, _aimPointPlayback, _settings.aimPointEaseMode);
            
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
