using System.Collections.Generic;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.LookLayer
{
    public struct LookLayerAtom
    {
        public TransformStreamHandle handle;
        public Vector2 clampedAngle;
    }
    
    public struct LookLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private LookLayerSettings _settings;
        private Vector4 _lookInput;
        
        private int _mouseInputPropertyIndex;
        private int _leanPropertyIndex;
        private int _turnProperty;
        
        // Animation Job
        private LayerJobData _jobData;
        private NativeArray<LookLayerAtom> _rollElements;
        private NativeArray<LookLayerAtom> _pitchElements;
        private NativeArray<LookLayerAtom> _yawElements;
        
        private void SetupChain(out NativeArray<LookLayerAtom> chain, List<LookLayerElement> elements)
        {
            int count = elements.Count;
            chain = new NativeArray<LookLayerAtom>(count, Allocator.Persistent);
            for (int i = 0; i < count; i++)
            {
                var transform = _jobData.rigComponent.GetRigTransform(elements[i].rigElement);
                chain[i] = new LookLayerAtom()
                {
                    handle = _jobData.animator.BindStreamTransform(transform),
                    clampedAngle = elements[i].clampedAngle
                };
            }
        }
        
        public void ProcessAnimation(AnimationStream stream)
        {
            float fraction = _lookInput.z / 90f;
            bool sign = fraction > 0f;
            
            foreach (var element in _rollElements)
            {
                float angle = sign ? element.clampedAngle.x : element.clampedAngle.y;
                
                AnimLayerJobUtility.RotateInSpace(stream, _jobData.rootHandle, element.handle, 
                    Quaternion.Euler(0f, 0f, angle * fraction), _jobData.weight);
            }
            
            fraction = _lookInput.x / 90f;
            sign = fraction > 0f;
            
            foreach (var element in _yawElements)
            {
                float angle = sign ? element.clampedAngle.x : element.clampedAngle.y;
                AnimLayerJobUtility.RotateInSpace(stream, _jobData.rootHandle, element.handle, 
                    Quaternion.Euler(0f, angle * fraction, 0f), _jobData.weight);
            }
            
            fraction = _lookInput.y / 90f;
            sign = fraction > 0f;
            
            Quaternion spaceRotation = _jobData.rootHandle.GetRotation(stream) 
                                       * Quaternion.Euler(0f, _lookInput.x, 0f);

            foreach (var element in _pitchElements)
            {
                float angle = sign ? element.clampedAngle.x : element.clampedAngle.y;

                Quaternion rotation = element.handle.GetRotation(stream);
                rotation = KAnimationMath.RotateInSpace(spaceRotation, rotation, 
                    Quaternion.Euler(angle * fraction, 0f, 0f), _jobData.weight);
                element.handle.SetRotation(stream, rotation);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (LookLayerSettings) settings;
            _jobData = jobData;
            
            _mouseInputPropertyIndex = _jobData.inputController.GetPropertyIndex(_settings.mouseInputProperty);
            _leanPropertyIndex = _jobData.inputController.GetPropertyIndex(_settings.leanInputProperty);
            _turnProperty = _jobData.inputController.GetPropertyIndex(_settings.turnOffsetProperty);

            SetupChain(out _rollElements, _settings.rollOffsetElements);
            SetupChain(out _pitchElements, _settings.pitchOffsetElements);
            SetupChain(out _yawElements, _settings.yawOffsetElements);
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
            
            _lookInput = _jobData.inputController.GetValue<Vector4>(_mouseInputPropertyIndex);
            _lookInput.z = _jobData.inputController.GetValue<float>(_leanPropertyIndex);
            
            if (_settings.useTurnOffset)
            {
                _lookInput.x = _jobData.inputController.GetValue<float>(_turnProperty);
            }
            
            playable.SetJobData(this);
        }
        
        public void LateUpdate()
        {
        }

        public void Destroy()
        {
            if (_rollElements.IsCreated) _rollElements.Dispose();
            if (_pitchElements.IsCreated) _pitchElements.Dispose();
            if (_yawElements.IsCreated) _yawElements.Dispose();
        }
    }
}