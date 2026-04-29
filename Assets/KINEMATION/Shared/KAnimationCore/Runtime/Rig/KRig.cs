// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Rig
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    public abstract class KRigBase : ScriptableObject, IRigProvider
    {
        public RuntimeAnimatorController targetAnimator;
        public List<KRigElement> rigHierarchy = new List<KRigElement>();
        
        [CustomElementChainDrawer(false, true)]
        public List<KRigElementChain> rigElementChains = new List<KRigElementChain>();
        
        public KRigElement[] GetHierarchy()
        {
            return rigHierarchy.ToArray();
        }
    }
    
    // Character skeleton asset.
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    public class KRig : KRigBase
    {
        public UserInputConfig inputConfig;
        public List<string> rigCurves = new List<string>();

        public KRigElementChain GetElementChainByName(string chainName)
        {
            var chain = rigElementChains.Find(item => item.chainName.Equals(chainName));
            return chain;
        }

        public KTransformChain GetPopulatedChain(string chainName, KRigComponent rigComponent)
        {
            KTransformChain result = new KTransformChain();
            var targetChain = GetElementChainByName(chainName);

            if (targetChain == null)
            {
                Debug.LogError($"Rig `{name}`: `{chainName}` chain not found!");
                return null;
            }

            foreach (var element in targetChain.elementChain)
            {
                result.transformChain.Add(rigComponent.GetRigTransform(element));
            }
            
            return result;
        }
        
#if UNITY_EDITOR
        public List<int> rigDepths = new List<int>();
        private List<Object> _rigObservers = new List<Object>();

        private void OnEnable()
        {
            // Force update rig depths for compatibility reasons.
            int count = rigHierarchy.Count;
            for (int i = 0; i < count; i++)
            {
                var element = rigHierarchy[i];
                element.depth = rigDepths[i];
                rigHierarchy[i] = element;
            }
        }

        public void ImportRig(KRigComponent rigComponent)
        {
            rigHierarchy.Clear();
            rigDepths.Clear();
            
            rigComponent.RefreshHierarchy();
            
            var hierarchy = rigComponent.GetRigTransforms();
            var depths = rigComponent.GetHierarchyDepths();
            
            for (int i = 0; i < hierarchy.Length; i++)
            {
                rigHierarchy.Add(new KRigElement(i, hierarchy[i].transform.name));
                rigDepths.Add(depths[i]);
            }
            
            NotifyObservers();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
        
        public KRigElement GetElementByName(string targetName)
        {
            return rigHierarchy.Find(item => item.name.Equals(targetName));
        }

        public void RegisterRigObserver(Object newRigObserver)
        {
            // Only register Rig Observers.
            IRigObserver observer = (IRigObserver) newRigObserver;
            if (observer == null) return;
            
            if (_rigObservers.Contains(newRigObserver))
            {
                return;
            }
            
            _rigObservers.Add(newRigObserver);
            EditorUtility.SetDirty(this);
        }

        public void UnRegisterObserver(Object rigObserver)
        {
            _rigObservers.Remove(rigObserver);
            EditorUtility.SetDirty(this);
        }

        public void NotifyObservers()
        {
            List<Object> validObservers = new List<Object>();
            foreach (var observer in _rigObservers)
            {
                if (observer is IRigObserver obj)
                {
                    obj.OnRigUpdated();
                    validObservers.Add(observer);
                }
            }

            _rigObservers = validObservers;
        }
#endif
    }
}