// Designed by KINEMATION, 2024.

using KINEMATION.Shared.KAnimationCore.Editor.Tools;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;

using UnityEditor;
using UnityEngine;

namespace KINEMATION.FPSAnimationFramework.Editor.Tools
{
    public class ExtractCurvesTool : IEditorTool
    {
        private AnimationClip _animation;
        private string _boneName = "ik_hand_gun";
        private bool _makeAdditive;
        private bool _normalizeCurves;

        private AnimationCurve _translationX = AnimationCurve.Constant(0f, 1f, 0f);
        private AnimationCurve _translationY = AnimationCurve.Constant(0f, 1f, 0f);
        private AnimationCurve _translationZ = AnimationCurve.Constant(0f, 1f, 0f);
        
        private AnimationCurve _rotationX = AnimationCurve.Constant(0f, 1f, 0f);
        private AnimationCurve _rotationY = AnimationCurve.Constant(0f, 1f, 0f);
        private AnimationCurve _rotationZ = AnimationCurve.Constant(0f, 1f, 0f);

        private void AddNormalizedCurveValue(AnimationCurve source, AnimationCurve target, float time, float maxValue)
        {
            if (Mathf.Approximately(maxValue, 0f))
            {
                target.AddKey(time, source.Evaluate(time));
                return;
            }

            target.AddKey(time, source.Evaluate(time) / maxValue);
        }

        private void ExtractCurves()
        {
            var bindings = AnimationUtility.GetCurveBindings(_animation);

            EditorCurveBinding[] tBindings = new EditorCurveBinding[3];
            EditorCurveBinding[] rBindings = new EditorCurveBinding[4];

            foreach (var binding in bindings)
            {
                if (!binding.path.EndsWith(_boneName)) continue;

                if (binding.propertyName.ToLower().Contains("m_localposition.x"))
                {
                    tBindings[0] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localposition.y"))
                {
                    tBindings[1] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localposition.z"))
                {
                    tBindings[2] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.x"))
                {
                    rBindings[0] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.y"))
                {
                    rBindings[1] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.z"))
                {
                    rBindings[2] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.w"))
                {
                    rBindings[3] = binding;
                }
            }

            AnimationCurve[] tCurves = new AnimationCurve[]
            {
                new AnimationCurve(),
                new AnimationCurve(),
                new AnimationCurve()
            };
            
            AnimationCurve[] rCurves = new AnimationCurve[]
            {
                new AnimationCurve(),
                new AnimationCurve(),
                new AnimationCurve(),
            };
            
            float playBack = 0f;

            Vector3 tRef = CurveEditorUtility.GetVectorValue(_animation, tBindings, 0f);
            Quaternion rRef = CurveEditorUtility.GetQuatValue(_animation, rBindings, 0f);
            rRef = Quaternion.Inverse(rRef);

            Vector3 tMax = Vector3.zero;
            Vector3 rMax = Vector3.zero;
            
            while (playBack <= _animation.length)
            {
                Vector3 tValue = CurveEditorUtility.GetVectorValue(_animation, tBindings, playBack);
                Quaternion rQuatValue = CurveEditorUtility.GetQuatValue(_animation, rBindings, playBack);

                if (_makeAdditive)
                {
                    tValue -= tRef;
                    rQuatValue = rRef * rQuatValue;
                }
                
                Vector3 rValue = rQuatValue.eulerAngles;

                rValue.x = KMath.NormalizeEulerAngle(rValue.x);
                rValue.y = KMath.NormalizeEulerAngle(rValue.y);
                rValue.z = KMath.NormalizeEulerAngle(rValue.z);

                tCurves[0].AddKey(playBack, tValue.x);
                tCurves[1].AddKey(playBack, tValue.y);
                tCurves[2].AddKey(playBack, tValue.z);

                rCurves[0].AddKey(playBack, rValue.x);
                rCurves[1].AddKey(playBack, rValue.y);
                rCurves[2].AddKey(playBack, rValue.z);

                if (_normalizeCurves)
                {
                    if (Mathf.Abs(tValue.x) > Mathf.Abs(tMax.x))
                    {
                        tMax.x = tValue.x;
                    }

                    if (Mathf.Abs(tValue.y) > Mathf.Abs(tMax.y))
                    {
                        tMax.y = tValue.y;
                    }

                    if (Mathf.Abs(tValue.z) > Mathf.Abs(tMax.z))
                    {
                        tMax.z = tValue.z;
                    }

                    if (Mathf.Abs(rValue.x) > Mathf.Abs(rMax.x))
                    {
                        rMax.x = rValue.x;
                    }

                    if (Mathf.Abs(rValue.y) > Mathf.Abs(rMax.y))
                    {
                        rMax.y = rValue.y;
                    }

                    if (Mathf.Abs(rValue.z) > Mathf.Abs(rMax.z))
                    {
                        rMax.z = rValue.z;
                    }
                }

                playBack += 1f / 60f;
            }

            if (!_normalizeCurves)
            {
                _translationX = tCurves[0];
                _translationY = tCurves[1];
                _translationZ = tCurves[2];

                _rotationX = rCurves[0];
                _rotationY = rCurves[1];
                _rotationZ = rCurves[2];
                return;
            }
            
            playBack = 0f;

            _translationX = new AnimationCurve();
            _translationY = new AnimationCurve();
            _translationZ = new AnimationCurve();
            
            _rotationX = new AnimationCurve();
            _rotationY = new AnimationCurve();
            _rotationZ = new AnimationCurve();
            
            while (playBack <= _animation.length)
            {
                AddNormalizedCurveValue(tCurves[0], _translationX,
                    playBack, tMax.x);
                
                AddNormalizedCurveValue(tCurves[1], _translationY,
                    playBack, tMax.y);
                
                AddNormalizedCurveValue(tCurves[2], _translationZ,
                    playBack, tMax.z);
                
                AddNormalizedCurveValue(rCurves[0], _rotationX,
                    playBack, rMax.x);
                
                AddNormalizedCurveValue(rCurves[1], _rotationY,
                    playBack, rMax.y);
                
                AddNormalizedCurveValue(rCurves[2], _rotationZ,
                    playBack, rMax.z);
                
                playBack += 1f / 60f;
            }
        }

        private void CopyCurveToBuffer(AnimationCurve curve)
        {
            string output =
                "UnityEditor.AnimationCurveWrapperJSON:{\"curve\":{\"serializedVerson\":\"2\",\"m_Curve\":[";

            foreach (var key in curve.keys)
            {
                output += "{\"serializedVersion\":\"3\"";
                output += $",\"time\":{key.time.ToString().Replace(",", ".")}";
                output += $",\"value\":{key.value.ToString().Replace(",", ".")}";
                output += $",\"inSlope\":{key.inTangent.ToString().Replace(",", ".")}";
                output += $",\"outSlope\":{key.outTangent.ToString().Replace(",", ".")}";
                output += $",\"tangentMode\":{key.tangentMode}";
                output += $",\"weightedMode\":0";
                output += $",\"inWeight\":{key.inWeight.ToString().Replace(",", ".")}";
                output += $",\"outWeight\":{key.outWeight.ToString().Replace(",", ".")}";
                output += "},";
            }
            
            output = output.Remove(output.Length - 1);
            output += "],\"m_PreInfinity\":2,\"m_PostInfinity\":2,\"m_RotationOrder\":4}}";

            EditorGUIUtility.systemCopyBuffer = output;
        }

        private void RenderCurveProperty(string curveName, AnimationCurve curve)
        {
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("\u21bb"))
            {
                CopyCurveToBuffer(curve);
            }
            
            EditorGUILayout.CurveField(curveName, curve);
            EditorGUILayout.EndHorizontal();
        }

        public void Init()
        {
        }

        public void Render()
        {
            _animation = (AnimationClip) EditorGUILayout.ObjectField("Clip", _animation,
                typeof(AnimationClip), false);

            _makeAdditive = EditorGUILayout.Toggle("Make Additive", _makeAdditive);
            _normalizeCurves = EditorGUILayout.Toggle("Normalize Curves", _normalizeCurves);
            
            if (_animation == null)
            {
                EditorGUILayout.HelpBox("Selected Fire Clip", MessageType.Warning);
                return;
            }

            _boneName = EditorGUILayout.TextField("Bone Name", _boneName);
            if (GUILayout.Button("Extract Curves")) ExtractCurves();
            
            RenderCurveProperty("Translation X", _translationX);
            RenderCurveProperty("Translation Y", _translationY);
            RenderCurveProperty("Translation Z", _translationZ);
            
            RenderCurveProperty("Rotation X", _rotationX);
            RenderCurveProperty("Rotation Y", _rotationY);
            RenderCurveProperty("Rotation Z", _rotationZ);
        }

        public string GetToolCategory()
        {
            return "Animation/FPS Animation Framework/";
        }

        public string GetToolName()
        {
            return "Curve Extractor";
        }

        public string GetDocsURL()
        {
            return string.Empty;
        }

        public string GetToolDescription()
        {
            return "Extracts translation and rotation curves from a specified bone.";
        }
    }
}
