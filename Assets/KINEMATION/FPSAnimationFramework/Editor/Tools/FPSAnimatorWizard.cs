// Designed by KINEMATION, 2024.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;

using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;

using System.IO;
using KINEMATION.Shared.KAnimationCore.Editor;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Editor.Tools
{
    public class FPSAnimatorWizard : EditorWindow
    {
        //~ Character Tab
        public Transform character;
        public Transform root;
        public Transform pelvis;
        public Transform spineRoot;
        public Transform head;
        public Transform rightHand;
        public Transform leftHand;
        public Transform rightFoot;
        public Transform leftFoot;

        public RuntimeAnimatorController animatorController;
        private KRig _rigAsset;
        private UserInputConfig _inputConfig;
        
        private Vector2 _scrollPosition;
        
        public static FPSAnimatorWizard CreateWindow()
        {
            return GetWindow<FPSAnimatorWizard>(false, "FPS Animator Wizard", true);
        }
        
        private T InitializeComponent<T>(GameObject parent) where T : Behaviour
        {
            var component = parent.GetComponent<T>();

            if (component == null)
            {
                component = parent.AddComponent<T>();
            }

            return component;
        }

        private Transform InitializeGameObject(Transform parent, string objectName)
        {
            Transform transform = parent.Find(objectName);
            if (transform == null)
            {
                GameObject gameObject = new GameObject(objectName);
                gameObject.transform.parent = parent;
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                transform = gameObject.transform;
            }

            return transform;
        }

        private void InitializeVirtualElement(Transform ikBone, Transform targetBone)
        {
            var virtualElement = InitializeComponent<KVirtualElement>(ikBone.gameObject);
            virtualElement.targetBone = targetBone;
        }

        private void SetupIkTargets()
        {
            InitializeGameObject(rightHand, FPSANames.IkWeaponBoneRight);
            InitializeGameObject(leftHand, FPSANames.IkWeaponBoneLeft);
            
            InitializeGameObject(root, FPSANames.WeaponBone);
            InitializeGameObject(root, FPSANames.WeaponBoneAdditive);
            
            var ikWeaponBone = InitializeGameObject(head, FPSANames.IkWeaponBone);
            var ikRightHand = InitializeGameObject(ikWeaponBone, FPSANames.IkRightHand);
            var ikLeftHand = InitializeGameObject(ikWeaponBone, FPSANames.IkLeftHand);
            var ikRightElbow = InitializeGameObject(ikWeaponBone, FPSANames.IkRightElbow);
            var ikLeftElbow = InitializeGameObject(ikWeaponBone, FPSANames.IkLeftElbow);
            
            InitializeVirtualElement(ikRightHand, rightHand);
            InitializeVirtualElement(ikLeftHand, leftHand);
            InitializeVirtualElement(ikRightElbow, rightHand.parent);
            InitializeVirtualElement(ikLeftElbow, leftHand.parent);
            
            var ikRightFoot = InitializeGameObject(root, FPSANames.IkRightFoot);
            var ikLeftFoot = InitializeGameObject(root, FPSANames.IkLeftFoot);
            var ikRightKnee = InitializeGameObject(root, FPSANames.IkRightKnee);
            var ikLeftKnee = InitializeGameObject(root, FPSANames.IkLeftKnee);
            
            InitializeVirtualElement(ikRightFoot, rightFoot);
            InitializeVirtualElement(ikLeftFoot, leftFoot);
            InitializeVirtualElement(ikRightKnee, rightFoot.parent);
            InitializeVirtualElement(ikLeftKnee, leftFoot.parent);
        }

        private void SetupCharacter()
        {
            var animator = InitializeComponent<Animator>(character.gameObject);
            animator.runtimeAnimatorController = animatorController;

            Camera camera = head.gameObject.GetComponentInChildren<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Camera");
                cameraObject.transform.parent = head;
                cameraObject.transform.localPosition = Vector3.zero;
                cameraObject.transform.rotation = character.rotation;
                camera = cameraObject.AddComponent<Camera>();
            }
            
            var fpsCamera = InitializeComponent<FPSCameraController>(camera.gameObject);
            fpsCamera.transform.rotation = character.rotation;
            
            var rigComponent = InitializeComponent<KRigComponent>(root.gameObject);
            
            InitializeComponent<FPSAnimator>(character.gameObject);
            InitializeComponent<FPSBoneController>(character.gameObject);
            var inputController = InitializeComponent<UserInputController>(character.gameObject);
            inputController.inputConfig = _inputConfig;
            
            InitializeComponent<FPSPlayablesController>(character.gameObject);
            InitializeComponent<RecoilAnimation>(character.gameObject);
            rigComponent.RefreshHierarchy();

            string path = $"{KEditorUtility.GetProjectActiveFolder()}/{character.name}_FPSAnimator";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (_rigAsset == null)
            {
                _rigAsset = ScriptableObject.CreateInstance<KRig>();
                _rigAsset.inputConfig = _inputConfig;
                _rigAsset.ImportRig(rigComponent);

                _rigAsset.targetAnimator = animatorController;
                
                _rigAsset.rigCurves.Add(FPSANames.Curve_Overlay);
                _rigAsset.rigCurves.Add(FPSANames.Curve_WeaponBoneWeight);
                _rigAsset.rigCurves.Add(FPSANames.Curve_MaskAttachHand);
                
                string filePath = AssetDatabase.GenerateUniqueAssetPath($"{path}/Rig_{character.name}.asset");
                AssetDatabase.CreateAsset(_rigAsset, filePath);
            }
            else
            {
                if (_rigAsset.GetElementChainByName(FPSANames.Chain_Pelvis) is var pelvisChain)
                {
                    _rigAsset.rigElementChains.Remove(pelvisChain);
                }
                
                if (_rigAsset.GetElementChainByName(FPSANames.Chain_SpineRoot) is var spineChain)
                {
                    _rigAsset.rigElementChains.Remove(spineChain);
                }
                
                if (_rigAsset.GetElementChainByName(FPSANames.Chain_RightHand) is var rightHandChain)
                {
                    _rigAsset.rigElementChains.Remove(rightHandChain);
                }
                
                if (_rigAsset.GetElementChainByName(FPSANames.Chain_LeftHand) is var leftHandChain)
                {
                    _rigAsset.rigElementChains.Remove(leftHandChain);
                }
                
                if (_rigAsset.GetElementChainByName(FPSANames.Chain_RightFoot) is var rightFootChain)
                {
                    _rigAsset.rigElementChains.Remove(rightFootChain);
                }
                
                if (_rigAsset.GetElementChainByName(FPSANames.Chain_LeftFoot) is var leftFootChain)
                {
                    _rigAsset.rigElementChains.Remove(leftFootChain);
                }
            }

            _rigAsset.rigElementChains.Add(new KRigElementChain()
            {
                chainName = FPSANames.Chain_Pelvis,
                elementChain = new List<KRigElement>() { _rigAsset.GetElementByName(pelvis.name) }
            });
            
            _rigAsset.rigElementChains.Add(new KRigElementChain()
            {
                chainName = FPSANames.Chain_SpineRoot,
                elementChain = new List<KRigElement>() { _rigAsset.GetElementByName(spineRoot.name) }
            });
            
            _rigAsset.rigElementChains.Add(new KRigElementChain()
            {
                chainName = FPSANames.Chain_RightHand,
                elementChain = new List<KRigElement>() { _rigAsset.GetElementByName(rightHand.name) }
            });
            
            _rigAsset.rigElementChains.Add(new KRigElementChain()
            {
                chainName = FPSANames.Chain_LeftHand,
                elementChain = new List<KRigElement>() { _rigAsset.GetElementByName(leftHand.name) }
            });
            
            _rigAsset.rigElementChains.Add(new KRigElementChain()
            {
                chainName = FPSANames.Chain_RightFoot,
                elementChain = new List<KRigElement>() { _rigAsset.GetElementByName(rightFoot.name) }
            });
            
            _rigAsset.rigElementChains.Add(new KRigElementChain()
            {
                chainName = FPSANames.Chain_LeftFoot,
                elementChain = new List<KRigElement>() { _rigAsset.GetElementByName(leftFoot.name) }
            });

            EditorUtility.SetDirty(_rigAsset);
            AssetDatabase.SaveAssets();
        }

        private bool RenderTransform(ref Transform bone, string label)
        {
            bone = (Transform) EditorGUILayout.ObjectField(label, bone, 
                typeof(Transform), true);
            
            if (bone == null)
            {
                EditorGUILayout.HelpBox($"Select {label}", MessageType.Warning);
                return false;
            }

            return true;
        }

        private void RenderCharacterSetup()
        {
            bool allowSetup = RenderTransform(ref root, "Root")
                              & RenderTransform(ref head, "Head")
                              & RenderTransform(ref rightHand, "Right Hand")
                              & RenderTransform(ref leftHand, "Left Hand")
                              & RenderTransform(ref rightFoot, "Right Foot")
                              & RenderTransform(ref leftFoot, "Left Foot")
                              & RenderTransform(ref pelvis, "Pelvis")
                              & RenderTransform(ref spineRoot, "Spine Root");
                
            animatorController = (RuntimeAnimatorController) EditorGUILayout.ObjectField("Animator Controller", animatorController, 
                typeof(RuntimeAnimatorController), false);
            
            if (animatorController == null)
            {
                EditorGUILayout.HelpBox("Select Animator Controller", MessageType.Warning);
                allowSetup = false;
            }
            
            _inputConfig = (UserInputConfig) EditorGUILayout.ObjectField("Input Config", _inputConfig, 
                typeof(UserInputConfig), false);

            if (_inputConfig == null)
            {
                EditorGUILayout.HelpBox("Select Input Config", MessageType.Warning);
                allowSetup = false;
            }
            
            _rigAsset = (KRig) EditorGUILayout.ObjectField("Rig Asset", _rigAsset, 
                typeof(KRig), false);
            
            if (!allowSetup)
            {
                EditorGUILayout.HelpBox("Fix warnings above to proceed.", MessageType.Error);
                return;
            }

            if (GUILayout.Button("Setup Character"))
            {
                SetupIkTargets();
                SetupCharacter();

                EditorUtility.DisplayDialog("Success", "Character was set up successfully!", "Good");
                Close();
            }
        }

        private void OnGUI()
        {
            GUIStyle paddedStyle = new GUIStyle()
            {
                // Set the padding you want (left, right, top, bottom)
                padding = new RectOffset(15, 5, 5, 5)
            };
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, paddedStyle);
            RenderCharacterSetup();
            
            EditorGUILayout.EndScrollView();
        }
    }
}
