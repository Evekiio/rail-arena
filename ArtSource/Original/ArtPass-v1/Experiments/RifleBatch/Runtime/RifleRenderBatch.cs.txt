using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena
{
    /// <summary>Combines only rigid rifle shells within each LOD; articulated parts keep their renderers.</summary>
    public sealed class RifleRenderBatch : MonoBehaviour
    {
        readonly Dictionary<MeshRenderer, bool> originals = new();
        readonly List<GameObject> visuals = new();
        readonly List<Mesh> meshes = new();
        LODGroup group;
        LOD[] sourceLods;
        public int SourceRenderers => originals.Count;
        public int BatchRenderers => visuals.Count;
        public bool Initialized => group != null;

        static bool Eligible(MeshRenderer renderer)
        {
            if (!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeSelf || renderer.HasPropertyBlock() || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial == null) return false;
            string name = renderer.name;
            if (!name.EndsWith("_shell_Surface", StringComparison.Ordinal)) return false;
            int part;
            if (name.Length < 6 || !int.TryParse(name.Substring(3, 2), out part)) return false;
            return name.StartsWith("RF_", StringComparison.Ordinal) && part >= 1 && part <= 12 && part != 7 && part != 11;
        }
        public void Initialize(RailgunPresentation weapon)
        {
            if (Initialized) return;
            group = weapon.GetComponent<LODGroup>();
            if (group == null) throw new InvalidOperationException("Rifle batching requires its original LOD group");
            sourceLods = group.GetLODs();
            var revised = (LOD[])sourceLods.Clone();
            try
            {
                for (int level = 0; level < sourceLods.Length; level++)
                {
                    var animated = (weapon.Actuators ?? Array.Empty<Transform>()).Concat(weapon.Triggers ?? Array.Empty<Transform>()).Where(item => item != null).ToArray();
                    var candidates = sourceLods[level].renderers.OfType<MeshRenderer>().Where(Eligible)
                        .Where(renderer => !animated.Any(item => renderer.transform == item || renderer.transform.IsChildOf(item)));
                    var batches = candidates.GroupBy(renderer => (renderer.sharedMaterial, renderer.shadowCastingMode,
                        renderer.receiveShadows, renderer.lightProbeUsage, renderer.reflectionProbeUsage,
                        renderer.renderingLayerMask, renderer.gameObject.layer, renderer.motionVectorGenerationMode,
                        renderer.lightmapIndex, renderer.realtimeLightmapIndex, renderer.probeAnchor, renderer.lightProbeProxyVolumeOverride, renderer.allowOcclusionWhenDynamic));
                    var members = sourceLods[level].renderers.ToList();
                    foreach (var batch in batches)
                    {
                        var parts = batch.ToArray();
                        if (parts.Length < 2) continue;
                        var first = parts[0]; var parent = first.transform.parent;
                        if (parts.Any(part => part.transform.parent != parent || part.GetComponent<MeshFilter>()?.sharedMesh == null))
                            throw new InvalidOperationException("Rigid rifle shells require the same LOD parent and mesh");
                        if (parts.Any(part => part.GetComponent<MeshFilter>().sharedMesh.subMeshCount != 1))
                            throw new InvalidOperationException("Rigid rifle shell must have exactly one submesh");
                        // Runtime lightmapped geometry is outside this character-attached visual contract.
                        if (first.lightmapIndex >= 0 && first.lightmapIndex < 65534 || first.realtimeLightmapIndex >= 0 && first.realtimeLightmapIndex < 65534)
                            throw new InvalidOperationException("Rifle shells must not be lightmapped");
                        var combines = parts.Select(part => new CombineInstance { mesh = part.GetComponent<MeshFilter>().sharedMesh,
                            transform = parent.worldToLocalMatrix * part.transform.localToWorldMatrix }).ToArray();
                        var mesh = new Mesh { name = "Rigid rifle shells LOD"+level, indexFormat = IndexFormat.UInt32 };
                        meshes.Add(mesh); mesh.CombineMeshes(combines, true, true, false); mesh.RecalculateBounds();
                        var visual = new GameObject("Rigid rifle shells LOD"+level); visuals.Add(visual);
                        visual.layer = first.gameObject.layer; visual.transform.SetParent(parent, false);
                        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = visual.AddComponent<MeshRenderer>(); renderer.sharedMaterial = first.sharedMaterial;
                        renderer.shadowCastingMode = first.shadowCastingMode; renderer.receiveShadows = first.receiveShadows;
                        renderer.lightProbeUsage = first.lightProbeUsage; renderer.reflectionProbeUsage = first.reflectionProbeUsage;
                        renderer.probeAnchor = first.probeAnchor; renderer.lightProbeProxyVolumeOverride = first.lightProbeProxyVolumeOverride;
                        renderer.allowOcclusionWhenDynamic = first.allowOcclusionWhenDynamic; renderer.renderingLayerMask = first.renderingLayerMask;
                        renderer.motionVectorGenerationMode = first.motionVectorGenerationMode;
                        foreach (var part in parts) { originals.Add(part, part.enabled); part.enabled = false; members.Remove(part); }
                        members.Add(renderer);
                    }
                    revised[level].renderers = members.ToArray();
                }
                group.SetLODs(revised);
            }
            catch { Restore(); throw; }
        }
        public void Restore()
        {
            if (sourceLods == null) return;
            if (group != null) group.SetLODs(sourceLods);
            foreach (var item in originals) if (item.Key != null) item.Key.enabled = item.Value;
            foreach (var visual in visuals)
            {
                if (visual == null) continue;
                visual.SetActive(false); if (Application.isPlaying) Destroy(visual); else DestroyImmediate(visual);
            }
            foreach (var mesh in meshes) if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            originals.Clear(); visuals.Clear(); meshes.Clear(); group = null; sourceLods = null;
        }
        void OnDestroy() => Restore();
    }
}
