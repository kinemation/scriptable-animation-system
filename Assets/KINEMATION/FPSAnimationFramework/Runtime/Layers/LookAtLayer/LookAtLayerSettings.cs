// Designed by KINEMATION, 2024.

using System;
using System.Collections.Generic;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.LookAtLayer
{
    [Serializable]
    public struct LookAtLayerElement
    {
        public KRigElement rigElement;
        public Vector2 clampedAngle;
        [HideInInspector] public Vector2 cachedClampedAngle;
    }

    [CreateAssetMenu(fileName = "LookAtLayerSettings",
        menuName = FPSANames.FileMenuLayers + "Look At Layer")]
    public class LookAtLayerSettings : FPSAnimatorLayerSettings
    {
        [Header("Spine Rotation")]
        public List<LookAtLayerElement> pitchSpineElements = new List<LookAtLayerElement>();
        public List<LookAtLayerElement> yawSpineElements = new List<LookAtLayerElement>();

        [Header("Look Source")]
        public KRigElement boneToAlign;

        [Header("Bone To Align Rotation")]
        public bool rotateBoneToAlignYaw = true;
        public Vector2 boneToAlignYawClamp = new Vector2(20f, 20f);
        public bool rotateBoneToAlignPitch = true;
        public Vector2 boneToAlignPitchClamp = new Vector2(20f, 20f);
        public Vector3 boneForwardAxis = Vector3.forward;

        [Header("Final Bone Alignment")]
        public bool useFinalBoneAlignment = true;
        [Range(0f, 1f)] public float finalBoneAlignmentWeight = 1f;
        [Range(0f, 180f)] public float finalBoneAlignmentMaxAngle = 180f;

        private void ApplyAngleDistribution(ref List<LookAtLayerElement> collection)
        {
            int count = collection.Count;
            int adjustStartIndex = 0;

            Vector2 angleToDistribute = Vector2.zero;
            bool shallDistribute = false;
            bool distributeForX = false;

            for (int i = 0; i < count; i++)
            {
                var element = collection[i];
                angleToDistribute.x += Mathf.Abs(element.clampedAngle.x);
                angleToDistribute.y += Mathf.Abs(element.clampedAngle.y);

                if (!Mathf.Approximately(element.cachedClampedAngle.x, element.clampedAngle.x))
                {
                    adjustStartIndex = i + 1;
                    shallDistribute = true;
                    distributeForX = true;
                    break;
                }

                if (!Mathf.Approximately(element.cachedClampedAngle.y, element.clampedAngle.y))
                {
                    adjustStartIndex = i + 1;
                    shallDistribute = true;
                    break;
                }
            }

            if (shallDistribute)
            {
                for (int i = adjustStartIndex; i < count; i++)
                {
                    var element = collection[i];

                    if (distributeForX)
                    {
                        element.clampedAngle.x = (90f - angleToDistribute.x) / Mathf.Max(1, count - adjustStartIndex);
                    }
                    else
                    {
                        element.clampedAngle.y = (90f - angleToDistribute.y) / Mathf.Max(1, count - adjustStartIndex);
                    }

                    collection[i] = element;
                }
            }

            for (int i = 0; i < count; i++)
            {
                var element = collection[i];
                element.clampedAngle = new Vector2(Mathf.Abs(element.clampedAngle.x), Mathf.Abs(element.clampedAngle.y));
                element.cachedClampedAngle = element.clampedAngle;
                collection[i] = element;
            }
        }

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new LookAtLayerJob();
        }

#if UNITY_EDITOR
        private void UpdateIndices(ref List<LookAtLayerElement> elements)
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

            ApplyAngleDistribution(ref pitchSpineElements);
            ApplyAngleDistribution(ref yawSpineElements);
        }

        public override void OnRigUpdated()
        {
            UpdateRigElement(ref boneToAlign);
            UpdateIndices(ref pitchSpineElements);
            UpdateIndices(ref yawSpineElements);
        }
#endif
    }
}
