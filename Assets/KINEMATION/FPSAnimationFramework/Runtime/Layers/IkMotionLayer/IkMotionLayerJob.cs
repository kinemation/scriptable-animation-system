using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.IkMotionLayer
{
    public struct IkMotionLayerJob : IAnimationJob, IAnimationLayerJob
    {
        public KTransform result;
        public LayerJobData jobData;
        public KTransform cachedResult;
        
        public bool isPlaying;
        public float playback;
        
        public IkMotionLayerSettings settings;
        
        // Animation Job
        private TransformStreamHandle _boneToAnimate;
        private KTransform _animation;
        private float _length;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!isPlaying || !KAnimationMath.IsWeightRelevant(jobData.weight))
            {
                return;
            }
            
            if (isPlaying)
            {
                float blendAlpha = 1f;
                if (!Mathf.Approximately(settings.blendTime, 0f))
                {
                    blendAlpha = Mathf.Clamp01(playback / settings.blendTime);
                }

                _animation.rotation = Quaternion.Euler(settings.rotationCurves.GetValue(playback));
                _animation.position = settings.translationCurves.GetValue(playback);
            
                // Blend between the cache and current value.
                result.rotation = Quaternion.Slerp(cachedResult.rotation, _animation.rotation, blendAlpha);
                result.position = Vector3.Lerp(cachedResult.position, _animation.position, blendAlpha);
            }
            
            AnimLayerJobUtility.MoveInSpace(stream, jobData.rootHandle, _boneToAnimate, result.position, 
                jobData.weight);
            AnimLayerJobUtility.RotateInSpace(stream, jobData.rootHandle, _boneToAnimate, result.rotation,
                jobData.weight);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData newJobData, FPSAnimatorLayerSettings newSettings)
        {
            settings = (IkMotionLayerSettings) newSettings;
            jobData = newJobData;

            var boneTransform = newJobData.rigComponent.GetRigTransform(this.settings.boneToAnimate);
            _boneToAnimate = newJobData.animator.BindStreamTransform(boneTransform);

            cachedResult = result = KTransform.Identity;
        }

        public AnimationScriptPlayable CreatePlayable(PlayableGraph graph)
        {
            return AnimationScriptPlayable.Create(graph, this);
        }

        public FPSAnimatorLayerSettings GetSettingAsset()
        {
            return settings;
        }

        public void OnLayerLinked(FPSAnimatorLayerSettings newSettings)
        {
            settings = (IkMotionLayerSettings) newSettings;
            
            isPlaying = true;
            cachedResult = result;
            playback = 0f;

            _length = Mathf.Max(settings.rotationCurves.GetCurveLength(), 
                settings.translationCurves.GetCurveLength());
        }

        public void UpdateEntity(FPSAnimatorEntity newEntity)
        {
        }
        
        public void OnPreGameThreadUpdate()
        {
        }
        
        public void UpdatePlayableJobData(AnimationScriptPlayable playable, float weight)
        {
            var job = playable.GetJobData<IkMotionLayerJob>();

            result = job.result;
            playback = Mathf.Clamp(playback + Time.deltaTime * settings.playRate, 0f, _length);
            
            if (Mathf.Approximately(playback, 1f) && settings.autoBlendOut) isPlaying = false;

            job.settings = settings;
            job.isPlaying = isPlaying;
            job.playback = playback;
            job.cachedResult = cachedResult;
            job.jobData.weight = weight;
            
            playable.SetJobData(job);
        }
        
        public void LateUpdate()
        {
        }

        public void Destroy()
        {
        }
    }
}