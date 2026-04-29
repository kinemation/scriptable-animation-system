// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Playables;

using System.Collections.Generic;
using System;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Core
{
    public struct AnimationBlendingJob : IAnimationJob
    {
        public NativeArray<TransformStreamPose> poses;
        public bool cachePose;
        public float blendTime;
        public EaseMode easeMode;
        
        private float _playback;
        private bool _blendCachedPose;

        public void Setup(Animator animator, KRigComponent rigComponent)
        {
            var hierarchy = rigComponent.GetRigTransforms();
            int count = hierarchy.Length;
            
            poses = new NativeArray<TransformStreamPose>(count, Allocator.Persistent);
            
            for (int i = 0; i < count; i++)
            {
                poses[i] = new TransformStreamPose()
                {
                    handle = animator.BindStreamTransform(hierarchy[i]),
                    pose = KTransform.Identity
                };
            }
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            int count = poses.Length;
            
            if (_playback >= blendTime) _blendCachedPose = false;
            
            if (_blendCachedPose)
            {
                _playback += stream.deltaTime;
                
                for (int i = 0; i < count; i++)
                {
                    var element = poses[i];
                    
                    float weight = KCurves.Ease(0f, 1f, Mathf.Clamp01(_playback / blendTime), easeMode);
                    var activePose = AnimLayerJobUtility.GetTransformFromHandle(stream, element.handle, false);
                    activePose = KTransform.Lerp(element.pose, activePose, weight);

                    element.handle.SetLocalPosition(stream, activePose.position);
                    element.handle.SetLocalRotation(stream, activePose.rotation);
                }
            }
            
            if (cachePose)
            {
                for (int i = 0; i < count; i++)
                {
                    var element = poses[i];
                    element.pose = AnimLayerJobUtility.GetTransformFromHandle(stream, element.handle, false);
                    poses[i] = element;
                }

                _blendCachedPose = true;
                cachePose = false;
                _playback = 0f;
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
    
    public struct AnimationLayer
    {
        public AnimationScriptPlayable playable;
        public IAnimationLayerJob job;
    }

    public struct VirtualElementHandle
    {
        [ReadOnly] public TransformStreamHandle targetHandle;
        public TransformStreamHandle ikTargetHandle;
    }

    public struct VirtualElementJob : IAnimationJob
    {
        public NativeArray<VirtualElementHandle> handles;

        public void Setup(Animator animator, GameObject root)
        {
            var virtualElements = root.GetComponentsInChildren<KVirtualElement>();
            int count = virtualElements.Length;

            handles = new NativeArray<VirtualElementHandle>(count, Allocator.Persistent);
            for (int i = 0; i < count; i++)
            {
                handles[i] = new VirtualElementHandle()
                {
                    targetHandle = animator.BindStreamTransform(virtualElements[i].targetBone),
                    ikTargetHandle = animator.BindStreamTransform(virtualElements[i].transform)
                };
            }
        }
        
        public void ProcessAnimation(AnimationStream stream)
        {
            foreach (var handle in handles)
            {
                AnimLayerJobUtility.CopyBone(stream, handle.targetHandle, handle.ikTargetHandle);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
    
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/workflow/components")]
    public class FPSBoneController : MonoBehaviour
    {
        protected KRigComponent _rigComponent;
        protected IPlayablesController _playablesController;
        protected UserInputController _inputController;
        protected FPSAnimatorProfile _activeProfile;

        protected FPSAnimatorEntity _entity;
        
        protected List<AnimationLayer> _animationLayers;
        protected AnimationPlayableOutput _proceduralOutput;

        protected AnimationBlendingJob _blendingJob;
        protected AnimationScriptPlayable _blendingPlayable;
        protected FPSAnimatorProfile _newProfile;
        protected LayerJobData _layerJobData;

        protected bool _linkLayers = false;

        protected VirtualElementJob _virtualElementJob;
        protected AnimationScriptPlayable _virtualElementPlayable;

        protected List<FPSAnimatorLayerSettings> _settingsToLink = new List<FPSAnimatorLayerSettings>();

        protected void UnlinkAnimationLayers()
        {
            foreach (var layer in _animationLayers)
            {
                layer.job.Destroy();
                if(layer.playable.IsValid()) layer.playable.Destroy();
            }
            
            _blendingPlayable.DisconnectInput(0);
            _animationLayers.Clear();
        }

        protected void LinkAnimationLayers()
        {
            _activeProfile = _newProfile;
            
            // 1. Destroy existing layers.
            UnlinkAnimationLayers();
            
            // 2. Allocate new features.
            foreach (var setting in _newProfile.settings)
            {
                var job = setting.CreateAnimationJob();
                if (job == null) continue;
                
                job.Initialize(_layerJobData, setting);
                job.UpdateEntity(_entity);
                
                var playable = job.CreatePlayable(_playablesController.GetPlayableGraph());
                
                _animationLayers.Add(new AnimationLayer()
                {
                    job = job,
                    playable = playable
                });
            }
            
            // 3. Chain new animation layers.
            int count = _animationLayers.Count;
            for (int i = 1; i < count; ++i)
            {
                _animationLayers[i].playable.AddInput(_animationLayers[i - 1].playable, 0);
            }

            _animationLayers[0].playable.AddInput(_virtualElementPlayable, 0);
            _blendingPlayable.ConnectInput(0, _animationLayers[count - 1].playable, 0, 1f);
            
            // 4. Update source playable.
            _proceduralOutput.SetSourcePlayable(_blendingPlayable);

            // 5. Link queued animation layers.
            foreach (var setting in _settingsToLink) LinkAnimatorLayer(setting);
            _settingsToLink.Clear();
        }
        
        protected float GetLayerWeight(FPSAnimatorLayerSettings settings)
        {
            // Enable by default.
            float weight = 1f;

            // Invalid settings.
            if (settings == null)
            {
                return 0f;
            }

            // Scale the weight based on the curve values.
            foreach (var blend in settings.curveBlending)
            {
                float value = blend.ComputeBlendValue(_inputController, _playablesController);
                value = Mathf.Lerp(blend.clampMin, 1f, value);
                weight *= value;
            }

            // Finally scale the result by the global settings alpha.
            return Mathf.Clamp01(weight * settings.alpha);
        }
        
        protected void BuildPlayableOutput()
        {
            if (_proceduralOutput.IsOutputValid())
            {
                _playablesController.GetPlayableGraph().DestroyOutput(_proceduralOutput);
            }
            
            _proceduralOutput = AnimationPlayableOutput.Create(_playablesController.GetPlayableGraph(), 
                "ProceduralAnimationOutput", _playablesController.GetAnimator());
            _proceduralOutput.SetAnimationStreamSource(AnimationStreamSource.PreviousInputs);
        }

        public void RebuildPlayables()
        {
            BuildPlayableOutput();
            _proceduralOutput.SetSourcePlayable(_blendingPlayable);
        }

        public void Initialize()
        {
            _rigComponent = GetComponentInChildren<KRigComponent>();
            _rigComponent.Initialize();

            _playablesController = GetComponent<FPSPlayablesController>();
            _inputController = GetComponent<UserInputController>();

            _animationLayers = new List<AnimationLayer>();

            BuildPlayableOutput();

            _blendingJob = new AnimationBlendingJob();
            _blendingJob.Setup(_playablesController.GetAnimator(), _rigComponent);
            
            _blendingPlayable = AnimationScriptPlayable.Create(_playablesController.GetPlayableGraph(), _blendingJob, 1);
            
            _virtualElementJob = new VirtualElementJob();
            _virtualElementJob.Setup(_playablesController.GetAnimator(), gameObject);
            
            _virtualElementPlayable =
                AnimationScriptPlayable.Create(_playablesController.GetPlayableGraph(), _virtualElementJob);
            
            _layerJobData = new LayerJobData()
            {
                animator = _playablesController.GetAnimator(),
                rootHandle = _playablesController.GetAnimator().BindSceneTransform(transform),
                rigComponent = _rigComponent,
                inputController = _inputController,
                playablesController = _playablesController
            };
            
            if (_rigComponent == null)
            {
                Debug.LogError("FPSAnimatorBoneController: no RigComponent found!");
            }
        }

        public virtual void UpdateController()
        {
            if (_linkLayers)
            {
                if (_newProfile == null)
                {
                    UnlinkAnimationLayers();
                }
                else
                {
                    LinkAnimationLayers();
                }

                _linkLayers = false;
            }

            int count = _animationLayers.Count;

            for (int i = 0; i < count; i++)
            {
                var layer = _animationLayers[i];
                layer.job.OnPreGameThreadUpdate();
            }

            for (int i = 0; i < count; i++)
            {
                var layer = _animationLayers[i];
                float weight = GetLayerWeight(layer.job.GetSettingAsset());
                layer.job.UpdatePlayableJobData(layer.playable, weight);
            }
        }

        public virtual void LateUpdateController()
        {
            if (_blendingJob.cachePose)
            {
                _linkLayers = true;
                _blendingJob.cachePose = false;
                return;
            }
            
            int count = _animationLayers.Count;

            for (int i = 0; i < count; i++)
            {
                _animationLayers[i].job.LateUpdate();
            }
        }

        public void UnlinkAnimatorProfile()
        {
            if (_activeProfile == null) return;
            
            _newProfile = null;
            if (Mathf.Approximately(_activeProfile.blendOutTime, 0f))
            {
                _activeProfile = null;
                UnlinkAnimationLayers();
                
                _blendingJob.cachePose = false;
                _blendingJob.blendTime = 0f;
                _blendingPlayable.SetJobData(_blendingJob);
                return;
            }
            
            var job = _blendingPlayable.GetJobData<AnimationBlendingJob>();

            _blendingJob.cachePose = true;
            job.cachePose = true;
            job.blendTime = _activeProfile.blendInTime;
            
            _blendingPlayable.SetJobData(job);
        }

        public void LinkAnimatorProfile(FPSAnimatorProfile newProfile)
        {
            if (newProfile == null)
            {
                Debug.LogWarning($"FPSBoneController: Profile is null, use UnlinkAnimatorProfile instead");
                return;
            }

            if (newProfile.Equals(_newProfile)) return;
            if (!newProfile.IsValid())
            {
                UnlinkAnimatorProfile();
                return;
            }

            _newProfile = newProfile;

            if (_activeProfile == null || Mathf.Approximately(newProfile.blendInTime, 0f))
            {
                LinkAnimationLayers();

                _blendingJob.cachePose = false;
                _blendingJob.blendTime = 0f;
                _blendingPlayable.SetJobData(_blendingJob);
                return;
            }

            var job = _blendingPlayable.GetJobData<AnimationBlendingJob>();

            _blendingJob.cachePose = true;
            job.cachePose = true;
            job.blendTime = newProfile.blendInTime;
            job.easeMode = newProfile.easeMode;
            
            _blendingPlayable.SetJobData(job);
        }

        // Will find and update settings for the current active animator state.
        public void LinkAnimatorLayer(FPSAnimatorLayerSettings newSettings)
        {
            if (_activeProfile != _newProfile)
            {
                if (_newProfile != null) _settingsToLink.Add(newSettings);
                return;
            }
            
            Type newSettingType = newSettings.GetType();
            
            foreach (var layer in _animationLayers)
            {
                if (layer.job.GetSettingAsset().GetType() == newSettingType) layer.job.OnLayerLinked(newSettings);
            }
        }

        public void UpdateEntity(FPSAnimatorEntity newEntity)
        {
            _entity = newEntity;
        }

        public void Dispose()
        {
            foreach (var layer in _animationLayers)
            {
                layer.job.Destroy();
                layer.playable.Destroy();
            }
            
            _animationLayers.Clear();
            if (_blendingJob.poses.IsCreated) _blendingJob.poses.Dispose();
            if (_virtualElementJob.handles.IsCreated) _virtualElementJob.handles.Dispose();
        }
    }
}