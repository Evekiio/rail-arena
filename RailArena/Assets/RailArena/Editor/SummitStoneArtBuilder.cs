using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    public static class SummitStoneArtBuilder
    {
        public static void Prepare()
        {
            const string folder = "Assets/RailArena/Art/ArtPass/Environment/SummitStone/";
            var source = JsonUtility.FromJson<Report>(File.ReadAllText("../ArtSource/Original/ArtPass-v1/Environment/SummitStone/summit-source.json"));
            string[] slots = { "DeckIvory", "DeckStone", "DeckTeal", "DeckBronze", "DeckShade" };
            var materials = slots.Select(name => AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/RailArena/Art/ArtPass/Environment/BasementDecks/" + name + ".mat")).ToArray();
            if (materials.Any(material => material == null)) throw new InvalidOperationException("Shared cel stone materials are missing");
            foreach (var profile in source.profiles)
            {
                var root = new GameObject("Summit stone " + profile.name);
                try
                {
                    var lods = new LOD[3]; int previous = int.MaxValue;
                    for (int level = 0; level < 3; level++)
                    {
                        string path = folder + profile.name + "-LOD" + level + ".fbx";
                        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                        importer.importAnimation = false; importer.addCollider = false;
                        importer.importNormals = ModelImporterNormals.Import;
                        importer.importTangents = ModelImporterTangents.CalculateMikk; importer.SaveAndReimport();
                        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform, false);
                        instance.name = "LOD" + level;
                        var renderer = instance.GetComponentsInChildren<MeshRenderer>().Single();
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                        {
                            int index = Array.IndexOf(slots, material.name);
                            if (index < 0) throw new InvalidOperationException("Unknown summit material " + material.name);
                            return materials[index];
                        }).ToArray();
                        var expected = new Bounds(Vector3.down * profile.thicknessMetres * .5f,
                            new Vector3(profile.radiusMetres * 2, profile.thicknessMetres, profile.radiusMetres * 2));
                        if ((renderer.bounds.center - expected.center).sqrMagnitude > .000001f || (renderer.bounds.size - expected.size).sqrMagnitude > .000001f)
                            throw new InvalidOperationException("Summit export metre scale/top pivot mismatch: " + profile.name + " LOD " + level);
                        int triangles = renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;
                        if (triangles >= previous) throw new InvalidOperationException("Summit LODs must strictly reduce triangles"); previous = triangles;
                        lods[level] = new LOD(new[] { .20f, .07f, .001f }[level], new Renderer[] { renderer });
                    }
                    if (root.GetComponentsInChildren<Collider>().Length != 0) throw new InvalidOperationException("Summit visual must not add collision");
                    var group = root.AddComponent<LODGroup>(); group.SetLODs(lods); group.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Art/SummitStone" + profile.name + ".prefab");
                }
                finally { Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SUMMIT_STONE_ART_READY three modules / nine reducing LODs / original top pivots / shared cel materials / no colliders");
        }
        [Serializable] sealed class Instance
        {
            public string name, profile;
            public Vector3 position, scale;
            public Quaternion rotation;
        }
        [Serializable] sealed class Report
        {
            public string scene = BasementRouteBuilder.ScenePath;
            public string axes = "Blender (x,y,z) -> Unity (x,z,-y); metres; original top-centre pivot";
            public string scope = "Read-only installed summit collision meshes. Existing painted visual meshes may differ from colliders; collision vertices define the construction envelope. Deck, central cover and four columns remain authoritative; no scene save or gameplay changes.";
            public BasementDeckArtBuilder.DeckSource[] profiles;
            public BasementDeckArtBuilder.DeckSource[] columnInstances;
            public float columnMaximumVertexDeviationMetres;
            public Instance[] instances;
        }
        public static void Audit()
        {
            EditorSceneManager.OpenScene(BasementRouteBuilder.ScenePath);
            string[] names = { "SUMMIT-DECK", "SUMMIT-CENTRAL-COVER", "SUMMIT-COLUMN-1", "SUMMIT-COLUMN-2", "SUMMIT-COLUMN-3", "SUMMIT-COLUMN-4" };
            var parts = names.Select(name => Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Single(renderer => renderer.name == name).transform).ToArray();
            var deck = BasementDeckArtBuilder.Measure(parts[0], "Summit", false);
            var cover = BasementDeckArtBuilder.Measure(parts[1], "Cover", false);
            var column = BasementDeckArtBuilder.Measure(parts[2], "Column", false);
            var columns = parts.Skip(2).Select(part => BasementDeckArtBuilder.Measure(part, part.name, false)).ToArray();
            float deviation = columns.Max(item => item.collision.verticesUnityMetres.Max(vertex =>
                column.collision.verticesUnityMetres.Min(reference => Vector3.Distance(vertex, reference))));
            if (deviation > .00002f) throw new InvalidOperationException("Summit column geometry differs by more than twenty micrometres: " + deviation);
            var report = new Report
            {
                profiles = new[] { deck, cover, column },
                columnInstances = columns, columnMaximumVertexDeviationMetres = deviation,
                instances = parts.Select((part, index) => new Instance
                {
                    name = part.name, profile = index == 0 ? "Summit" : index == 1 ? "Cover" : "Column",
                    position = part.position, rotation = part.rotation, scale = part.lossyScale
                }).ToArray()
            };
            string folder = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Environment/SummitStone");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "summit-source.json"), JsonUtility.ToJson(report, true));
            Debug.Log("SUMMIT_STONE_SOURCE_AUDITED three exact collision profiles / six placements; scene unchanged");
        }
    }
}
