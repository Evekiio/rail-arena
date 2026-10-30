using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace RailArena.Editor
{
    public static class BasementRouteBuilder
    {
        public const string ScenePath = "Assets/RailArena/Generated/SkyGardensBasement.unity";
        public static void Build()
        {

            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var session = Object.FindAnyObjectByType<ArenaSession>(); var world = session.World;
            var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-structure.mat");
            var teal = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-teal.mat");
            if (stone == null || teal == null) throw new InvalidOperationException("Missing map materials");
            var root = new GameObject("Four summit to basement routes"); root.transform.SetParent(world.transform);
            var pads = world.Pads.ToList(); var gates = world.Portals.ToList();
            for (int i = 0; i < 4; i++)
            {
                var direction = Quaternion.Euler(0, 45 + i * 90, 0) * Vector3.forward;
                var route = new GameObject("Basement route " + (i + 1)).AddComponent<BasementRoute>(); route.transform.SetParent(root.transform); route.Direction = direction;
                Deck(route.transform, "Summit takeoff marker", direction * 6.1f + Vector3.up * 22.025f, .45f, teal, false);
                var routeLabel = new GameObject("Route label").AddComponent<TextMesh>(); routeLabel.transform.SetParent(route.transform);
                routeLabel.transform.position = direction * 6.1f + Vector3.up * 22.08f;
                routeLabel.transform.rotation = Quaternion.LookRotation(Vector3.down, direction);
                routeLabel.text = "BASEMENT " + (i + 1); routeLabel.anchor = TextAnchor.MiddleCenter; routeLabel.characterSize = .09f; routeLabel.fontSize = 48;
                routeLabel.color = new Color(.3f, 1, .9f);
                route.Staging = Deck(route.transform, "Staging", direction * 21 + Vector3.up * 14, 3, stone);
                route.Landing = Deck(route.transform, "Basement", direction * 45 + Vector3.up * -10, 6, stone);
                Deck(route.transform, "Landing marker", direction * 45 + Vector3.up * -9.98f, 2, teal, false);
                if (i % 2 == 0)
                {
                    var pad = Object.Instantiate(world.Pads[0], route.transform); pad.name = "Basement return bounce";
                    pad.transform.position = direction * 48 + Vector3.up * -10;
                    pad.Destination = direction * 2 + Vector3.up * 4.08f; pad.FlightSeconds = 2.6f;
                    foreach (var label in pad.GetComponentsInChildren<TextMesh>()) label.text = "RETURN // CENTER BRIDGE";
                    pads.Add(pad);
                }
                else
                {
                    var below = Object.Instantiate(world.Portals[0], route.transform); below.name = "Basement return portal";
                    var exit = Object.Instantiate(world.Portals[1], root.transform); exit.name = "Under bridge portal " + i;
                    var exitForward = i == 1 ? Vector3.right : Vector3.left;
                    below.transform.SetPositionAndRotation(direction * 48 + Vector3.up * -10, Quaternion.LookRotation(-exitForward));
                    exit.transform.SetPositionAndRotation(-exitForward * 3.1f, Quaternion.LookRotation(exitForward));
                    UnderbridgePortalArtBuilder.Mount(exit);
                    below.Exit = exit; exit.Exit = below; below.DestinationName = "UNDER CENTER BRIDGE"; exit.DestinationName = "BASEMENT " + (i + 1);
                    gates.Add(below); gates.Add(exit);
                }
            }
            world.Pads = pads.ToArray(); world.Portals = gates.ToArray(); world.LayoutName = "SKY GARDENS // BASEMENT ROUTES";
            Physics.SyncTransforms();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(SkyGardensBuilder.ScenePath, false) };
            AssetDatabase.SaveAssets(); Debug.Log("BASEMENT_ROUTES_BUILT four routes, two pads, two portal pairs");
        }
        static Transform Deck(Transform parent, string name, Vector3 top, float radius, Material material, bool collision = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = name; obj.transform.SetParent(parent);
            obj.transform.position = top - Vector3.up * .3f; obj.transform.localScale = new Vector3(radius * 2, .3f, radius * 2);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(obj.GetComponent<Collider>());
            else { Object.DestroyImmediate(obj.GetComponent<Collider>()); obj.AddComponent<MeshCollider>().sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh; }
            return obj.transform;
        }
    }
}





