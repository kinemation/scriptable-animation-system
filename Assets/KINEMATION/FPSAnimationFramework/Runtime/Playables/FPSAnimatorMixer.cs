using System;
using System.Collections.Generic;
using System.Linq;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace KINEMATION.FPSAnimationFramework.Runtime.Playables
{
    [Serializable]
    public struct BlendTime
    {
        [Min(0f)] public float blendInTime;
        [Min(0f)] public float blendOutTime;
        [Min(0f)] public float rateScale;
        [NonSerialized] public float startTime;
        [NonSerialized] public float endTime;

        public BlendTime(float blendIn, float blendOut)
        {
            blendInTime = blendIn;
            blendOutTime = blendOut;
            rateScale = 1f;
            startTime = endTime = 0f;
        }
    }
    
    [Serializable]
    public struct AnimCurve
    {
        [FormerlySerializedAs("Name")] 
        [CurveSelector(false, true, false)] public string name;
        [FormerlySerializedAs("Curve")] public AnimationCurve curve;
        [NonSerialized] public float valueCache;
    }
    
    public struct FPSAnimatorPlayable
    {
        public AnimationClipPlayable playable;
        public AnimCurve[] curves;
        public BlendTime blendTime;
        public float cachedWeight;
        public bool autoBlendOut;

        public FPSAnimatorPlayable(PlayableGraph graph, AnimationClip clip, AnimCurve[] curves)
        {
            playable = AnimationClipPlayable.Create(graph, clip);
            blendTime = new BlendTime(0f, 0f);
            this.curves = curves;
            cachedWeight = 0f;
            autoBlendOut = true;
        }
        
        public float GetLength()
        {
            return playable.IsValid() ? playable.GetAnimationClip().length : 0f;
        }

        public bool IsValid()
        {
            return playable.IsValid();
        }

        public void Release()
        {
            if (playable.IsValid()) playable.Destroy();
        }
    }
    
    public struct FPSAnimatorMixer
    {
        public AnimationLayerMixerPlayable mixer;

        public float BlendInWeight { get; private set; }
        public float BlendOutWeight { get; private set; }
        
        private BlendTime _blendTime;
        private int _activeIndex;
        
        private List<FPSAnimatorPlayable> _playables;
        private Dictionary<string, AnimCurve> _blendedCurves;

        private bool _isMixerActive;
        private bool _blendingIn;
        private bool _autoBlendOut;

        private int _layerLevel;
        
        public FPSAnimatorMixer(PlayableGraph graph, int inputCount, int layerLevel = 0)
        {
            mixer = AnimationLayerMixerPlayable.Create(graph, inputCount + layerLevel);
            
            _playables = new List<FPSAnimatorPlayable>();
            for (int i = 0; i < inputCount; i++)
            {
                _playables.Add(new FPSAnimatorPlayable());
            }
            
            _activeIndex = -1;
            BlendInWeight = BlendOutWeight = 0f;
            
            _blendedCurves = new Dictionary<string, AnimCurve>();
            _blendTime = new BlendTime(0f, 0f);
            
            _isMixerActive = _blendingIn = _autoBlendOut = false;
            _layerLevel = layerLevel;
        }
        
        public void Play(FPSAnimatorPlayable clip, AvatarMask mask, bool bAdditive = false)
        {
            // Prepare the new playable and curves.
            AddUserCurves(clip.curves);
            UpdateActiveIndex();
            
            _playables[_activeIndex - _layerLevel] = clip;
            
            // Connect new playable.
            mixer.ConnectInput(_activeIndex, clip.playable, 0, 0f);
            mixer.SetLayerMaskFromAvatarMask((uint) _activeIndex, mask);
            mixer.SetLayerAdditive((uint) _activeIndex, bAdditive);

            // Initialize blending properties.
            _blendTime = clip.blendTime;
            _blendTime.endTime = clip.GetLength();
            BlendInWeight = BlendOutWeight = 0f;
            
            _blendingIn = _isMixerActive = true;
            _autoBlendOut = clip.autoBlendOut;
        }
        
        public void Update()
        {
            if (!_isMixerActive)
            {
                return;
            }
            
            if (_blendingIn)
            {
                BlendInPlayable();
                return;
            }
            
            BlendOutPlayable();
        }

        public void Stop(float blendOutTime)
        {
            if (!_isMixerActive) return;
            
            _blendingIn = false;
            _autoBlendOut = true;
            
            _blendTime.blendOutTime = blendOutTime;
            _blendTime.endTime = GetPlayingTime();

            if (_activeIndex == _layerLevel) return;
            
            //If we have inactive playables, cache their weights.
            for (int i = _layerLevel; i < _activeIndex; i++)
            {
                var inactivePlayable = _playables[i - _layerLevel];
                inactivePlayable.cachedWeight = mixer.GetInputWeight(i);
                _playables[i - _layerLevel] = inactivePlayable;
            }
        }

        public float GetCurveValue(string curveName)
        {
            if (_blendedCurves == null || !_blendedCurves.ContainsKey(curveName) || !_isMixerActive)
            {
                return 0f;
            }
            
            return GetBlendedCurveValue(_blendedCurves[curveName]);
        }

        private float GetPlayingTime()
        {
            return GetActivePlayable().IsValid() ? (float) GetActivePlayable().playable.GetTime() : 0f;
        }

        private float GetNormalizedTime()
        {
            if (Mathf.Approximately(_blendTime.endTime, 0f)) return 0f;
            return Mathf.Clamp01(GetPlayingTime() / _blendTime.endTime);
        }

        private FPSAnimatorPlayable GetActivePlayable()
        {
            return _playables[_activeIndex - _layerLevel];
        }
        
        private float GetBlendedCurveValue(AnimCurve blendedAnimCurve)
        {
            // Inactive curve
            if (blendedAnimCurve.curve == null)
            {
                return Mathf.Lerp(blendedAnimCurve.valueCache, 0f, BlendInWeight);
            }

            float rawValue = blendedAnimCurve.curve.Evaluate(GetNormalizedTime());
            
            float blendedValue = Mathf.Lerp(blendedAnimCurve.valueCache, rawValue, BlendInWeight);
            blendedValue = Mathf.Lerp(blendedValue, 0f, BlendOutWeight);

            return blendedValue;
        }
        
        private void AddUserCurves(AnimCurve[] newCurves)
        {
            var blendedCurves = _blendedCurves.ToArray();
            
            Dictionary<string, AnimCurve> userCurveSet = null;
            if (newCurves != null)
            {
                userCurveSet = newCurves.ToDictionary(curve => curve.name);
            }
            
            foreach (var curve in blendedCurves)
            {
                // Cache all blended curves.
                AnimCurve animCurve = curve.Value;
                animCurve.valueCache = GetBlendedCurveValue(animCurve);

                if (userCurveSet != null)
                {
                    // If the input curve is in both sets, refresh the curve reference.
                    // If the curve is not present in the user set, set to null.

                    if (userCurveSet.ContainsKey(curve.Key))
                    {
                        animCurve.curve = userCurveSet[curve.Key].curve;
                        userCurveSet.Remove(curve.Key);
                    }
                    else
                    {
                        animCurve.curve = null;
                    }
                }
                
                _blendedCurves[curve.Key] = animCurve;
            }

            if (newCurves == null) return;
            
            // Add new curves.
            foreach (var userCurve in userCurveSet)
            {
                _blendedCurves.Add(userCurve.Key, userCurve.Value);
            }
        }

        private void UpdateActiveIndex()
        {
            if (_activeIndex == -1)
            {
                _activeIndex = _layerLevel;
                return;
            }
            
            // Try to use the next slot
            if (_activeIndex + 1 < mixer.GetInputCount())
            {
                _activeIndex++;
                // Save current weights
                for (int i = _layerLevel; i < _activeIndex; i++)
                {
                    var clip = _playables[i - _layerLevel];
                    clip.cachedWeight = mixer.GetInputWeight(i);
                    _playables[i - _layerLevel] = clip;
                }
                return;
            }

            _playables[0].Release();
            mixer.DisconnectInput(_layerLevel);
            // Reconnect
            for (int i = _layerLevel; i < mixer.GetInputCount() - 1; i++)
            {
                float inputWeight = mixer.GetInputWeight(i + 1);
                FPSAnimatorPlayable clip = _playables[i + 1 - _layerLevel];
                clip.cachedWeight = inputWeight;
                _playables[i - _layerLevel] = clip;
                
                var source = mixer.GetInput(i + 1);
                mixer.DisconnectInput(i + 1);
                mixer.ConnectInput(i, source, 0, inputWeight);
            }
            
            _activeIndex = mixer.GetInputCount() - 1;
            mixer.DisconnectInput(_activeIndex);
        }
        
        private void BlendOutInactive()
        {
            for (int i = _layerLevel; i < _activeIndex; i++)
            {
                if (!_blendingIn)
                {
                    mixer.DisconnectInput(i);
                    _playables[i - _layerLevel].Release();
                    continue;
                }

                float weight = _playables[i - _layerLevel].cachedWeight;
                weight = Mathf.Lerp(weight, 0f, BlendInWeight);
                mixer.SetInputWeight(i, weight);
            }

            if (_blendingIn) return;
            
            var curves = _blendedCurves.ToArray();
            foreach (var curve in curves)
            {
                if (curve.Value.curve == null) _blendedCurves.Remove(curve.Key);
            }
        }
        
        private void BlendInPlayable()
        {
            float alpha = 1f;
            if (!Mathf.Approximately(_blendTime.blendInTime, 0f))
            {
                alpha = (GetPlayingTime() - _blendTime.startTime) / _blendTime.blendInTime;
            }
            
            BlendInWeight = Mathf.Clamp01(alpha);
            mixer.SetInputWeight(_activeIndex, BlendInWeight);

            if (Mathf.Approximately(BlendInWeight, 1f))
            {
                _blendingIn = false;
                _isMixerActive = _autoBlendOut;
            }
            
            BlendOutInactive();
        }

        private void BlendOutPlayable()
        {
            if (GetPlayingTime() < _blendTime.endTime) return;
            
            float alpha = 1f;
            if (!Mathf.Approximately(_blendTime.blendOutTime, 0f))
            {
                alpha = (GetPlayingTime() - _blendTime.endTime) / _blendTime.blendOutTime;
            }
            
            BlendOutWeight = Mathf.Clamp01(alpha);
            mixer.SetInputWeight(_activeIndex,Mathf.Lerp(BlendInWeight, 0f, BlendOutWeight));
            
            // In case of force blending out.
            if (_activeIndex > _layerLevel)
            {
                for (int i = _layerLevel; i < _activeIndex; i++)
                {
                    float cache = _playables[i - _layerLevel].cachedWeight;
                    float inactiveWeight = Mathf.Lerp(cache, 0f, BlendOutWeight);
                    mixer.SetInputWeight(i, inactiveWeight);
                }
            }

            if (!Mathf.Approximately(BlendOutWeight, 1f)) return;

            for (int i = _layerLevel; i <= _activeIndex; i++)
            {
                mixer.DisconnectInput(i);
                _playables[i - _layerLevel].Release();
            }
            
            _activeIndex = -1;
            BlendOutWeight = 1f;

            _blendedCurves.Clear();
            _isMixerActive = false;
        }
    }
}