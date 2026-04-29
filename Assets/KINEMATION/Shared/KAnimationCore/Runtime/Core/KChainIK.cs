// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using UnityEngine;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    public struct ChainIKData
    {
        public Vector3[] positions;
        public float[] lengths;
        
        public Vector3 target;
        public float tolerance;
        public float maxReach;
        public int maxIterations;
    }
    
    public class KChainIK
    {
        public static bool SolveFABRIK(ref ChainIKData ikData)
        {
            // If the target is unreachable
            var rootToTargetDir = ikData.target - ikData.positions[0];
            if (rootToTargetDir.sqrMagnitude > KMath.Square(ikData.maxReach))
            {
                // Line up chain towards target
                var dir = rootToTargetDir.normalized;
                for (int i = 1; i < ikData.positions.Length; ++i)
                {
                    ikData.positions[i] = ikData.positions[i - 1] + dir * ikData.lengths[i - 1];
                }

                return true;
            }

            int tipIndex = ikData.positions.Length - 1;
            float sqrTolerance = KMath.Square(ikData.tolerance);
            
            if (KMath.SqrDistance(ikData.positions[tipIndex], ikData.target) > sqrTolerance)
            {
                var rootPos = ikData.positions[0];
                int iteration = 0;

                do
                {
                    // Forward reaching phase
                    // Set tip to target and propagate displacement to rest of chain
                    ikData.positions[tipIndex] = ikData.target;
                    for (int i = tipIndex - 1; i > -1; --i)
                    {
                        ikData.positions[i] = ikData.positions[i + 1] +
                                                   ((ikData.positions[i] - ikData.positions[i + 1]).normalized *
                                                    ikData.lengths[i]);
                    }
                    
                    // Backward reaching phase
                    // Set root back at it's original position and propagate displacement to rest of chain
                    ikData.positions[0] = rootPos;
                    for (int i = 1; i < ikData.positions.Length; ++i)
                        ikData.positions[i] = ikData.positions[i - 1] +
                                                   ((ikData.positions[i] - ikData.positions[i - 1]).normalized * ikData.lengths[i - 1]);
                } while ((KMath.SqrDistance(ikData.positions[tipIndex], ikData.target) > sqrTolerance) &&
                         (++iteration < ikData.maxIterations));

                return true;
            }

            return false;
        }
    }
}