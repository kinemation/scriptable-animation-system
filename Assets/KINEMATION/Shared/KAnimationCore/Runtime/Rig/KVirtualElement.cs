// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Rig
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Rig")]
    public class KVirtualElement : MonoBehaviour
    {
        public Transform targetBone;

        public void Animate()
        {
            transform.position = targetBone.position;
            transform.rotation = targetBone.rotation;
        }
    }
}