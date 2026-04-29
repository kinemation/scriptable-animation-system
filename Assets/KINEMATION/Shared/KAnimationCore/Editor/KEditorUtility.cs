// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KINEMATION.Shared.KAnimationCore.Editor
{
    public class KEditorUtility
    {
        public const string EditorToolsPath = "Tools/KINEMATION";
        
        public static GUIStyle boldLabel = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            richText = true,
            wordWrap = false
        };
        
        public static string GetProjectActiveFolder()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            
            MethodInfo getActiveFolderPathMethod = projectWindowUtilType.GetMethod("GetActiveFolderPath", 
                BindingFlags.Static | BindingFlags.NonPublic);
            
            if (getActiveFolderPathMethod != null)
            {
                object result = getActiveFolderPathMethod.Invoke(null, null);
                if (result != null)
                {
                    return result.ToString();
                }
            }

            return "No folder is currently opened.";
        }
        
        public static void SaveAsset(Object asset, string directory, string nameWithExtension)
        {
            string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, nameWithExtension));
            
            AssetDatabase.CreateAsset(asset, filePath);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
        
        public static bool IsSubAsset(Object asset)
        {
            if (asset == null) return false;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return false;

            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            return asset != mainAsset;
        }
        
        public static AnimationClip GetAnimationClipFromSelection()
        {
            Object selected = Selection.activeObject;
            if (selected == null) return null;

            AnimationClip clip = selected as AnimationClip;
            string path = AssetDatabase.GetAssetPath(selected);

            // Try to find a clip in an FBX file.
            if (clip == null && Path.GetExtension(path).ToLower() == ".fbx")
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object asset in assets)
                {
                    clip = asset as AnimationClip;
                    if (clip != null && (clip.hideFlags & HideFlags.HideInHierarchy) == 0) break;
                }
            }

            return clip;
        }
    }
}