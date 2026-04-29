// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace KINEMATION.Shared.ScriptableWidget.Editor
{
    public class ScriptableComponentDropdownItem : AdvancedDropdownItem
    {
        public int index;
        
        public ScriptableComponentDropdownItem(string name, int index) : base(name)
        {
            this.index = index;
        }
    }
    
    public class ScriptableComponentDropdown : AdvancedDropdown
    {
        public Action<int, string> onTypeSelected;
        private List<string> _options;
        
        public ScriptableComponentDropdown(AdvancedDropdownState state, List<string> options) : base(state)
        {
            _options = options;
        }
        
        public void SetWindowSize(Vector2 minSize)
        {
            var window = EditorWindow.focusedWindow;

            if(window == null)
            {
                Debug.LogWarning("EditorWindow.focusedWindow was null.");
                return;
            }

            if(!string.Equals(window.GetType().Namespace, typeof(AdvancedDropdown).Namespace))
            {
                Debug.LogWarning("EditorWindow.focusedWindow " + window.GetType().FullName + " was not in expected namespace.");
                return;
            }
            
            Vector2 originalMinSize = window.minSize;
            if (minSize.x >= 0f) originalMinSize.x = minSize.x;
            if (minSize.y >= 0f) originalMinSize.y = minSize.y;
            window.minSize = originalMinSize;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select type");
            Dictionary<string, ScriptableComponentDropdownItem> itemsMap = new();
            
            int selectionIndex = 0;
            foreach (var option in _options)
            {
                var localRoot = root;
                string[] parts = option.Split(".");
                string fullPath = "";
                
                int count = parts.Length;
                for (int i = 0; i < count; i++)
                {
                    var part = parts[i];
                    fullPath = i == 0 ? part : $"{fullPath}.{part}";

                    itemsMap.TryGetValue(fullPath, out var item);
                    if (item == null)
                    {
                        item = new ScriptableComponentDropdownItem(part, selectionIndex);
                        itemsMap[fullPath] = item;
                        localRoot.AddChild(item);
                        if (i == count - 1) selectionIndex++;
                    }

                    localRoot = item;
                }
            }
            
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            onTypeSelected?.Invoke(((ScriptableComponentDropdownItem) item).index, item.name);
        }
    }
}