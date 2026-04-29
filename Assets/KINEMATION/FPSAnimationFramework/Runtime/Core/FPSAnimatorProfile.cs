// Designed by KINEMATION, 2024.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Core
{
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/fundamentals/animator-profiles")]
    [CreateAssetMenu(fileName = "NewAnimatorProfile", menuName = FPSANames.FileMenuGeneral + "Animator Profile")]
    public class FPSAnimatorProfile : ScriptableObject, IRigObserver
    {
        public KRig rigAsset;
        [Min(0f)] public float blendInTime = 0f;
        [Min(0f)] public float blendOutTime = 0f;
        public EaseMode easeMode;
        
        public List<FPSAnimatorLayerSettings> settings = new List<FPSAnimatorLayerSettings>();

        public bool Equals(FPSAnimatorProfile anotherProfile)
        {
            if (anotherProfile == null || settings.Count != anotherProfile.settings.Count)
            {
                return false;
            }
            
            for (int i = 0; i < settings.Count; i++)
            {
                // Type mismatch or a different data asset.
                if (settings[i].GetType() != anotherProfile.settings[i].GetType() 
                    || settings[i] != anotherProfile.settings[i])
                {
                    return false;
                }
            }
            
            return true;
        }

        public bool IsValid()
        {
            bool isValid = true;
            foreach (var setting in settings)
            {
                if (setting == null)
                {
                    isValid = false;
                    break;
                }
            }

            return isValid && settings.Count > 0;
        }

        public void OnRigUpdated()
        {
#if UNITY_EDITOR
            foreach (var setting in settings)
            {
                setting.rigAsset = rigAsset;
                setting.OnRigUpdated();
            }
#endif
        }
        
#if UNITY_EDITOR
        private KRig _cachedRigAsset;
        private void OnValidate()
        {
            // If the asset has changed, we must update the element indices.
            // If the asset was set to null, don't do anything
            if (rigAsset == _cachedRigAsset)
            {
                return;
            }

            if (_cachedRigAsset != null)
            {
                _cachedRigAsset.UnRegisterObserver(this);
            }

            // Update the references when changing the asset.
            // Useful for different characters.
            if (rigAsset != null)
            {
                rigAsset.RegisterRigObserver(this);
                OnRigUpdated();
            }

            _cachedRigAsset = rigAsset;
        }
#endif
    }
}