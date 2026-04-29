using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.ViewLayer
{
    public struct ViewLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private ViewLayerSettings _settings;
        private LayerJobData _jobData;
        private TransformStreamHandle _weaponHandle;
        private TransformStreamHandle _rightHandIkHandle;
        private TransformStreamHandle _leftHandIkHandle;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!KAnimationMath.IsWeightRelevant(_jobData.weight))
            {
                return;
            }
            
            AnimLayerJobUtility.ModifyTransform(stream, _jobData.rootHandle, _weaponHandle, _settings.ikHandGun, 
                _jobData.weight);
            AnimLayerJobUtility.ModifyTransform(stream, _jobData.rootHandle, _rightHandIkHandle, _settings.ikHandRight, 
                _jobData.weight);
            AnimLayerJobUtility.ModifyTransform(stream, _jobData.rootHandle, _leftHandIkHandle, _settings.ikHandLeft, 
                _jobData.weight);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (ViewLayerSettings) settings;
            _jobData = jobData;

            var transform = jobData.rigComponent.GetRigTransform(_settings.ikHandGun.element);
            _weaponHandle = jobData.animator.BindStreamTransform(transform);
            
            // Required for correct ads calculation.
            KAnimationMath.ModifyTransform(jobData.Owner, transform, _settings.ikHandGun);
            
            transform = jobData.rigComponent.GetRigTransform(_settings.ikHandRight.element);
            _rightHandIkHandle = jobData.animator.BindStreamTransform(transform);
            
            transform = jobData.rigComponent.GetRigTransform(_settings.ikHandLeft.element);
            _leftHandIkHandle = jobData.animator.BindStreamTransform(transform);
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