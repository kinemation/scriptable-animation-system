// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.LookLayer
{
    [Serializable]
    public struct LookLayerElement
    {
        public KRigElement rigElement;
        public Vector2 clampedAngle;
        [HideInInspector] public Vector2 cachedClampedAngle;
    }
    
    public class LookLayerSettings : FPSAnimatorLayerSettings
    {
        [InputProperty] public string mouseInputProperty = FPSANames.MouseInput;
        [InputProperty] public string leanInputProperty = FPSANames.LeanInput;

        public bool useTurnOffset = false;
        [InputProperty] public string turnOffsetProperty = FPSANames.TurnOffset;
        
        public List<LookLayerElement> pitchOffsetElements = new List<LookLayerElement>();
        public List<LookLayerElement> yawOffsetElements = new List<LookLayerElement>();
        public List<LookLayerElement> rollOffsetElements = new List<LookLayerElement>();

        private void ApplyAngleDistribution(ref List<LookLayerElement> collection)
        {
            int count = collection.Count;
            int adjustStartIndex = 0;
            
            Vector2 angleToDistribute = Vector2.zero;

            bool bShallDistribute = false;
            bool bDistributeForX = false;
            
            for (int i = 0; i < count; i++)
            {
                var element = collection[i];
                
                angleToDistribute.x += Mathf.Abs(element.clampedAngle.x);
                angleToDistribute.y += Mathf.Abs(element.clampedAngle.y);
                
                if (!Mathf.Approximately(element.cachedClampedAngle.x,element.clampedAngle.x))
                {
                    adjustStartIndex = i + 1;
                    bShallDistribute = true;
                    bDistributeForX = true;
                    break;
                }

                if (!Mathf.Approximately(element.cachedClampedAngle.y, element.clampedAngle.y))
                {
                    adjustStartIndex = i + 1;
                    bShallDistribute = true;
                    break;
                }
            }

            if (bShallDistribute)
            {
                for (int i = adjustStartIndex; i < count; i++)
                {
                    var element = collection[i];

                    if (bDistributeForX)
                    {
                        element.clampedAngle.x = (90f - angleToDistribute.x) / (count - adjustStartIndex);
                    }
                    else
                    {
                        element.clampedAngle.y = (90f - angleToDistribute.y) / (count - adjustStartIndex);
                    }
                    
                    collection[i] = element;
                }
            }
            
            for (int i = 0; i < count; i++)
            {
                var element = collection[i];
                element.cachedClampedAngle.x = element.clampedAngle.x;
                element.cachedClampedAngle.y = element.clampedAngle.y;
                collection[i] = element;
            }
        }

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new LookLayerJob();
        }

#if UNITY_EDITOR
        private void UpdateIndices(ref List<LookLayerElement> elements)
        {
            int count = elements.Count;

            for (int i = 0; i < count; i++)
            {
                var element = elements[i];
                UpdateRigElement(ref element.rigElement);
                elements[i] = element;
            }
        }
        
        protected new void OnValidate()
        {
            base.OnValidate();
            
            ApplyAngleDistribution(ref pitchOffsetElements);
            ApplyAngleDistribution(ref yawOffsetElements);
            ApplyAngleDistribution(ref rollOffsetElements);
        }
        
        public override void OnRigUpdated()
        {
            UpdateIndices(ref pitchOffsetElements);
            UpdateIndices(ref yawOffsetElements);
            UpdateIndices(ref rollOffsetElements);
        }
#endif
    }
}