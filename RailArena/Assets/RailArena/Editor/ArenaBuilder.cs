using System.Collections.Generic;
using RailArena.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RailArena.Editor
{
    public static class ArenaBuilder
    {
        private const string Folder = "Assets/RailArena/Generated";
        private static Material steel, trim, amber, cyan, dark;

        [MenuItem("Rail Arena/Build Foundry scene")]
        public static void Build()
        {
            BotTuningBuilder.EnsureAsset();
            System.IO.Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            steel = Material("Deck steel", new Color(.23f, .27f, .31f), false);
            trim = Material("Pale alloy", new Color(.55f, .61f, .65f), false);
            amber = Material("Amber energy", new Color(1, .28f, .035f), true);
            cyan = Material("Ion energy", new Color(.03f, .8f, 1), true);
            dark = Material("Blackened structure", new Color(.045f, .06f, .08f), false);
            var world = new GameObject("FOUNDRY // Orbital combat facility").AddComponent<ArenaWorld>();
            var decks = new GameObject("Architecture").transform;
            Deck(decks, new Vector3(0, 0, -18), new Vector3(48, 1, 12));
            Deck(decks, new Vector3(0, 0, 18), new Vector3(48, 1, 12));
            Deck(decks, new Vector3(-18, 0, 0), new Vector3(12, 1, 24));
            Deck(decks, new Vector3(18, 0, 0), new Vector3(12, 1, 24));
            Deck(decks, new Vector3(-18, 6, 0), new Vector3(12, 1, 10));
            Deck(decks, new Vector3(18, 6, 0), new Vector3(12, 1, 10));
            Deck(decks, new Vector3(0, 6, 0), new Vector3(24, 1, 6));
            for (int side = -1; side <= 1; side += 2)
            {
                Ramp(decks, new Vector3(side * 18, 0, -18), new Vector3(side * 18, 6, -4), 6);
                Ramp(decks, new Vector3(side * 18, 0, 18), new Vector3(side * 18, 6, 4), 6);
                Box(decks, "Reactor pylon", new Vector3(side * 22, 3, 0), new Vector3(2, 12, 3), dark);
                Box(decks, "Pylon luminous core", new Vector3(side * 20.95f, 4, 0), new Vector3(.12f, 8, 1), amber);
                for (int z = -1; z <= 1; z += 2)
                {
                    Box(decks, "Sightline cover", new Vector3(side * 8, 1.3f, z * 20), new Vector3(3, 2.6f, 3), steel);
                    Box(decks, "Cover cap", new Vector3(side * 8, 2.65f, z * 20), new Vector3(3.2f, .12f, 3.2f), trim);
                    Box(decks, "Warning strip", new Vector3(side * 8, 2, z * 18.47f), new Vector3(2.6f, .2f, .05f), amber);
                }
            }
            var nodes = new List<Transform>();
            var points = new[] { new Vector3(-18, 0, -18), new Vector3(0, 0, -18), new Vector3(18, 0, -18),
                new Vector3(18, 0, 18), new Vector3(0, 0, 18), new Vector3(-18, 0, 18),
                new Vector3(-18, 6, 0), new Vector3(0, 6, 0), new Vector3(18, 6, 0) };
            for (int i = 0; i < points.Length; i++)
            {
                var node = new GameObject($"Route {i:00}").transform;
                node.SetParent(world.transform);
                node.position = points[i] + Vector3.up * .08f;
                nodes.Add(node);
            }
            var links = new List<Vector2Int>();
            var walkLinks = new[] { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 8),
                new Vector2Int(8, 3), new Vector2Int(3, 4), new Vector2Int(4, 5), new Vector2Int(5, 6),
                new Vector2Int(6, 0), new Vector2Int(6, 7), new Vector2Int(7, 8) };
            foreach (var link in walkLinks) { links.Add(link); links.Add(new Vector2Int(link.y, link.x)); }
            var spawns = new List<Transform>();
            var spawnPoints = new[] { new Vector3(-16, .2f, -19), new Vector3(16, .2f, 19), new Vector3(-16, .2f, 19),
                new Vector3(16, .2f, -19), new Vector3(0, .2f, -19), new Vector3(0, .2f, 19),
                new Vector3(-17, 6.2f, 0), new Vector3(17, 6.2f, 0) };
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var spawn = new GameObject($"Spawn {i:00}").transform;
                spawn.SetParent(world.transform);
                spawn.position = spawnPoints[i];
                spawns.Add(spawn);
            }
            world.Spawns = spawns.ToArray();
            var pads = new List<JumpPad>();
            foreach (var point in new[] { new Vector3(-10, 0, -18), new Vector3(10, 0, 18), new Vector3(-10, 0, 18), new Vector3(10, 0, -18) })
            {
                var root = new GameObject("Ion launch pad");
                root.transform.SetParent(world.transform);
                root.transform.position = point;
                var pad = root.AddComponent<JumpPad>();
                pad.Destination = new Vector3(point.x < 0 ? -18 : 18, 6.2f, 0);
                var padNode = new GameObject($"Launch route {pads.Count}").transform;
                padNode.SetParent(world.transform);
                padNode.position = point + Vector3.up * .08f;
                int index = nodes.Count;
                nodes.Add(padNode);
                int lower = point.z < 0 ? 1 : 4;
                links.Add(new Vector2Int(lower, index));
                links.Add(new Vector2Int(index, lower));
                links.Add(new Vector2Int(index, point.x < 0 ? 6 : 8));
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Launch coil";
                disc.transform.SetParent(root.transform, false);
                disc.transform.localPosition = Vector3.up * .08f;
                disc.transform.localScale = new Vector3(2.8f, .08f, 2.8f);
                disc.GetComponent<Renderer>().sharedMaterial = cyan;
                Object.DestroyImmediate(disc.GetComponent<Collider>());
                pads.Add(pad);
            }
            world.Pads = pads.ToArray();
            world.Nodes = nodes.ToArray();
            world.Links = links.ToArray();
            var pickups = new List<HealthPickup>();
            foreach (var point in new[] { new Vector3(0, 1, -18), new Vector3(0, 1, 18), new Vector3(0, 7, 0) })
            {
                var root = new GameObject("Contested health +25");
                root.transform.SetParent(world.transform);
                root.transform.position = point;
                var pickup = root.AddComponent<HealthPickup>();
                Box(root.transform, "Health vertical", point, new Vector3(.25f, 1, .25f), cyan);
                Box(root.transform, "Health horizontal", point, new Vector3(.85f, .25f, .25f), cyan);
                foreach (var collider in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                pickup.Visuals = root.GetComponentsInChildren<Renderer>();
                pickups.Add(pickup);
            }
            world.Pickups = pickups.ToArray();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(39, 28, -43), Quaternion.Euler(25, -40, 0));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.008f, .014f, .035f);
            camera.farClipPlane = 500;
            camera.nearClipPlane = .04f;
            camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var light = new GameObject("Orbital sunlight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2;
            light.color = new Color(.65f, .78f, 1);
            light.transform.rotation = Quaternion.Euler(45, -25, 0);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.3f, .35f, .45f);
            var volume = new GameObject("Foundry color grade").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = new VolumeProfile();
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(.5f);
            bloom.threshold.Override(1);
            bloom.clamp.Override(10);
            bloom.dirtIntensity.Override(0);
            profile.Add<Vignette>().intensity.Override(.18f);
            Save(profile, "Foundry grading.asset");
            volume.sharedProfile = profile;
            Stars();
            var session = new GameObject("Game authority").AddComponent<ArenaSession>();
            session.World = world;
            session.Tuning = ScriptableObject.CreateInstance<MovementTuning>();
            Save(session.Tuning, "Movement.asset");
            session.PlayerMaterial = trim;
            session.BotMaterial = Material("Contender orange", new Color(.95f, .2f, .05f), false);
            session.RailMaterial = cyan;
            PlayerSettings.productName = "Rail Arena";
            PlayerSettings.companyName = "Evekiio";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            Physics.IgnoreLayerCollision(9, 9, true);
            Time.fixedDeltaTime = 1f / 60;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Folder + "/Foundry.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Folder + "/Foundry.unity", true) };
            AssetDatabase.SaveAssets();
            FoundryArtBuilder.Apply();
            Debug.Log($"Foundry scene created: {world.Pads.Length} launch pads, {world.Pickups.Length} health pickups, {world.Spawns.Length} spawns, {world.Nodes.Length} route nodes.");
        }

        private static void Stars()
        {
            var root = new GameObject("Orbital starfield").transform;
            var random = new System.Random(8271);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < 600; i++)
            {
                var direction = new Vector3((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1,
                    (float)random.NextDouble() * 2 - 1).normalized;
                Vector3 center = direction * 230;
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * (.07f + (float)random.NextDouble() * .2f);
                Vector3 up = Vector3.Cross(right, direction).normalized * right.magnitude;
                int offset = vertices.Count;
                vertices.Add(center - right - up); vertices.Add(center + right - up); vertices.Add(center + up);
                triangles.Add(offset); triangles.Add(offset + 2); triangles.Add(offset + 1);
                triangles.Add(offset); triangles.Add(offset + 1); triangles.Add(offset + 2);
            }
            var mesh = new Mesh { name = "Original seeded starfield" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            Save(mesh, "Starfield.asset");
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var stars = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Starlight" };
            stars.SetColor("_BaseColor", new Color(.55f, .68f, .85f));
            Save(stars, "Starlight.mat");
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial = stars;
        }

        private static void Deck(Transform parent, Vector3 top, Vector3 size)
        {
            Box(parent, "Load bearing deck", top - Vector3.up * size.y / 2, size, steel);
            Box(parent, "Underslung structure", top - Vector3.up * 1.2f, new Vector3(size.x * .94f, 1.4f, size.z * .94f), dark);
            Box(parent, "Amber perimeter", top - Vector3.up * .45f, new Vector3(size.x + .06f, .1f, size.z + .06f), amber);
            for (float x = -size.x / 2 + 2; x < size.x / 2; x += 4)
                Box(parent, "Deck seam", top + new Vector3(x, .008f, 0), new Vector3(.035f, .012f, size.z), dark);
            for (float z = -size.z / 2 + 2; z < size.z / 2; z += 4)
                Box(parent, "Deck seam", top + new Vector3(0, .008f, z), new Vector3(size.x, .012f, .035f), dark);
        }

        private static void Ramp(Transform parent, Vector3 from, Vector3 to, float width)
        {
            Vector3 direction = to - from;
            var ramp = Box(parent, "Access ramp", (from + to) / 2 - Vector3.up * .15f,
                new Vector3(width, .3f, direction.magnitude), steel);
            ramp.transform.rotation = Quaternion.LookRotation(direction);
            for (int side = -1; side <= 1; side += 2)
            {
                var line = Box(parent, "Ramp guidance", (from + to) / 2 + new Vector3(side * (width / 2 - .12f), .025f, 0),
                    new Vector3(.12f, .035f, direction.magnitude), cyan);
                line.transform.rotation = ramp.transform.rotation;
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static Material Material(string name, Color color, bool emission)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", emission ? 0 : .6f);
            material.SetFloat("_Smoothness", .35f);
            if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 3); }
            Save(material, name + ".mat");
            return material;
        }

        private static void Save(Object asset, string name)
        {
            string path = Folder + "/" + name;
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        public static void BuildWindows()
            => BuildWindowsTo("../Builds/Windows/RailArena.exe");

        public static void BuildWindowsTo(string output)
            => BuildWindowsPlayer(output, true, false);

        public static void BuildWindowsPlaytestTo(string output, bool nativeInputReview = false)
            => BuildWindowsPlayer(output, false, nativeInputReview);

        private static void BuildWindowsPlayer(string output, bool development, bool nativeInputReview)
        {
            var scenes = System.Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(scenes, scene => scene.path),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None,
                extraScriptingDefines = nativeInputReview ? new[] { "RAIL_ARENA_NATIVE_REVIEW" } : System.Array.Empty<string>()
            });
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + result.summary.result);
        }
    }
}
