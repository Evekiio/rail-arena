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
                foreach (var id in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
                    for (int i = 0; i < arms.Surface.bones.Length; i++)
                        if (arms.Surface.bones[i].IsChildOf(arms.Animator.GetBoneTransform(id))) indices.Add(i);
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
        public IEnumerator BothWristsFollowSharedGripsThroughAimTravelLandingAndRecoil()
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
                    var motion = new FirstPersonWeaponMotion();
                    motion.Reset(true);
                    yield return null; yield return null;
                    for (int frame = 0; frame < 32; frame++)
                    {
                        bool airborne = frame >= 8 && frame < 16;
                        float recoil = frame >= 20 ? Mathf.Max(0, 1 - (frame - 20) / 12f) : 0;
                        arms.SetRecoil(recoil);
                        motion.Step(1f / 60, airborne ? new Vector3(12, -20, 0) : new Vector3(10, -1, 0),
                            !airborne, new Vector2(2, -1), recoil);
                        var aim = Quaternion.Euler(pose.x, pose.y, 0);
                        gun.transform.SetPositionAndRotation(FirstPersonWeaponMotion.RestPosition + aim *
                            (motion.Offset - Vector3.forward * (recoil * RailgunPresentation.ViewRecoilTravel)), aim * motion.Rotation);
                        yield return null;
                        Assert.That(Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, weapon.LeftGrip.position), Is.LessThan(.015f));
                        Assert.That(Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, weapon.RightGrip.position), Is.LessThan(.015f));
                        Assert.That(Vector3.Angle(arms.Hands.Joints[6].position - arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position,
                            gun.transform.right), Is.LessThan(1));
                        Assert.That(Vector3.Angle(arms.Hands.Joints[21].position - arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position,
                            -gun.transform.right), Is.LessThan(1));
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
