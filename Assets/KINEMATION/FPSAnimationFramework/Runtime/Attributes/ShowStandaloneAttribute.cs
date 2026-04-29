using System;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ShowStandaloneAttribute : PropertyAttribute { }
}