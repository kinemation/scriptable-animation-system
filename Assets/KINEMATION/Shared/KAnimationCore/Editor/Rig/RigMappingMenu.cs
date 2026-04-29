// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Rig
{
    public class StringListWidget
    {
        public List<string> list = new List<string>();
        private ReorderableList _reorderableOptionsList;

        public void Initialize(string listName)
        {
            _reorderableOptionsList = new ReorderableList(list, typeof(string), true, 
                true, true, true);

            _reorderableOptionsList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, listName);
            };

            _reorderableOptionsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                list[index] = EditorGUI.TextField(rect, list[index]);
            };

            _reorderableOptionsList.onAddCallback = (ReorderableList list) =>
            {
                this.list.Add("");
            };

            _reorderableOptionsList.onRemoveCallback = (ReorderableList list) =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            };
        }

        public void Render()
        {
            _reorderableOptionsList.DoLayoutList();
        }
    }
    
    public class RigMappingWindow : EditorWindow
    {
        private KRig _rigAsset;
        private GameObject _root;
        private StringListWidget _entriesToAvoid;
        
        public static RigMappingWindow CreateWindow()
        {
            var window = GetWindow<RigMappingWindow>(false, "Rig Mapping", true);
            return window;
        }
        
        private void TraverseHierarchy(Transform root, ref KRigElementChain chain, KRig rig)
        {
            KRigElement element = rig.rigHierarchy.Find(item => item.name.Equals(root.name));
            chain.elementChain.Add(element);
            
            // Filter child bones from corrections, twists and IKs.
            List<int> fkBoneIndexes = new List<int>();
            for (int i = 0; i < root.childCount; i++)
            {
                string childName = root.GetChild(i).name.ToLower();

                bool bSkipIteration = false;
                foreach (var entry in _entriesToAvoid.list)
                {
                    if(string.IsNullOrEmpty(entry)) continue;
                    if (childName.Contains(entry.ToLower()))
                    {
                        bSkipIteration = true;
                        break;
                    }
                }
                
                if (bSkipIteration)
                {
                    continue;
                }
                
                fkBoneIndexes.Add(i);
            }
            
            // If no extra branches, traverse the old chain.
            if (fkBoneIndexes.Count == 1)
            {
                TraverseHierarchy(root.GetChild(fkBoneIndexes[0]), ref chain, rig);
                return;
            }
            
            // If we have branches, create new chains and start traversing them.
            foreach (var boneIndex in fkBoneIndexes)
            {
                string chainName = root.GetChild(boneIndex).name;
                KRigElementChain newChain = new KRigElementChain()
                {
                    chainName = chainName,
                    elementChain = new List<KRigElement>()
                };
                
                rig.rigElementChains.Add(newChain);
                TraverseHierarchy(root.GetChild(boneIndex), ref newChain, rig);
            }
        }
        
        private void MapRigChains(GameObject root)
        {
            if (root == null)
            {
                Debug.LogWarning("RigMappingWindow: Selected GameObject is NULL!");
                return;
            }
            
            KRigComponent rigComponent = root.GetComponent<KRigComponent>();
            if (rigComponent == null)
            {
                rigComponent = root.AddComponent<KRigComponent>();
                rigComponent.RefreshHierarchy();
            }

            if (_rigAsset == null)
            {
                _rigAsset = ScriptableObject.CreateInstance<KRig>();
            }
            else
            {
                _rigAsset.rigElementChains.Clear();
            }
            
            _rigAsset.ImportRig(rigComponent);

            KRigElementChain newChain = new KRigElementChain()
            {
                chainName = root.name,
                elementChain = new List<KRigElement>()
            };
            
            _rigAsset.rigElementChains.Add(newChain);
            TraverseHierarchy(root.transform, ref newChain, _rigAsset);

            if (EditorUtility.IsPersistent(_rigAsset))
            {
                EditorUtility.SetDirty(_rigAsset);
                AssetDatabase.SaveAssetIfDirty(_rigAsset);
                return;
            }
            
            Undo.RegisterCreatedObjectUndo(_rigAsset, "Create Rig Asset");
            string path = $"{KEditorUtility.GetProjectActiveFolder()}/Rig_{root.transform.root.name}.asset";
            AssetDatabase.CreateAsset(_rigAsset, AssetDatabase.GenerateUniqueAssetPath(path));
        }

        private void OnEnable()
        {
            _entriesToAvoid = new StringListWidget();
            _entriesToAvoid.Initialize("Bone Names to Avoid");

            _entriesToAvoid.list.Add("twist");
            _entriesToAvoid.list.Add("correct");
            
            _root = Selection.activeObject as GameObject;
        }

        public void OnGUI()
        {
            EditorGUILayout.HelpBox("If empty, a new asset will be created.", MessageType.Info);
            _rigAsset = (KRig) EditorGUILayout.ObjectField("Rig Asset", _rigAsset, typeof(KRig),
                false);
            
            _root = (GameObject) EditorGUILayout.ObjectField("Root Bone", _root, typeof(GameObject), 
                true);
             
            _entriesToAvoid.Render();

            if (GUILayout.Button("Create Rig Mapping"))
            {
                MapRigChains(_root);
            }
        }
    }
    
    public class RigMappingMenu
    {
        private const string RigItemName = "GameObject/Auto Rig Mapping";
        
        [MenuItem(RigItemName, true)]
        private static bool ValidateCreateRigMapping()
        {
            return Selection.activeObject is GameObject;
        }

        [MenuItem(RigItemName)]
        private static void CreateRigMapping()
        {
            var window = RigMappingWindow.CreateWindow();
            window.Show();
        }
    }
}