// Designed by KINEMATION, 2024.

using KINEMATION.Shared.KAnimationCore.Runtime.Rig;

using FuzzySharp;

using UnityEditor;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Editor.Tools
{
    public class FPSAnimatorWizardMenu
    {
        private const string MenuName = "GameObject/FPS ANIMATOR Wizard";

        public struct BoneSearchResult
        {
            public Transform transform;
            public int bestScore;
        }

        private static void ComputeStringMatchCost(Transform input, string[] candidates, ref BoneSearchResult boneSearch)
        {
            if (candidates == null)
            {
                return;
            }

            int score = Fuzz.WeightedRatio(input.name.ToLower(), candidates[0].ToLower());
            
            foreach (var candidate in candidates)
            {
                int candidateScore = Fuzz.WeightedRatio(input.name.ToLower(), candidate.ToLower());
                if(candidateScore <= score) continue;
                
                score = candidateScore;
            }
            
            if(boneSearch.bestScore >= score)
            {
                return;
            }
            
            boneSearch.transform = input;
            boneSearch.bestScore = score;
        }
        
        [MenuItem(MenuName, true)]
        private static bool ValidateWizardMenu()
        {
            return Selection.activeObject is GameObject;
        }

        [MenuItem(MenuName)]
        private static void RunWizardMenu()
        {
            GameObject character = Selection.activeObject as GameObject;
            if (character == null) return;

            Transform root = null;
            BoneSearchResult head = new BoneSearchResult();
            BoneSearchResult rightHand = new BoneSearchResult();
            BoneSearchResult leftHand = new BoneSearchResult();
            BoneSearchResult rightFoot = new BoneSearchResult();
            BoneSearchResult leftFoot = new BoneSearchResult();

            Transform pelvis = null;
            Transform spineRoot = null;
            
            int childNum = character.transform.childCount;
            for (int i = 0; i < childNum; i++)
            {
                Transform childBone = character.transform.GetChild(i);
                string boneName = childBone.name.ToLower();
                
                if(boneName.Contains("mesh")) continue;
                
                if (boneName.Contains("armature") || boneName.Contains("skeleton") || boneName.Contains("root"))
                {
                    root = childBone;
                    break;
                }
                
                if(boneName.Contains("pelvis") || boneName.Contains("hip"))
                {
                    root = childBone;
                    pelvis = root;
                    break;
                }
            }

            if (root != null)
            {
                var rigComponent = root.gameObject.GetComponent<KRigComponent>();
                if (rigComponent == null)
                {
                    rigComponent = root.gameObject.AddComponent<KRigComponent>();
                }
                
                rigComponent.RefreshHierarchy();

                var hierarchy = rigComponent.GetHierarchy();
                foreach (var element in hierarchy)
                {
                    if (pelvis == null && element.name.ToLower().Contains("hip")
                        || element.name.ToLower().Contains("pelvis"))
                    {
                        pelvis = element;
                    }
                    
                    if (pelvis != null && spineRoot == null 
                                       && element.name.ToLower().Contains("spine") && element.IsChildOf(pelvis))
                    {
                        spineRoot = element;
                    }
                    
                    ComputeStringMatchCost(element, new [] {"head"}, ref head);
                    ComputeStringMatchCost(element, new [] {"right_hand","hand_right", "hand_r"}, 
                        ref rightHand);
                    ComputeStringMatchCost(element, new [] {"left_hand", "hand_left", "hand_l"}, 
                        ref leftHand);
                    ComputeStringMatchCost(element, new [] {"right_foot","r_foot", "foot_right", "foot_r"}, 
                        ref rightFoot);
                    ComputeStringMatchCost(element, new [] {"left_foot", "l_foot", "foot_left", "foot_l"}, 
                        ref leftFoot);
                }
            }
            
            var window = FPSAnimatorWizard.CreateWindow();

            window.character = character.transform;
            window.root = root;
            window.head = head.transform;
            window.rightHand = rightHand.transform;
            window.leftHand = leftHand.transform;
            window.rightFoot = rightFoot.transform;
            window.leftFoot = leftFoot.transform;

            window.pelvis = pelvis;
            window.spineRoot = spineRoot;
            
            Animator animator = character.GetComponent<Animator>();
            if (animator != null)
            {
                window.animatorController = animator.runtimeAnimatorController;
            }
            
            window.Show(true);
        }
    }
}