using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailArena
{
    /// <summary>Cosmetic joints around the authored grip. Applied after Animator and wrist IK.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class HandArticulation : MonoBehaviour
    {
        public Transform[] Joints = Array.Empty<Transform>();
        public Quaternion[] GripRotations = Array.Empty<Quaternion>();
        private float recoil;

        public void Configure(Transform skeleton)
        {
            var lookup = new Dictionary<string, Transform>();
            foreach (var bone in skeleton.GetComponentsInChildren<Transform>()) lookup.Add(bone.name, bone);
            Joints = new Transform[30]; GripRotations = new Quaternion[30];
            int index = 0;
            foreach (string side in new[] { "Left", "Right" })
                foreach (string finger in new[] { "Thumb", "Index", "Middle", "Ring", "Pinky" })
                    for (int segment = 1; segment <= 3; segment++)
                    {
                        string name = side + "Hand" + finger + segment;
                        if (!lookup.TryGetValue(name, out var joint)) throw new InvalidOperationException("Missing articulated joint: " + name);
                        Joints[index] = joint; GripRotations[index++] = joint.localRotation;
                    }
        }

        public void SetRecoil(float strength) => recoil = Mathf.Clamp01(strength);

        private void LateUpdate() => Pose(recoil);

        public void Pose(float strength)
        {
            strength = Mathf.Clamp01(strength);
            for (int i = 0; i < Joints.Length; i++)
            {
                int finger = i % 15 / 3, segment = i % 3;
                float degrees = finger == 0 ? .5f : i >= 15 && finger == 1 ?
                    (segment == 0 ? 2 : segment == 1 ? 4 : 2) : 1;
                Joints[i].localRotation = GripRotations[i] * Quaternion.AngleAxis(-degrees * strength, Vector3.right);
            }
        }
    }
}
