using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.AttachHandLayer
{
    public struct LeftHandPose
    {
        public TransformStreamHandle handle;
        public KTransform pose;
    }
    
    public struct AttachHandLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private AttachHandLayerSettings _settings;
        
        private Transform _handBone;
        
        private Transform _weaponBone;
        private KTransform _handPose;
        
        //Animation Job
        private TransformStreamHandle _ikHandBoneHandle;
        private TransformStreamHandle _weaponBoneHandle;
        
        private LayerJobData _jobData;
        private NativeArray<LeftHandPose> _leftHandChain;

        private void OnInitialized(FPSAnimatorLayerSettings settings)
        {
            _settings = (AttachHandLayerSettings) settings;
            
            _handBone = _jobData.rigComponent.GetRigTransform(_settings.handBone);
            _weaponBone = _jobData.rigComponent.GetRigTransform(_settings.weaponBone);

            var ikWeaponBone = _jobData.rigComponent.GetRigTransform(_settings.ikWeaponBone);
            var ikHandBone = _jobData.rigComponent.GetRigTransform(_settings.ikHandBone);

            _weaponBoneHandle = _jobData.animator.BindStreamTransform(ikWeaponBone);
            _ikHandBoneHandle = _jobData.animator.BindStreamTransform(ikHandBone);

            bool hasValidCustomPose = _settings.customHandPose != null;
            
            if (hasValidCustomPose)
            {
                _jobData.rigComponent.CacheHierarchyPose();
                _settings.customHandPose.SampleAnimation(_jobData.Owner.gameObject, 0f);
            }
            
            _handPose = new KTransform(_weaponBone).GetRelativeTransform(new KTransform(_handBone), false);
            
            var chain = _settings.rigAsset.GetPopulatedChain(_settings.elementChainName, _jobData.rigComponent);

            if (_leftHandChain.IsCreated) _leftHandChain.Dispose();
            
            _leftHandChain = new NativeArray<LeftHandPose>(chain.transformChain.Count, Allocator.Persistent);

            for (int i = 0; i < chain.transformChain.Count; i++)
            {
                _leftHandChain[i] = new LeftHandPose()
                {
                    handle = _jobData.animator.BindStreamTransform(chain.transformChain[i]),
                    pose = new KTransform(chain.transformChain[i], false)
                };
            }

            if (hasValidCustomPose)
            {
                _jobData.rigComponent.CacheHierarchyPose();
            }
        }
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!KAnimationMath.IsWeightRelevant(_jobData.weight))
            {
                return;
            }

            KTransform weaponBone = AnimLayerJobUtility.GetTransformFromHandle(stream, _weaponBoneHandle);
            
            KTransform attachedPose = new KTransform();
            
            attachedPose.position 
                = weaponBone.TransformPoint(_handPose.position + _settings.handPoseOffset.position, false);
            attachedPose.rotation = weaponBone.rotation * (_handPose.rotation * _settings.handPoseOffset.rotation);

            KTransform handBone = AnimLayerJobUtility.GetTransformFromHandle(stream, _ikHandBoneHandle);
            handBone.position = Vector3.Lerp(handBone.position, attachedPose.position, _jobData.weight);
            handBone.rotation = Quaternion.Slerp(handBone.rotation, attachedPose.rotation, _jobData.weight);
            
            _ikHandBoneHandle.SetPosition(stream, handBone.position);
            _ikHandBoneHandle.SetRotation(stream, handBone.rotation);

            foreach (var item in _leftHandChain)
            {
                Quaternion rotation = item.handle.GetLocalRotation(stream);
                rotation = Quaternion.Slerp(rotation, item.pose.rotation, _jobData.weight);
                item.handle.SetLocalRotation(stream, rotation);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData newJobData, FPSAnimatorLayerSettings settings)
        {
            this._jobData = newJobData;
            OnInitialized(settings);
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
            OnInitialized(newSettings);
        }

        public void UpdateEntity(FPSAnimatorEntity newEntity)
        {
        }
        
        public void OnPreGameThreadUpdate()
        {
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
            if (_leftHandChain.IsCreated) _leftHandChain.Dispose();
        }
    }
}