// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.ScriptableWidget.Runtime
{
    [MovedFrom("KINEMATION.PropertyBindings.Runtime")]
    [AttributeUsage(AttributeTargets.Class)]
    public class ScriptableComponentGroupAttribute : PropertyAttribute
    {
        public string group;
        public string shortName;

        public ScriptableComponentGroupAttribute(string group, string shortName)
        {
            this.group = group;
            this.shortName = shortName;
        }
    }
}