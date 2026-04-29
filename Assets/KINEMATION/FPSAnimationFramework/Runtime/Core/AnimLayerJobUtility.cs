// Designed by KINEMATION, 2024.

using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.Shared.KAnimationCore.Runtime.Core;
using KINEMATION.Shared.KAnimationCore.Runtime.Input;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;
using UnityEngine.Animations;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace KINEMATION.FPSAnimationFramework.Runtime.Core
{
    public struct LayerJobData
    {
        public Transform Owner => animator.transform;
        public Animator animator;
        public TransformSceneHandle rootHandle;
        public KRigComponent rigComponent;
        public UserInputController inputController;
        public IPlayablesController playablesController;
        public float weight;
    }

    public struct TransformStreamPose
    {
        public TransformStreamHandle handle;
        public KTransform pose;
    }

    public class AnimLayerJobUtility
    {
        public static void PassThrough(AnimationStream stream, TransformStreamHandle handle)
        {
            handle.GetLocalTRS(stream, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            handle.SetLocalTRS(stream, position, rotation, scale, false);
        }
        
        public static KTransform GetTransformFromHandle(AnimationStream stream, TransformStreamHandle handle,
            bool isWorld = true)
        {
            KTransform output = new KTransform()
            {
                position = isWorld ? handle.GetPosition(stream) : handle.GetLocalPosition(stream),
                rotation = isWorld ? handle.GetRotation(stream) : handle.GetLocalRotation(stream),
            };

            return output;
        }
        
        public static KTransform GetTransformFromHandle(AnimationStream stream, TransformSceneHandle handle,
            bool isWorld = true)
        {
            KTransform output = new KTransform()
            {
                position = isWorld ? handle.GetPosition(stream) : handle.GetLocalPosition(stream),
                rotation = isWorld ? handle.GetRotation(stream) : handle.GetLocalRotation(stream),
            };

            return output;
        }
        
        public static void MoveInSpace(AnimationStream stream, TransformSceneHandle space, 
            TransformStreamHandle target, Vector3 offset, float weight)
        {
            KTransform spaceT = GetTransformFromHandle(stream, space);
            KTransform targetT = GetTransformFromHandle(stream, target);

            var result = KAnimationMath.MoveInSpace(spaceT, targetT, offset, weight);
            target.SetPosition(stream, result);
        }
        
        public static void MoveInSpace(AnimationStream stream, TransformStreamHandle space, 
            TransformStreamHandle target, Vector3 offset, float weight)
        {
            KTransform spaceT = GetTransformFromHandle(stream, space);
            KTransform targetT = GetTransformFromHandle(stream, target);

            var result = KAnimationMath.MoveInSpace(spaceT, targetT, offset, weight);
            target.SetPosition(stream, result);
        }

        public static void RotateInSpace(AnimationStream stream, TransformStreamHandle space,
            TransformStreamHandle target, Quaternion offset, float weight)
        {
            KTransform spaceT = GetTransformFromHandle(stream, space);
            KTransform targetT = GetTransformFromHandle(stream, target);

            var result = KAnimationMath.RotateInSpace(spaceT, targetT, offset, weight);
            target.SetRotation(stream, result);
        }
        
        public static void RotateInSpace(AnimationStream stream, TransformSceneHandle space,
            TransformStreamHandle target, Quaternion offset, float weight)
        {
            KTransform spaceT = GetTransformFromHandle(stream, space);
            KTransform targetT = GetTransformFromHandle(stream, target);

            var result = KAnimationMath.RotateInSpace(spaceT, targetT, offset, weight);
            target.SetRotation(stream, result);
        }
        
        public static void ModifyTransform(AnimationStream stream, TransformSceneHandle root,
            TransformStreamHandle target, KPose pose, float weight)
        {
            KTransform rootTransform = GetTransformFromHandle(stream, root);
            
            if (pose.modifyMode == EModifyMode.Add)
            {
                if (pose.space == ESpaceType.BoneSpace)
                {
                    MoveInSpace(stream, target, target, pose.pose.position, weight);
                    RotateInSpace(stream, target, target, pose.pose.rotation, weight);
                    return;
                }

                if (pose.space == ESpaceType.ParentBoneSpace)
                {
                    var local = GetTransformFromHandle(stream, target, false);
                    
                    target.SetLocalPosition(stream, Vector3.Lerp(local.position, 
                        local.position + pose.pose.position, weight));
                    target.SetLocalRotation(stream, Quaternion.Slerp(local.rotation, 
                        local.rotation * pose.pose.rotation, weight));
                    return;
                }

                if (pose.space == ESpaceType.ComponentSpace)
                {
                    MoveInSpace(stream, root, target, pose.pose.position, weight);
                    RotateInSpace(stream, root, target, pose.pose.rotation, weight);
                    return;
                }

                KTransform world = GetTransformFromHandle(stream, target);
                
                target.SetPosition(stream, 
                    Vector3.Lerp(world.position, world.position + pose.pose.position, weight));
                target.SetRotation(stream, 
                    Quaternion.Slerp(world.rotation, world.rotation * pose.pose.rotation, weight));
                return;
            }

            if (pose.space is ESpaceType.BoneSpace or ESpaceType.ParentBoneSpace)
            {
                target.SetLocalPosition(stream, 
                    Vector3.Lerp(target.GetLocalPosition(stream), pose.pose.position, weight));
                target.SetLocalRotation(stream,
                    Quaternion.Slerp(target.GetLocalRotation(stream), pose.pose.rotation, weight));
                return;
            }

            if (pose.space == ESpaceType.ComponentSpace)
            {
                var worldTransform = rootTransform.GetWorldTransform(pose.pose, false);
                target.SetPosition(stream, worldTransform.position);
                target.SetRotation(stream, worldTransform.rotation);
                return;
            }
            
            target.SetPosition(stream, 
                Vector3.Lerp(target.GetPosition(stream), pose.pose.position, weight));
            target.SetRotation(stream, Quaternion.Slerp(target.GetRotation(stream), pose.pose.rotation, 
                weight));
        }

        public static void ModifyTransform(AnimationStream stream, TransformStreamHandle root,
            TransformStreamHandle target, KPose pose, float weight)
        {
            KTransform rootTransform = GetTransformFromHandle(stream, root);
            
            if (pose.modifyMode == EModifyMode.Add)
            {
                if (pose.space == ESpaceType.BoneSpace)
                {
                    MoveInSpace(stream, target, target, pose.pose.position, weight);
                    RotateInSpace(stream, target, target, pose.pose.rotation, weight);
                    return;
                }

                if (pose.space == ESpaceType.ParentBoneSpace)
                {
                    var local = GetTransformFromHandle(stream, target, false);
                    
                    target.SetLocalPosition(stream, Vector3.Lerp(local.position, 
                        local.position + pose.pose.position, weight));
                    target.SetLocalRotation(stream, Quaternion.Slerp(local.rotation, 
                        local.rotation * pose.pose.rotation, weight));
                    return;
                }

                if (pose.space == ESpaceType.ComponentSpace)
                {
                    MoveInSpace(stream, root, target, pose.pose.position, weight);
                    RotateInSpace(stream, root, target, pose.pose.rotation, weight);
                    return;
                }

                KTransform world = GetTransformFromHandle(stream, target);
                
                target.SetPosition(stream, 
                    Vector3.Lerp(world.position, world.position + pose.pose.position, weight));
                target.SetRotation(stream, 
                    Quaternion.Slerp(world.rotation, world.rotation * pose.pose.rotation, weight));
                return;
            }

            if (pose.space is ESpaceType.BoneSpace or ESpaceType.ParentBoneSpace)
            {
                target.SetLocalPosition(stream, 
                    Vector3.Lerp(target.GetLocalPosition(stream), pose.pose.position, weight));
                target.SetLocalRotation(stream,
                    Quaternion.Slerp(target.GetLocalRotation(stream), pose.pose.rotation, weight));
                return;
            }

            if (pose.space == ESpaceType.ComponentSpace)
            {
                var worldTransform = rootTransform.GetWorldTransform(pose.pose, false);
                target.SetPosition(stream, worldTransform.position);
                target.SetRotation(stream, worldTransform.rotation);
                return;
            }
            
            target.SetPosition(stream, 
                Vector3.Lerp(target.GetPosition(stream), pose.pose.position, weight));
            target.SetRotation(stream, Quaternion.Slerp(target.GetRotation(stream), pose.pose.rotation, 
                weight));
        }

        // Copies a bone pose in world space.
        public static void CopyBone(AnimationStream stream, TransformStreamHandle from, TransformStreamHandle to)
        {
            to.SetPosition(stream, from.GetPosition(stream));
            to.SetRotation(stream, from.GetRotation(stream));
        }
        
        // Copies a bone pose in world space.
        public static void CopyBone(AnimationStream stream, TransformSceneHandle from, TransformStreamHandle to)
        {
            to.SetPosition(stream, from.GetPosition(stream));
            to.SetRotation(stream, from.GetRotation(stream));
        }
    }
}