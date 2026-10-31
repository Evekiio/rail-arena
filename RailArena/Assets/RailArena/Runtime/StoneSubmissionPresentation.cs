using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RailArena
{
    /// <summary>Opt-in research presentation. No transforms, LOD memberships, collision or simulation writes.</summary>
    public sealed class StoneSubmissionPresentation : MonoBehaviour
    {
        sealed class Binding { public MeshFilter filter; public Mesh source; public MeshRenderer renderer; public Material[] materials; }
        readonly List<Binding> bindings = new();
        public int ReplacedRenderers => bindings.Count;
        public int OriginalMaterialSlots { get; private set; }

        public void Apply()
        {
            if (bindings.Count != 0) throw new InvalidOperationException("Stone submission is already applied");
            var art = Resources.Load<StoneSubmissionArt>("Art/StoneSubmissionResearch");
            if (art == null || art.palette == null || art.parts == null || art.parts.Length == 0)
                throw new InvalidOperationException("Derived stone submission exports are missing");
            if (art.colorSpace != QualitySettings.activeColorSpace) throw new InvalidOperationException("Derived stone palette must be rebuilt for the active colour space");
            var lookup = art.parts.GroupBy(part => part.source).ToDictionary(group => group.Key, group => group.ToArray());
            foreach (var filter in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (filter.sharedMesh == null || !lookup.TryGetValue(filter.sharedMesh, out var candidates)) continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.HasPropertyBlock()) continue;
                var materials = renderer.sharedMaterials;
                var part = candidates.SingleOrDefault(candidate => candidate.sourceMaterials.SequenceEqual(materials));
                if (part == null) continue;
                bindings.Add(new Binding { filter = filter, source = filter.sharedMesh, renderer = renderer, materials = materials });
                OriginalMaterialSlots += materials.Length;
                filter.sharedMesh = part.derived;
                renderer.sharedMaterials = new[] { art.palette };
            }
            if (bindings.Count == 0) throw new InvalidOperationException("No source stone meshes matched the derived exports");
        }

        public void Restore()
        {
            foreach (var item in bindings)
            {
                if (item.filter != null) item.filter.sharedMesh = item.source;
                if (item.renderer != null) item.renderer.sharedMaterials = item.materials;
            }
            bindings.Clear();
            OriginalMaterialSlots = 0;
        }

        void OnDestroy() => Restore();
    }
}
