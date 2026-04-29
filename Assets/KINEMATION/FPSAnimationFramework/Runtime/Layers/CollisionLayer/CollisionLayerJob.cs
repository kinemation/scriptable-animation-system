using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.CollisionLayer
{
    public struct CollisionLayerJob : IAnimationJob, IAnimationLayerJob
    {
        public LayerJobData jobData;
        public bool isHit;
        public RaycastHit hit;
        public float mouseInput;
        
        public Vector3 direction;
        public Vector3 start;
        
        private int _mouseInputPropertyIndex;
        
        private CollisionLayerSettings _settings;
        
        private TransformStreamHandle _weaponHandle;
        private Transform _weaponIkBone;
        private KTransform _blockingPose;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            direction = _weaponHandle.GetRotation(stream) * Vector3.forward;
            start = _weaponHandle.GetPosition(stream) - direction * _settings.rayStartOffset;
            
            KTransform target = KTransform.Identity;
            if (isHit)
            {
                float blockRatio = 1f - hit.distance / (_settings.barrelLength + _settings.rayStartOffset);
                target = KTransform.Lerp(target, mouseInput > 0f ? _settings.secondaryPose : _settings.primaryPose, 
                    blockRatio);
            }
            
            float alpha = KMath.ExpDecayAlpha(_settings.smoothingSpeed, stream.deltaTime);
            _blockingPose = KTransform.Lerp(_blockingPose, target, alpha);
            
            KPose pose = new KPose()
            {
                modifyMode = EModifyMode.Add,
                pose = _blockingPose,
                space = _settings.targetSpace
            };
            
            AnimLayerJobUtility.ModifyTransform(stream, jobData.rootHandle, _weaponHandle, pose, jobData.weight);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData newJobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (CollisionLayerSettings) settings;
            this.jobData = newJobData;

            _weaponIkBone = jobData.rigComponent.GetRigTransform(_settings.weaponIkBone);
            _weaponHandle = jobData.animator.BindStreamTransform(_weaponIkBone);
            
            _blockingPose = KTransform.Identity;
            
            _mouseInputPropertyIndex = jobData.inputController.GetPropertyIndex(FPSANames.MouseInput);
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

        public void UpdatePlayableJobData(AnimationScriptPlayable playable, float weight)
        {
            var job = playable.GetJobData<CollisionLayerJob>();

            start = job.start;
            direction = job.direction;

            job.isHit = isHit;
            job.hit = hit;
            job.jobData.weight = weight;
            job.mouseInput = jobData.inputController.GetValue<Vector4>(_mouseInputPropertyIndex).y;
            
            playable.SetJobData(job);
        }
        
        public void LateUpdate()
        {
            isHit = Physics.Raycast(start, direction, out hit, _settings.barrelLength, 
                _settings.layerMask);
        }

        public void Destroy()
        {
        }
    }
}