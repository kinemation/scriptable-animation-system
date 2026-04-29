// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    [MovedFrom("KINEMATION.KAnimationCore.Runtime.Core")]
    public class SetFloat : StateMachineBehaviour
    {
        [SerializeField] private string paramName;
        [SerializeField] private float paramTargetValue;
        [SerializeField] private EaseMode easeMode = new EaseMode(EEaseFunc.Linear);

        private int _paramId;
        private float _paramStartValue;
        private bool _isInitialized;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!_isInitialized)
            {
                _paramId = Animator.StringToHash(paramName);
                _isInitialized = true;
            }
            
            _paramStartValue = animator.GetFloat(_paramId);
        }
        
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            int nextHash = animator.GetNextAnimatorStateInfo(layerIndex).fullPathHash;
            if (nextHash != stateInfo.fullPathHash && nextHash != 0)
            {
                return;
            }
        
            float alpha = 0f;
            if (animator.IsInTransition(layerIndex))
            {
                alpha = animator.GetAnimatorTransitionInfo(layerIndex).normalizedTime;
            }
            else
            {
                alpha = 1f;
            }
        
            animator.SetFloat(_paramId, KCurves.Ease(_paramStartValue, paramTargetValue, alpha, easeMode));
        }
        
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
        }
    }
}