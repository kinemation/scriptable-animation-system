// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.PoseSamplerLayer
{
    public struct PoseSamplerJob : IAnimationJob, IAnimationLayerJob
    {
        private PoseSamplerLayerSettings _settings;

        private TransformStreamHandle _ikRightHand;
        private TransformStreamHandle _ikLeftHand;
        private TransformStreamHandle _ikRightHandHint;
        private TransformStreamHandle _ikLeftHandHint;

        private TransformStreamHandle _weaponBone;
        private TransformStreamHandle _weaponBoneRight;
        private TransformStreamHandle _weaponBoneLeft;
        private TransformStreamHandle _ikWeaponBone;
        
        private TransformStreamHandle _spineRootHandle;
        private TransformStreamHandle _hipHandle;
        private TransformStreamHandle _hipParentHandle;
        
        private Quaternion _cachedPelvisPose;
        
        private KTransform _weaponBoneComponentPose;
        private KTransform _weaponBoneSpinePose;
        
        private Transform _weaponBoneTransform;
        
        private int _stabilizationWeightIndex;
        private bool _overwriteRoot;
        
        private float _weaponBoneWeight;
        private float _stabilizationWeight;

        public LayerJobData _jobData;
        private bool _hasValidRoot;

        private void StabilizeSpine(in AnimationStream stream)
        {
            Quaternion rootRotation = _jobData.rootHandle.GetRotation(stream);
            Quaternion pelvisWorldRotation = rootRotation * _cachedPelvisPose;
            Quaternion stabilizedSpineRotation = pelvisWorldRotation * _spineRootHandle.GetLocalRotation(stream);

            Quaternion finalRotation = _spineRootHandle.GetRotation(stream);
            finalRotation = Quaternion.Slerp(finalRotation, stabilizedSpineRotation, 
                _stabilizationWeight * _jobData.weight);
            
            _spineRootHandle.SetRotation(stream, finalRotation);
        }

        private void PositionWeaponBone(in AnimationStream stream)
        {
            KTransform rootTransform = AnimLayerJobUtility.GetTransformFromHandle(stream, _jobData.rootHandle);
            KTransform spineRootTransform = AnimLayerJobUtility.GetTransformFromHandle(stream, _spineRootHandle);
            
            if (_weaponBoneWeight > 0f)
            {
                KTransform componentPose = rootTransform.GetWorldTransform(_weaponBoneComponentPose, false);
                KTransform spinePose = spineRootTransform.GetWorldTransform(_weaponBoneSpinePose, false);

                spinePose.position -= componentPose.position;
                spinePose.rotation = Quaternion.Inverse(componentPose.rotation) * spinePose.rotation;
                
                _weaponBone.SetPosition(stream, _weaponBone.GetPosition(stream) + spinePose.position);
                _weaponBone.SetRotation(stream, _weaponBone.GetRotation(stream) * spinePose.rotation);
            }

            // -1, 0, 1.
            KTransform pose = AnimLayerJobUtility.GetTransformFromHandle(stream, _weaponBoneRight);
            KTransform poseRight = AnimLayerJobUtility.GetTransformFromHandle(stream, _weaponBone);
            KTransform poseLeft = AnimLayerJobUtility.GetTransformFromHandle(stream,_weaponBoneLeft);

            pose = _weaponBoneWeight >= 0f
                ? KTransform.Lerp(pose, poseRight, _weaponBoneWeight)
                : KTransform.Lerp(pose, poseLeft, -_weaponBoneWeight);

            KTransform cachedIkRightHand = AnimLayerJobUtility.GetTransformFromHandle(stream, _ikRightHand);
            KTransform cachedIkLeftHand = AnimLayerJobUtility.GetTransformFromHandle(stream, _ikLeftHand);
            KTransform cachedIkRightHandHint = AnimLayerJobUtility.GetTransformFromHandle(stream, _ikRightHandHint);
            KTransform cachedIkLeftHandHint = AnimLayerJobUtility.GetTransformFromHandle(stream, _ikLeftHandHint);
            
            _ikWeaponBone.SetPosition(stream, pose.position);
            _ikWeaponBone.SetRotation(stream, pose.rotation * _settings.weaponBoneOffset.rotation);
            
            _ikRightHand.SetPosition(stream, cachedIkRightHand.position);
            _ikRightHand.SetRotation(stream, cachedIkRightHand.rotation);
            
            _ikLeftHand.SetPosition(stream, cachedIkLeftHand.position);
            _ikLeftHand.SetRotation(stream, cachedIkLeftHand.rotation);
            
            _ikRightHandHint.SetPosition(stream, cachedIkRightHandHint.position);
            _ikRightHandHint.SetRotation(stream, cachedIkRightHandHint.rotation);
            
            _ikLeftHandHint.SetPosition(stream, cachedIkLeftHandHint.position);
            _ikLeftHandHint.SetRotation(stream, cachedIkLeftHandHint.rotation);
        }
        
        public void ProcessAnimation(AnimationStream stream)
        {
            KTransform localRoot = KTransform.Identity;
            KTransform worldPelvis = AnimLayerJobUtility.GetTransformFromHandle(stream, _hipHandle);

            if (_overwriteRoot && _hasValidRoot)
            {
                localRoot = AnimLayerJobUtility.GetTransformFromHandle(stream, _hipParentHandle, false);
                _hipParentHandle.SetLocalPosition(stream, Vector3.zero);
                _hipParentHandle.SetLocalRotation(stream, Quaternion.identity);
            }
            
            _hipHandle.SetPosition(stream, worldPelvis.position);
            _hipHandle.SetRotation(stream, worldPelvis.rotation);
            
            StabilizeSpine(stream);
            PositionWeaponBone(stream);

            if (_overwriteRoot && _hasValidRoot)
            {
                _hipParentHandle.SetLocalPosition(stream, localRoot.position);
                _hipParentHandle.SetLocalRotation(stream, localRoot.rotation);
                
                _hipHandle.SetPosition(stream, worldPelvis.position);
                _hipHandle.SetRotation(stream, worldPelvis.rotation);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (PoseSamplerLayerSettings) settings;
            _jobData = jobData;

            Transform spineRoot = jobData.rigComponent.GetRigTransform(_settings.spineRoot);
            Transform pelvis = jobData.rigComponent.GetRigTransform(_settings.pelvis);
            Transform root = jobData.Owner;

            _hasValidRoot = pelvis.parent != jobData.animator.transform;

            _weaponBoneTransform = jobData.rigComponent.GetRigTransform(_settings.weaponBone);
            Transform weaponBoneRight = jobData.rigComponent.GetRigTransform(_settings.weaponBoneRight);
            Transform weaponBoneLeft = jobData.rigComponent.GetRigTransform(_settings.weaponBoneLeft);
            Transform ikWeaponBone = jobData.rigComponent.GetRigTransform(_settings.ikWeaponBone);

            Transform ikHandRight = jobData.rigComponent.GetRigTransform(_settings.ikHandRight);
            Transform ikHandLeft = jobData.rigComponent.GetRigTransform(_settings.ikHandLeft);
            Transform ikRightHandHint = jobData.rigComponent.GetRigTransform(_settings.ikHandRightHint);
            Transform ikLeftHandHint = jobData.rigComponent.GetRigTransform(_settings.ikHandLeftHint);
            
            _spineRootHandle = jobData.animator.BindStreamTransform(spineRoot);
            _hipHandle = jobData.animator.BindStreamTransform(pelvis);

            if (_settings.overwriteRoot && _hasValidRoot)
            {
                _hipParentHandle = jobData.animator.BindStreamTransform(pelvis.parent);
            }

            _weaponBone = jobData.animator.BindStreamTransform(_weaponBoneTransform);
            _weaponBoneRight = jobData.animator.BindStreamTransform(weaponBoneRight);
            _weaponBoneLeft = jobData.animator.BindStreamTransform(weaponBoneLeft);
            
            _ikWeaponBone = jobData.animator.BindStreamTransform(ikWeaponBone);
            
            _ikRightHand = jobData.animator.BindStreamTransform(ikHandRight);
            _ikLeftHand = jobData.animator.BindStreamTransform(ikHandLeft);
            
            _ikRightHandHint = jobData.animator.BindStreamTransform(ikRightHandHint);
            _ikLeftHandHint = jobData.animator.BindStreamTransform(ikLeftHandHint);
            
            if (_settings.overwriteRoot && _hasValidRoot)
            {
                pelvis.parent.localRotation = Quaternion.identity;
            }

            _weaponBoneTransform.position = root.TransformPoint(_settings.defaultWeaponPose.position);
            _weaponBoneTransform.rotation = root.rotation * _settings.defaultWeaponPose.rotation;
            
            // Try overriding with the animation.
            _settings.poseToSample.clip.SampleAnimation(jobData.Owner.gameObject, 0f);
            
            // Avoid unnecessary root modification by the pose.
            if (_settings.overwriteRoot && _hasValidRoot)
            {
                KTransform pelvisCache = new KTransform(pelvis);
                pelvis.parent.localRotation = Quaternion.identity;
                pelvis.position = pelvisCache.position;
                pelvis.rotation = pelvisCache.rotation;
            }

            if (_settings.overwriteWeaponBone)
            {
                _weaponBoneTransform.position = root.TransformPoint(_settings.defaultWeaponPose.position);
                _weaponBoneTransform.rotation = root.rotation * _settings.defaultWeaponPose.rotation;
            }

            weaponBoneRight.position = _weaponBoneTransform.position;
            weaponBoneRight.rotation = _weaponBoneTransform.rotation;
            
            weaponBoneLeft.position = weaponBoneRight.position;
            weaponBoneLeft.rotation = weaponBoneRight.rotation;
            
            // ReSharper disable all
            _cachedPelvisPose = Quaternion.Inverse(root.rotation) * pelvis.rotation;
            
            _weaponBoneComponentPose = 
                new KTransform(root).GetRelativeTransform(new KTransform(_weaponBoneTransform), false);
            
            _weaponBoneSpinePose = 
                new KTransform(spineRoot).GetRelativeTransform(new KTransform(_weaponBoneTransform), false);
            
            _weaponBoneTransform.rotation *= _settings.weaponBoneOffset.rotation;
            
            ikWeaponBone.position = _weaponBoneTransform.position;
            ikWeaponBone.rotation = _weaponBoneTransform.rotation;

            _jobData.playablesController.PlayPose(_settings.poseToSample);
            _jobData.playablesController.UpdateAvatarMask(_settings.poseToSample.mask);
            _stabilizationWeightIndex = _jobData.inputController.GetPropertyIndex(_settings.stabilizationWeight);
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
        }

        public void OnGameThreadUpdate()
        {
            _weaponBoneTransform.position = _jobData.Owner.TransformPoint(_settings.defaultWeaponPose.position);
            _weaponBoneTransform.rotation = _jobData.Owner.transform.rotation * _settings.defaultWeaponPose.rotation;

            _overwriteRoot = _settings.overwriteRoot;
            _weaponBoneWeight = _jobData.playablesController.GetCurveValue(_settings.weaponBoneWeight);
            _stabilizationWeight = _jobData.inputController.GetValue<float>(_stabilizationWeightIndex);
        }

        public void UpdatePlayableJobData(AnimationScriptPlayable playable, float weight)
        {
            _jobData.weight = weight;
            
            _weaponBoneTransform.position = _jobData.Owner.TransformPoint(_settings.defaultWeaponPose.position);
            _weaponBoneTransform.rotation = _jobData.Owner.transform.rotation * _settings.defaultWeaponPose.rotation;

            _overwriteRoot = _settings.overwriteRoot;
            _weaponBoneWeight = _jobData.playablesController.GetCurveValue(_settings.weaponBoneWeight);
            _stabilizationWeight = _jobData.inputController.GetValue<float>(_stabilizationWeightIndex);
            
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