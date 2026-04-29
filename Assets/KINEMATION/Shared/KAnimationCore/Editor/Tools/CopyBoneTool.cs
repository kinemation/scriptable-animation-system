// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEditor;
using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Editor.Tools
{
    public class CopyBoneTool : IEditorTool
    {
        private Transform _root;
        private Transform _extractFrom;
        private Transform _extractTo;

        private AnimationClip _clip;
        private AnimationClip _refClip;

        private Vector3 _rotationOffset;
        private bool _isAdditive;

        private struct TransformData
        {
            public Vector3 localPosition;
            public Quaternion localRotation;

            public TransformData(Transform t)
            {
                localPosition = t.localPosition;
                localRotation = t.localRotation;
            }

            public void Restore(Transform t)
            {
                t.localPosition = localPosition;
                t.localRotation = localRotation;
            }
        }

        private string GetBonePath(Transform targetBone, Transform root)
        {
            if (targetBone == null || root == null) return "";

            string path = targetBone.name;
            Transform current = targetBone.parent;

            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return (current == root) ? path : null;
        }

        private void ExtractAndSetAnimationData()
        {
            // 0. Cache all bone transforms recursively
            // GetComponentsInChildren(true) recursively fetches all transforms including inactive ones
            Transform[] allBones = _root.GetComponentsInChildren<Transform>(true);
            TransformData[] cache = new TransformData[allBones.Length];

            for (int i = 0; i < allBones.Length; i++)
            {
                cache[i] = new TransformData(allBones[i]);
            }

            AnimationCurve tX = new AnimationCurve();
            AnimationCurve tY = new AnimationCurve();
            AnimationCurve tZ = new AnimationCurve();

            AnimationCurve rX = new AnimationCurve();
            AnimationCurve rY = new AnimationCurve();
            AnimationCurve rZ = new AnimationCurve();
            AnimationCurve rW = new AnimationCurve();

            try
            {
                // 1. Sample Reference (if additive)
                Vector3 refTranslation = Vector3.zero;
                Quaternion refRotation = Quaternion.identity;

                if (_isAdditive && _refClip != null)
                {
                    _refClip.SampleAnimation(_root.gameObject, 0f);
                    refTranslation = _root.InverseTransformPoint(_extractFrom.position);
                    refRotation = Quaternion.Inverse(_root.rotation) * _extractFrom.rotation *
                                  Quaternion.Euler(_rotationOffset);
                }

                // 2. Iterate frames and sample
                float playLength = _clip.length;
                float frameRate = _clip.frameRate > 0 ? 1f / _clip.frameRate : 1f / 30f;
                float playBack = 0f;

                while (playBack <= playLength)
                {
                    _clip.SampleAnimation(_root.gameObject, playBack);

                    Vector3 position = _extractFrom.position;
                    Quaternion rotation = _extractFrom.rotation * Quaternion.Euler(_rotationOffset);

                    if (_isAdditive)
                    {
                        position = _root.InverseTransformPoint(position);
                        rotation = Quaternion.Inverse(_root.rotation) * rotation;
                        
                        position -= refTranslation;
                        rotation = Quaternion.Inverse(refRotation) * rotation;
                        
                        position = _root.TransformPoint(position);
                        rotation = _root.rotation * rotation;
                    }

                    position = _extractTo.parent.InverseTransformPoint(position);
                    rotation = Quaternion.Inverse(_extractTo.parent.rotation) * rotation;

                    // 4. Bake into curves
                    tX.AddKey(playBack, position.x);
                    tY.AddKey(playBack, position.y);
                    tZ.AddKey(playBack, position.z);

                    rX.AddKey(playBack, rotation.x);
                    rY.AddKey(playBack, rotation.y);
                    rZ.AddKey(playBack, rotation.z);
                    rW.AddKey(playBack, rotation.w);

                    playBack += frameRate;
                }

                // 5. Save Clip
                string path = GetBonePath(_extractTo, _root);
                if (!string.IsNullOrEmpty(path))
                {
                    _clip.SetCurve(path, typeof(Transform), "localPosition.x", tX);
                    _clip.SetCurve(path, typeof(Transform), "localPosition.y", tY);
                    _clip.SetCurve(path, typeof(Transform), "localPosition.z", tZ);

                    _clip.SetCurve(path, typeof(Transform), "localRotation.x", rX);
                    _clip.SetCurve(path, typeof(Transform), "localRotation.y", rY);
                    _clip.SetCurve(path, typeof(Transform), "localRotation.z", rZ);
                    _clip.SetCurve(path, typeof(Transform), "localRotation.w", rW);
                }
            }
            finally
            {
                // 6. Restore initial pose
                for (int i = 0; i < allBones.Length; i++)
                {
                    if (allBones[i] != null) cache[i].Restore(allBones[i]);
                }
            }
        }

        public void Init()
        {
        }

        public void Render()
        {
            if (!EditorGUIUtility.wideMode) EditorGUIUtility.wideMode = true;

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            GUIContent content = new GUIContent("Target Animation", "Animation to modify.");
            _clip = EditorGUILayout.ObjectField(content, _clip, typeof(AnimationClip),
                true) as AnimationClip;
            
            content = new GUIContent("Character Model", "Model Game Object.");
            _root = EditorGUILayout.ObjectField(content, _root, typeof(Transform), true) as Transform;
            
            content = new GUIContent("Copy From", "Bone to copy pose from.");
            _extractFrom = EditorGUILayout.ObjectField(content, _extractFrom, typeof(Transform), true) as Transform;
            
            content = new GUIContent("Copy To", "Bone to copy pose to.");
            _extractTo = EditorGUILayout.ObjectField(content, _extractTo, typeof(Transform), true) as Transform;
            _rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", _rotationOffset);
            
            EditorGUILayout.Space();
            
            content = new GUIContent("Is Additive", "If true, pose will be copied relative to the reference animation.");
            _isAdditive = EditorGUILayout.Toggle(content, _isAdditive);

            GUI.enabled = _isAdditive;
            _refClip = EditorGUILayout.ObjectField("Reference Animation", _refClip, typeof(AnimationClip), true) as
                AnimationClip;
            GUI.enabled = true;

            bool valid = _clip != null && _root != null && _extractFrom != null && _extractTo != null;
            if (_isAdditive && _refClip == null) valid = false;

            if (!valid)
            {
                EditorGUILayout.HelpBox("Assign all references!", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Apply"))
            {
                ExtractAndSetAnimationData();
            }
        }

        public string GetToolCategory() => "Animation";
        public string GetToolName() => "Copy Bone";
        public string GetDocsURL() => string.Empty;
        public string GetToolDescription() => "Samples and bakes animation from one bone to another.";
    }
}