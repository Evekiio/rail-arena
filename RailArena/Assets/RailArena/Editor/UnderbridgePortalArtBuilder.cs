using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    public static class UnderbridgePortalArtBuilder
    {
        public const float MountingMargin = .03f;
        static float ProjectedExtent(Bounds bounds, Vector3 normal) =>
            Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z)));

        // Keep the field outside the pier and cross the entry plane before the capsule touches stone.
        // This is an authoring operation; runtime movement and portal crossing rules stay authoritative.
        public static void Mount(PortalGate gate)
        {
            string pierName = gate.transform.position.x < 0 ? "Underpass-Pier-1" : "Underpass-Pier1";
            var pier = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Single(renderer => renderer.name == pierName);
            var normal = gate.transform.forward;
            if (Mathf.Abs(Vector3.Dot(normal, Vector3.right)) < .999f)
                throw new InvalidOperationException("Under-bridge wall mounting requires an X-aligned gate");
            float face = Vector3.Dot(pier.bounds.center, normal) + ProjectedExtent(pier.bounds, normal);
            var surface = gate.Presentation.Surface.bounds;
            float rear = Vector3.Dot(surface.center, normal) - ProjectedExtent(surface, normal);
            float entry = Vector3.Dot(gate.transform.position, normal) + gate.EntryOffset;
            float skin = new Simulation.MovementTuning().SkinWidth;
            float offset = Mathf.Max(0, face + MountingMargin - rear,
                face + ArenaActor.StandingRadius + skin + MountingMargin - entry);
            if (offset <= .00001f) return;
            gate.transform.position += normal * offset;
            EditorUtility.SetDirty(gate.transform);
        }
        public static void Prepare()
        {
            EditorSceneManager.OpenScene(BasementRouteBuilder.ScenePath);
            foreach (var gate in Object.FindObjectsByType<PortalGate>(FindObjectsSortMode.None)
                .Where(gate => gate.name.StartsWith("Under bridge", StringComparison.Ordinal))) Mount(gate);
            Physics.SyncTransforms();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("UNDERBRIDGE_PORTALS_MOUNTED");
        }
        [Serializable] sealed class Part
        {
            public string name;
            public Vector3 position, size, minimum, maximum;
        }
        [Serializable] sealed class Gate
        {
            public string name;
            public Vector3 position, forward, surfacePosition, surfaceSize;
            public float entryOffset, halfWidth, height;
            public Part[] frame;
        }
        [Serializable] sealed class Report
        {
            public string scope = "Original scene-space under-bridge portal and pier bounds in metres; no runtime placement changes.";
            public Gate[] gates;
            public Part[] piers;
        }
        static Part Describe(Renderer renderer) => new()
        {
            name = renderer.name, position = renderer.transform.position,
            size = renderer.bounds.size, minimum = renderer.bounds.min, maximum = renderer.bounds.max
        };
        public static void Audit()
        {
            EditorSceneManager.OpenScene(BasementRouteBuilder.ScenePath);
            Physics.SyncTransforms();
            var report = new Report
            {
                gates = Object.FindObjectsByType<PortalGate>(FindObjectsSortMode.None)
                    .Where(gate => gate.name.StartsWith("Under bridge", StringComparison.Ordinal))
                    .OrderBy(gate => gate.name).Select(gate => new Gate
                    {
                        name = gate.name, position = gate.transform.position, forward = gate.transform.forward,
                        surfacePosition = gate.Presentation.Surface.bounds.center, surfaceSize = gate.Presentation.Surface.bounds.size,
                        entryOffset = gate.EntryOffset, halfWidth = gate.HalfWidth, height = gate.Height,
                        frame = gate.GetComponentsInChildren<Renderer>().Select(Describe).ToArray()
                    }).ToArray(),
                piers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                    .Where(renderer => renderer.name.StartsWith("Underpass-Pier", StringComparison.Ordinal))
                    .OrderBy(renderer => renderer.name).Select(Describe).ToArray()
            };
            string output = Path.GetFullPath("../Validation/underbridge-original-bounds.json");
            if (File.Exists(output)) throw new IOException("Preserve the original bounds audit: " + output);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("UNDERBRIDGE_BOUNDS_RECORDED " + output);
        }
    }
}
