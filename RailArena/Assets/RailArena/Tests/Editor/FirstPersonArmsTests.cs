using System;
using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class FirstPersonArmsTests
    {
        [Test]
        public void ViewModelRetainsOnlyArmWeightedGeometryAndHasNoGameplayComponents()
        {
            var root = Object.Instantiate(Resources.Load<GameObject>("Characters/ViewArms"));
            try
            {
                var arms = root.GetComponent<FirstPersonArms>();
                Assert.That(arms.Animator.avatar.isHuman && arms.Animator.avatar.isValid, Is.True);
                Assert.That(arms.Animator.applyRootMotion, Is.False);
                Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Hitbox>(), Is.Empty);
                Assert.That(root.GetComponentsInChildren<ArenaActor>(), Is.Empty);
                Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>().Length, Is.EqualTo(1));
                var mesh = arms.Surface.sharedMesh;
                Assert.That(mesh.triangles.Length / 3, Is.InRange(1000, 25000));
                var indices = new HashSet<int>();
                foreach (var id in new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                    HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand })
                    indices.Add(Array.IndexOf(arms.Surface.bones, arms.Animator.GetBoneTransform(id)));
                foreach (var w in mesh.boneWeights)
                {
                    float arm = (indices.Contains(w.boneIndex0) ? w.weight0 : 0) + (indices.Contains(w.boneIndex1) ? w.weight1 : 0) +
                        (indices.Contains(w.boneIndex2) ? w.weight2 : 0) + (indices.Contains(w.boneIndex3) ? w.weight3 : 0);
                    Assert.That(arm, Is.GreaterThanOrEqualTo(.5999f));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator BothWristsFollowTheSharedGripThroughAimAndRecoilInThePlayerLoop()
        {
            yield return new EnterPlayMode();
            foreach (var pose in new[] { Vector2.zero, new Vector2(89, 120), new Vector2(-89, -120) })
            {
                var gun = Object.Instantiate(Resources.Load<GameObject>("Weapons/Railgun"));
                try
                {
                    gun.transform.SetPositionAndRotation(new Vector3(.33f, -.3f, .62f), Quaternion.Euler(pose.x, pose.y, 0));
                    var weapon = gun.GetComponent<RailgunPresentation>();
                    var arms = FirstPersonArms.CreateOnWeapon(weapon);
                    yield return null; yield return null;
                    for (int frame = 0; frame < 8; frame++)
                    {
                        gun.transform.position -= gun.transform.forward * .02f;
                        yield return null;
                        Assert.That(Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, weapon.LeftGrip.position), Is.LessThan(.015f));
                        Assert.That(Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, weapon.RightGrip.position), Is.LessThan(.015f));
                    }
                }
                finally { Object.DestroyImmediate(gun); }
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorMode()
        { if (Application.isPlaying) yield return new ExitPlayMode(); }

        [Test]
        public void ThirdPersonPresentationUsesCanonicalWeaponGripMarkers()
        {
            var presentation = Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>();
            Assert.That(presentation.RightGrip, Is.EqualTo(presentation.Railgun.RightGrip));
            Assert.That(presentation.LeftGrip, Is.EqualTo(presentation.Railgun.LeftGrip));
        }
    }
}
