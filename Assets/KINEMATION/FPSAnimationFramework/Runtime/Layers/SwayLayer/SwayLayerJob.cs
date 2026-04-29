using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.WeaponLayer;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KINEMATION.FPSAnimationFramework.Runtime.Layers.SwayLayer
{
    public struct VectorSpringState
    {
        public FloatSpringState x;
        public FloatSpringState y;
        public FloatSpringState z;

        public void Reset()
        {
            x.Reset();
            y.Reset();
            z.Reset();
        }
    }
    
    public struct SwayLayerJob : IAnimationJob, IAnimationLayerJob
    {
        public bool useFreeAim;
        public Vector4 mouseDelta;
        public Vector4 moveInput;
        
        private SwayLayerSettings _settings;

        // Free aiming.
        private Vector2 _freeAimTarget;
        private Vector2 _freeAimValue;
        
        // Move sway.
        private Vector3 _moveSwayPositionTarget;
        private Vector3 _moveSwayRotationTarget;
        
        private Vector3 _moveSwayPositionResult;
        private Vector3 _moveSwayRotationResult;
        
        private VectorSpringState _moveSwayPositionSpring;
        private VectorSpringState _moveSwayRotationSpring;

        // Aim sway.
        private Vector2 _aimSwayTarget;
        private Vector3 _aimSwayPositionResult;
        private Vector3 _aimSwayRotationResult;

        private VectorSpringState _aimSwayPositionSpring;
        private VectorSpringState _aimSwayRotationSpring;

        private int _freeAimPropertyIndex;
        private int _moveInputPropertyIndex;
        private int _mouseInputPropertyIndex;
        
        // Animation Job
        public LayerJobData jobData;
        public WeaponLayerJobData weaponJobData;
        private TransformStreamHandle _weaponBoneHandle;
        private TransformStreamHandle _headHandle;
        
        private Vector3 VectorSpringInterp(Vector3 current, in Vector3 target, in VectorSpring spring, 
            ref VectorSpringState state, float deltaTime)
        {
            current.x = KSpringMath.FloatSpringInterp(current.x, target.x, spring.speed.x, spring.damping.x,
                spring.stiffness.x, spring.scale.x, ref state.x, deltaTime);
            
            current.y = KSpringMath.FloatSpringInterp(current.y, target.y, spring.speed.y, spring.damping.y,
                spring.stiffness.y, spring.scale.y, ref state.y, deltaTime);
            
            current.z = KSpringMath.FloatSpringInterp(current.z, target.z, spring.speed.z, spring.damping.z,
                spring.stiffness.z, spring.scale.z, ref state.z, deltaTime);

            return current;
        }

        private void ProcessFreeAim(AnimationStream stream)
        {
            if (useFreeAim)
            {
                // Accumulate the input.
                _freeAimTarget.x += mouseDelta.x;
                _freeAimTarget.y += mouseDelta.y;
                _freeAimTarget *= _settings.freeAimInputScale;

                // Clamp the user input.
                _freeAimTarget.x = Mathf.Clamp(_freeAimTarget.x, -_settings.freeAimClamp, 
                    _settings.freeAimClamp);
            
                _freeAimTarget.y = Mathf.Clamp(_freeAimTarget.y, -_settings.freeAimClamp, 
                    _settings.freeAimClamp);
            }
            else
            {
                _freeAimTarget = Vector2.zero;
            }
            
            // Finally interpolate the value.
            _freeAimValue = Vector2.Lerp(_freeAimValue, _freeAimTarget,
                KMath.ExpDecayAlpha(_settings.freeAimInterpSpeed, stream.deltaTime));
            
            Quaternion rotation = Quaternion.Euler(new Vector3(_freeAimValue.y, _freeAimValue.x, 0f));
            rotation.Normalize();

            KTransform rootTransform = AnimLayerJobUtility.GetTransformFromHandle(stream, jobData.rootHandle);

            Vector3 headMS = rootTransform.InverseTransformPoint(_headHandle.GetPosition(stream), false);
            Vector3 gunMS = rootTransform.InverseTransformPoint(_weaponBoneHandle.GetPosition(stream), false);

            Vector3 offset = headMS - gunMS;
            offset = rotation * offset - offset;

            KTransform freeAimTransform = new KTransform()
            {
                position = -offset,
                rotation = rotation
            };

            KPose pose = new KPose()
            {
                modifyMode = EModifyMode.Add,
                pose = freeAimTransform,
                space = _settings.freeAimSpace
            };
            
            AnimLayerJobUtility.ModifyTransform(stream, jobData.rootHandle, _weaponBoneHandle, pose, 
                jobData.weight);
        }

        private void ProcessMoveSway(AnimationStream stream)
        {
            var rotationTarget = new Vector3()
            {
                x = moveInput.y,
                y = moveInput.x,
                z = moveInput.x
            };

            var positionTarget = new Vector3()
            {
                x = moveInput.x,
                y = moveInput.y,
                z = moveInput.y
            };

            float alpha = KMath.ExpDecayAlpha(_settings.moveSwayTargetDamping, stream.deltaTime);
            
            _moveSwayPositionTarget = Vector3.Lerp(_moveSwayPositionTarget, positionTarget / 100f, alpha);
            _moveSwayRotationTarget = Vector3.Lerp(_moveSwayRotationTarget, rotationTarget, alpha);

            _moveSwayPositionResult = VectorSpringInterp(_moveSwayPositionResult,
                _moveSwayPositionTarget, _settings.moveSwayPositionSpring, ref _moveSwayPositionSpring, stream.deltaTime);

            _moveSwayRotationResult = VectorSpringInterp(_moveSwayRotationResult,
                _moveSwayRotationTarget, _settings.moveSwayRotationSpring, ref _moveSwayRotationSpring, stream.deltaTime);

            KTransform transform = new KTransform()
            {
                position = _moveSwayPositionResult,
                rotation = Quaternion.Euler(_moveSwayRotationResult).normalized,
                scale = Vector3.one
            };
            
            KPose pose = new KPose()
            {
                modifyMode = EModifyMode.Add,
                pose = transform,
                space = _settings.moveSwaySpace
            };
            
            AnimLayerJobUtility.ModifyTransform(stream, jobData.rootHandle, _weaponBoneHandle, pose, jobData.weight);
        }

        private void ProcessAimSway(AnimationStream stream)
        {
            _aimSwayTarget += new Vector2(mouseDelta.x, mouseDelta.y) * 0.01f;

            float alpha = KMath.ExpDecayAlpha(_settings.aimSwayTargetDamping, stream.deltaTime);
            _aimSwayTarget = Vector2.Lerp(_aimSwayTarget, Vector2.zero, alpha);
            
            Vector3 targetLoc = new Vector3()
            {
                x = _aimSwayTarget.x,
                y = _aimSwayTarget.y,
                z = 0f
            };
            
            Vector3 targetRot = new Vector3()
            {
                x = _aimSwayTarget.y,
                y = _aimSwayTarget.x,
                z = _aimSwayTarget.x
            };

            _aimSwayPositionResult = VectorSpringInterp(_aimSwayPositionResult,
                targetLoc / 100f, _settings.aimSwayPositionSpring, ref _aimSwayPositionSpring, stream.deltaTime);

            _aimSwayRotationResult = VectorSpringInterp(_aimSwayRotationResult,
                targetRot, _settings.aimSwayRotationSpring, ref _aimSwayRotationSpring, stream.deltaTime);
            
            KTransform aimSwayTransform = new KTransform()
            {
                position = _aimSwayPositionResult,
                rotation = Quaternion.Euler(_aimSwayRotationResult)
            };
            
            KPose pose = new KPose()
            {
                modifyMode = EModifyMode.Add,
                pose = aimSwayTransform,
                space = _settings.aimSwaySpace
            };
            
            AnimLayerJobUtility.ModifyTransform(stream, jobData.rootHandle, _weaponBoneHandle, pose, jobData.weight);
        }
        
        public void ProcessAnimation(AnimationStream stream)
        {
            weaponJobData.Cache(stream);
            
            ProcessFreeAim(stream);
            ProcessMoveSway(stream);
            ProcessAimSway(stream);
            
            weaponJobData.PostProcessPose(stream, jobData.weight);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void Initialize(LayerJobData newJobData, FPSAnimatorLayerSettings settings)
        {
            _settings = (SwayLayerSettings) settings;
            
            jobData = newJobData;
            weaponJobData = new WeaponLayerJobData();
            weaponJobData.Setup(jobData, _settings);

            _weaponBoneHandle = weaponJobData.weaponHandle;
            _headHandle 
                = jobData.animator.BindStreamTransform(this.jobData.rigComponent.GetRigTransform(_settings.headBone));
            
            _moveSwayPositionSpring.Reset();
            _moveSwayRotationSpring.Reset();

            _aimSwayPositionSpring.Reset();
            _aimSwayRotationSpring.Reset();

            _freeAimPropertyIndex = jobData.inputController.GetPropertyIndex(_settings.useFreeAimProperty);
            _moveInputPropertyIndex = jobData.inputController.GetPropertyIndex(_settings.moveInputProperty);
            _mouseInputPropertyIndex = jobData.inputController.GetPropertyIndex(_settings.mouseDeltaInputProperty);
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
            var job = playable.GetJobData<SwayLayerJob>();
            
            job.jobData.weight = weight;
            job.useFreeAim = jobData.inputController.GetValue<bool>(_freeAimPropertyIndex);
            job.mouseDelta = jobData.inputController.GetValue<Vector4>(_mouseInputPropertyIndex) * Time.timeScale;
            job.moveInput = jobData.inputController.GetValue<Vector4>(_moveInputPropertyIndex);
            
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