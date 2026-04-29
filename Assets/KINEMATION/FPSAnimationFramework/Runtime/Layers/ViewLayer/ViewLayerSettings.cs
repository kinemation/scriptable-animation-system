// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.ViewLayer
{
    public class ViewLayerSettings : FPSAnimatorLayerSettings
    {
        public KPose ikHandGun = new KPose()
        {
            element = new KRigElement(-1, FPSANames.IkWeaponBone),
            pose = KTransform.Identity
        };
        
        public KPose ikHandRight = new KPose()
        {
            element = new KRigElement(-1, FPSANames.IkRightHand),
            pose = KTransform.Identity
        };
        
        public KPose ikHandLeft = new KPose()
        {
            element = new KRigElement(-1, FPSANames.IkLeftHand),
            pose = KTransform.Identity
        };

        public override IAnimationLayerJob CreateAnimationJob()
        {
            return new ViewLayerJob();
        }

#if UNITY_EDITOR
        public override void OnRigUpdated()
        {
            UpdateRigElement(ref ikHandGun.element);
            UpdateRigElement(ref ikHandRight.element);
            UpdateRigElement(ref ikHandLeft.element);
        }
#endif
    }
}