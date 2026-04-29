// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KINEMATION.ProceduralRecoilAnimationSystem.Runtime
{
    public enum FireMode
    {
        Semi,
        Burst,
        Auto
    }
    
    public struct StartRest
    {
        public StartRest(bool x, bool y, bool z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
	
        public bool x;
        public bool y;
        public bool z;
    }

    public delegate bool ConditionDelegate();
    public delegate void PlayDelegate();
    public delegate void StopDelegate();
    
    public struct AnimState
    {
        public ConditionDelegate checkCondition;
        public PlayDelegate onPlay;
        public StopDelegate onStop;
    }
    
    [Serializable]
    public struct RecoilCurves
    {
        public VectorCurve semiRotCurve;
        public VectorCurve semiLocCurve;
        public VectorCurve autoRotCurve;
        public VectorCurve autoLocCurve;
        
        public static float GetMaxTime(AnimationCurve curve)
        {
            return curve[curve.length - 1].time;
        }

        public RecoilCurves(Keyframe[] keyFrame)
        {
            semiRotCurve = new VectorCurve(keyFrame);
            semiLocCurve = new VectorCurve(keyFrame);
            autoRotCurve = new VectorCurve(keyFrame);
            autoLocCurve = new VectorCurve(keyFrame);
        }
    }
    
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/recoil-system/recoil-animation")]
    [AddComponentMenu("KINEMATION/Recoil Animation")]
    public class RecoilAnimation : MonoBehaviour
    {
        public RecoilAnimData recoilData;
        public bool isAiming;
        public FireMode fireMode;

        public KTransform RecoilTransform => new KTransform(OutLoc, OutRot);
        
        public Quaternion OutRot { get; private set; }
        public Vector3 OutLoc { get; private set; }
        
        // This property exists for compatibility reasons only.
        public RecoilAnimData RecoilData => recoilData;
        
        private float _fireRate;
        private List<AnimState> _stateMachine;
        private int _stateIndex;

        private Vector3 _targetRot;
        private Vector3 _targetLoc;

        private VectorCurve _tempRotCurve;
        private VectorCurve _tempLocCurve;

        private Vector3 _startValRot;
        private Vector3 _startValLoc;

        private StartRest _canRestRot;
        private StartRest _canRestLoc;

        private Vector3 _rawRotOut;
        private Vector3 _rawLocOut;

        private Vector3 _smoothRotOut;
        private Vector3 _smoothLocOut;

        private Vector2 _noiseTarget;
        private Vector2 _noiseOut;

        private float _pushTarget;
        private float _pushOut;
    
        private float _lastFrameTime;
        private float _playBack;
        private float _lastTimeShot;
    
        private bool _isPlaying;
        private bool _isLooping;
        private bool _enableSmoothing;

        private Vector2 _pitchSway;
        private Vector2 _yawSway;
        
        private Vector2 _pitchProgress;
        private Vector2 _upProgress;
        
        // Controller recoil.
        private Vector2 _deltaInput;
        private Vector2 _compensation;
        private Vector2 _targetRecoil;
        private Vector2 _recoil;
        private Vector2 _cachedRecoil;
        private Vector2 _recoilDelta;
        private bool _isFiring;
        
        public Vector2 GetRecoilDelta()
        {
            return _recoilDelta;
        }

        public void UpdateDeltaInput(Vector2 deltaInput)
        {
            if (recoilData == null) return;
            _deltaInput = deltaInput;
        }
        
        private float Compensate(float recoil, float compensation)
        {
            float multiplier = 1f;
            bool isOpposite = recoil * compensation <= 0f;
	
            if(!Mathf.Approximately(compensation, 0f) && isOpposite)
            {
                multiplier -= Mathf.Clamp01(Mathf.Abs(compensation / recoil));
            }
            
            return multiplier;
        }
        
        private void UpdateControllerRecoil()
        {
            if (_isFiring)
            {
                // Accumulate player delta input when firing.
                _compensation.x += _deltaInput.x;
                _compensation.y += _deltaInput.y;
            }
            
            float alpha = KMath.ExpDecayAlpha(recoilData.horizontalSmoothing, Time.deltaTime);
            _recoil.x = Mathf.Lerp(_recoil.x, _targetRecoil.x, alpha);
            
            alpha = KMath.ExpDecayAlpha(recoilData.verticalSmoothing, Time.deltaTime);
            _recoil.y = Mathf.Lerp(_recoil.y, _targetRecoil.y, alpha);

            if (!_isFiring)
            {
                alpha = KMath.ExpDecayAlpha(recoilData.damping, Time.deltaTime);
                _targetRecoil = Vector2.Lerp(_targetRecoil, Vector2.zero, alpha);
            }
            
            _recoilDelta = _recoil - _cachedRecoil;
            _cachedRecoil = _recoil;
        }
    
        public void Init(RecoilAnimData data, float fireRate, FireMode newFireMode)
        {
            fireMode = newFireMode;
            
            recoilData = data;

            OutRot = Quaternion.identity;
            OutLoc = Vector3.zero;

            if (Mathf.Approximately(fireRate, 0f))
            {
                _fireRate = 0.001f;
                Debug.LogWarning("RecoilAnimation: FireRate is zero!");
            }

            _fireRate = fireRate;
            _targetRot = Vector3.zero;
            _targetLoc = Vector3.zero;

            _pushTarget = 0f;
            _noiseTarget = Vector2.zero;
            
            _compensation = _recoilDelta = _targetRecoil = _cachedRecoil = _recoil = Vector2.zero;

            SetupStateMachine();
        }

        public void Play()
        {
            if (recoilData == null) return;
            
            if (!_isFiring) _compensation = _recoilDelta = Vector2.zero;
            
            _isFiring = true;
            _targetRecoil.x += Random.Range(recoilData.horizontalRecoil.x, recoilData.horizontalRecoil.y);
            _targetRecoil.y += Random.Range(recoilData.verticalRecoil.x, recoilData.verticalRecoil.y);
            
            //Iterate through each transition, if true execute
            for (int i = 0; i < _stateMachine.Count; i++)
            {
                if (_stateMachine[i].checkCondition.Invoke())
                {
                    _stateIndex = i;
                    break;
                }
            }
        
            _stateMachine[_stateIndex].onPlay.Invoke();
            _lastTimeShot = Time.unscaledTime;
        }

        public void Stop()
        {
            if (recoilData == null) return;
            
            _isFiring = false;
            
            _recoil.x *= Compensate(_recoil.x, _compensation.x);
            _recoil.y *= Compensate(_recoil.y, _compensation.y);
            _cachedRecoil = _recoil;
            _targetRecoil = _recoil;

            _stateMachine[_stateIndex].onStop.Invoke();
            _isLooping = false;
        }

        private void Start()
        {
            OutRot = Quaternion.identity;
            OutLoc = Vector3.zero;
        }

        private void Update()
        {
            if (recoilData == null) return;

            UpdateControllerRecoil();
            
            if (_isPlaying)
            {
                UpdateSolver();
                UpdateTimeline();
            }
        
            Vector3 finalLoc = _smoothLocOut;
            Vector3 finalEulerRot = _smoothRotOut;
            
            ApplyNoise(ref finalLoc);
            ApplyPushback(ref finalLoc);
            ApplyProgression(ref finalLoc, ref finalEulerRot);
            
            Quaternion finalRot = Quaternion.Euler(finalEulerRot);

            Vector3 pivotOffset = isAiming ? recoilData.aimPivotOffset : recoilData.hipPivotOffset;
            finalLoc += finalRot * pivotOffset - pivotOffset;
            
            ApplySway(ref finalLoc, ref finalRot);

            OutRot = finalRot;
            OutLoc = finalLoc;
        }

        private void CalculateTargetData()
        {
            float pitch = Random.Range(recoilData.pitch.x, recoilData.pitch.y);
            float yawMin = Random.Range(recoilData.yaw.x, recoilData.yaw.y);
            float yawMax = Random.Range(recoilData.yaw.z, recoilData.yaw.w);

            float yaw = Random.value >= 0.5f ? yawMax : yawMin;

            float rollMin = Random.Range(recoilData.roll.x, recoilData.roll.y);
            float rollMax = Random.Range(recoilData.roll.z, recoilData.roll.w);

            float roll = Random.value >= 0.5f ? rollMax : rollMin;

            roll = _targetRot.z * roll > 0f && recoilData.smoothRoll ? -roll : roll;

            float kick = Random.Range(recoilData.kickback.x, recoilData.kickback.y);
            float kickRight = Random.Range(recoilData.kickRight.x, recoilData.kickRight.y);
            float kickUp = Random.Range(recoilData.kickUp.x, recoilData.kickUp.y);

            _noiseTarget.x += Random.Range(recoilData.noiseX.x, recoilData.noiseX.y);
            _noiseTarget.y += Random.Range(recoilData.noiseY.x, recoilData.noiseY.y);

            _noiseTarget.x *= isAiming ? recoilData.noiseScalar : 1f;
            _noiseTarget.y *= isAiming ? recoilData.noiseScalar : 1f;

            pitch *= isAiming ? recoilData.aimRot.x : 1f;
            yaw *= isAiming ? recoilData.aimRot.y : 1f;
            roll *= isAiming ? recoilData.aimRot.z : 1f;

            kick *= isAiming ? recoilData.aimLoc.z : 1f;
            kickRight *= isAiming ? recoilData.aimLoc.x : 1f;
            kickUp *= isAiming ? recoilData.aimLoc.y : 1f;

            _targetRot = new Vector3(pitch, yaw, roll);
            _targetLoc = new Vector3(kickRight, kickUp, kick);
            
            // Compute target progression.
            float adsScalar = isAiming ? recoilData.adsProgressAlpha : 1f;
            _pitchProgress.y += recoilData.pitchProgress.amount * adsScalar;
            _upProgress.y += recoilData.upProgress.amount * adsScalar;
            
            // Compute target recoil sway.
            var recoilSway = recoilData.recoilSway;

            float value = Random.Range(recoilSway.pitchSway.x, recoilSway.pitchSway.y);
            if (isAiming) value *= recoilSway.adsScale;
            _pitchSway.y += value;

            value = Random.Range(recoilSway.yawSway.x, recoilSway.yawSway.y);
            if (isAiming) value *= recoilSway.adsScale;
            _yawSway.y += value;
        }

        private void UpdateTimeline()
        {
            _playBack += Time.deltaTime * recoilData.playRate;
            _playBack = Mathf.Clamp(_playBack, 0f, _lastFrameTime);

            // Stop updating if the end is reached
            if (Mathf.Approximately(_playBack, _lastFrameTime))
            {
                if (_isLooping)
                {
                    _playBack = 0f;
                    _isPlaying = true;
                }
                else
                {
                    _isPlaying = false;
                    _playBack = 0f;
                }
            }
        }

        private void UpdateSolver()
        {
            if (Mathf.Approximately(_playBack, 0f))
            {
                CalculateTargetData();
            }
        
            // Current playback position
            float lastPlayback = _playBack - Time.deltaTime * recoilData.playRate;
            lastPlayback = Mathf.Max(lastPlayback, 0f);

            Vector3 alpha = _tempRotCurve.GetValue(_playBack);
            Vector3 lastAlpha = _tempRotCurve.GetValue(lastPlayback);
            
            Vector3 output = Vector3.zero;

            output.x = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.x, alpha.x, ref _canRestRot.x, ref _startValRot.x),
                _targetRot.x, alpha.x);

            output.y = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.y, alpha.y, ref _canRestRot.y, ref _startValRot.y),
                _targetRot.y, alpha.y);

            output.z = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.z, alpha.z, ref _canRestRot.z, ref _startValRot.z),
                _targetRot.z, alpha.z);

            _rawRotOut = output;

            alpha = _tempLocCurve.GetValue(_playBack);
            lastAlpha = _tempLocCurve.GetValue(lastPlayback);

            output.x = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.x, alpha.x, ref _canRestLoc.x, ref _startValLoc.x),
                _targetLoc.x, alpha.x);

            output.y = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.y, alpha.y, ref _canRestLoc.y, ref _startValLoc.y),
                _targetLoc.y, alpha.y);

            output.z = Mathf.LerpUnclamped(
                CorrectStart(ref lastAlpha.z, alpha.z, ref _canRestLoc.z, ref _startValLoc.z),
                _targetLoc.z, alpha.z);

            _rawLocOut = output;
            ApplySmoothing();
        }

        private void ApplySmoothing()
        {
            if(_enableSmoothing)
            {
                Vector3 lerped = _smoothRotOut;

                Vector3 smooth = recoilData.smoothRot;

                Func<float, float, float, float, float> Interp = (a, b, speed, scale) =>
                {
                    scale = Mathf.Approximately(scale, 0f) ? 1f : scale;
                    return Mathf.Approximately(speed, 0f)
                        ? b * scale
                        : Mathf.Lerp(a, b * scale, KMath.ExpDecayAlpha(speed, Time.deltaTime));
                };

                lerped.x = Interp(_smoothRotOut.x, _rawRotOut.x, smooth.x, recoilData.extraRot.x);
                lerped.y = Interp(_smoothRotOut.y, _rawRotOut.y, smooth.y, recoilData.extraRot.y);
                lerped.z = Interp(_smoothRotOut.z, _rawRotOut.z, smooth.z, recoilData.extraRot.z);
                _smoothRotOut = lerped;

                lerped = _smoothLocOut;
                smooth = recoilData.smoothLoc;
                
                lerped.x = Interp(_smoothLocOut.x, _rawLocOut.x, smooth.x, recoilData.extraLoc.x);
                lerped.y = Interp(_smoothLocOut.y, _rawLocOut.y, smooth.y, recoilData.extraLoc.y);
                lerped.z = Interp(_smoothLocOut.z, _rawLocOut.z, smooth.z, recoilData.extraLoc.z);

                _smoothLocOut = lerped;
            }
            else
            {
                _smoothRotOut = _rawRotOut;
                _smoothLocOut = _rawLocOut;
            }
        }

        private void ApplyNoise(ref Vector3 finalized)
        {
            _noiseTarget.x = Mathf.Lerp(_noiseTarget.x, 0f, KMath.ExpDecayAlpha(recoilData.noiseDamp.x, Time.deltaTime));
            _noiseTarget.y = Mathf.Lerp(_noiseTarget.y, 0f, KMath.ExpDecayAlpha(recoilData.noiseDamp.y, Time.deltaTime));
	
            _noiseOut.x = Mathf.Lerp(_noiseOut.x, _noiseTarget.x, 
                KMath.ExpDecayAlpha(recoilData.noiseAccel.x, Time.deltaTime));

            _noiseOut.y = Mathf.Lerp(_noiseOut.y, _noiseTarget.y, 
                KMath.ExpDecayAlpha(recoilData.noiseAccel.y, Time.deltaTime));
            
            finalized += new Vector3(_noiseOut.x, _noiseOut.y, 0f);
        }

        private void ApplyPushback(ref Vector3 finalized)
        {
            _pushTarget = Mathf.Lerp(_pushTarget, 0f, 
                KMath.ExpDecayAlpha(recoilData.pushDamp, Time.deltaTime));
            
            _pushOut = Mathf.Lerp(_pushOut, _pushTarget, 
                KMath.ExpDecayAlpha(recoilData.pushAccel, Time.deltaTime));

            finalized += new Vector3(0f, 0f, _pushOut);
        }

        private void ApplyProgression(ref Vector3 translation, ref Vector3 rotation)
        {
            float alpha = KMath.ExpDecayAlpha(recoilData.pitchProgress.acceleration, Time.deltaTime);
            _pitchProgress.x = Mathf.Lerp(_pitchProgress.x, _pitchProgress.y, alpha);
            
            alpha = KMath.ExpDecayAlpha(recoilData.upProgress.acceleration, Time.deltaTime);
            _upProgress.x = Mathf.Lerp(_upProgress.x, _upProgress.y, alpha);
            
            alpha = KMath.ExpDecayAlpha(recoilData.pitchProgress.damping, Time.deltaTime);
            _pitchProgress.y = Mathf.Lerp(_pitchProgress.y, 0f, alpha);
            
            alpha = KMath.ExpDecayAlpha(recoilData.upProgress.damping, Time.deltaTime);
            _upProgress.y = Mathf.Lerp(_upProgress.y, 0f, alpha);

            translation.y += _upProgress.x;
            rotation.x += _pitchProgress.x;
        }

        private void ApplySway(ref Vector3 translation, ref Quaternion rotation)
        {
            float alpha = KMath.ExpDecayAlpha(recoilData.recoilSway.acceleration, Time.deltaTime);
            _pitchSway.x = Mathf.Lerp(_pitchSway.x, _pitchSway.y, alpha);
            _yawSway.x = Mathf.Lerp(_yawSway.x, _yawSway.y, alpha);
            
            alpha = KMath.ExpDecayAlpha(recoilData.recoilSway.damping, Time.deltaTime);
            _pitchSway.y = Mathf.Lerp(_pitchSway.y, 0f, alpha);
            _yawSway.y = Mathf.Lerp(_yawSway.y, 0f, alpha);

            Quaternion swayRotation = Quaternion.Euler(new Vector3(_pitchSway.x, _yawSway.x,
                _yawSway.x * recoilData.recoilSway.rollMultiplier));
            Vector3 swayPosition = swayRotation * recoilData.recoilSway.pivotOffset - recoilData.recoilSway.pivotOffset;

            rotation *= swayRotation;
            translation += swayPosition;
        }
    
        private float CorrectStart(ref float last, float current, ref bool bStartRest, ref float startVal)
        {
            if (Mathf.Abs(last) > Mathf.Abs(current) && bStartRest && !_isLooping)
            {
                startVal = 0f;
                bStartRest = false;
            }
	
            last = current;
	
            return startVal;
        }
        
        private void SetupStateMachine()
        {
            _stateMachine ??= new List<AnimState>();

            AnimState semiState;
            AnimState autoState;

            semiState.checkCondition = () =>
            {
                float timerError = (60f / _fireRate) / Time.deltaTime + 1;
                timerError *= Time.deltaTime;
            
                if(_enableSmoothing && !_isLooping)
                {
                    _enableSmoothing = false;
                }
            
                return GetDelta() > timerError + 0.01f && !_isLooping || fireMode == FireMode.Semi;
            };

            semiState.onPlay = () =>
            {
                SetupTransition(_smoothRotOut, _smoothLocOut, recoilData.recoilCurves.semiRotCurve, 
                    recoilData.recoilCurves.semiLocCurve);
            };

            semiState.onStop = () =>
            {
                //Intended to be empty
            };

            autoState.checkCondition = () => true;

            autoState.onPlay = () =>
            {
                if (_isLooping)
                {
                    return;
                }
            
                var curves = recoilData.recoilCurves;
                bool bCurvesValid = curves.autoRotCurve.IsValid() && curves.autoLocCurve.IsValid();

                _enableSmoothing = bCurvesValid;
                float correction = 60f / _fireRate;

                if (bCurvesValid)
                {
                    CorrectAlpha(curves.autoRotCurve, curves.autoLocCurve, correction);
                    SetupTransition(_startValRot, _startValLoc, curves.autoRotCurve, curves.autoLocCurve);
                }
                else if(curves.autoRotCurve.IsValid() && curves.autoLocCurve.IsValid())
                {
                    CorrectAlpha(curves.semiRotCurve, curves.semiLocCurve, correction);
                    SetupTransition(_startValRot, _startValLoc, curves.semiRotCurve, curves.semiLocCurve);
                }

                _pushTarget = recoilData.pushAmount;
            
                _lastFrameTime = correction;
                _isLooping = true;
            };

            autoState.onStop = () =>
            {
                if (!_isLooping)
                {
                    return;
                }
                
                float tempRot = _tempRotCurve.GetCurveLength();
                float tempLoc = _tempLocCurve.GetCurveLength();
                _lastFrameTime = tempRot > tempLoc ? tempRot : tempLoc;
                _isPlaying = true;
            };

            _stateMachine.Add(semiState);
            _stateMachine.Add(autoState);
        }

        private void SetupTransition(Vector3 startRot, Vector3 startLoc, VectorCurve rot, VectorCurve loc)
        {
            if(!rot.IsValid() || !loc.IsValid())
            {
                Debug.Log("RecoilAnimation: Rot or Loc curve is nullptr");
                return;
            }
        
            _startValRot = startRot;
            _startValLoc = startLoc;
	
            _canRestRot = _canRestLoc = new StartRest(true, true, true);

            _tempRotCurve = rot;
            _tempLocCurve = loc;

            _lastFrameTime = rot.GetCurveLength() > loc.GetCurveLength() ? rot.GetCurveLength() : loc.GetCurveLength();
        
            PlayFromStart();
        }
    
        private void CorrectAlpha(VectorCurve rot, VectorCurve loc, float time)
        {
            Vector3 curveAlpha = rot.GetValue(time);
        
            _startValRot.x = Mathf.LerpUnclamped(_startValRot.x, _targetRot.x, curveAlpha.x);
            _startValRot.y = Mathf.LerpUnclamped(_startValRot.y, _targetRot.y, curveAlpha.y);
            _startValRot.z = Mathf.LerpUnclamped(_startValRot.z, _targetRot.z, curveAlpha.z);

            curveAlpha = loc.GetValue(time);
	
            _startValLoc.x = Mathf.LerpUnclamped(_startValLoc.x, _targetLoc.x, curveAlpha.x);
            _startValLoc.y = Mathf.LerpUnclamped(_startValLoc.y, _targetLoc.y, curveAlpha.y);
            _startValLoc.z = Mathf.LerpUnclamped(_startValLoc.z, _targetLoc.z, curveAlpha.z);
        }
    
        private void PlayFromStart()
        { 
            _playBack = 0f;
            _isPlaying = true;
        }
    
        private float GetDelta()
        {
            return Time.unscaledTime - _lastTimeShot;
        }
    }
}