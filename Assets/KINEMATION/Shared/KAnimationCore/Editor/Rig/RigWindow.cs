// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Rig
{
    public class RigWindow : EditorWindow
    {
        private OnItemClicked _onClicked;
        private OnSelectionChanged _onSelectionChanged;
        
        private Vector2 _scrollPosition;
        private string _searchEntry = string.Empty;

        private RigTreeWidget _rigTreeWidget;
        private bool _useSelection = false;
        
        public static void ShowWindow(KRigElement[] hierarchy, OnItemClicked onClicked, 
            OnSelectionChanged onSelectionChanged, bool useSelection, List<int> selection = null, string title = "Selection")
        {
            RigWindow window = CreateInstance<RigWindow>();

            window._useSelection = useSelection;
            window._onClicked = onClicked;
            window._onSelectionChanged = onSelectionChanged;
            window.titleContent = new GUIContent(title);
            
            window._rigTreeWidget = new RigTreeWidget
            {
                rigTreeView =
                {
                    useToggle = useSelection,
                    onItemClicked = window.OnItemClicked
                }
            };

            if (selection != null)
            {
                window._rigTreeWidget.rigTreeView.SetSelection(selection);
            }
            
            window._rigTreeWidget.Refresh(hierarchy);
            window.minSize = new Vector2(450f, 550f);
            window.ShowAuxWindow();
        }

        private void OnItemClicked(KRigElement selection)
        {
            _onClicked.Invoke(selection);
            Close();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
            _searchEntry = EditorGUILayout.TextField(_searchEntry, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();
            
            _rigTreeWidget.rigTreeView.Filter(_searchEntry);
            _rigTreeWidget.Render();
        }
        
        private void OnDisable()
        {
            if (_useSelection) _onSelectionChanged?.Invoke(_rigTreeWidget.rigTreeView.GetToggledItems());
        }
    }
}