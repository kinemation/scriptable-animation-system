// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using System;
using System.Collections.Generic;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.PoseOffsetLayer
{
    [Serializable]
    public struct PoseOffset
    {
        [Unfold] public KPose pose;
        [CurveSelector] public CurveBlend blend;
        public bool keepChildrenPose;
    }
    
    public class PoseOffsetLayerSettings : FPSAnimatorLayerSettings
    {
        public List<PoseOffset> poseOffsets = new List<PoseOffset>();

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new PoseOffsetJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            int count = poseOffsets.Count;

            for (int i = 0; i < count; i++)
            {
                PoseOffset item = poseOffsets[i];
                UpdateRigElement(ref item.pose.element);
                poseOffsets[i] = item;
            }
        }
#endif
    }
}