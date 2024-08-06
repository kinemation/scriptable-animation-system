// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using UnityEngine;

namespace Demo.Scripts.Runtime.Item
{
    public class MeleeWeapon : FPSItem
    {
        [SerializeField] private FPSAnimationAsset meleeAttackAnimation;
        [SerializeField, Min(0f)] private float meleeAttackDelay = 0f;
        
        [SerializeField] protected FPSAnimationAsset equipClip;
        [SerializeField] protected FPSAnimationAsset unEquipClip;
        
        private Animator _controllerAnimator;
        private IPlayablesController _playablesController;
        private FPSAnimator _fpsAnimator;
        
        private float _previousAttackTime;
        
        public override void OnEquip(GameObject parent)
        {
            if (parent == null) return; 
            
            _controllerAnimator = parent.GetComponent<Animator>();
            _playablesController = parent.GetComponent<IPlayablesController>();
            _fpsAnimator = _fpsAnimator = parent.GetComponent<FPSAnimator>();
            
            if (overrideController != _controllerAnimator.runtimeAnimatorController)
            {
                _playablesController.UpdateAnimatorController(overrideController);
            }
            
            _fpsAnimator.LinkAnimatorProfile(gameObject);

            if (equipClip != null)
            {
                _playablesController.PlayAnimation(equipClip);
                return;
            }
            
            _fpsAnimator.LinkAnimatorLayer(equipMotion);
            _previousAttackTime = -meleeAttackDelay;
        }

        public override void OnUnEquip()
        {
            if (unEquipClip != null)
            {
                _playablesController.PlayAnimation(unEquipClip);
                return;
            }
            
            _fpsAnimator.LinkAnimatorLayer(unEquipMotion);
        }

        public override bool OnFirePressed()
        {
            if (Time.timeSinceLevelLoad - _previousAttackTime < meleeAttackDelay) return false;
            
            _playablesController.PlayAnimation(meleeAttackAnimation, 0f);
            _previousAttackTime = Time.timeSinceLevelLoad;
            return true;
        }
    }
}