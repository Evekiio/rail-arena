using UnityEngine;

namespace RailArena
{
    public sealed class RifleSurfaceArt : ScriptableObject
    {
        public Material sourceSurface;
        public Texture2D cavityAtlas;
        public GameObject[] models;
    }
}
