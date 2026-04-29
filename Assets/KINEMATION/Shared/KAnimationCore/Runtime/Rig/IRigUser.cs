// Copyright (c) 2026 KINEMATION.
// All rights reserved.

namespace KINEMATION.Shared.KAnimationCore.Runtime.Rig
{
    public interface IRigUser
    {
        // Must return a reference to the used Rig Asset.
        public KRig GetRigAsset();
    }
}