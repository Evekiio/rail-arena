using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class HandArticulationTests
    {
        [TestCase("Characters/Contender")]
        [TestCase("Characters/ViewArms")]
        public void EveryFingerJointHasNormalizedVisibleSkin(string resource)
        {
            var root = Resources.Load<GameObject>(resource);
            var hands = root.GetComponent<HandArticulation>();
            Assert.That(hands, Is.Not.Null);
            Assert.That(hands.Joints.Length, Is.EqualTo(30));
            Assert.That(hands.Joints.Distinct().Count(), Is.EqualTo(30));
            Assert.That(hands.Joints.All(j => j != null), Is.True);
            Assert.That(hands.GripRotations.Length, Is.EqualTo(30));
            var surface = root.GetComponentInChildren<SkinnedMeshRenderer>();
            var totals = new float[surface.bones.Length];
            foreach (var weight in surface.sharedMesh.boneWeights)
            {
                Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, Is.EqualTo(1).Within(.001f));
                totals[weight.boneIndex0] += weight.weight0; totals[weight.boneIndex1] += weight.weight1;
                totals[weight.boneIndex2] += weight.weight2; totals[weight.boneIndex3] += weight.weight3;
            }
            foreach (var joint in hands.Joints)
            {
                int index = Array.IndexOf(surface.bones, joint);
                Assert.That(index, Is.GreaterThanOrEqualTo(0), joint.name);
                Assert.That(totals[index], Is.GreaterThan(.01f), joint.name + " must deform visible geometry.");
            }
        }

        [TestCase("Characters/Contender")]
        [TestCase("Characters/ViewArms")]
        public void RecoilDeformsFingersWithoutMovingWristsAndReturnsExactlyToGrip(string resource)
        {
            var root = Object.Instantiate(Resources.Load<GameObject>(resource));
            var before = new Mesh(); var after = new Mesh();
            try
            {
                var hands = root.GetComponent<HandArticulation>();
                var animator = root.GetComponent<Animator>(); animator.Rebind(); animator.Update(0);
                var surface = root.GetComponentInChildren<SkinnedMeshRenderer>();
                var left = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                var right = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                // Compensate the FBX renderer scale before converting baked deltas back to world space.
                hands.Pose(0); surface.BakeMesh(before, true);
                hands.Pose(1); surface.BakeMesh(after, true);
                var a = before.vertices; var b = after.vertices;
                float delta = a.Zip(b, (x, y) => surface.transform.TransformVector(y - x).magnitude).Max();
                Assert.That(delta, Is.InRange(.0001f, .04f), "A visible but bounded finger deformation is required.");
                Assert.That(Vector3.Distance(left, animator.GetBoneTransform(HumanBodyBones.LeftHand).position), Is.LessThan(.000001f));
                Assert.That(Vector3.Distance(right, animator.GetBoneTransform(HumanBodyBones.RightHand).position), Is.LessThan(.000001f));
                var rotations = hands.Joints.Select(j => j.localRotation).ToArray();
                hands.Pose(1);
                for (int i = 0; i < rotations.Length; i++) Assert.That(Quaternion.Angle(rotations[i], hands.Joints[i].localRotation), Is.LessThan(.01f));
                hands.Pose(0); surface.BakeMesh(after, true); b = after.vertices;
                Assert.That(a.Zip(b, (x, y) => surface.transform.TransformVector(y - x).magnitude).Max(), Is.LessThan(.00001f));
            }
            finally { Object.DestroyImmediate(before); Object.DestroyImmediate(after); Object.DestroyImmediate(root); }
        }
    }
}
