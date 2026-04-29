// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Core")]
    [Serializable]
    public struct VectorCurve
    {
        public AnimationCurve x;
        public AnimationCurve y;
        public AnimationCurve z;

        public static VectorCurve Linear(float timeStart, float timeEnd, float valueStart, float valueEnd)
        {
            VectorCurve result = new VectorCurve()
            {
                x = AnimationCurve.Linear(timeStart, timeEnd, valueStart, valueEnd),
                y = AnimationCurve.Linear(timeStart, timeEnd, valueStart, valueEnd),
                z = AnimationCurve.Linear(timeStart, timeEnd, valueStart, valueEnd)
            };

            return result;
        }

        public static VectorCurve Constant(float timeStart, float timeEnd, float value)
        {
            VectorCurve result = new VectorCurve()
            {
                x = AnimationCurve.Constant(timeStart, timeEnd, value),
                y = AnimationCurve.Constant(timeStart, timeEnd, value),
                z = AnimationCurve.Constant(timeStart, timeEnd, value)
            };
            
            return result;
        }

        public float GetCurveLength()
        {
            float maxTime = -1f;

            float curveTime = KCurves.GetCurveLength(x);
            maxTime = curveTime > maxTime ? curveTime : maxTime;
        
            curveTime = KCurves.GetCurveLength(y);
            maxTime = curveTime > maxTime ? curveTime : maxTime;
        
            curveTime = KCurves.GetCurveLength(z);
            maxTime = curveTime > maxTime ? curveTime : maxTime;

            return maxTime;
        }

        public Vector3 GetValue(float time)
        {
            return new Vector3(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time));
        }

        public Vector3 GetLastValue()
        {
            float length = GetCurveLength();
            return GetValue(length);
        }

        public bool IsValid()
        {
            return x != null && y != null && z != null;
        }

        public VectorCurve(Keyframe[] keyFrame)
        {
            x = new AnimationCurve(keyFrame);
            y = new AnimationCurve(keyFrame);
            z = new AnimationCurve(keyFrame);
        }
    }
    
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Core")]
    [Serializable]
    public enum EEaseFunc
    {
        Linear,
        Sine,
        Cubic,
        Custom
    }
    
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Core")]
    [Serializable]
    public struct EaseMode
    {
        public EEaseFunc easeFunc;
        public AnimationCurve curve;

        public EaseMode(EEaseFunc func)
        {
            easeFunc = func;
            curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        }
    }
    
    public class KCurves
    {
        public static float GetCurveLength(AnimationCurve curve)
        {
            float length = 0f;
            
            if (curve != null)
            {
                length = curve[curve.length - 1].time;
            }

            return length;
        }
        
        public static float EaseSine(float a, float b, float alpha)
        {
            return Mathf.Lerp(a, b, -(Mathf.Cos(Mathf.PI * alpha) - 1) / 2);
        }

        public static float EaseCubic(float a, float b, float alpha)
        {
            alpha = alpha < 0.5 ? 4 * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 3) / 2;
            return Mathf.Lerp(a, b, alpha);
        }

        public static float EaseCurve(float a, float b, float alpha, AnimationCurve curve)
        {
            alpha = curve?.Evaluate(alpha) ?? alpha;
            return Mathf.Lerp(a, b, alpha);
        }
        
        public static float Ease(float a, float b, float alpha, EaseMode ease)
        {
            alpha = Mathf.Clamp01(alpha);

            if (ease.easeFunc == EEaseFunc.Sine)
            {
                return EaseSine(a, b, alpha);
            }
            
            if (ease.easeFunc == EEaseFunc.Cubic)
            {
                return EaseCubic(a, b, alpha);
            }
            
            if (ease.easeFunc == EEaseFunc.Custom)
            {
                return EaseCurve(a, b, alpha, ease.curve);
            }

            return Mathf.Lerp(a, b, alpha);
        }
    }
}