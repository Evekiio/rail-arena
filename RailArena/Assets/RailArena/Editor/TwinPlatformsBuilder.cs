using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Separate editable layout; never rewrites the preserved Foundry scene.</summary>
    public static class TwinPlatformsBuilder
    {
        public const string ScenePath = "Assets/RailArena/Generated/TwinPlatforms.unity";
        private const string Folder = "Assets/RailArena/Generated/TwinPlatformsArt";
        private static Material deck, dark, trim, blue, red, gold;

        [MenuItem("Rail Arena/Build Twin Platforms scene")]
        public static void Build()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            var session = Object.FindAnyObjectByType<ArenaSession>();
            var previous = session.World;
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            deck = Mat("Deck", new Color(.29f, .36f, .40f), false);
            dark = Mat("Structure", new Color(.055f, .08f, .10f), false);
            trim = Mat("Ceramic", new Color(.72f, .77f, .75f), false);
            blue = Mat("Blue gate", new Color(.025f, .42f, 1), true);
            red = Mat("Red gate", new Color(1, .055f, .025f), true);
            gold = Mat("Super pad", new Color(1, .57f, .04f), true);
            var world = new GameObject("FOUNDRY // Twin Platforms").AddComponent<ArenaWorld>();
            world.LayoutName = "TWIN PLATFORMS";
            world.MenuEye = new Vector3(56, 38, -58);
            world.MenuFieldOfView = 50;
            world.MenuLook = Quaternion.LookRotation(-world.MenuEye).eulerAngles;
            var pickups = new List<HealthPickup>();
            for (int side = -1; side <= 1; side += 2)
            {
                var clone = Object.Instantiate(previous.Pickups[0].gameObject, world.transform);
                clone.name = side < 0 ? "Blue health" : "Red health";
                clone.transform.position = new Vector3(side * 40, .92f, 0);
                var pickup = clone.GetComponent<HealthPickup>(); pickup.AvailableTick = 0; pickup.SetAvailable(true); pickups.Add(pickup);
            }
            Object.DestroyImmediate(previous.gameObject);
            foreach (string name in new[] { "Architecture", "Foundry surface detailing", "Foundry void-edge markings" })
            { var old = GameObject.Find(name); if (old != null) Object.DestroyImmediate(old); }
            session.World = world; world.Pickups = pickups.ToArray();
            Platform(world.transform, -34, blue, "BLUE PLATFORM");
            Platform(world.transform, 34, red, "RED PLATFORM");
            var nodes = new List<Transform>(); var links = new List<Vector2Int>();
            for (int side = 0; side < 2; side++)
            {
                float center = side == 0 ? -34 : 34;
                for (int z = 0; z < 3; z++) for (int x = 0; x < 3; x++)
                    nodes.Add(Point(world.transform, "Route " + nodes.Count, new Vector3(center - 10 + x * 10, .08f, -12 + z * 12)));
                int first = side * 9;
                for (int z = 0; z < 3; z++) for (int x = 0; x < 3; x++)
                {
                    int i = first + z * 3 + x;
                    if (x < 2) Both(links, i, i + 1);
                    if (z < 2) Both(links, i, i + 3);
                }
            }
            var pads = new List<JumpPad>();
            for (int side = -1; side <= 1; side += 2)
            {
                var root = new GameObject(side < 0 ? "Blue super jump pad" : "Red super jump pad"); root.transform.SetParent(world.transform);
                root.transform.position = new Vector3(side * 22, 0, 10);
                var pad = root.AddComponent<JumpPad>(); pad.Destination = new Vector3(-side * 32, .08f, 10); pad.FlightSeconds = 2.4f; pads.Add(pad);
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); disc.name = "Super launch coil"; disc.transform.SetParent(root.transform, false);
                disc.transform.localPosition = Vector3.up * .055f; disc.transform.localScale = new Vector3(2.7f, .05f, 2.7f);
                disc.GetComponent<Renderer>().sharedMaterial = gold; Object.DestroyImmediate(disc.GetComponent<Collider>());
                for (int i = 0; i < 3; i++)
                {
                    var arrow = Cube(root.transform, "Launch direction", new Vector3(-side * (.4f - i * .4f), .12f, 0), new Vector3(.12f, .04f, 1.1f), dark, false);
                    arrow.transform.rotation = Quaternion.Euler(0, -side * 35, 0);
                }
                Text(root.transform, "SUPER JUMP", new Vector3(side * 2, 1.8f, 1), Quaternion.Euler(0, side < 0 ? 90 : -90, 0), gold.color);
                var landing = GameObject.CreatePrimitive(PrimitiveType.Cylinder); landing.name = "Safe landing zone";
                landing.transform.SetParent(world.transform); landing.transform.position = pad.Destination - Vector3.up * .05f;
                landing.transform.localScale = new Vector3(4, .015f, 4); landing.GetComponent<Renderer>().sharedMaterial = side < 0 ? red : blue;
                Object.DestroyImmediate(landing.GetComponent<Collider>());
            }
            int leftPad = nodes.Count; nodes.Add(Point(world.transform, "Blue pad route", pads[0].transform.position + Vector3.up * .08f));
            int rightPad = nodes.Count; nodes.Add(Point(world.transform, "Red pad route", pads[1].transform.position + Vector3.up * .08f));
            int rightLanding = nodes.Count; nodes.Add(Point(world.transform, "Red landing route", pads[0].Destination));
            int leftLanding = nodes.Count; nodes.Add(Point(world.transform, "Blue landing route", pads[1].Destination));
            Both(links, 8, leftPad); Both(links, 15, rightPad); Both(links, 16, rightLanding); Both(links, 7, leftLanding);
            links.Add(new Vector2Int(leftPad, rightLanding)); links.Add(new Vector2Int(rightPad, leftLanding));
            var blueGate = Gate(world.transform, "BLUE PORTAL", new Vector3(-24, 0, -10), Vector3.left, blue);
            var redGate = Gate(world.transform, "RED PORTAL", new Vector3(24, 0, -10), Vector3.right, red);
            blueGate.Exit = redGate; redGate.Exit = blueGate;
            blueGate.DestinationName = "RED PLATFORM"; redGate.DestinationName = "BLUE PLATFORM";
            int blueRoute = nodes.Count; nodes.Add(Point(world.transform, "Blue portal approach", new Vector3(-26, .08f, -10)));
            int redRoute = nodes.Count; nodes.Add(Point(world.transform, "Red portal approach", new Vector3(26, .08f, -10)));
            Both(links, 1, blueRoute); Both(links, 10, redRoute); Both(links, blueRoute, redRoute);
            world.Portals = new[] { blueGate, redGate }; world.Pads = pads.ToArray(); world.Nodes = nodes.ToArray(); world.Links = links.ToArray();
            var spawns = new List<Transform>();
            foreach (var position in new[] { new Vector3(-44, .2f, 0), new Vector3(44, .2f, 0), new Vector3(-44, .2f, 14),
                new Vector3(44, .2f, -14), new Vector3(-34, .2f, -14), new Vector3(34, .2f, 14) })
                spawns.Add(Point(world.transform, "Spawn " + spawns.Count, position));
            world.Spawns = spawns.ToArray();
            Camera.main.transform.SetPositionAndRotation(world.MenuEye, Quaternion.Euler(world.MenuLook));
            TraversalEffectsBuilder.ApplyToWorld(world);
            Physics.SyncTransforms();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene("Assets/RailArena/Generated/Foundry.unity", false) };
            AssetDatabase.SaveAssets();
            Debug.Log("TWIN_PLATFORMS_BUILT gap=36m pads=2 portals=2 pickups=2 nodes=" + world.Nodes.Length);
        }

        private static void Platform(Transform world, float x, Material accent, string label)
        {
            var root = new GameObject(label).transform; root.SetParent(world);
            Cube(root, "Floating deck", new Vector3(x, -.7f, 0), new Vector3(32, 1.4f, 36), deck);
            Cube(root, "Floating understructure", new Vector3(x, -2.2f, 0), new Vector3(29, 2.1f, 33), dark);
            for (int z = -1; z <= 1; z += 2)
            {
                Cube(root, "Perimeter light", new Vector3(x, -.08f, z * 17.8f), new Vector3(32, .12f, .16f), accent, false);
                Cube(root, "Outer safety rail", new Vector3(x, .65f, z * 18), new Vector3(32, .15f, .2f), trim);
            }
            float outer = x + Mathf.Sign(x) * 16;
            Cube(root, "Outer safety rail", new Vector3(outer, .65f, 0), new Vector3(.2f, .15f, 36), trim);
            for (int z = -14; z <= 14; z += 4)
                Cube(root, "Deck panel joint", new Vector3(x, .003f, z), new Vector3(31, .006f, .025f), dark, false);
            foreach (var offset in new[] { new Vector2(-5, -6), new Vector2(5, 6), new Vector2(-5, 6), new Vector2(5, -6) })
            {
                Cube(root, "Cover", new Vector3(x + offset.x, 1.1f, offset.y), new Vector3(3, 2.2f, 3), dark);
                Cube(root, "Cover cap", new Vector3(x + offset.x, 2.25f, offset.y), new Vector3(3.1f, .1f, 3.1f), trim);
            }
            Cube(root, "Inner void warning", new Vector3(x - Mathf.Sign(x) * 15.8f, .02f, -3), new Vector3(.2f, .025f, 22), gold, false);
            Text(root, label, new Vector3(x, 4, 16), Quaternion.identity, Color.white);
        }

        private static PortalGate Gate(Transform world, string name, Vector3 position, Vector3 normal, Material material)
        {
            var root = new GameObject(name).transform; root.SetParent(world); root.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
            var portal = root.gameObject.AddComponent<PortalGate>();
            Cube(root, "Gate left pillar", new Vector3(-1.55f, 1.75f, 0), new Vector3(.3f, 3.5f, .5f), dark);
            Cube(root, "Gate right pillar", new Vector3(1.55f, 1.75f, 0), new Vector3(.3f, 3.5f, .5f), dark);
            Cube(root, "Gate header", new Vector3(0, 3.5f, 0), new Vector3(3.4f, .25f, .5f), trim);
            Cube(root, "Gate base", new Vector3(0, .04f, 0), new Vector3(3.4f, .08f, .5f), trim);
            Cube(root, "Solid gate backing", new Vector3(0, 1.75f, -.9f), new Vector3(3.4f, 3.5f, .3f), dark);
            var face = Cube(root, "Opaque shot-blocking portal", new Vector3(0, 1.7f, 0), new Vector3(2.8f, 3.3f, .06f), material);
            face.GetComponent<BoxCollider>().isTrigger = true;
            foreach (float side in new[] { -1.38f, 1.38f })
                Cube(root, "Portal light strip", new Vector3(side, 1.7f, .08f), new Vector3(.05f, 3.3f, .05f), material, false);
            foreach (float side in new[] { -1.38f, 1.38f })
                Cube(root, "Rear portal light strip", new Vector3(side, 1.7f, -1.07f), new Vector3(.05f, 3.3f, .05f), material, false);
            Text(root, name, new Vector3(0, 3.85f, -1.08f), Quaternion.identity, Color.white);
            Text(root, name, new Vector3(0, 3.85f, .08f), Quaternion.Euler(0, 180, 0), Color.white);
            return portal;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collision = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
        }
        private static Transform Point(Transform parent, string name, Vector3 position)
        { var obj = new GameObject(name).transform; obj.SetParent(parent); obj.position = position; return obj; }
        private static void Both(List<Vector2Int> links, int a, int b) { links.Add(new Vector2Int(a, b)); links.Add(new Vector2Int(b, a)); }
        private static Material Mat(string name, Color color, bool emission)
        {
            string path = Folder + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", .25f); material.SetFloat("_Smoothness", .35f);
            if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 2); }
            EditorUtility.SetDirty(material); return material;
        }
        private static void Text(Transform parent, string label, Vector3 position, Quaternion rotation, Color color)
        {
            var obj = new GameObject(label); obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localRotation = rotation;
            var text = obj.AddComponent<TextMesh>(); text.text = label; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.font = Resources.Load<Font>("UI/Brand/Fonts/BarlowCondensed-SemiBold"); text.fontSize = 48; text.characterSize = .1f; text.color = color;
            obj.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }
    }
}
