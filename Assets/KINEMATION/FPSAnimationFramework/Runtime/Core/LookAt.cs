// Designed by KINEMATION, 2024.

using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Core
{
    [Tooltip("Computes Vector2 input to look at a target in world space.")]
    public class LookAt : MonoBehaviour
    {
        [Range(0f, 1f)] public float lookAtWeight = 1f;
        
        [SerializeField] private Transform boneToAlign;
        [SerializeField] private Transform lookTarget;
        [SerializeField] [InputProperty] private string lookInputProperty = FPSANames.MouseInput;

        public Transform BoneToAlign => boneToAlign;
        public Transform LookTarget => lookTarget;
        public bool IsValid => boneToAlign != null && lookTarget != null;
        
        private UserInputController _userInputController;
        private int _lookInputPropertyIndex;
        private void Start()
        {
            _userInputController = GetComponent<UserInputController>();
            
            if (_userInputController == null) return;
            _lookInputPropertyIndex = _userInputController.GetPropertyIndex(lookInputProperty);
        }

        private void Update()
        {
            if (_userInputController == null || !IsValid) return;
            
            var lookInput = KMath.ComputeLookAtInput(transform, boneToAlign, lookTarget);

            Vector4 value = _userInputController.GetValue<Vector4>(_lookInputPropertyIndex);
            value = Vector4.Lerp(value, new Vector4(lookInput.y, lookInput.x), lookAtWeight);
            _userInputController.SetValue(lookInputProperty, value);
        }

        public bool TryGetLookSource(out Transform sourceBone, out Transform target)
        {
            sourceBone = boneToAlign;
            target = lookTarget;
            return IsValid;
        }
    }
}
