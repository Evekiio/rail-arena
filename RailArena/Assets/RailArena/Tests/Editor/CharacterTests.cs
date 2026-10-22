using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class CharacterTests
    {
        [Test]
        public void AllLocomotionAndAirLoopsAreMarkedToRepeat()
        {
            var prefab = Resources.Load<GameObject>("Characters/Contender");
            var clips = prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips;
            foreach (var clip in clips.Where(c => c.name.EndsWith("_Loop") || c.name.StartsWith("Backward")))
                Assert.That(clip.isLooping, Is.True, clip.name + " must repeat beyond its first cycle.");
        }

        [Test]
        public void ContenderHasValidHumanoidLodsAndBoneFollowingHurtboxes()
        {
            var prefab = Resources.Load<GameObject>("Characters/Contender");
            Assert.That(prefab, Is.Not.Null);
            var animator = prefab.GetComponent<Animator>();
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(prefab.GetComponent<LODGroup>().GetLODs().Length, Is.EqualTo(3));
            var hitboxes = prefab.GetComponentsInChildren<Hitbox>();
            Assert.That(hitboxes.Length, Is.EqualTo(11));
            Assert.That(hitboxes.Count(h => h.Region == Simulation.HitRegion.Head), Is.EqualTo(1));
            Assert.That(hitboxes.Count(h => h.Region == Simulation.HitRegion.Body), Is.EqualTo(2));
            Assert.That(hitboxes.Count(h => h.Region == Simulation.HitRegion.Limb), Is.EqualTo(8));
            Assert.That(hitboxes.All(h => h.GetComponent<Collider>().isTrigger), Is.True);
        }

        [TestCase(8f)]
        [TestCase(-8f)]
        public void RetargetedLocomotionMovesFeetWithoutRootMotionAndDeathLowersHead(float speed)
        {
            var instance = Object.Instantiate(Resources.Load<GameObject>("Characters/Contender"));
            try
            {
                var animator = instance.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.SetBool("Grounded", true);
                animator.SetFloat("Speed", speed);
                animator.SetFloat("Gait", 1);
                animator.Play("Base Layer.Locomotion", 0, 0);
                animator.Update(.1f);
                Vector3 root = instance.transform.position;
                Vector3 foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                float head = animator.GetBoneTransform(HumanBodyBones.Head).position.y;
                animator.Update(.23f);
                Assert.That(Vector3.Distance(foot, animator.GetBoneTransform(HumanBodyBones.LeftFoot).position), Is.GreaterThan(.01f));
                Assert.That(Vector3.Distance(root, instance.transform.position), Is.LessThan(.001f));
                animator.SetBool("Dead", true);
                animator.Update(.1f);
                animator.Update(1.3f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), Is.True);
                Assert.That(animator.GetBoneTransform(HumanBodyBones.Head).position.y, Is.LessThan(head - .25f));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
