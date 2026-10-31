using System;
using UnityEngine;
namespace RailArena
{
    public sealed class GardenNatureSet : ScriptableObject
    {
        [Serializable] public sealed class Part
        {
            public string sourceName,module;
            public GameObject prefab;
            public Vector3 center,size;
            public bool wind;
        }
        public Part[] parts;
    }
}
