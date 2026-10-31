using System;
using UnityEngine;

namespace RailArena
{
    public sealed class FirstPersonEquipArt : ScriptableObject
    {
        [Serializable] public struct Pose { public Vector3 position; public Quaternion rotation; }
        public int fps = 60;
        public Pose[] samples = Array.Empty<Pose>();
        public string masterSha256;
        public float Duration => (samples.Length - 1) / (float)fps;

        public Pose Evaluate(float seconds)
        {
            if (fps <= 0 || samples.Length < 2) throw new InvalidOperationException("Equip art has no authored track");
            float frame = Mathf.Clamp(seconds * fps, 0, samples.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(frame), samples.Length - 2);
            float blend = frame - index;
            return new Pose { position = Vector3.Lerp(samples[index].position, samples[index + 1].position, blend),
                rotation = Quaternion.Slerp(samples[index].rotation, samples[index + 1].rotation, blend) };
        }
    }
}
