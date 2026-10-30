using UnityEngine;

namespace RailArena
{
    /// <summary>Cosmetic arm-only rig. It observes weapon grips and never owns gameplay or hurtboxes.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class FirstPersonArms : MonoBehaviour
    {
        public Animator Animator;
        public SkinnedMeshRenderer Surface;
        public HandArticulation Hands;
        private RailgunPresentation weapon;
        public int LastPoseFrame { get; private set; } = -1;

        public static FirstPersonArms CreateOnWeapon(RailgunPresentation railgun)
        {
            var prefab = Resources.Load<GameObject>("Characters/ViewArms");
            if (prefab == null || railgun.RightGrip == null || railgun.LeftGrip == null)
                throw new System.InvalidOperationException("The view-arm prefab or railgun grip markers are missing.");
            var arms = Object.Instantiate(prefab, railgun.transform, false).GetComponent<FirstPersonArms>();
            arms.Initialize(railgun);
            railgun.ConfigureFirstPerson();
            return arms;
        }

        public void Initialize(RailgunPresentation railgun)
        {
            weapon = railgun;
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Animator.Rebind();
            Animator.Update(0);
        }

        public void SetRecoil(float strength) => Hands.SetRecoil(strength);

        private void LateUpdate()
        {
            if (weapon == null) return;
            ArmContactPose.AlignBody(Animator);
            ArmContactPose.Apply(Animator, true, weapon.LeftGrip);
            ArmContactPose.Apply(Animator, false, weapon.RightGrip);
            LastPoseFrame = Time.frameCount;
        }
    }
}
