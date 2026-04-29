// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Rig
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    public class KRigComponent : MonoBehaviour
    {
        [SerializeField] private List<Transform> hierarchy = new List<Transform>();
        private List<KVirtualElement> _virtualElements;
        private Dictionary<string, int> _hierarchyMap;
        private List<KTransform> _cachedHierarchyPose;

#if UNITY_EDITOR
        [SerializeField] private List<int> hierarchyDepths = new List<int>();

        public bool CompareRig(KRig compareTo)
        {
            if (compareTo == null || hierarchy == null || compareTo.rigHierarchy.Count != hierarchy.Count)
            {
                return false;
            }

            int count = hierarchy.Count;
            for (int i = 0; i < count; i++)
            {
                if (!compareTo.rigHierarchy[i].name.Equals(hierarchy[i].name)) return false;
            }
            
            return true;
        }
        
        public int[] GetHierarchyDepths()
        {
            return hierarchyDepths.ToArray();
        }
        
        public void RefreshHierarchy()
        {
            hierarchy.Clear();
            hierarchyDepths.Clear();
            TraverseHierarchyByLayer(transform, 0);
        }

        public Transform[] GetHierarchy()
        {
            if (hierarchy == null)
            {
                return null;
            }
            
            return hierarchy.ToArray();
        }

        public bool Contains(string entry)
        {
            if (hierarchy == null) return false;

            HashSet<string> set = new HashSet<string>();
            foreach (var element in hierarchy)
            {
                set.Add(element.name);
            }

            return set.Contains(entry);
        }
        
        private void TraverseHierarchyByLayer(Transform currentTransform, int depth)
        {
            hierarchy.Add(currentTransform);
            hierarchyDepths.Add(depth);
            
            foreach (Transform child in currentTransform)
            {
                TraverseHierarchyByLayer(child, depth + 1);
            }
        }
#endif
        
        public void Initialize()
        {
            // Register Virtual Elements.
            _virtualElements = new List<KVirtualElement>();
            KVirtualElement[] virtualElements = GetComponentsInChildren<KVirtualElement>();

            foreach (var virtualElement in virtualElements)
            {
                _virtualElements.Add(virtualElement);
            }

            // Map the hierarchy indexes to the element names.
            _hierarchyMap = new Dictionary<string, int>();

            int count = hierarchy.Count;
            for (int i = 0; i < count; i++)
            {
                _hierarchyMap.TryAdd(hierarchy[i].name, i);
            }

            _cachedHierarchyPose = new List<KTransform>();
        }

        public void AnimateVirtualElements()
        {
            foreach (var virtualElement in _virtualElements)
            {
                virtualElement.Animate();
            }
        }
        
        public Transform[] GetRigTransforms()
        {
            return hierarchy.ToArray();
        }

        public Transform GetRigTransform(KRigElement rigElement)
        {
            int index = rigElement.index;
            
            // Invalid index, try to use the element name instead.
            if (index < 0 || index > hierarchy.Count - 1)
            {
                index = _hierarchyMap[rigElement.name];
            }

            // Total failure, return null.
            if (index < 0 || index > hierarchy.Count - 1)
            {
                return null;
            }
            
            return hierarchy[index].transform;
        }
        
        public Transform GetRigTransform(string elementName)
        {
            if (_hierarchyMap.TryGetValue(elementName, out var element))
            {
                return hierarchy[element];
            }
            
            return null;
        }
        
        public Transform GetRigTransform(int elementIndex)
        {
            if (elementIndex < 0 || elementIndex > hierarchy.Count - 1)
            {
                return null;
            }

            return hierarchy[elementIndex].transform;
        }

        public void CacheHierarchyPose()
        {
            _cachedHierarchyPose.Clear();
            foreach (var element in hierarchy) _cachedHierarchyPose.Add(new KTransform(element, 
                false));
        }

        public void ApplyHierarchyCachedPose()
        {
            int count = hierarchy.Count;
            for (int i = 0; i < count; i++)
            {
                var cachedPose = _cachedHierarchyPose[i];

                hierarchy[i].localPosition = cachedPose.position;
                hierarchy[i].localRotation = cachedPose.rotation;
            }
        }
    }
}