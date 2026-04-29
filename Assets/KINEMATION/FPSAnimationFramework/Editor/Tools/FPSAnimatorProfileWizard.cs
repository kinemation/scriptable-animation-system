// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using KINEMATION.FPSAnimationFramework.Runtime.Layers.AdditiveLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.AdsLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.IkLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.IkMotionLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.LookLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.PoseSamplerLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.SwayLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.TurnLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.ViewLayer;

using UnityEditor;
using UnityEngine;

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Editor;

namespace KINEMATION.FPSAnimationFramework.Editor.Tools
{
    public class FPSAnimatorProfileWizard
    {
        private const string MenuName = "Assets/FPS PROFILE Wizard";
        
        private static void AddComponent(FPSAnimatorProfile profile, FPSAnimatorLayerSettings layerSettings)
        {
            layerSettings.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            layerSettings.name = layerSettings.GetType().Name;
            
            profile.settings.Add(layerSettings);
            AssetDatabase.AddObjectToAsset(layerSettings, profile);
        }
        
        [MenuItem(MenuName, true)]
        private static bool ValidateWizardMenu()
        {
            return Selection.activeObject is KRig;
        }

        [MenuItem(MenuName)]
        private static void RunWizardMenu()
        {
            KRig rig = Selection.activeObject as KRig;
            if (rig == null)
            {
                Debug.Log("Failure");
                return;
            }

            FPSAnimatorProfile profile = ScriptableObject.CreateInstance<FPSAnimatorProfile>();
            profile.rigAsset = rig;
            profile.settings = new List<FPSAnimatorLayerSettings>();
            
            KEditorUtility.SaveAsset(profile, KEditorUtility.GetProjectActiveFolder(), 
                $"AnimatorProfile_{rig.name}.asset");

            var poseSampler = ScriptableObject.CreateInstance<PoseSamplerLayerSettings>();
            AddComponent(profile, poseSampler);
            AddComponent(profile, ScriptableObject.CreateInstance<ViewLayerSettings>());
            AddComponent(profile, ScriptableObject.CreateInstance<AdsLayerSettings>());
            AddComponent(profile, ScriptableObject.CreateInstance<SwayLayerSettings>());
            AddComponent(profile, ScriptableObject.CreateInstance<IkMotionLayerSettings>());
            AddComponent(profile, ScriptableObject.CreateInstance<AdditiveLayerSettings>());

            var lookLayer = ScriptableObject.CreateInstance<LookLayerSettings>();
            lookLayer.useTurnOffset = true;
            AddComponent(profile, lookLayer);

            var turnLayer = ScriptableObject.CreateInstance<TurnLayerSettings>();
            AddComponent(profile, turnLayer);
            
            var ikLayer = ScriptableObject.CreateInstance<IkLayerSettings>();
            AddComponent(profile,ikLayer);
            
            KRigElementChain pelvis = rig.GetElementChainByName(FPSANames.Chain_Pelvis);
            KRigElementChain spineRoot = rig.GetElementChainByName(FPSANames.Chain_SpineRoot);
            KRigElementChain rightHand = rig.GetElementChainByName(FPSANames.Chain_RightHand);
            KRigElementChain leftHand = rig.GetElementChainByName(FPSANames.Chain_LeftHand);
            KRigElementChain rightFoot = rig.GetElementChainByName(FPSANames.Chain_RightFoot);
            KRigElementChain leftFoot = rig.GetElementChainByName(FPSANames.Chain_LeftFoot);
            
            if (pelvis != null)
            {
                poseSampler.pelvis = pelvis.elementChain[0];
                turnLayer.characterHipBone = pelvis.elementChain[0];
            }

            if (spineRoot != null)
            {
                poseSampler.spineRoot = spineRoot.elementChain[0];
                turnLayer.characterRootBone = rig.rigHierarchy[0];
            }

            if (rightHand != null) ikLayer.rightHand = rightHand.elementChain[0];
            if (leftHand != null) ikLayer.leftHand = leftHand.elementChain[0];
            if (rightFoot != null) ikLayer.rightFoot = rightFoot.elementChain[0];
            if (leftFoot != null) ikLayer.leftFoot = leftFoot.elementChain[0];
            
            profile.OnRigUpdated();
            
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }
    }
}