// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KINEMATION.Shared.KAnimationCore.Editor.Widgets
{
    public class AssetObjectWidget<T> where T : Object
    {
        public UnityEditor.Editor editor;
        
        public string _objectLabel;

        private Object _cachedObject;
        private Object _parentAsset;
        
        private SerializedObject _targetObject;
        private SerializedProperty _targetProperty;
        
        private bool _isExpanded;

        private bool IsSubAsset(Object asset)
        {
            return AssetDatabase.GetAssetPath(_parentAsset).Equals(AssetDatabase.GetAssetPath(asset));
        }

        private void DestroyObject(Object objectToDestroy, bool displayDialog)
        {
            if (objectToDestroy != null && IsSubAsset(objectToDestroy))
            {
                if (!displayDialog || EditorUtility.DisplayDialog("Deletion",
                        $"Are you sure you want to delete {objectToDestroy.name}?", "Yes", "No"))
                {
                    Undo.DestroyObjectImmediate(objectToDestroy);
                }
            }
        }

        private void OnCreatePressed()
        {
            DestroyObject(_targetProperty.objectReferenceValue, true);

            Object newComponent = Activator.CreateInstance<T>();
            newComponent.name = _objectLabel.Replace(" ", string.Empty);
            newComponent.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            
            Undo.RegisterCreatedObjectUndo(newComponent, "Add Component");
            AssetDatabase.AddObjectToAsset(newComponent, _parentAsset);
            EditorUtility.SetDirty(_parentAsset);
            AssetDatabase.SaveAssetIfDirty(_parentAsset);

            _targetProperty.objectReferenceValue = newComponent;
        }
        
        public AssetObjectWidget(SerializedObject target, string propertyName, string label)
        {
            _objectLabel = label;
            _targetObject = target;
            _targetProperty = _targetObject.FindProperty(propertyName);
            _parentAsset = target.targetObject;
        }
        
        public void OnInspectorGUI()
        {
            _targetObject.Update();

            if (EditorUtility.IsPersistent(_parentAsset))
            {
                EditorGUILayout.BeginHorizontal();

                Object objectRef = _targetProperty.objectReferenceValue;
                objectRef = (T) EditorGUILayout.ObjectField(_objectLabel, objectRef, typeof(T), false);
                _targetProperty.objectReferenceValue = objectRef;

                if (GUILayout.Button("Create"))
                {
                    OnCreatePressed();
                }
                
                if (GUILayout.Button("Show"))
                {
                    if(editor != null) _isExpanded = !_isExpanded;
                }

                EditorGUILayout.EndHorizontal();

                if (_cachedObject != _targetProperty.objectReferenceValue)
                {
                    if (!IsSubAsset(_targetProperty.objectReferenceValue))
                    {
                        DestroyObject(_cachedObject, false);
                    }

                    editor = _targetProperty.objectReferenceValue == null
                        ? null
                        : UnityEditor.Editor.CreateEditor(_targetProperty.objectReferenceValue);
                }

                _cachedObject = _targetProperty.objectReferenceValue;

                if (_isExpanded && editor != null)
                {
                    var style = GUI.skin.box;
                    style.padding = new RectOffset(15, 5, 5, 5);

                    EditorGUILayout.BeginVertical(style);
                    editor.OnInspectorGUI();
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.PropertyField(_targetProperty);
            }
            
            _targetObject.ApplyModifiedProperties();
        }
    }
}