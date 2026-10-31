using System;
using UnityEngine;

namespace RailArena
{
    /// <summary>Derived GPU exports; the original measured meshes and material assignments remain source authority.</summary>
    public sealed class StoneSubmissionArt : ScriptableObject
    {
        [Serializable] public sealed class Part
        {
            public Mesh source, derived;
            public Material[] sourceMaterials;
        }
        public Material palette;
        public ColorSpace colorSpace;
        public Part[] parts;
    }
}
