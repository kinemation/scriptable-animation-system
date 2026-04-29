using KINEMATION.FPSAnimationFramework.Runtime.Core;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.PoseOffsetLayer
{
    public struct PoseOffsetJob : IAnimationJob, IAnimationLayerJob
    {
        private PoseOffsetLayerSettings _settings;
        
        // Animation Job
        private LayerJobData _jobData;
        private NativeArray<TransformStreamHandle> _offsetBones;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (Mathf.Approximately(_jobData.weight, 0f))
            {
                return;
            }
            
            int count = _offsetBones.Length;
            for (int i = 0; i < count; i++)
            {
                var poseOffset = _settings.poseOffsets[i];
                AnimLayerJobUtility.ModifyTransform(stream, _jobData.rootHandle, _offsetBones[i], 
                    poseOffset.pose, _jobData.weight);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (PoseOffsetLayerSettings) settings;
            _jobData = jobData;

            int count = _settings.poseOffsets.Count;
            _offsetBones = new NativeArray<TransformStreamHandle>(count, Allocator.Persistent);

            for (int i = 0; i < count; i++)
            {
                var transform = _jobData.rigComponent.GetRigTransform(_settings.poseOffsets[i].pose.element);
                _offsetBones[i] = _jobData.animator.BindStreamTransform(transform);
            }
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
            if (_offsetBones.IsCreated) _offsetBones.Dispose();
        }
    }
}