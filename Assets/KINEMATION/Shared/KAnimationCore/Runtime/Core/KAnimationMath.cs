// Copyright (c) 2026 KINEMATION.
// All rights reserved.

using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEngine;
using UnityEngine.Animations;

namespace KINEMATION.Shared.KAnimationCore.Runtime.Core
{
    public class KAnimationMath
    {
        public static Quaternion RotateInSpace(Quaternion space, Quaternion target, Quaternion rotation, float alpha)
        {
            return Quaternion.Slerp(target, space * rotation * (Quaternion.Inverse(space) * target), alpha);
        }

        public static Quaternion RotateInSpace(KTransform space, KTransform target, Quaternion offset, float alpha)
        {
            return RotateInSpace(space.rotation, target.rotation, offset, alpha);
        }

        public static void RotateInSpace(Transform space, Transform target, Quaternion offset, float alpha)
        {
            target.rotation = RotateInSpace(space.rotation, target.rotation, offset, alpha);
        }

        public static Vector3 MoveInSpace(KTransform space, KTransform target, Vector3 offset, float alpha)
        {
            return target.position + (space.TransformPoint(offset, false) - space.position) * alpha;
        }

        public static void MoveInSpace(Transform space, Transform target, Vector3 offset, float alpha)
        {
            target.position += (space.TransformPoint(offset) - space.position) * alpha;
        }

        public static Vector3 MoveInSpace(Transform space, Vector3 target, Vector3 offset, float alpha)
        {
            return target + (space.TransformPoint(offset) - space.position) * alpha;
        }

        public static KTransform GetTransform(AnimationStream stream, TransformStreamHandle handle,
            bool isWorld = true)
        {
            if (!stream.isValid || !handle.IsValid(stream))
            {
                return KTransform.Identity;
            }

            KTransform output = new KTransform()
            {
                position = isWorld ? handle.GetPosition(stream) : handle.GetLocalPosition(stream),
                rotation = isWorld ? handle.GetRotation(stream) : handle.GetLocalRotation(stream),
            };

            return output;
        }

        public static KTransform GetTransform(AnimationStream stream, TransformSceneHandle handle,
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
            KTransform spaceT = GetTransform(stream, space);
            KTransform targetT = GetTransform(stream, target);

            var result = MoveInSpace(spaceT, targetT, offset, weight);
            target.SetPosition(stream, result);
        }

        public static void MoveInSpace(AnimationStream stream, TransformStreamHandle space,
            TransformStreamHandle target, Vector3 offset, float weight)
        {
            KTransform spaceT = GetTransform(stream, space);
            KTransform targetT = GetTransform(stream, target);

            var result = MoveInSpace(spaceT, targetT, offset, weight);
            target.SetPosition(stream, result);
        }

        public static void RotateInSpace(AnimationStream stream, TransformStreamHandle space,
            TransformStreamHandle target, Quaternion offset, float weight)
        {
            KTransform spaceT = GetTransform(stream, space);
            KTransform targetT = GetTransform(stream, target);

            var result = RotateInSpace(spaceT, targetT, offset, weight);
            target.SetRotation(stream, result);
        }

        public static void RotateInSpace(AnimationStream stream, TransformSceneHandle space,
            TransformStreamHandle target, Quaternion offset, float weight)
        {
            KTransform spaceT = GetTransform(stream, space);
            KTransform targetT = GetTransform(stream, target);

            var result = RotateInSpace(spaceT, targetT, offset, weight);
            target.SetRotation(stream, result);
        }

        public static void ModifyPosition(AnimationStream stream, TransformSceneHandle root, TransformStreamHandle bone,
            Vector3 position, ESpaceType space, EModifyMode mode, float weight)
        {
            if (mode == EModifyMode.Ignore) return;

            KTransform rootTransform = GetTransform(stream, root);

            if (mode == EModifyMode.Add)
            {
                if (space == ESpaceType.BoneSpace)
                {
                    MoveInSpace(stream, bone, bone, position, weight);
                    return;
                }

                if (space == ESpaceType.ParentBoneSpace)
                {
                    var local = GetTransform(stream, bone, false);

                    bone.SetLocalPosition(stream, Vector3.Lerp(local.position,
                        local.position + position, weight));
                    return;
                }

                if (space == ESpaceType.ComponentSpace)
                {
                    MoveInSpace(stream, root, bone, position, weight);
                    return;
                }

                KTransform world = GetTransform(stream, bone);

                bone.SetPosition(stream,
                    Vector3.Lerp(world.position, world.position + position, weight));
                return;
            }

            if (space is ESpaceType.BoneSpace or ESpaceType.ParentBoneSpace)
            {
                bone.SetLocalPosition(stream,
                    Vector3.Lerp(bone.GetLocalPosition(stream), position, weight));
                return;
            }

            if (space == ESpaceType.ComponentSpace)
            {
                position = rootTransform.TransformPoint(position, false);
                bone.SetPosition(stream, Vector3.Lerp(bone.GetPosition(stream), position, weight));
                return;
            }

            bone.SetPosition(stream, Vector3.Lerp(bone.GetPosition(stream), position, weight));
        }

        public static void ModifyRotation(AnimationStream stream, TransformSceneHandle root, TransformStreamHandle bone,
            Quaternion rotation, ESpaceType space, EModifyMode mode, float weight)
        {
            if (mode == EModifyMode.Ignore) return;

            KTransform rootTransform = GetTransform(stream, root);

            if (mode == EModifyMode.Add)
            {
                if (space == ESpaceType.BoneSpace)
                {
                    RotateInSpace(stream, bone, bone, rotation, weight);
                    return;
                }

                if (space == ESpaceType.ParentBoneSpace)
                {
                    var local = GetTransform(stream, bone, false);

                    bone.SetLocalRotation(stream, Quaternion.Slerp(local.rotation,
                        local.rotation * rotation, weight));
                    return;
                }

                if (space == ESpaceType.ComponentSpace)
                {
                    RotateInSpace(stream, root, bone, rotation, weight);
                    return;
                }

                KTransform world = GetTransform(stream, bone);

                bone.SetRotation(stream,
                    Quaternion.Slerp(world.rotation, world.rotation * rotation, weight));
                return;
            }

            if (space is ESpaceType.BoneSpace or ESpaceType.ParentBoneSpace)
            {
                bone.SetLocalRotation(stream,
                    Quaternion.Slerp(bone.GetLocalRotation(stream), rotation, weight));
                return;
            }

            if (space == ESpaceType.ComponentSpace)
            {
                rotation = rootTransform.rotation * rotation;
                bone.SetRotation(stream, Quaternion.Slerp(bone.GetRotation(stream), rotation, weight));
                return;
            }

            bone.SetRotation(stream, Quaternion.Slerp(bone.GetRotation(stream), rotation,
                weight));
        }

        public static void ModifyTransform(AnimationStream stream, TransformSceneHandle root,
            TransformStreamHandle target, KPose pose, float weight)
        {
            KTransform rootTransform = GetTransform(stream, root);

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
                    var local = GetTransform(stream, target, false);

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

                KTransform world = GetTransform(stream, target);

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
                worldTransform = KTransform.Lerp(GetTransform(stream, target), worldTransform, weight);

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
            KTransform rootTransform = GetTransform(stream, root);

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
                    var local = GetTransform(stream, target, false);

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

                KTransform world = GetTransform(stream, target);

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
        public static void CopyBone(AnimationStream stream, TransformStreamHandle from, TransformStreamHandle to, 
            float weight = 1f)
        {
            to.SetPosition(stream, Vector3.Lerp(to.GetPosition(stream), from.GetPosition(stream), weight));
            to.SetRotation(stream, Quaternion.Slerp(to.GetRotation(stream), from.GetRotation(stream), weight));
        }

        // Copies a bone pose in world space.
        public static void CopyBone(AnimationStream stream, TransformSceneHandle from, TransformStreamHandle to, 
            float weight = 1f)
        {
            to.SetPosition(stream, Vector3.Lerp(to.GetPosition(stream), from.GetPosition(stream), weight));
            to.SetRotation(stream, Quaternion.Slerp(to.GetRotation(stream), from.GetRotation(stream), weight));
        }

        public static bool IsWeightFull(float weight)
        {
            return Mathf.Approximately(weight, 1f);
        }

        public static bool IsWeightRelevant(float weight)
        {
            return !Mathf.Approximately(weight, 0f);
        }

        public static void ModifyTransform(Transform component, Transform target, in KPose pose, float alpha = 1f)
        {
            if (pose.modifyMode == EModifyMode.Add)
            {
                AddTransform(component, target, in pose, alpha);
                return;
            }

            ReplaceTransform(component, target, in pose, alpha);
        }

        private static void AddTransform(Transform component, Transform target, in KPose pose, float alpha = 1f)
        {
            if (pose.space == ESpaceType.BoneSpace)
            {
                MoveInSpace(target, target, pose.pose.position, alpha);
                RotateInSpace(target, target, pose.pose.rotation, alpha);
                return;
            }

            if (pose.space == ESpaceType.ParentBoneSpace)
            {
                Transform parent = target.parent;

                MoveInSpace(parent, target, pose.pose.position, alpha);
                RotateInSpace(parent, target, pose.pose.rotation, alpha);
                return;
            }

            if (pose.space == ESpaceType.ComponentSpace)
            {
                MoveInSpace(component, target, pose.pose.position, alpha);
                RotateInSpace(component, target, pose.pose.rotation, alpha);
                return;
            }

            Vector3 position = target.position;
            Quaternion rotation = target.rotation;

            target.position = Vector3.Lerp(position, position + pose.pose.position, alpha);
            target.rotation = Quaternion.Slerp(rotation, rotation * pose.pose.rotation, alpha);
        }

        private static void ReplaceTransform(Transform component, Transform target, in KPose pose, float alpha = 1f)
        {
            if (pose.space == ESpaceType.BoneSpace || pose.space == ESpaceType.ParentBoneSpace)
            {
                target.localPosition = Vector3.Lerp(target.localPosition, pose.pose.position, alpha);
                target.localRotation = Quaternion.Slerp(target.localRotation, pose.pose.rotation, alpha);
                return;
            }

            if (pose.space == ESpaceType.ComponentSpace)
            {
                target.position = Vector3.Lerp(target.position, component.TransformPoint(pose.pose.position), alpha);
                target.rotation = Quaternion.Slerp(target.rotation, component.rotation * pose.pose.rotation, alpha);
                return;
            }

            target.position = Vector3.Lerp(target.position, pose.pose.position, alpha);
            target.rotation = Quaternion.Slerp(target.rotation, pose.pose.rotation, alpha);
        }
    }
}