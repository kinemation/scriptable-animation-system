// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Rig
{
    public delegate void OnItemClicked(KRigElement selection);
    public delegate void OnSelectionChanged(KRigElement[] selectedItems);
    
    public class RigTreeView : TreeView
    {
        public OnItemClicked onItemClicked;
        
        public float singleRowHeight = 0f;
        public bool useToggle;
        
        private List<TreeViewItem> _treeItems;
        private KRigElement[] _originalItems;
        private bool[] _selectedItems;
        
        public RigTreeView(TreeViewState state) : base(state)
        {
            _treeItems = new List<TreeViewItem>();
            Reload();
        }

        public KRigElement[] GetToggledItems()
        {
            List<KRigElement> toggledItems = new List<KRigElement>();

            int index = 0;
            foreach (var element in _originalItems)
            {
                if (_selectedItems[index])
                {
                    var newElement = element;
                    newElement.index = index;
                    toggledItems.Add(newElement);
                }
                index++;
            }

            return toggledItems.ToArray();
        }

        public void InitializeTreeItems(KRigElement[] hierarchy)
        {
            _treeItems.Clear();
            
            int count = hierarchy.Length;
            _originalItems = new KRigElement[count];

            int depthOffset = useToggle ? 1 : 0;
            for (int i = 0; i < count; i++)
            {
                _treeItems.Add(new TreeViewItem(i + 1, hierarchy[i].depth + depthOffset, hierarchy[i].name));
            }
            
            hierarchy.CopyTo(_originalItems, 0);
            
            _selectedItems = new bool[count];
            var selection = GetSelection();
            
            foreach (var index in selection)
            {
                _selectedItems[index - 1] = true;
            }
        }
        
        public void Filter(string query)
        {
            int depthOffset = useToggle ? 1 : 0;
            
            _treeItems.Clear();
            query = query.ToLower().Trim();
            
            int count = _originalItems.Length;
            for (int i = 0; i < count; i++)
            {
                if (string.IsNullOrEmpty(query))
                {
                    _treeItems.Add(new TreeViewItem(i + 1, _originalItems[i].depth + depthOffset,
                        _originalItems[i].name));
                    continue;
                }
                
                if (!_originalItems[i].name.ToLower().Trim().Contains(query)) continue;
                
                _treeItems.Add(new TreeViewItem(i + 1, depthOffset, _originalItems[i].name));
            }
            
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            // 0 is the root ID, -1 means the root has no parent
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
            // Utility method to setup the parent/children relationship
            SetupParentsAndChildrenFromDepths(root, _treeItems);

            return root;
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            Color darkGrey = new Color(0.2f, 0.2f, 0.2f);
            Color lightGrey = new Color(0.25f, 0.25f, 0.25f);
            Color blue = new Color(115f / 255f, 147f / 255f, 179f / 255f, 0.25f);
            
            bool isSelected = args.selected;
            if (args.rowRect.Contains(Event.current.mousePosition)) isSelected = true;

            var color = isSelected ? blue : args.row % 2 == 0 ? lightGrey : darkGrey;
            EditorGUI.DrawRect(args.rowRect, color);

            if (useToggle)
            {
                var rect = args.rowRect;
                rect.width = rect.height;

                bool prevToggle = _selectedItems[args.item.id - 1];
                bool toggle = EditorGUI.Toggle(rect, prevToggle);

                if (toggle != prevToggle)
                {
                    // If this item is a part of a larger selection, update the status globally.
                    if (IsSelected(args.item.id))
                    {
                        var selection = GetSelection();
                        foreach (var selectedId in selection) _selectedItems[selectedId - 1] = toggle;
                    } // Otherwise, change this toggle only.
                    else _selectedItems[args.item.id - 1] = toggle;
                }
            }

            singleRowHeight = rowHeight;

            if (!useToggle)
            {
                Rect buttonRect = args.rowRect;
                float indent = GetContentIndent(args.item);
                buttonRect.x += indent;
                
                if (GUI.Button(buttonRect, args.item.displayName, EditorStyles.label))
                {
                    var element = _originalItems[args.item.id - 1];
                    element.index = args.item.id - 1;
                    onItemClicked?.Invoke(element);
                }

                return;
            }
            
            base.RowGUI(args);
        }
    }

    public class RigTreeWidget
    {
        public RigTreeView rigTreeView = new RigTreeView(new TreeViewState());

        public void Refresh(KRigElement[] hierarchy)
        {
            rigTreeView.InitializeTreeItems(hierarchy);
            rigTreeView.Reload();
            rigTreeView.ExpandAll();
        }
        
        public void Render()
        {
            float maxHeight = rigTreeView.singleRowHeight + rigTreeView.totalHeight;
            float height = Mathf.Max(rigTreeView.singleRowHeight * 2f, maxHeight);
            
            EditorGUILayout.BeginHorizontal();
            Rect parentRect = GUILayoutUtility.GetRect(0f, 0f, 0f, height);
            EditorGUILayout.EndHorizontal();
            
            float padding = 7f;
            
            GUI.Box(parentRect, "", EditorStyles.helpBox);

            parentRect.x += padding;
            parentRect.y += padding;

            parentRect.width -= 2f * padding;
            parentRect.height -= 2f * padding;
        
            rigTreeView.OnGUI(parentRect);
        }
    }
}