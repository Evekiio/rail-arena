using System;
using UnityEngine;

namespace RailArena
{
    public sealed class ContenderArtSet : ScriptableObject
    {
        [Serializable] public sealed class SkinMesh { public Mesh mesh; public string[] bones; }
        [Serializable] public sealed class Variant { public string identity; public SkinMesh[] lods; public SkinMesh arms; public Material surface, outline; }
        public Variant[] variants;
    }
}
