using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.IkLayer
{
    public struct IkHandle
    {
        public TransformStreamHandle root;
        public TransformStreamHandle mid;
        public TransformStreamHandle tip;
        
        public TransformStreamHandle target;
        public TransformStreamHandle hint;

        public KTwoBoneIkData ikData;

        public IkHandle(Animator animator, Transform tip, Transform target, Transform hint)
        {
            var midBone = tip.parent;
            
            this.tip = animator.BindStreamTransform(tip);
            this.mid = animator.BindStreamTransform(midBone);
            this.root = animator.BindStreamTransform(midBone.parent);
            
            this.target = animator.BindStreamTransform(target);
            this.hint = animator.BindStreamTransform(hint);

            ikData = new KTwoBoneIkData()
            {
                hasValidHint = true,
                hintWeight = 1f,
                rotWeight = 1f,
                posWeight = 1f,
            };
        }

        public void OnProcessIK(AnimationStream stream, float w)
        {
            if (Mathf.Approximately(w, 0f)) return;
            
            ikData.tip = AnimLayerJobUtility.GetTransformFromHandle(stream, tip);
            ikData.mid = AnimLayerJobUtility.GetTransformFromHandle(stream, mid);
            ikData.root = AnimLayerJobUtility.GetTransformFromHandle(stream,root);
            
            ikData.target = AnimLayerJobUtility.GetTransformFromHandle(stream,target);
            ikData.hint = AnimLayerJobUtility.GetTransformFromHandle(stream, hint);

            if (ikData.tip.Equals(ikData.target)) return;
            
            KTwoBoneIK.Solve(ref ikData);
            
            root.SetRotation(stream, Quaternion.Slerp(root.GetRotation(stream), ikData.root.rotation, w));
            mid.SetRotation(stream, Quaternion.Slerp(mid.GetRotation(stream), ikData.mid.rotation, w));
            tip.SetRotation(stream, Quaternion.Slerp(tip.GetRotation(stream), ikData.tip.rotation, w));
        }
    }
    
    public struct IkLayerJob : IAnimationJob, IAnimationLayerJob
    {
        private IkLayerSettings _settings;
        private LayerJobData _jobData;

        private IkHandle _rightHandHandle;
        private IkHandle _leftHandHandle;
        
        private IkHandle _rightFootHandle;
        private IkHandle _leftFootHandle;

        private float _turnOffset;
        private int _turnProperty;

        private void SetupIkHandle(out IkHandle handle, KRigElement tip, KRigElement target, KRigElement hint)
        {
            var handBone = _jobData.rigComponent.GetRigTransform(tip);
            var targetBone = _jobData.rigComponent.GetRigTransform(target);
            var hintBone = _jobData.rigComponent.GetRigTransform(hint);

            if (handBone == null) Debug.LogError($"IK Layer: couldn't find {tip.name}.");
            if (targetBone == null) Debug.LogError($"IK Layer: couldn't find {target.name}.");
            if (hintBone == null) Debug.LogError($"IK Layer: couldn't find {hint.name}.");

            handle = new IkHandle(_jobData.animator, handBone, targetBone, hintBone);
        }
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!KAnimationMath.IsWeightRelevant(_jobData.weight))
            {
                return;
            }
            
            _rightHandHandle.OnProcessIK(stream, _jobData.weight * _settings.rightHandWeight);
            _leftHandHandle.OnProcessIK(stream, _jobData.weight * _settings.leftHandWeight);
            
            if (stream.isHumanStream)
            {
                var humanStream = stream.AsHuman();

                KTransform rootTransform = AnimLayerJobUtility.GetTransformFromHandle(stream, _jobData.rootHandle);
                KTransform rightFootGoal = AnimLayerJobUtility.GetTransformFromHandle(stream, _rightFootHandle.target);
                KTransform leftFootGoal = AnimLayerJobUtility.GetTransformFromHandle(stream, _leftFootHandle.target);
                
                rightFootGoal = rootTransform.GetRelativeTransform(rightFootGoal, false);
                leftFootGoal = rootTransform.GetRelativeTransform(leftFootGoal, false);

                if (_settings.offsetFeetTargets)
                {
                    rootTransform.rotation *= Quaternion.Euler(0f, -_turnOffset, 0f);
                }
                
                rightFootGoal = rootTransform.GetWorldTransform(rightFootGoal, false);
                leftFootGoal = rootTransform.GetWorldTransform(leftFootGoal, false);
                
                humanStream.SetGoalWeightPosition(AvatarIKGoal.RightFoot, 1f);
                humanStream.SetGoalPosition(AvatarIKGoal.RightFoot, rightFootGoal.position);
                
                humanStream.SetGoalWeightRotation(AvatarIKGoal.RightFoot, 1f);
                humanStream.SetGoalRotation(AvatarIKGoal.RightFoot, 
                    humanStream.GetGoalRotationFromPose(AvatarIKGoal.RightFoot));
                
                humanStream.SetGoalWeightPosition(AvatarIKGoal.LeftFoot, 1f);
                humanStream.SetGoalPosition(AvatarIKGoal.LeftFoot, leftFootGoal.position);
                
                humanStream.SetGoalWeightRotation(AvatarIKGoal.LeftFoot, 1f);
                humanStream.SetGoalRotation(AvatarIKGoal.LeftFoot, 
                    humanStream.GetGoalRotationFromPose(AvatarIKGoal.LeftFoot));
                return;
            }
            
            _rightFootHandle.OnProcessIK(stream, _jobData.weight * _settings.rightFootWeight);
            _leftFootHandle.OnProcessIK(stream, _jobData.weight * _settings.leftFootWeight);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData jobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (IkLayerSettings) settings;
            _jobData = jobData;

            if (jobData.animator.isHuman)
            {
                _turnProperty = jobData.inputController.GetPropertyIndex(FPSANames.TurnOffset);
            }

            SetupIkHandle(out _rightHandHandle, _settings.rightHand, _settings.rightHandIk, 
                _settings.rightHandHint);
            SetupIkHandle(out _leftHandHandle, _settings.leftHand, _settings.leftHandIk, 
                _settings.leftHandHint);
            SetupIkHandle(out _rightFootHandle, _settings.rightFoot, _settings.rightFootIk, 
                _settings.rightFootHint);
            SetupIkHandle(out _leftFootHandle, _settings.leftFoot, _settings.leftFootIk, 
                _settings.leftFootHint);
        }

        public AnimationScriptPlayable CreatePlayable(PlayableGraph graph)
        {
            return AnimationScriptPlayable.Create(graph, this);
        }

        public FPSAnimatorLayerSettings GetSettingAsset()
        {
            return _settings;
        }

        public void OnLayerLinked(FPSAnimatorLayerSettings newSettings)
        {
        }

        public void UpdateEntity(FPSAnimatorEntity newEntity)
        {
        }

        public void OnPreGameThreadUpdate()
        {
        }

        public void UpdatePlayableJobData(AnimationScriptPlayable playable, float weight)
        {
            _jobData.weight = weight;

            if (_jobData.animator.isHuman)
            {
                _turnOffset = _jobData.inputController.GetValue<float>(_turnProperty);
            }
            
            playable.SetJobData(this);
        }
        
        public void LateUpdate()
        {
        }

        public void Destroy()
        {
        }
    }
}