// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Rig
{
    // Represents the space we will modify bone transform in.
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    public enum ESpaceType
    {
        BoneSpace,
        ParentBoneSpace,
        ComponentSpace,
        WorldSpace
    }

    // Whether the operation is additive or absolute.
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    public enum EModifyMode
    {
        Add,
        Replace,
        Ignore
    }
    
    // Represents the pose for the specific rig element.
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    [Serializable]
    public struct KPose
    {
        public KRigElement element;
        public KTransform pose;
        public ESpaceType space;
        public EModifyMode modifyMode;
    }
}