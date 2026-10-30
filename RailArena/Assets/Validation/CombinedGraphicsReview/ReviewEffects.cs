using System.Collections.Generic;
using UnityEngine;
using RailArena;

namespace RailArena.GraphicsReview
{
    public sealed class ReviewEffects : MonoBehaviour
    {
        public Shader waterShader;
        public Material water, foam, rifleSurface;
        public Texture2D rifleAo;
        public GameObject[] rifleModels;
        readonly HashSet<Object> seen = new HashSet<Object>();
        readonly List<Material> owned = new List<Material>();
        float nextScan;
        void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.5f;
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer.sharedMaterial != water && renderer.sharedMaterial != foam) continue;
                var old = renderer.sharedMaterial; var mat = new Material(waterShader); mat.SetColor("_BaseColor", old.GetColor("_BaseColor")); mat.SetFloat("_Foam", old.GetFloat("_Foam"));
                renderer.sharedMaterial = mat; owned.Add(mat);
            }
            foreach (var weapon in FindObjectsByType<RailgunPresentation>(FindObjectsSortMode.None))
            {
                if (!seen.Add(weapon)) continue;
                for (int level = 0; level < rifleModels.Length; level++)
                {
                    var lod = weapon.transform.Find("Carbine LOD" + level); if (lod == null || rifleModels[level] == null) continue;
                    var candidates = new Dictionary<string, Mesh>();
                    foreach (var f in rifleModels[level].GetComponentsInChildren<MeshFilter>(true)) candidates[f.name] = f.sharedMesh;
                    foreach (var f in lod.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (f.name.StartsWith("RF_07_", System.StringComparison.Ordinal)) continue;
                        if (candidates.TryGetValue(f.name, out var mesh)) f.sharedMesh = mesh;
                        var r = f.GetComponent<Renderer>(); if (r == null) continue;
                        var materials = r.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) if (materials[i] == rifleSurface)
                        { var m = new Material(rifleSurface); m.SetTexture("_OcclusionMap", rifleAo); m.SetFloat("_OcclusionStrength", 1); m.EnableKeyword("_OCCLUSIONMAP"); materials[i] = m; owned.Add(m); }
                        r.sharedMaterials = materials;
                    }
                }
            }
        }
        void OnDestroy() { foreach (var m in owned) if (m != null) Destroy(m); }
    }
}


