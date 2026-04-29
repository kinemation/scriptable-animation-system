// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Attributes;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;

using System;
using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Core
{
    public enum ECurveBlendMode
    {
        Direct,
        Mask
    }

    public enum ECurveSource
    {
        Animator,
        Playables,
        Input
    }
    
    [Serializable]
    public struct CurveBlend
    {
        public string name;
        public ECurveBlendMode mode;
        [Range(0f, 1f)] public float clampMin;
        [HideInInspector] public ECurveSource source;

        public float ComputeBlendValue(UserInputController inputController, IPlayablesController playablesController)
        {
            if (string.IsNullOrEmpty(name) || name.Equals("None"))
            {
                return 1f;
            }
            
            float value = 0f;
            
            if (source == ECurveSource.Input)
            {
                value = inputController.GetValue<float>(name);
            }
            else
            {
                value = playablesController.GetCurveValue(name, source == ECurveSource.Animator);
            }

            value = Mathf.Clamp01(value);
            return mode == ECurveBlendMode.Direct ? value : 1f - value;
        }
    }
    
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/fundamentals/animator-layer")]
    public abstract class FPSAnimatorLayerSettings : ScriptableObject, IRigProvider, IRigObserver, IRigUser
    {
        [ShowStandalone] public KRig rigAsset;
        [Range(0f, 1f)] public float alpha = 1f;
        [CurveSelector] public List<CurveBlend> curveBlending = new List<CurveBlend>();
        
        [Tooltip("Will call OnUpdateSettings on the layer state if true.")]
        public bool linkDynamically;

        public virtual IAnimationLayerJob CreateAnimationJob()
        {
            return null;
        }

        [Obsolete("Use CreateAnimationJob instead.")]
        public virtual FPSAnimatorLayerState CreateState() { return null; }
        
        public virtual void OnRigUpdated()
        {
            // Update bone indices here.
        }
        
#if UNITY_EDITOR
        [HideInInspector] public bool isStandalone = true;
        private KRig _cachedRigAsset;
        
        protected void UpdateRigElement(ref KRigElement element)
        {
            element = rigAsset.GetElementByName(element.name);
        }

        protected void OnValidate()
        {
            if (!isStandalone) return;
            
            if (rigAsset == _cachedRigAsset)
            {
                return;
            }

            if (_cachedRigAsset != null)
            {
                _cachedRigAsset.UnRegisterObserver(this);
            }

            if (rigAsset != null)
            {
                rigAsset.RegisterRigObserver(this);
                OnRigUpdated();
            }

            _cachedRigAsset = rigAsset;
        }
#endif
        public KRigElement[] GetHierarchy()
        {
            return rigAsset == null ? null : rigAsset.GetHierarchy();
        }

        public KRig GetRigAsset()
        {
            return rigAsset;
        }
    }
}