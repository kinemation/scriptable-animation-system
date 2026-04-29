// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Input
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Input")]
    public class UserInputController : MonoBehaviour
    {
        [SerializeField] public UserInputConfig inputConfig;
        
        protected List<object> _inputProperties;
        protected Dictionary<string, int> _inputPropertyMap;
        protected (int, float, float)[] _floatsToInterpolate;

        public UserInputConfig GetConfig()
        {
            return inputConfig;
        }

        protected virtual void Update()
        {
            if (_floatsToInterpolate == null) return;
            
            foreach (var tuple in _floatsToInterpolate)
            {
                float value = (float) _inputProperties[tuple.Item1];
                
                if (Mathf.Approximately(value, tuple.Item3))
                {
                    value = tuple.Item3;
                }
                else
                {
                    float alpha = KMath.ExpDecayAlpha(Time.deltaTime, tuple.Item2);
                    value = Mathf.LerpUnclamped(value, tuple.Item3, alpha);
                }
                
                _inputProperties[tuple.Item1] = value;
            }
        }
        
        public virtual void Initialize()
        {
#if UNITY_EDITOR
            _propertyNames = new List<(string, object)>();
#endif
            _inputProperties = new List<object>();
            _inputPropertyMap = new Dictionary<string, int>();

            List<(int, float, float)> floatsToInterpolate = new List<(int, float, float)>();
            
            int index = 0;
            
            foreach (var property in inputConfig.boolProperties)
            {
                _inputProperties.Add(property.defaultValue);
                _inputPropertyMap.TryAdd(property.name, index);
                index++;
                
#if UNITY_EDITOR
                _propertyNames.Add((property.name, null));
#endif
            }
            
            foreach (var property in inputConfig.intProperties)
            {
                _inputProperties.Add(property.defaultValue);
                _inputPropertyMap.TryAdd(property.name, index);
                index++;
                
#if UNITY_EDITOR
                _propertyNames.Add((property.name, null));
#endif
            }
            
            foreach (var property in inputConfig.floatProperties)
            {
                _inputProperties.Add(property.defaultValue);
                _inputPropertyMap.TryAdd(property.name, index);

                if (!Mathf.Approximately(property.interpolationSpeed, 0f))
                {
                    floatsToInterpolate.Add((index, property.interpolationSpeed, property.defaultValue));
                }
                
                index++;
                
#if UNITY_EDITOR
                _propertyNames.Add((property.name, null));
#endif
            }

            if (floatsToInterpolate.Count > 0)
            {
                _floatsToInterpolate = floatsToInterpolate.ToArray();
            }
            
            foreach (var property in inputConfig.vectorProperties)
            {
                _inputProperties.Add(property.defaultValue);
                _inputPropertyMap.TryAdd(property.name, index);
                index++;
                
#if UNITY_EDITOR
                _propertyNames.Add((property.name, null));
#endif
            }
        }

        public int GetPropertyIndex(string propertyName)
        {
            if (_inputPropertyMap.TryGetValue(propertyName, out int index))
            {
                return index;
            }
            
            return -1;
        }

        public virtual void SetValue(string propertyName, object value)
        {
            SetValue(GetPropertyIndex(propertyName), value);
        }

        public virtual T GetValue<T>(string propertyName)
        {
            return GetValue<T>(GetPropertyIndex(propertyName));
        }
        
        public virtual void SetValue(int propertyIndex, object value)
        {
            if (propertyIndex < 0 || propertyIndex > _inputProperties.Count - 1)
            {
                return;
            }

            if (_floatsToInterpolate != null)
            {
                int floatToInterpolateIndex = -1;

                for (int i = 0; i < _floatsToInterpolate.Length; i++)
                {
                    if (_floatsToInterpolate[i].Item1 == propertyIndex)
                    {
                        floatToInterpolateIndex = i;
                    }
                }

                if (floatToInterpolateIndex != -1)
                {
                    var tuple = _floatsToInterpolate[floatToInterpolateIndex];
                    tuple.Item3 = (float) value;
                    _floatsToInterpolate[floatToInterpolateIndex] = tuple;
                    return;
                }
            }
            
            _inputProperties[propertyIndex] = value;
        }

        public virtual T GetValue<T>(int propertyIndex)
        {
            if (propertyIndex < 0 || propertyIndex > _inputProperties.Count - 1)
            {
                return default(T);
            }
            
            return (T) _inputProperties[propertyIndex];
        }

#if UNITY_EDITOR
        protected List<(string, object)> _propertyNames;
        
        public virtual (string, object)[] GetPropertyBindings()
        {
            if (_propertyNames == null) return null;

            int count = _propertyNames.Count;

            for (int i = 0; i < count; i++)
            {
                var item = _propertyNames[i];
                item.Item2 = _inputProperties[i];
                _propertyNames[i] = item;
            }
            
            return _propertyNames.ToArray();
        }
#endif
    }
}