// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Runtime.Misc;
using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Widgets
{
    public abstract class AssetDragAndDrop<T1, T2> where T1 : MonoBehaviour where T2 : ScriptableObject
    {
        protected static DragAndDropVisualMode HandleDragAndDrop(bool perform)
        {
            var asset = DragAndDrop.objectReferences[0] as T2;
            if (asset == null)
            {
                return DragAndDropVisualMode.None;
            }
            
            if (perform)
            {
                var selection = Selection.activeGameObject;
                if (selection != null)
                {
                    var component = selection.GetComponent<T1>();
                    if (component == null) component = selection.AddComponent<T1>();
                    if(component is IAssetDragAndDrop assetDragAndDrop) assetDragAndDrop.SetAsset(asset);
                }
            }
            
            return DragAndDropVisualMode.Copy;
        }
        
        protected static DragAndDropVisualMode OnHierarchyDrop(int dropTargetInstanceID, HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects, bool perform)
        {
            return HandleDragAndDrop(perform);
        }
        
        protected static DragAndDropVisualMode OnInspectorDrop(UnityEngine.Object[] targets, bool perform)
        {
            return HandleDragAndDrop(perform);
        }
    }
}