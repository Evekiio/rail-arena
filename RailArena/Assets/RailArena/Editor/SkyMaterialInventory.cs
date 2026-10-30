using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RailArena.Editor
{
    public static class SkyMaterialInventory
    {
        [Serializable] private sealed class Entry
        {
            public string material, path, shader;
            public int renderers;
            public bool albedo, normal, detailNormal, metallic, occlusion, height, emission;
        }
        [Serializable] private sealed class Report { public string scene, note; public Entry[] materials; }
        public static void Capture()
        {
            const string scene = "Assets/RailArena/Generated/SkyGardens.unity";
            EditorSceneManager.OpenScene(scene);
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var groups = new Dictionary<Material, int>();
            foreach (var renderer in renderers)
                foreach (var material in renderer.sharedMaterials.Where(m => m != null).Distinct())
                    groups[material] = groups.TryGetValue(material, out int n) ? n + 1 : 1;
            bool Has(Material m, string property) => m.HasProperty(property) && m.GetTexture(property) != null;
            var entries = groups.Select(pair => new Entry
            {
                material = pair.Key.name, path = AssetDatabase.GetAssetPath(pair.Key), shader = pair.Key.shader != null ? pair.Key.shader.name : "missing",
                renderers = pair.Value, albedo = Has(pair.Key, "_BaseMap"), normal = Has(pair.Key, "_BumpMap"),
                detailNormal = Has(pair.Key, "_DetailNormalMap"), metallic = Has(pair.Key, "_MetallicGlossMap"),
                occlusion = Has(pair.Key, "_OcclusionMap"), height = Has(pair.Key, "_ParallaxMap"), emission = Has(pair.Key, "_EmissionMap")
            }).OrderByDescending(e => e.renderers).ThenBy(e => e.material).ToArray();
            var report = new Report { scene = scene, note = "Texture assignment inventory only; shader keywords, texture quality, runtime-created materials and visual acceptance are not verified.", materials = entries };
            var output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/sky-material-inventory-20261030.json");
            if (File.Exists(output)) throw new IOException("Inventory output already exists");
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("SKY_MATERIAL_INVENTORY materials=" + entries.Length + " renderers=" + renderers.Length + " output=" + output);
        }
    }
}
