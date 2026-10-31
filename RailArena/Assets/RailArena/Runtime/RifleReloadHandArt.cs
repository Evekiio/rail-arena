using System;
using UnityEngine;
namespace RailArena
{
    /// <summary>Blender-evaluated left-finger offsets; observes the existing reload clock.</summary>
    public sealed class RifleReloadHandArt : ScriptableObject
    {
        [Serializable] public struct Pose { public Quaternion[] leftDeltas; }
        public string[] joints=Array.Empty<string>();
        public Pose[] samples=Array.Empty<Pose>();
        public string sourceSha256;
        public Quaternion Evaluate(float progress,int joint)
        {
            if(samples.Length<2)throw new InvalidOperationException("Reload finger action is missing");
            float frame=Mathf.Clamp01(progress)*(samples.Length-1);int index=Mathf.Min(Mathf.FloorToInt(frame),samples.Length-2);
            return Quaternion.Slerp(samples[index].leftDeltas[joint],samples[index+1].leftDeltas[joint],frame-index);
        }
    }
}
