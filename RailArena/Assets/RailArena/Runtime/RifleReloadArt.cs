using System;
using UnityEngine;

namespace RailArena
{
    /// <summary>Blender-evaluated visual tracks. The shared magazine simulation owns time and ammunition.</summary>
    public sealed class RifleReloadArt : ScriptableObject
    {
        [Serializable] public struct Pose
        {
            public Vector3 magazineOffset;
            public Quaternion magazineRotation;
            public Vector3 leftPosition;
            public Quaternion leftRotation;
        }

        public Vector3 magazinePivot;
        public Pose[] samples = Array.Empty<Pose>();
        public string sourceSha256;
        public Pose Evaluate(float progress)
        {
            if (samples.Length < 2) throw new InvalidOperationException("Reload art has no authored track");
            float frame = Mathf.Clamp01(progress) * (samples.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(frame), samples.Length - 2);
            float blend = frame - index;
            Pose a = samples[index], b = samples[index + 1];
            return new Pose
            {
                magazineOffset = Vector3.Lerp(a.magazineOffset, b.magazineOffset, blend),
                magazineRotation = Quaternion.Slerp(a.magazineRotation, b.magazineRotation, blend),
                leftPosition = Vector3.Lerp(a.leftPosition, b.leftPosition, blend),
                leftRotation = Quaternion.Slerp(a.leftRotation, b.leftRotation, blend)
            };
        }
    }
}
