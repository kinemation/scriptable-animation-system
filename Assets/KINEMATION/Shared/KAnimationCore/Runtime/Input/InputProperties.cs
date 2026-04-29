// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Input
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Input")]
    [Serializable]
    public struct BoolProperty
    {
        public string name;
        public bool defaultValue;
    }
    
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Input")]
    [Serializable]
    public struct IntProperty
    {
        public string name;
        public int defaultValue;
    }
    
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Input")]
    [Serializable]
    public struct FloatProperty
    {
        public string name;
        public float defaultValue;
        public float interpolationSpeed;
    }
    
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Input")]
    [Serializable]
    public struct VectorProperty
    {
        public string name;
        public Vector4 defaultValue;
    }
}