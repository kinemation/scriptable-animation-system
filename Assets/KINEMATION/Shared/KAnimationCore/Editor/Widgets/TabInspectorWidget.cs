// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KINEMATION.Shared.KAnimationCore.Runtime.Attributes;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Widgets
{
    public struct EditorTab
    {
        public string name;
        public List<SerializedProperty> properties;
    }
    
    public class TabInspectorWidget
    {
        private SerializedObject _serializedObject;

        private List<SerializedProperty> _defaultProperties;
        private List<EditorTab> _editorTabs;

        private string[] _tabNames;
        private bool _foundTab;

        private int _selectedIndex = 0;
        
        private T[] GetPropertyAttributes<T>(SerializedProperty property) where T : System.Attribute
        {
            T[] output = null;
            
            FieldInfo fieldInfo = _serializedObject.targetObject.GetType().GetField(property.propertyPath,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                output = (T[]) fieldInfo.GetCustomAttributes(typeof(T), true);
            }

            if (output == null || output.Length == 0)
            {
                return null;
            }
            
            return output;
        }
        
        public TabInspectorWidget(SerializedObject targetObject)
        {
            _serializedObject = targetObject;
        }

        public void Init()
        {
            _defaultProperties = new List<SerializedProperty>();
            _editorTabs = new List<EditorTab>();
            
            SerializedProperty property = _serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                TabAttribute[] tabAttributes = GetPropertyAttributes<TabAttribute>(property);
                if (tabAttributes == null)
                {
                    if (_foundTab)
                    {
                        _editorTabs[^1].properties.Add(property.Copy());
                        continue;
                    }
                    
                    _defaultProperties.Add(property.Copy());
                    continue;
                }
                
                _editorTabs.Add(new EditorTab()
                {
                    name = tabAttributes[0].tabName,
                    properties = new List<SerializedProperty>() { property.Copy() }
                });
                
                _foundTab = true;
            }

            _tabNames = _editorTabs.Select(item => item.name).ToArray();
        }
        
        public void OnGUI()
        {
            _serializedObject.Update();
            
            foreach (var defaultProperty in _defaultProperties)
            {
                EditorGUILayout.PropertyField(defaultProperty, true);
            }

            if (_tabNames.Length > 0)
            {
                _selectedIndex = GUILayout.Toolbar(_selectedIndex, _tabNames);
                foreach (var tabProperty in _editorTabs[_selectedIndex].properties)
                {
                    EditorGUILayout.PropertyField(tabProperty, true);
                }
            }
            
            _serializedObject.ApplyModifiedProperties();
        }
    }
}