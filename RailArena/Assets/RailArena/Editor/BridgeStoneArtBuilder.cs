using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Read-only metre-space source contract for the bridge stone family.</summary>
    public static class BridgeStoneArtBuilder
    {
        public static void Prepare()
        {
            const string folder = "Assets/RailArena/Art/ArtPass/Environment/BridgeStone/";
            string[] materialNames = { "Ivory", "Stone", "Teal", "Bronze", "Shade" };
            var materials = materialNames.Select(name => AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/RailArena/Art/ArtPass/Environment/BasementDecks/Deck" + name + ".mat")).ToArray();
            if (materials.Any(material => material == null)) throw new InvalidOperationException("Prepare the shared basement stone materials first");
            var source = JsonUtility.FromJson<Report>(File.ReadAllText("../ArtSource/Original/ArtPass-v1/Environment/BridgeStone/bridge-source.json"));
            string[] families = { "Deck", "Screen", "Pier" };
            string[] sources = { "Bridge", "Bridge-Screen1", "Underpass-Pier1" };
            for (int index = 0; index < families.Length; index++)
            {
                var root = new GameObject("Bridge stone " + families[index]);
                try
                {
                    var lods = new LOD[3];
                    int previousTriangles = int.MaxValue;
                    var expected = source.parts.Single(part => part.name == sources[index]).size;
                    for (int level = 0; level < 3; level++)
                    {
                        string path = folder + families[index] + "-LOD" + level + ".fbx";
                        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                        importer.importAnimation = false; importer.addCollider = false;
                        importer.importNormals = ModelImporterNormals.Import;
                        importer.importTangents = ModelImporterTangents.CalculateMikk;
                        importer.SaveAndReimport();
                        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform, false);
                        instance.name = "LOD" + level;
                        var renderer = instance.GetComponentsInChildren<MeshRenderer>().Single();
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                        {
                            int slot = Array.FindIndex(materialNames, name => material.name == "Bridge" + name);
                            if (slot < 0) throw new InvalidOperationException("Unknown bridge material " + material.name);
                            return materials[slot];
                        }).ToArray();
                        if (renderer.bounds.center.sqrMagnitude > .000001f || (renderer.bounds.size - expected).sqrMagnitude > .000001f)
                            throw new InvalidOperationException("Bridge metre scale or bounds-center pivot mismatch: " + families[index]);
                        int triangles = renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
                        if (triangles >= previousTriangles) throw new InvalidOperationException("Bridge LOD must reduce triangles");
                        previousTriangles = triangles;
                        lods[level] = new LOD(new[] { .20f, .07f, .001f }[level], new Renderer[] { renderer });
                    }
                    if (root.GetComponentsInChildren<Collider>().Length != 0) throw new InvalidOperationException("Bridge art must not add colliders");
                    var group = root.AddComponent<LODGroup>(); group.SetLODs(lods); group.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Art/BridgeStone" + families[index] + ".prefab");
                }
                finally { Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("BRIDGE_STONE_ART_READY three measured modules / nine LOD exports / shared cel stone materials / no colliders");
        }
        [Serializable] public sealed class Geometry
        {
            public Vector3[] vertices;
            public int[] triangles;
        }
        [Serializable] public sealed class Part
        {
            public string name, hierarchy;
            public Vector3 center, size;
            public string[] materials, colliderTypes;
            public Geometry geometry;
        }
        [Serializable] public sealed class Report
        {
            public string scene = BasementRouteBuilder.ScenePath;
            public string axes = "World Unity metres (x,y,z) to Blender metres (x,-z,y). Each module pivot is its measured world bounds center.";
            public string scope = "Read-only installed bridge, screens and piers, including decorative source children. Source collision and portal placements remain authoritative.";
            public Part[] parts;
        }
        public static void Audit()
        {
            EditorSceneManager.OpenScene(BasementRouteBuilder.ScenePath);
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(renderer => renderer.name == "Bridge" || renderer.name.StartsWith("Bridge-Screen", StringComparison.Ordinal)
                    || renderer.name.StartsWith("Underpass-Pier", StringComparison.Ordinal))
                .OrderBy(renderer => renderer.name, StringComparer.Ordinal).ToArray();
            if (!renderers.Any(renderer => renderer.name == "Bridge") || renderers.Length < 5)
                throw new InvalidOperationException("Installed bridge family is incomplete");
            var parts = renderers.Select(renderer =>
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException("Missing source mesh: " + renderer.name);
                var mesh = filter.sharedMesh;
                string hierarchy = renderer.name;
                for (var parent = renderer.transform.parent; parent != null; parent = parent.parent) hierarchy = parent.name + "/" + hierarchy;
                return new Part
                {
                    name = renderer.name, hierarchy = hierarchy, center = renderer.bounds.center, size = renderer.bounds.size,
                    materials = renderer.sharedMaterials.Select(material => material != null ? material.name : "missing").ToArray(),
                    colliderTypes = renderer.GetComponents<Collider>().Select(collider => collider.GetType().Name).ToArray(),
                    geometry = new Geometry { vertices = mesh.vertices.Select(renderer.transform.TransformPoint).ToArray(), triangles = mesh.triangles }
                };
            }).ToArray();
            string folder = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Environment/BridgeStone");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "bridge-source.json"), JsonUtility.ToJson(new Report { parts = parts }, true));
            Debug.Log("BRIDGE_STONE_SOURCE_AUDITED " + parts.Length + " measured source parts; scene unchanged");
        }
    }
}
