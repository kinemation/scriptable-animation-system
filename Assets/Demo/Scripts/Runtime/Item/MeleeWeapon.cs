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
        
        private Animator _controllerAnimator;
        private IPlayablesController _playablesController;
        private FPSAnimator _fpsAnimator;
        
        private static readonly int CurveEquip = Animator.StringToHash("CurveEquip");
        private static readonly int CurveUnequip = Animator.StringToHash("CurveUnequip");
        private static readonly int OverlayType = Animator.StringToHash("OverlayType");

        private float _previousAttackTime;
        
        public override void OnEquip(GameObject parent)
        {
            if (parent == null) return; 
            
            _controllerAnimator = parent.GetComponent<Animator>();
            _playablesController = parent.GetComponent<IPlayablesController>();
            _fpsAnimator = _fpsAnimator = parent.GetComponent<FPSAnimator>();
            
            _fpsAnimator.LinkAnimatorProfile(gameObject);
            _controllerAnimator.SetFloat(OverlayType, (float) Item.OverlayType.Pistol);
            
            _controllerAnimator.CrossFade(CurveEquip, 0.15f);
        }

        public override void OnUnEquip()
        {
            _controllerAnimator.CrossFade(CurveUnequip, 0.15f);
        }

        public override bool OnFirePressed()
        {
            if (Time.unscaledTime - _previousAttackTime < meleeAttackDelay) return false;
            
            _playablesController.PlayAnimation(meleeAttackAnimation, 0f);
            _previousAttackTime = Time.unscaledTime;
            return true;
        }
    }
}