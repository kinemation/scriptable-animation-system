// Designed by Kinemation, 2023

using Demo.Scripts.Runtime;

using System.Collections.Generic;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace Demo.Scripts.Editor
{
    [CustomEditor(typeof(FPSController), true)]
    public class TabAttribute : UnityEditor.Editor
    {
        private int selectedTab;
        private List<string> tabHeaders;
        private Dictionary<string, List<SerializedProperty>> tabProperties;

        private void OnEnable()
        {
            tabHeaders = new List<string>();
            tabProperties = new Dictionary<string, List<SerializedProperty>>();

            SerializedProperty property = serializedObject.GetIterator();
            string currentHeader = null;

            do
            {
                if (property.depth > 0)
                {
                    continue;
                }

                // Check if the property has a Header attribute
                var attributes = GetPropertyAttributes<Runtime.TabAttribute>(property);
                if (attributes.Length > 0)
                {
                    currentHeader = attributes[0].tabName;
                    tabHeaders.Add(currentHeader);
                    tabProperties[currentHeader] = new List<SerializedProperty>();
                }
                
                if (currentHeader != null)
                {
                    tabProperties[currentHeader].Add(property.Copy());
                }

            } while (property.NextVisible(true));
        }

        public override void OnInspectorGUI()
        {
            if (tabHeaders.Count > 0)
            {
                selectedTab = GUILayout.Toolbar(selectedTab, tabHeaders.ToArray());

                if (selectedTab >= 0 && selectedTab < tabHeaders.Count)
                {
                    string header = tabHeaders[selectedTab];
                    foreach (SerializedProperty property in tabProperties[header])
                    {
                        serializedObject.Update(); // Add this line
                        EditorGUILayout.PropertyField(property, true);
                        serializedObject.ApplyModifiedProperties(); // Add this line
                    }
                }
            }
            else
            {
                serializedObject.Update(); // Add this line
                base.OnInspectorGUI();
                serializedObject.ApplyModifiedProperties(); // Add this line
            }
        }

        private T[] GetPropertyAttributes<T>(SerializedProperty property) where T : System.Attribute
        {
            FieldInfo fieldInfo = serializedObject.targetObject.GetType().GetField(property.propertyPath,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                return (T[]) fieldInfo.GetCustomAttributes(typeof(T), true);
            }

            return new T[0];
        }
    }
}
