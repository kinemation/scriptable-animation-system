// Designed by KINEMATION, 2024

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Playables
{
    public interface IPlayablesController
    {
        public bool InitializeController();
        
        public void SetControllerWeight(float weight);
        
        public bool PlayPose(FPSAnimationAsset asset);
        public bool PlayAnimation(FPSAnimationAsset asset, float startTime = 0f);
        public void UpdateAnimatorController(RuntimeAnimatorController newController);
        public void UpdateAvatarMask(AvatarMask newMask);
        public void StopAnimation(float blendOutTime);
        public bool IsPlaying();

        public float GetCurveValue(string curveName, bool isAnimator = false);

        public Animator GetAnimator();

        public PlayableGraph GetPlayableGraph();

        public void RebuildPlayables();
        
#if UNITY_EDITOR
        public void StartEditorPreview();
        public void StopEditorPreview();
#endif
    }
}