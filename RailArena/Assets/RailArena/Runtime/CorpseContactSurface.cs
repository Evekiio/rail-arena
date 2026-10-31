using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailArena
{
    /// <summary>Cached extremal skin vertices for cosmetic corpse floor support. No physics shapes or root motion.</summary>
    internal sealed class CorpseContactSurface
    {
        struct Sample { public Vector3 position; public BoneWeight weights; }
        static readonly Vector3[] Directions = MakeDirections();
        readonly Sample[] samples;
        readonly Transform[] bones;
        readonly Matrix4x4[] bindposes, matrices;
        public SkinnedMeshRenderer Skin { get; }
        public int SampleCount => samples.Length;

        public CorpseContactSurface(SkinnedMeshRenderer skin)
        {
            Skin = skin; bones = skin.bones; bindposes = skin.sharedMesh.bindposes; matrices = new Matrix4x4[bones.Length];
            var vertices = skin.sharedMesh.vertices; var weights = skin.sharedMesh.boneWeights;
            if (vertices.Length == 0 || weights.Length != vertices.Length || bones.Length != bindposes.Length)
                throw new InvalidOperationException("Corpse contact requires readable, compatible four-weight skin data");
            int count = bones.Length * Directions.Length;
            var indices = new int[count]; var scores = new float[count];
            for (int i = 0; i < count; i++) { indices[i] = -1; scores[i] = float.NegativeInfinity; }
            for (int vertex = 0; vertex < vertices.Length; vertex++)
            {
                var weight = weights[vertex]; int bone = weight.boneIndex0; float maximum = weight.weight0;
                if (weight.weight1 > maximum) { bone = weight.boneIndex1; maximum = weight.weight1; }
                if (weight.weight2 > maximum) { bone = weight.boneIndex2; maximum = weight.weight2; }
                if (weight.weight3 > maximum) bone = weight.boneIndex3;
                var point = bindposes[bone].MultiplyPoint3x4(vertices[vertex]);
                for (int direction = 0; direction < Directions.Length; direction++)
                {
                    int index = bone * Directions.Length + direction; float score = Vector3.Dot(point, Directions[direction]);
                    if (score > scores[index]) { scores[index] = score; indices[index] = vertex; }
                }
            }
            var unique = new HashSet<int>(indices); unique.Remove(-1); var selected = new List<int>(unique); selected.Sort();
            samples = new Sample[selected.Count];
            for (int i = 0; i < selected.Count; i++) samples[i] = new Sample { position = vertices[selected[i]], weights = weights[selected[i]] };
        }

        public float MinimumWorldHeight()
        {
            for (int i = 0; i < bones.Length; i++) matrices[i] = bones[i].localToWorldMatrix * bindposes[i];
            float minimum = float.PositiveInfinity;
            foreach (var sample in samples)
            {
                var point = sample.position; var weight = sample.weights;
                float height = Height(matrices[weight.boneIndex0], point) * weight.weight0 + Height(matrices[weight.boneIndex1], point) * weight.weight1 +
                    Height(matrices[weight.boneIndex2], point) * weight.weight2 + Height(matrices[weight.boneIndex3], point) * weight.weight3;
                minimum = Mathf.Min(minimum, height);
            }
            return minimum;
        }
        static float Height(in Matrix4x4 matrix, Vector3 point) => matrix.m10 * point.x + matrix.m11 * point.y + matrix.m12 * point.z + matrix.m13;

        static Vector3[] MakeDirections()
        {
            var directions = new List<Vector3>();
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                if (x != 0 || y != 0 || z != 0) directions.Add(new Vector3(x, y, z).normalized);
            const int sphereSamples = 32;
            for (int i = 0; i < sphereSamples; i++)
            {
                float y = 1 - 2 * (i + .5f) / sphereSamples, radius = Mathf.Sqrt(1 - y * y), angle = i * Mathf.PI * (3 - Mathf.Sqrt(5));
                directions.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
            }
            return directions.ToArray();
        }
    }
}
