using UnityEngine;

namespace RailArena
{
    /// <summary>Cosmetic arm-only rig. It observes weapon grips and never owns gameplay or hurtboxes.</summary>
    public sealed class FirstPersonArms : MonoBehaviour
    {
        public Animator Animator;
        public SkinnedMeshRenderer Surface;
        private RailgunPresentation weapon;

        public static FirstPersonArms CreateOnWeapon(RailgunPresentation railgun)
        {
            var prefab = Resources.Load<GameObject>("Characters/ViewArms");
            if (prefab == null || railgun.RightGrip == null || railgun.LeftGrip == null)
                throw new System.InvalidOperationException("The view-arm prefab or railgun grip markers are missing.");
            var arms = Object.Instantiate(prefab, railgun.transform, false).GetComponent<FirstPersonArms>();
            arms.Initialize(railgun);
            return arms;
        }

        public void Initialize(RailgunPresentation railgun)
        {
            weapon = railgun;
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void OnAnimatorIK(int layer)
        {
            if (weapon == null) return;
            Hand(AvatarIKGoal.LeftHand, weapon.LeftGrip);
            Hand(AvatarIKGoal.RightHand, weapon.RightGrip);
            Animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, .85f);
            Animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, .85f);
            Animator.SetIKHintPosition(AvatarIKHint.LeftElbow, transform.TransformPoint(new Vector3(-.4f, 1.16f, .12f)));
            Animator.SetIKHintPosition(AvatarIKHint.RightElbow, transform.TransformPoint(new Vector3(.38f, 1.18f, .1f)));
        }

        private void Hand(AvatarIKGoal hand, Transform grip)
        {
            Animator.SetIKPositionWeight(hand, 1);
            Animator.SetIKRotationWeight(hand, .85f);
            Animator.SetIKPosition(hand, grip.position);
            Animator.SetIKRotation(hand, grip.rotation);
        }
    }
}
