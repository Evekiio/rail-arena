using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RailArena.Editor
{
    /// <summary>Read-only art audit. Writes evidence outside Assets and never saves inspected scenes or prefabs.</summary>
    public static class ArtProductionInventory
    {
        [Serializable] class AssetRecord
        {
            public string id, path, type;
            public string[] dependencies, sceneDependencies;
            public bool resourceIncluded;
            public MeshRecord[] meshes;
            public ClipRecord[] clips;
        }
        [Serializable] class MeshRecord { public string name; public long localId; public Vector3 localSize; public int vertices, submeshes; }
        [Serializable] class ClipRecord { public string name; public long localId; public float seconds, framesPerSecond; public bool loop; }
        [Serializable] class ObjectRecord
        {
            public string id, container, hierarchy, mesh, materialEvidence;
            public string[] materials, components;
            public Vector3 worldCenter, worldSize;
            public bool active;
        }
        [Serializable] class ContainerRecord { public string path; public bool enabledBuildScene; public Vector3 renderedSize; public int rendererCount, colliders, missingScripts; }
        [Serializable] class GeometryRecord { public string id, scene, hierarchy; public Vector3[] worldVertices; public int[] triangles; }
        [Serializable] class Report
        {
            public string schema = "rail-arena-art-audit/1", unityVersion, createdUtc;
            public string boundsNote = "World dimensions are Editor renderer AABBs in Unity metres, not certified authored dimensions. Mesh local sizes are unscaled imported units. Runtime-created objects and conditional build injections need separate evidence.";
            public AssetRecord[] assets;
            public ObjectRecord[] objects;
            public ContainerRecord[] containers;
            public GeometryRecord[] representativeGeometry;
        }

        public static void Audit()
        {
            var assets = new List<AssetRecord>(); var objects = new List<ObjectRecord>(); var containers = new List<ContainerRecord>();
            var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p)).OrderBy(p => p).ToArray();
            var scenes = paths.Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) && p.StartsWith("Assets/RailArena/Generated/", StringComparison.Ordinal)).ToArray();
            var sceneDependencies = scenes.ToDictionary(p => p, p => new HashSet<string>(AssetDatabase.GetDependencies(p, true)));
            var enabled = new HashSet<string>(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
            var geometry = new List<GeometryRecord>();
            foreach (var path in paths)
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                var record = new AssetRecord { id = "unity:" + AssetDatabase.AssetPathToGUID(path), path = path, type = type?.Name ?? "Unknown", dependencies = AssetDatabase.GetDependencies(path, false).Where(p => p != path).OrderBy(p => p).ToArray(), sceneDependencies = scenes.Where(s => sceneDependencies[s].Contains(path)).ToArray(), resourceIncluded = path.Contains("/Resources/") };
                if (type == typeof(Mesh) || path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    record.meshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Select(m => { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string guid, out long id); return new MeshRecord { name = m.name, localId = id, localSize = m.bounds.size, vertices = m.vertexCount, submeshes = m.subMeshCount }; }).ToArray();
                }
                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || type == typeof(AnimationClip))
                {
                    record.clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__", StringComparison.Ordinal)).Select(c => { AssetDatabase.TryGetGUIDAndLocalFileIdentifier(c, out string guid, out long id); return new ClipRecord { name = c.name, localId = id, seconds = c.length, framesPerSecond = c.frameRate, loop = c.isLooping }; }).ToArray();
                }
                assets.Add(record);
            }
            foreach (var path in scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Inspect(path, scene.GetRootGameObjects(), enabled.Contains(path), objects, containers);
                if (path.EndsWith("SkyGardensBasement.unity", StringComparison.Ordinal))
                    foreach (var root in scene.GetRootGameObjects()) foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                        if (filter.name == "Basement" && filter.transform.parent != null && filter.transform.parent.name == "Basement route 1")
                            geometry.Add(new GeometryRecord { id = "basement-deck-current", scene = path, hierarchy = "Basement route 1/Basement", worldVertices = filter.sharedMesh.vertices.Select(v => filter.transform.TransformPoint(v)).ToArray(), triangles = filter.sharedMesh.triangles });
            }
            foreach (var path in paths.Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && p.Contains("/Resources/")))
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Inspect(path, new[] { root }, false, objects, containers); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var output = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Gate1/unity-audit.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new Report { unityVersion = Application.unityVersion, createdUtc = DateTime.UtcNow.ToString("O"), assets = assets.ToArray(), objects = objects.ToArray(), containers = containers.ToArray(), representativeGeometry = geometry.ToArray() }, true));
            Debug.Log($"ART_INVENTORY_AUDIT assets={assets.Count} renderers={objects.Count} containers={containers.Count} output={output}");
        }
        static void Inspect(string path, GameObject[] roots, bool enabled, List<ObjectRecord> objects, List<ContainerRecord> containers)
        {
            var transforms = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
            Bounds bounds = default; bool first = true;
            foreach (var renderer in renderers)
            {
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : filter != null ? filter.sharedMesh : null;
                string hierarchy = renderer.name; for (var parent = renderer.transform.parent; parent != null; parent = parent.parent) hierarchy = parent.name + "/" + hierarchy;
                objects.Add(new ObjectRecord { id = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(), container = path, hierarchy = hierarchy, worldCenter = renderer.bounds.center, worldSize = renderer.bounds.size, active = renderer.gameObject.activeInHierarchy, mesh = mesh != null ? AssetDatabase.GetAssetPath(mesh) + "#" + mesh.name : "", materials = renderer.sharedMaterials.Select(m => m != null ? AssetDatabase.GetAssetPath(m) : "MISSING").ToArray(), materialEvidence = "serialized renderer binding; runtime overrides not evaluated", components = renderer.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray() });
            }
            containers.Add(new ContainerRecord { path = path, enabledBuildScene = enabled, renderedSize = bounds.size, rendererCount = renderers.Length, colliders = roots.Sum(r => r.GetComponentsInChildren<Collider>(true).Length), missingScripts = transforms.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) });
        }
    }
}
