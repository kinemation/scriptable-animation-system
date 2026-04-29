// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Rig
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    [Serializable]
    public class KRigElementChain
    {
        public int Count => elementChain.Count;
        
        public string chainName;
        [HideInInspector] public List<KRigElement> elementChain = new List<KRigElement>();

        public KRigElementChain GetCopy()
        {
            KRigElementChain copy = new KRigElementChain();
            copy.chainName = chainName;
            foreach (var element in elementChain) copy.elementChain.Add(element);
            return copy;
        }
    }

    // A simplified version of the KRigElementChain, which contains transforms only.
    public class KTransformChain
    {
        public List<Transform> transformChain = new List<Transform>();
        public List<KTransform> cachedTransforms = new List<KTransform>();
        public ESpaceType spaceType;

        public void CacheTransforms(ESpaceType targetSpace, Transform root = null)
        {
            cachedTransforms.Clear();
            spaceType = targetSpace;
            
            foreach (var element in transformChain)
            {
                KTransform cache = new KTransform();
                
                if (targetSpace == ESpaceType.WorldSpace)
                {
                    cache.position = element.position;
                    cache.rotation = element.rotation;
                } 
                else if (targetSpace == ESpaceType.ComponentSpace)
                {
                    if (root == null)
                    {
                        root = element.root;
                    }
                    
                    cache.position = root.InverseTransformPoint(element.position);
                    cache.rotation = Quaternion.Inverse(root.rotation) * element.rotation;
                }
                else
                {
                    cache.position = element.localPosition;
                    cache.rotation = element.localRotation;
                }
                
                cachedTransforms.Add(cache);
            }
        }

        public void BlendTransforms(float weight)
        {
            int count = transformChain.Count;

            for (int i = 0; i < count; i++)
            {
                Transform element = transformChain[i];
                KTransform cache = cachedTransforms[i];

                KPose pose = new KPose()
                {
                    modifyMode = EModifyMode.Replace,
                    pose = cache,
                    space = spaceType
                };
                
                KAnimationMath.ModifyTransform(element.root, element, pose, weight);
            }
        }

        public float GetLength(Transform root = null)
        {
            float chainLength = 0f;
            int count = transformChain.Count;

            if (count > 0 && root == null) root = transformChain[0];
            
            for (int i = 0; i < count; i++)
            {
                Transform targetBone = transformChain[i];
                
                if (count == 1)
                {
                    Vector3 targetMS = root.InverseTransformPoint(targetBone.position);
                    chainLength = targetMS.magnitude;
                }
                
                if (i > 0)
                {
                    chainLength += (targetBone.position - transformChain[i - 1].position).magnitude;
                }
            }

            return chainLength;
        }

        public bool IsValid()
        {
            if (transformChain == null || cachedTransforms == null) return false;
            if (transformChain.Count == 0) return false;
            
            return true;
        }
    }
}