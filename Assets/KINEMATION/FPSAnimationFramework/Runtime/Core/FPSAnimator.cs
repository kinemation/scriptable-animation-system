// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;

using System;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Runtime.Core
{
    [HelpURL("https://kinemation.gitbook.io/scriptable-animation-system/workflow/components")]
    public class FPSAnimator : MonoBehaviour
    {
        public bool HasLinkedProfile { get; private set; }

        [SerializeField] protected FPSAnimatorProfile animatorProfile;
        
        [NonSerialized] public IPlayablesController playablesController;
        [NonSerialized] protected FPSBoneController _boneController;
        [NonSerialized] protected UserInputController _inputController;
        [NonSerialized] protected FPSCameraController _cameraController;

        protected bool _isInitialized;

        private bool _wasAnimatorEnabled;
        private Animator _animator;
        
        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void Update()
        {
            if (_animator.isActiveAndEnabled != _wasAnimatorEnabled && !_wasAnimatorEnabled)
            {
                RebuildPlayables();
            }
            
            _wasAnimatorEnabled = _animator.isActiveAndEnabled;
            
            if (_boneController == null) return;
            _boneController.UpdateController();
        }

        protected virtual void LateUpdate()
        {
            if(_boneController != null) _boneController.LateUpdateController();
            if (_cameraController != null) _cameraController.UpdateCamera();
        }

        protected virtual void OnDestroy()
        {
            if (_boneController == null) return;
            _boneController.Dispose();
        }

        public void RebuildPlayables()
        {
            playablesController.RebuildPlayables();
            _boneController.RebuildPlayables();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            _animator = GetComponent<Animator>();
            _wasAnimatorEnabled = _animator.isActiveAndEnabled;
            
            _boneController = GetComponent<FPSBoneController>();
            _inputController = GetComponent<UserInputController>();
            playablesController = GetComponent<IPlayablesController>();
            _cameraController = GetComponentInChildren<FPSCameraController>();

            _inputController.Initialize();
            playablesController.InitializeController();
            _boneController.Initialize();
            
            if(_cameraController != null) _cameraController.Initialize();
            
            _boneController.LinkAnimatorProfile(animatorProfile);
            _isInitialized = true;
        }

        public void UnlinkAnimatorProfile()
        {
            if (_boneController == null) return;
            
            _boneController.UnlinkAnimatorProfile();
            HasLinkedProfile = false;
        }

        public void LinkAnimatorProfile(GameObject itemEntity)
        {
            if (_boneController == null) return;
            
            if (itemEntity.GetComponent<FPSAnimatorEntity>() is var entity && entity != null)
            {
                _boneController.UpdateEntity(entity);
                LinkAnimatorProfile(entity.animatorProfile);
            }
        }

        public void LinkAnimatorProfile(FPSAnimatorProfile newProfile)
        {
            if (_boneController == null) return;
            
            _boneController.LinkAnimatorProfile(newProfile);
            HasLinkedProfile = true;
        }
        
        // Will force to dynamically link the layer via OnSettingsUpdated callback.
        public void LinkAnimatorLayer(FPSAnimatorLayerSettings newSettings)
        {
            if (_boneController == null) return;
            
            _boneController.LinkAnimatorLayer(newSettings);
        }
    }
}