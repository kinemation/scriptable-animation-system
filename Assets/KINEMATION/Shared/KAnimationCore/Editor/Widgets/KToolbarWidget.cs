// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Widgets
{
    public struct KToolbarTab
    {
        public delegate void KOnTabRendered();
        
        public string name;
        public KOnTabRendered onTabRendered;
    }
    
    public class KToolbarWidget
    {
        private int _toolbarIndex = 0;
        private string[] _toolbarTabNames;
        private KToolbarTab[] _toolbarTabs;

        public KToolbarWidget(KToolbarTab[] tabs)
        {
            _toolbarTabs = tabs;
            _toolbarTabNames = new string[_toolbarTabs.Length];

            for (int i = 0; i < _toolbarTabs.Length; i++)
            {
                _toolbarTabNames[i] = _toolbarTabs[i].name;
            }
        }

        public void Render()
        {
            if (_toolbarTabNames.Length == 0) return;

            _toolbarIndex = GUILayout.Toolbar(_toolbarIndex, _toolbarTabNames);
            _toolbarTabs[_toolbarIndex].onTabRendered?.Invoke();
        }
    }
}