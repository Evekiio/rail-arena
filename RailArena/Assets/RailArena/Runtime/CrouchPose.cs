using UnityEngine;

namespace RailArena
{
    /// <summary>Lower the hips and solve bent legs onto the animated feet, without scaling the body.</summary>
    internal sealed class CrouchPose
    {
        private readonly Animator animator;
        private readonly Transform hips;
        private readonly Transform[] legs;
        private readonly Quaternion[] rotations = new Quaternion[6];
        private Vector3 hipPosition;
        private bool applied;

        public CrouchPose(Animator animator)
        {
            this.animator = animator;
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            legs = new[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg), animator.GetBoneTransform(HumanBodyBones.LeftFoot),
                animator.GetBoneTransform(HumanBodyBones.RightUpperLeg), animator.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                animator.GetBoneTransform(HumanBodyBones.RightFoot) };
        }

        public void Restore()
        {
            if (!applied) return;
            hips.localPosition = hipPosition;
            for (int i = 0; i < legs.Length; i++) legs[i].localRotation = rotations[i];
            applied = false;
        }

        public void Apply()
        {
            Restore();
            hipPosition = hips.localPosition;
            for (int i = 0; i < legs.Length; i++) rotations[i] = legs[i].localRotation;
            var left = legs[2].position; var right = legs[5].position;
            var leftRotation = legs[2].rotation; var rightRotation = legs[5].rotation;
            hips.position -= Vector3.up * (ArenaActor.StandingHeight - ArenaActor.CrouchingHeight);
            Solve(0, left); Solve(3, right);
            legs[2].rotation = leftRotation; legs[5].rotation = rightRotation;
            applied = true;
        }

        private void Solve(int index, Vector3 target)
        {
            var upper = legs[index]; var lower = legs[index + 1]; var foot = legs[index + 2];
            Vector3 origin = upper.position, delta = target - origin;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, foot.position);
            float length = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
            var direction = delta.normalized;
            var bend = Vector3.ProjectOnPlane(animator.transform.forward, direction).normalized;
            float along = (a * a - b * b + length * length) / (2 * length);
            var knee = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            Rotate(upper, lower.position - origin, knee - origin);
            Rotate(lower, foot.position - lower.position, target - lower.position);
        }

        private static void Rotate(Transform bone, Vector3 from, Vector3 to)
            => bone.localRotation = Quaternion.FromToRotation(bone.parent.InverseTransformVector(from),
                bone.parent.InverseTransformVector(to)) * bone.localRotation;
    }
}
