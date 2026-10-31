using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    public static class ArenaStoneArtBuilder
    {
        public const string Folder = "../ArtSource/Original/ArtPass-v1/Environment/ArenaStone";
        [Serializable] public sealed class Geometry { public Vector3[] vertices; public int[] triangles; }
        [Serializable] public sealed class Part
        {
            public string name, family, collisionAsset, collisionGuid;
            public Vector3 center, size;
            public Quaternion rotation;
            public Geometry canonicalCollision;
            public float familyMaximumVertexDeviationMetres;
        }
        [Serializable] public sealed class Report
        {
            public string scene = BasementRouteBuilder.ScenePath;
            public string sceneSha256;
            public string axes = "Unity metre-space centred on each original collider bounds; canonical ramp rises toward +X. Blender (x,y,z) -> Unity (x,z,-y).";
            public string scope = "Read-only collision authority for nineteen lower/upper arena decks and four-ramp route. Existing scene, physics, devices and navigation remain authoritative.";
            public Part[] parts;
        }
        [Serializable] sealed class Export { public string export, exportSha256; public int triangles; }
        [Serializable] sealed class Production { public string master, masterSha256, sourceSha256; public Export[] lods; }
        [Serializable] sealed class AxesEvidence
        {
            public string scope = "Read-only native FBX ramp orientation before canonical prefab correction";
            public Vector3 rootEuler, meshEuler;
            public float negativeXTop, positiveXTop;
        }
        static string Digest(byte[] data)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
        public static void AuditImportedAxes()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RailArena/Art/ArtPass/Environment/ArenaStone/Ramp-LOD0.fbx");
            var filter = model.GetComponentsInChildren<MeshFilter>().Single();
            var points = filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
            float low = points.Min(point => point.x), high = points.Max(point => point.x);
            var evidence = new AxesEvidence { rootEuler=model.transform.localEulerAngles,meshEuler=filter.transform.localEulerAngles,
                negativeXTop=points.Where(point=>Mathf.Abs(point.x-low)<.0001f).Max(point=>point.y),
                positiveXTop=points.Where(point=>Mathf.Abs(point.x-high)<.0001f).Max(point=>point.y) };
            File.WriteAllText("../Validation/arena-stone-import-axes.json",JsonUtility.ToJson(evidence,true));
            Debug.Log("ARENA_NATIVE_RAMP_AXES "+JsonUtility.ToJson(evidence));
        }
        public static void Prepare()
        {
            var source = JsonUtility.FromJson<Report>(File.ReadAllText(Folder+"/arena-source.json"));
            var production = JsonUtility.FromJson<Production>(File.ReadAllText(Folder+"/production.json"));
            if (Digest(File.ReadAllBytes(Folder+"/arena-source.json")) != production.sourceSha256 || Digest(File.ReadAllBytes(Folder+"/"+production.master)) != production.masterSha256)
                throw new InvalidOperationException("Canonical arena source/master hash mismatch");
            string[] materialNames = { "Ivory", "Stone", "Teal", "Bronze", "Shade" };
            var materials = materialNames.Select(name => AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/RailArena/Art/ArtPass/Environment/BasementDecks/Deck"+name+".mat")).ToArray();
            if (materials.Any(material => material == null)) throw new InvalidOperationException("Prepare the shared stone materials first");
            var prefabs = new System.Collections.Generic.Dictionary<string, GameObject>();
            foreach (var family in source.parts.GroupBy(part => part.family))
            {
                var root = new GameObject("Arena stone "+family.Key);
                try
                {
                    var lods = new LOD[3];
                    int previous = int.MaxValue;
                    var size = family.First().size;
                    float longest = Mathf.Max(size.x, size.z);
                    float[] thresholds = longest >= 20 ? new[] { .55f,.18f,.001f } : longest >= 10 ? new[] { .32f,.10f,.001f } : new[] { .20f,.065f,.001f };
                    for (int level = 0; level < 3; level++)
                    {
                        string path = "Assets/RailArena/Art/ArtPass/Environment/ArenaStone/"+family.Key+"-LOD"+level+".fbx";
                        var export = production.lods.Single(item => item.export == "RailArena/"+path);
                        if (Digest(File.ReadAllBytes(path)) != export.exportSha256) throw new InvalidOperationException("Arena export hash differs from the canonical manifest: "+path);
                        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                        if (importer == null) throw new InvalidOperationException("Missing authored arena export: "+path);
                        importer.importAnimation = false; importer.addCollider = false;
                        importer.importNormals = ModelImporterNormals.Import;
                        importer.importTangents = ModelImporterTangents.CalculateMikk;
                        importer.SaveAndReimport();
                        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform, false);
                        instance.name = "LOD"+level;
                        // Unity's FBX conversion supplies a half-turn relative to the canonical Blender axes.
                        // Symmetric decks hide it; the measured asymmetric ramp establishes the correction.
                        instance.transform.localRotation = Quaternion.Euler(0,180,0)*instance.transform.localRotation;
                        var renderer = instance.GetComponentsInChildren<MeshRenderer>().Single();
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                        {
                            int slot = Array.FindIndex(materialNames, name => material.name == "Arena"+name);
                            if (slot < 0) throw new InvalidOperationException("Unknown arena stone material: "+material.name);
                            return materials[slot];
                        }).ToArray();
                        if (renderer.bounds.center.sqrMagnitude > .000001f || (renderer.bounds.size-size).sqrMagnitude > .000001f)
                            throw new InvalidOperationException("Arena metre scale or bounds-centre pivot differs: "+family.Key+" "+renderer.bounds);
                        int triangles = renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
                        if (family.Key == "Ramp")
                        {
                            var points = renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
                            float negative = points.Where(point=>Mathf.Abs(point.x+size.x/2)<.0001f).Max(point=>point.y);
                            float positive = points.Where(point=>Mathf.Abs(point.x-size.x/2)<.0001f).Max(point=>point.y);
                            if (positive-negative < 3.999f) throw new InvalidOperationException("Native ramp must climb toward canonical +X");
                        }
                        if (triangles != export.triangles) throw new InvalidOperationException("Arena import changed triangle count: "+path);
                        if (triangles >= previous) throw new InvalidOperationException("Arena LOD must reduce triangles: "+family.Key);
                        previous = triangles;
                        lods[level] = new LOD(thresholds[level], new Renderer[] { renderer });
                    }
                    if (root.GetComponentsInChildren<Collider>().Length != 0) throw new InvalidOperationException("Arena art must not add colliders");
                    var group = root.AddComponent<LODGroup>(); group.SetLODs(lods); group.RecalculateBounds();
                    prefabs.Add(family.Key, PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Art/ArenaStone"+family.Key+".prefab"));
                }
                finally { Object.DestroyImmediate(root); }
            }
            const string setPath = "Assets/Resources/Art/LowerArenaStone.asset";
            var set = AssetDatabase.LoadAssetAtPath<ArenaStoneSet>(setPath);
            if (set == null) { set = ScriptableObject.CreateInstance<ArenaStoneSet>(); AssetDatabase.CreateAsset(set,setPath); }
            set.parts = source.parts.Select(part => new ArenaStoneSet.Part { sourceName = part.name, prefab = prefabs[part.family], rotation = part.rotation }).ToArray();
            EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
            Debug.Log("ARENA_STONE_ART_READY seven measured families / twenty-one LOD exports / nineteen bindings / no colliders");
        }
        public static void Audit()
        {
            byte[] sceneBefore = File.ReadAllBytes(BasementRouteBuilder.ScenePath);
            EditorSceneManager.OpenScene(BasementRouteBuilder.ScenePath);
            var renderers = Object.FindAnyObjectByType<ArenaSession>().World.GetComponentsInChildren<MeshRenderer>(true);
            var names = new[] {
                ("Outer-X-1", "OuterX"), ("Outer-X1", "OuterX"), ("Outer-N-1", "OuterN"), ("Outer-N1", "OuterN"),
                ("Upper-Spine-1", "Spine"), ("Upper-Spine1", "Spine"),
                ("Wing-1_-1", "Wing"), ("Wing-1_1", "Wing"), ("Wing1_-1", "Wing"), ("Wing1_1", "Wing"),
                ("Upper-Landing-1_-1", "Landing"), ("Upper-Landing-1_1", "Landing"), ("Upper-Landing1_-1", "Landing"), ("Upper-Landing1_1", "Landing"),
                ("Underpass", "Underpass"), ("R1", "Ramp"), ("R2", "Ramp"), ("R3", "Ramp"), ("R4", "Ramp") };
            var parts = names.Select(pair =>
            {
                var renderer = renderers.Single(item => item.name == pair.Item1);
                var source = renderer.transform;
                var collider = renderer.GetComponent<MeshCollider>();
                if (collider == null || collider.sharedMesh == null || Quaternion.Angle(source.rotation, Quaternion.identity) > .001f || (source.lossyScale-Vector3.one).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Arena source requires its unchanged metre-scale collider: "+source.name);
                var mesh = collider.sharedMesh;
                var world = mesh.vertices.Select(source.TransformPoint).ToArray();
                var bounds = new Bounds(world[0], Vector3.zero);
                foreach (var vertex in world) bounds.Encapsulate(vertex);
                Quaternion rotation = pair.Item2 == "Ramp" && pair.Item1 != "R1" && pair.Item1 != "R2" ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                var geometry = new Geometry { vertices = world.Select(vertex => Quaternion.Inverse(rotation)*(vertex-bounds.center)).ToArray(), triangles = mesh.triangles };
                string asset = AssetDatabase.GetAssetPath(mesh);
                return new Part { name = pair.Item1, family = pair.Item2, center = bounds.center, size = bounds.size, rotation = rotation,
                    collisionAsset = asset, collisionGuid = AssetDatabase.AssetPathToGUID(asset), canonicalCollision = geometry };
            }).ToArray();
            foreach (var family in parts.GroupBy(part => part.family))
            {
                var reference = family.First().canonicalCollision;
                foreach (var part in family)
                {
                    part.familyMaximumVertexDeviationMetres = part.canonicalCollision.vertices.Max(vertex => reference.vertices.Min(candidate => Vector3.Distance(vertex, candidate)));
                    if (part.familyMaximumVertexDeviationMetres > .00002f) throw new InvalidOperationException("Canonical family mismatch: "+part.name+" deviation "+part.familyMaximumVertexDeviationMetres);
                    if (part.canonicalCollision.triangles.Length != reference.triangles.Length) throw new InvalidOperationException("Family collision topology differs: "+part.name);
                }
            }
            if (!sceneBefore.SequenceEqual(File.ReadAllBytes(BasementRouteBuilder.ScenePath))) throw new InvalidOperationException("Read-only source audit changed the scene");
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder+"/arena-source.json", JsonUtility.ToJson(new Report { sceneSha256 = Digest(sceneBefore), parts = parts }, true));
            Debug.Log("ARENA_STONE_SOURCE_AUDITED nineteen exact collision parts / seven canonical families; scene unchanged");
        }
    }
}
