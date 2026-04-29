using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;

using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.BlendingLayer
{
    public struct BlendJobAtom
    {
        public KTransform activePose;
        public KTransform cachedPose;
        public TransformStreamHandle handle;

        //public float weight;
    }
    
    public struct BlendingLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private BlendingLayerSettings _settings;
        private LayerJobData _jobData;
        
        // Animation Job
        private NativeArray<BlendJobAtom> _blendingElements;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!KAnimationMath.IsWeightRelevant(_jobData.weight))
            {
                return;
            }

            KTransform rootTransform = AnimLayerJobUtility.GetTransformFromHandle(stream, _jobData.rootHandle);

            // Cache the current active pose.
            int count = _blendingElements.Length;
            for (int i = 0; i < count; i++)
            {
                var atom = _blendingElements[i];
                atom.activePose = AnimLayerJobUtility.GetTransformFromHandle(stream, atom.handle);
                _blendingElements[i] = atom;
            }
            
            for (int i = 0; i < count; i++)
            {
                var atom = _blendingElements[i];

                var target = rootTransform.GetWorldTransform(atom.cachedPose, false);
                float weight = _jobData.weight * _settings.blendingElements[i].weight;

                if (_settings.blendPosition) atom.handle.SetPosition(stream, 
                    Vector3.Lerp(atom.activePose.position, target.position, weight));
                
                atom.handle.SetRotation(stream, 
                    Quaternion.Slerp(atom.activePose.rotation, target.rotation, weight));
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (BlendingLayerSettings) settings;
            _jobData = jobData;
            
            if (_settings.desiredPose == null) return;
            
            jobData.rigComponent.CacheHierarchyPose();
            _settings.desiredPose.SampleAnimation(jobData.Owner.gameObject, 0f);

            int count = _settings.blendingElements.Count;
            _blendingElements = new NativeArray<BlendJobAtom>(count, Allocator.Persistent);

            for (int i = 0; i < count; i++)
            {
                var transform = jobData.rigComponent.GetRigTransform(_settings.blendingElements[i].elementToBlend);
                var basePose = KTransform.Identity;
                var cachedPose = new KTransform(_jobData.Owner).GetRelativeTransform(new KTransform(transform),
                    false);

                _blendingElements[i] = new BlendJobAtom()
                {
                    activePose = basePose,
                    cachedPose = cachedPose,
                    handle = jobData.animator.BindStreamTransform(transform)
                };
            }
            
            jobData.rigComponent.ApplyHierarchyCachedPose();
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
            if (_blendingElements.IsCreated) _blendingElements.Dispose();
        }
    }
}