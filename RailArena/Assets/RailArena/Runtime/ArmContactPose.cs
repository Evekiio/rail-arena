using UnityEngine;
namespace RailArena
{
    /// <summary>Deterministic cosmetic two-bone solve in bone space, after humanoid retargeting.</summary>
    public static class ArmContactPose
    {
        public static void AlignBody(Animator animator)
        {
            var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var right = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var axis = Vector3.ProjectOnPlane(right.position - left.position, animator.transform.up);
            var desired = animator.transform.right;
            RotateDirection(hips, axis, desired);
        }

        private static void RotateDirection(Transform bone, Vector3 from, Vector3 to)
        {
            // Imported rigs may carry mirrored scales. Solve in the parent's actual
            // basis instead of assuming its world quaternion describes that basis.
            var parent = bone.parent;
            var localFrom = parent.InverseTransformVector(from);
            var localTo = parent.InverseTransformVector(to);
            bone.localRotation = Quaternion.FromToRotation(localFrom, localTo) * bone.localRotation;
        }

        public static void Apply(Animator animator, bool left, Transform target)
        {
            var upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
            var lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
            var wrist=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            Vector3 origin=upper.position,delta=target.position-origin;
            float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,wrist.position);
            float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);
            if(delta.sqrMagnitude<1e-10f)return;
            var direction=delta.normalized;
            var hint=animator.transform.TransformDirection(new Vector3(left?-.45f:.45f,-1,-.1f));
            var bend=Vector3.ProjectOnPlane(hint,direction).normalized;
            float along=(a*a-b*b+length*length)/(2*length);
            var elbow=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            RotateDirection(upper, lower.position-origin, elbow-origin);
            RotateDirection(lower, wrist.position-lower.position, origin+direction*length-lower.position);
            wrist.rotation=target.rotation;
        }
    }
}
