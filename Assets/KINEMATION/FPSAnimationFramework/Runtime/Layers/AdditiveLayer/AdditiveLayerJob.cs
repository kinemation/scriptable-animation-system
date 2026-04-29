// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.WeaponLayer;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.AdditiveLayer
{
    public struct AdditiveLayerJob : IAnimationJob, IAnimationLayerJob
    {
        public KTransform recoil;
        public LayerJobData jobData;
        public float aimWeight;
        public float curveAimScale;
        
        private TransformStreamHandle _weaponBoneHandle;
        private TransformStreamHandle _additiveBoneHandle;
        
        private AdditiveLayerSettings _settings;
        private RecoilAnimation _recoilAnimation;
        
        private KTransform _ikMotion;
        
        private float _curveSmoothing;
        private int _aimingWeightPropertyIndex;
        
        private WeaponLayerJobData _weaponJobData;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            if (!KAnimationMath.IsWeightRelevant(jobData.weight))
            {
                return;
            }
            
            if (Mathf.Approximately(_settings.interpSpeed, 0f))
            {
                _curveSmoothing = 1f;
            }
            else
            {
                _curveSmoothing = KMath.ExpDecayAlpha(_settings.interpSpeed, stream.deltaTime);
            }
            
            _weaponJobData.Cache(stream);
            
            AnimLayerJobUtility.MoveInSpace(stream, _weaponBoneHandle, _weaponBoneHandle, recoil.position, 
                jobData.weight);
            AnimLayerJobUtility.RotateInSpace(stream, _weaponBoneHandle, _weaponBoneHandle, recoil.rotation,
                jobData.weight);

            var additive = AnimLayerJobUtility.GetTransformFromHandle(stream, _additiveBoneHandle, false);
            _ikMotion = KTransform.Lerp(_ikMotion, additive, _curveSmoothing);
            
            AnimLayerJobUtility.MoveInSpace(stream, jobData.rootHandle, _weaponBoneHandle, 
                _ikMotion.position, jobData.weight * curveAimScale);
            AnimLayerJobUtility.RotateInSpace(stream, jobData.rootHandle, _weaponBoneHandle, 
                _ikMotion.rotation, jobData.weight * curveAimScale);
            
            _weaponJobData.PostProcessPose(stream, jobData.weight);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData newJobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (AdditiveLayerSettings) settings;
            
            this.jobData = newJobData;
            _weaponJobData = new WeaponLayerJobData();
            _weaponJobData.Setup(this.jobData, _settings);

            _recoilAnimation = jobData.animator.GetComponent<RecoilAnimation>();
            _aimingWeightPropertyIndex = this.jobData.inputController.GetPropertyIndex(_settings.aimingInputProperty);
            
            recoil = KTransform.Identity;

            Transform ikWeaponBone = jobData.rigComponent.GetRigTransform(_settings.weaponIkBone);
            _weaponBoneHandle = jobData.animator.BindStreamTransform(ikWeaponBone);
            
            var additiveBone = jobData.rigComponent.GetRigTransform(_settings.additiveBone);
            _additiveBoneHandle = jobData.animator.BindStreamTransform(additiveBone);
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
            var job = playable.GetJobData<AdditiveLayerJob>();
            
            job.jobData.weight = weight;
            job.aimWeight = jobData.inputController.GetValue<float>(_aimingWeightPropertyIndex);
            job.curveAimScale = Mathf.Lerp(1f, _settings.adsScalar, job.aimWeight);
            
            if (_recoilAnimation != null && _recoilAnimation.RecoilData != null)
            {
                job.recoil.rotation = _recoilAnimation.OutRot;
                job.recoil.position = _recoilAnimation.OutLoc;
            }
            
            playable.SetJobData(job);
        }

        public void LateUpdate()
        {
        }

        public void Destroy()
        {
        }
    }
}