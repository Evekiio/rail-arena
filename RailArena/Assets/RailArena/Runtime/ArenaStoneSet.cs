using System;
using UnityEngine;

namespace RailArena
{
    [CreateAssetMenu(menuName = "Rail Arena/Measured arena stone set")]
    public sealed class ArenaStoneSet : ScriptableObject
    {
        [Serializable]
        public sealed class Part
        {
            public string sourceName;
            public GameObject prefab;
            public Quaternion rotation = Quaternion.identity;
        }

        public Part[] parts;
    }
}
