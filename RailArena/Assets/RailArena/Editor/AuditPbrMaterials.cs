using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class AuditPbrMaterials
    {
        [Serializable] private sealed class Entry
        {
            public string path, shader; public int sceneRenderers, weaponRenderers;
            public bool albedo, normal, detailNormal, metallicSmoothness, occlusion, height;
        }
        [Serializable] private sealed class MeshEntry { public string path, name; public int vertices, uv, tangents; }
        [Serializable] private sealed class Report { public string scene; public Entry[] materials; public MeshEntry[] weaponMeshes; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var entries = new Dictionary<Material,Entry>();
            void Collect(Renderer renderer, bool weapon)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    if (!entries.TryGetValue(material, out var entry))
                    {
                        bool HasTexture(string property) => material.HasProperty(property) && material.GetTexture(property) != null;
                        entry = new Entry { path = AssetDatabase.GetAssetPath(material), shader = material.shader != null ? material.shader.name : "MISSING",
                            albedo = HasTexture("_BaseMap"), normal = HasTexture("_BumpMap"), detailNormal = HasTexture("_DetailNormalMap"),
                            metallicSmoothness = HasTexture("_MetallicGlossMap"), occlusion = HasTexture("_OcclusionMap"), height = HasTexture("_ParallaxMap") };
                        entries.Add(material, entry);
                    }
                    if (weapon) entry.weaponRenderers++; else entry.sceneRenderers++;
                }
            }
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Collect(renderer, false);
            var weaponRoot = PrefabUtility.LoadPrefabContents("Assets/Resources/Weapons/Railgun.prefab");
            var weaponMeshes = new List<MeshEntry>();
            try
            {
                foreach (var renderer in weaponRoot.GetComponentsInChildren<Renderer>(true)) Collect(renderer, true);
                var seen = new HashSet<Mesh>();
                foreach (var filter in weaponRoot.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh; if (mesh == null || !seen.Add(mesh)) continue;
                    weaponMeshes.Add(new MeshEntry { path = AssetDatabase.GetAssetPath(mesh), name = mesh.name, vertices = mesh.vertexCount, uv = mesh.uv.Length, tangents = mesh.tangents.Length });
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(weaponRoot); }
            string[] args = Environment.GetCommandLineArgs(); int option = Array.IndexOf(args, "--pbr-audit-output");
            if (option < 0 || option + 1 >= args.Length) throw new ArgumentException("Supply --pbr-audit-output");
            File.WriteAllText(args[option + 1], JsonUtility.ToJson(new Report { scene = "SkyGardens", materials = entries.Values.OrderBy(e => e.path).ToArray(), weaponMeshes = weaponMeshes.OrderBy(e => e.name).ToArray() }, true));
            Debug.Log("PBR_MATERIAL_AUDIT_COMPLETE materials=" + entries.Count);
        }
    }
}
