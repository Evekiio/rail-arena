using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Original cosmetic dressing for the saved arena. Never regenerates gameplay geometry.</summary>
    public static class FoundryArtBuilder
    {
        private const string Root = "Assets/RailArena/Art/Foundry";
        private const string Scene = "Assets/RailArena/Generated/Foundry.unity";
        private const string Dressing = "Foundry surface detailing";
        private static readonly Dictionary<Material, List<CombineInstance>> groups = new();
        private static Mesh cube;

        [MenuItem("Rail Arena/Apply Foundry surface art")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode before applying arena art.");
            if (EditorSceneManager.GetActiveScene().path != Scene) EditorSceneManager.OpenScene(Scene);
            var world = Object.FindFirstObjectByType<ArenaWorld>();
            if (world == null) throw new InvalidOperationException("The Foundry gameplay world is missing.");
            string before = GameplaySignature(world);
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var previous = GameObject.Find(Dressing);
            if (previous != null) Object.DestroyImmediate(previous);
            var root = new GameObject(Dressing);
            root.layer = 2;
            groups.Clear();
            GenerateSurfaces();
            cube = BeveledCube();
            var floor = Surface("Machined deck", new Color(.46f, .5f, .54f), .4f, .4f, true);
            var plate = Surface("Ceramic steel plating", new Color(.64f, .69f, .71f), .35f, .48f, true);
            var dark = Surface("Vent recess", new Color(.025f, .04f, .055f), .5f, .3f);
            var alloy = Surface("Edge alloy", new Color(.36f, .43f, .48f), .7f, .55f);
            var warning = Surface("Safety ochre", new Color(.85f, .4f, .065f), .2f, .35f);
            var energy = Surface("Launch field", new Color(.02f, .62f, .85f), .05f, .4f);
            energy.EnableKeyword("_EMISSION"); energy.SetColor("_EmissionColor", new Color(.02f, .8f, 1) * 2);
            string fieldPath = Root + "/Launch surface.mat";
            var field = AssetDatabase.LoadAssetAtPath<Material>(fieldPath);
            if (field == null) { field = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(field, fieldPath); }
            field.name = "Launch surface"; field.SetColor("_BaseColor", new Color(.015f, .34f, .55f)); EditorUtility.SetDirty(field);

            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.name == "Load bearing deck" || renderer.name == "Access ramp")
                {
                    // Cube local Y=.5 is the exact top face, including ramp slope and thickness.
                    var t = renderer.transform;
                    Vector3 a = t.TransformPoint(new Vector3(-.5f, .5f, -.5f)) + t.up * .004f;
                    Vector3 b = t.TransformPoint(new Vector3(-.5f, .5f, .5f)) + t.up * .004f;
                    Vector3 c = t.TransformPoint(new Vector3(.5f, .5f, .5f)) + t.up * .004f;
                    Vector3 d = t.TransformPoint(new Vector3(.5f, .5f, -.5f)) + t.up * .004f;
                    var mesh = Quad(a, b, c, d, Vector3.Distance(a, d) / 2, Vector3.Distance(a, b) / 2);
                    Add(floor, mesh, Matrix4x4.identity);
                }
                if (renderer.name == "Sightline cover")
                {
                    renderer.GetComponent<MeshFilter>().sharedMesh = cube;
                    renderer.sharedMaterial = plate;
                    CoverDetail(renderer.transform, dark, alloy, warning);
                }
                if (renderer.name == "Reactor pylon")
                {
                    renderer.GetComponent<MeshFilter>().sharedMesh = cube;
                    PylonDetail(renderer.transform, alloy, dark);
                }
            }
            foreach (var pad in world.Pads)
            {
                var old = pad.transform.Find("Launch coil");
                if (old != null) old.GetComponent<MeshRenderer>().enabled = false;
                Vector3 center = pad.transform.position + Vector3.up * .17f;
                Add(alloy, Ring(1.4f, 1.12f, 64), Matrix4x4.TRS(center, Quaternion.identity, Vector3.one));
                Add(dark, Ring(1.12f, 0, 64), Matrix4x4.TRS(center + Vector3.up * .002f, Quaternion.identity, Vector3.one));
                Add(field, Ring(.9f, 0, 64), Matrix4x4.TRS(center + Vector3.up * .004f, Quaternion.identity, Vector3.one));
                for (int arc = 0; arc < 8; arc++)
                    Add(energy, Ring(1.03f, .94f, 6, arc * 45 + 3, 39), Matrix4x4.TRS(center + Vector3.up * .005f, Quaternion.identity, Vector3.one));
                Vector3 toward = pad.Destination - pad.transform.position; toward.y = 0;
                var rotation = Quaternion.LookRotation(toward);
                for (int n = 0; n < 3; n++)
                {
                    Vector3 p = center + rotation * new Vector3(0, .008f, n * .3f - .4f);
                    Box(energy, p + rotation * new Vector3(-.13f, 0, 0), rotation * Quaternion.Euler(0, 35, 0), new Vector3(.08f, .012f, .38f));
                    Box(energy, p + rotation * new Vector3(.13f, 0, 0), rotation * Quaternion.Euler(0, -35, 0), new Vector3(.08f, .012f, .38f));
                }
            }
            var landing = Surface("Landing guide", new Color(.72f,.76f,.72f), .05f, .25f);
            foreach(var destination in world.Pads.Select(pad=>pad.Destination).Distinct())
            {
                var surfaces=Physics.RaycastAll(destination+Vector3.up,Vector3.down,3,1<<0,QueryTriggerInteraction.Ignore)
                    .Where(hit=>hit.collider.name=="Load bearing deck").OrderBy(hit=>hit.distance).ToArray();
                if(surfaces.Length==0)
                    throw new InvalidOperationException("A pad destination has no landing surface: "+destination);
                var center=surfaces[0].point+Vector3.up*.012f;
                foreach(float x in new[]{-1f,1f})
                    foreach(float z in new[]{-1f,1f})
                    {
                        Box(landing,center+new Vector3(x*.7f,0,z*.51f),Quaternion.identity,new Vector3(.045f,.008f,.42f));
                        Box(landing,center+new Vector3(x*.51f,0,z*.7f),Quaternion.identity,new Vector3(.42f,.008f,.045f));
                    }
            }
            foreach (var pair in groups)
            {
                var combined = new Mesh { name = pair.Key.name, indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(pair.Value.ToArray(), true, true);
                Save(combined, pair.Key.name + " mesh.asset");
                // Geometry is already combined per material; a second static batch is unnecessary.
                var obj = new GameObject(pair.Key.name) { layer = 2 };
                obj.transform.SetParent(root.transform, false);
                obj.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/" + pair.Key.name + " mesh.asset");
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
            foreach (var mesh in groups.Values.SelectMany(g => g).Select(g => g.mesh).Distinct())
                if (mesh != cube && !AssetDatabase.Contains(mesh)) Object.DestroyImmediate(mesh);
            groups.Clear();
            string after = GameplaySignature(world);
            if (before != after) throw new InvalidOperationException("Cosmetic dressing changed the gameplay signature. Scene was not saved.");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("../Validation");
            File.WriteAllText("../Validation/foundry-art-geometry.json", "{\n  \"before\": \"" + before + "\",\n  \"after\": \"" + after + "\",\n  \"unchanged\": true,\n  \"cosmeticColliders\": " + root.GetComponentsInChildren<Collider>().Length + "\n}\n");
        }

        private static void CoverDetail(Transform cover, Material dark, Material alloy, Material warning)
        {
            foreach (Vector3 outward in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                CoverFace(cover.position, outward, dark, alloy, warning);
        }

        private static void CoverFace(Vector3 center, Vector3 outward, Material dark, Material alloy, Material warning)
        {
            Quaternion q = Quaternion.LookRotation(outward);
            Vector3 front = center + outward * 1.505f;
            Box(dark, front + Vector3.up * .15f, q, new Vector3(2.3f, .65f, .025f));
            for (int i = 0; i < 11; i++)
                Box(alloy, front + q * new Vector3(i * .19f - .95f, .15f, .018f), q, new Vector3(.065f, .49f, .045f));
            Box(dark, front + Vector3.down * .85f, q, new Vector3(2.4f, .25f, .025f));
            for (int i = 0; i < 9; i++)
                Box(warning, front + q * new Vector3(i * .25f - 1, -.85f, .02f), q * Quaternion.Euler(0, 0, -30), new Vector3(.11f, .19f, .012f));
            for (int side = -1; side <= 1; side += 2)
                for (int row = -1; row <= 1; row += 2)
                    Box(alloy, front + q * new Vector3(side * 1.28f, row * .98f, .01f), q, new Vector3(.07f, .07f, .025f));
        }

        private static void PylonDetail(Transform pylon, Material alloy, Material dark)
        {
            for (int i = 0; i < 6; i++)
                Box(alloy, pylon.position + Vector3.up * (i * 1.9f - 4.7f), Quaternion.identity, new Vector3(1.99f, .14f, 2.99f));
            for (int side = -1; side <= 1; side += 2)
                Box(dark, pylon.position + new Vector3(side * .84f, 0, 0), Quaternion.identity, new Vector3(.16f, 11.8f, 2.98f));
        }

        private static void Box(Material material, Vector3 position, Quaternion rotation, Vector3 size) =>
            Add(material, cube, Matrix4x4.TRS(position, rotation, size));
        private static void Add(Material material, Mesh mesh, Matrix4x4 matrix)
        {
            if (!groups.TryGetValue(material, out var list)) groups[material] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mesh, transform = matrix });
        }

        private static Mesh Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float width, float length)
        {
            var mesh = new Mesh { vertices = new[] { a, b, c, d }, triangles = new[] { 0, 1, 2, 0, 2, 3 },
                uv = new[] { Vector2.zero, new Vector2(0, length), new Vector2(width, length), new Vector2(width, 0) } };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); return mesh;
        }

        private static Mesh Ring(float outer, float inner, int segments, float start = 0, float span = 360)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = (start + span * i / segments) * Mathf.Deg2Rad;
                foreach (float radius in new[] { inner, outer })
                { var p = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius); vertices.Add(p); uv.Add(new Vector2(p.x, p.z)); }
                if (i == segments) continue;
                int v = i * 2;
                triangles.AddRange(new[] { v, v + 1, v + 3, v, v + 3, v + 2 });
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); return mesh;
        }

        internal static Mesh BeveledCube()
        {
            // Intersect an inset cube with corner cuts. Flat normals retain crisp machined faces.
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            void Face(params Vector3[] points)
            {
                Vector3 normal = Vector3.zero; foreach (var p in points) normal += p;
                Vector3 center = normal / points.Length; normal.Normalize();
                Vector3 u = Vector3.Cross(Mathf.Abs(normal.y) > .9f ? Vector3.forward : Vector3.up, normal).normalized;
                Vector3 v = Vector3.Cross(normal, u);
                var ordered = points.OrderBy(p => Mathf.Atan2(Vector3.Dot(p - center, v), Vector3.Dot(p - center, u))).ToArray();
                int index = vertices.Count; vertices.AddRange(ordered);
                foreach (var p in ordered) uv.Add(new Vector2(Vector3.Dot(p, u) + .5f, Vector3.Dot(p, v) + .5f));
                for (int i = 1; i < points.Length - 1; i++) triangles.AddRange(new[] { index, index + i, index + i + 1 });
            }
            const float edge = .465f, outer = .5f;
            for (int axis = 0; axis < 3; axis++)
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 P(float x, float y) { var p = Vector3.zero; p[axis] = sign * outer; p[(axis + 1) % 3] = x; p[(axis + 2) % 3] = y; return p; }
                    Face(P(-edge, -edge), P(-edge, edge), P(edge, edge), P(edge, -edge));
                }
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int axis = 0; axis < 3; axis++)
                    {
                        Vector3 P(float a, float b, float c) { var p = Vector3.zero; p[axis] = a; p[(axis + 1) % 3] = b; p[(axis + 2) % 3] = c; return p; }
                        Face(P(-edge, x * outer, y * edge), P(edge, x * outer, y * edge), P(edge, x * edge, y * outer), P(-edge, x * edge, y * outer));
                    }
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        Face(new Vector3(x * outer, y * edge, z * edge), new Vector3(x * edge, y * outer, z * edge), new Vector3(x * edge, y * edge, z * outer));
            var mesh = new Mesh { name = "Original beveled module" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            Save(mesh, "Beveled module.asset");
            return AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Beveled module.asset");
        }

        private static Material Surface(string name, Color color, float metallic, float smoothness, bool textured = false)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.name = name; material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
            if (textured)
            {
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Panel color.png"));
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Panel normal.png"));
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Panel metal smoothness.png"));
                material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_BumpScale", .8f);
            }
            EditorUtility.SetDirty(material); return material;
        }

        private static void GenerateSurfaces()
        {
            const int size = 512;
            var height = new float[size * size]; var colors = new Color[size * size]; var masks = new Color[size * size]; var normals = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float edge = Mathf.Min(u, v, 1 - u, 1 - v);
                    float grain = ((x * 73856093u ^ y * 19349663u) & 1023) / 1023f;
                    bool gutter = edge < .014f;
                    float d = Vector2.Distance(new Vector2(u, v), new Vector2(u < .5f ? .055f : .945f, v < .5f ? .055f : .945f));
                    bool bolt = d < .012f;
                    float value = gutter ? .21f : bolt ? .37f : .79f + grain * .09f;
                    if (edge > .014f && edge < .021f) value += .12f;
                    int row = (int)(v * 32);
                    float treadX = Mathf.Abs(Mathf.Repeat(u * 16 + (row % 2) * .5f, 1) - .5f);
                    float treadY = Mathf.Abs(Mathf.Repeat(v * 32, 1) - .5f);
                    bool tread = treadX * 3 + treadY < .22f && edge > .04f && !bolt;
                    int index = y * size + x;
                    colors[index] = new Color(value, value, value, 1);
                    masks[index] = new Color(gutter ? .15f : .5f, 0, 0, .3f + grain * .12f);
                    height[index] = gutter ? 0 : bolt ? .002f : .006f + (tread ? .0008f : 0) + grain * .00005f;
                }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (height[y * size + (x + 1) % size] - height[y * size + (x + size - 1) % size]) * size / 4;
                    float dy = (height[((y + 1) % size) * size + x] - height[((y + size - 1) % size) * size + x]) * size / 4;
                    Vector3 n = new Vector3(-dx, -dy, 1).normalized;
                    normals[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                }
            WriteTexture("Panel color", colors, false, false);
            WriteTexture("Panel normal", normals, true, true);
            WriteTexture("Panel metal smoothness", masks, false, true);
        }

        private static void WriteTexture(string name, Color[] pixels, bool normal, bool linear)
        {
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false, linear);
            texture.SetPixels(pixels); texture.Apply();
            string path = Root + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !linear; importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 8; importer.maxTextureSize = 512; importer.SaveAndReimport();
        }

        private static void Save(Mesh mesh, string name)
        {
            string path = Root + "/" + name;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else
            {
                // CopySerialized updated CPU arrays but left old render buffers during regeneration.
                // Native mesh setters invalidate those buffers while preserving asset GUIDs.
                existing.Clear(); existing.indexFormat = mesh.indexFormat; existing.name = mesh.name;
                existing.vertices = mesh.vertices; existing.normals = mesh.normals;
                existing.tangents = mesh.tangents; existing.uv = mesh.uv;
                existing.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) existing.SetTriangles(mesh.GetTriangles(i), i);
                existing.bounds = mesh.bounds; existing.UploadMeshData(false);
                Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing);
            }
        }

        public static string GameplaySignature(ArenaWorld world)
        {
            var parts = new List<string>();
            string V(Vector3 v) => $"{v.x:R},{v.y:R},{v.z:R}";
            string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
            foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                string shape = collider is BoxCollider b ? V(b.center) + "/" + V(b.size) : collider.GetType().Name;
                parts.Add(PathOf(collider.transform) + "/" + shape + "/" + V(collider.transform.position) + "/" +
                    V(collider.transform.eulerAngles) + "/" + V(collider.transform.lossyScale) + "/" + collider.enabled + "/" + collider.isTrigger + "/" + collider.gameObject.layer);
            }
            foreach (var node in world.Nodes) parts.Add("node/" + node.name + "/" + V(node.position));
            foreach (var spawn in world.Spawns) parts.Add("spawn/" + spawn.name + "/" + V(spawn.position));
            foreach (var pad in world.Pads) parts.Add("pad/" + V(pad.transform.position) + "/" + V(pad.Destination));
            foreach (var pickup in world.Pickups) parts.Add("health/" + V(pickup.transform.position));
            foreach (var link in world.Links) parts.Add("link/" + link.x + "/" + link.y);
            parts.Sort(StringComparer.Ordinal);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", parts)))).Replace("-", "").ToLowerInvariant();
        }
    }
}
