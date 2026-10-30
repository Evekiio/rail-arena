using System.Collections.Generic;
using UnityEngine;

namespace RailArena
{
    /// <summary>Sky Gardens slice-only surface treatment. Source materials and simulation stay authoritative.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class ArtSlicePresentation : MonoBehaviour
    {
        public CelArtProfile Profile;
        readonly Dictionary<Material, Material> replacements = new();
        readonly Dictionary<Material, Material> originals = new();
        readonly HashSet<Renderer> surfaces = new();
        ArenaSession session;
        ArenaActor firstActor;
        int actorCount = -1;
        readonly Dictionary<Renderer, bool> hiddenCover = new();
        readonly List<GameObject> coverInstances = new();

        void Start()
        {
            session = FindFirstObjectByType<ArenaSession>();
            if (Profile == null) Profile = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            if (Profile == null || Profile.SurfaceShader == null) { Debug.LogError("Art slice cel profile is missing", this); enabled = false; return; }
            InstallStoneCover();
            Apply(FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        void LateUpdate()
        {
            if (session == null) return;
            var first = session.Actors.Count > 0 ? session.Actors[0] : null;
            if (actorCount == session.Actors.Count && firstActor == first) return;
            actorCount = session.Actors.Count; firstActor = first;
            foreach (var actor in session.Actors) Apply(actor.GetComponentsInChildren<Renderer>(true));
            Apply(session.View.GetComponentsInChildren<Renderer>(true));
        }

        void Apply(IEnumerable<Renderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    // Portal surfaces, particles, water and transparent specialists retain their shaders.
                    if (source == null || source.shader.name != "Universal Render Pipeline/Lit" || source.GetFloat("_Surface") > .5f) continue;
                    if (!replacements.TryGetValue(source, out var material))
                    {
                        material = new Material(source) { name = source.name + " / Cel slice" };
                        Profile.Configure(material, source);
                        replacements.Add(source, material); originals.Add(material, source);
                    }
                    materials[i] = material; changed = true;
                }
                if (changed) { renderer.sharedMaterials = materials; surfaces.Add(renderer); }
            }
        }

        void InstallStoneCover()
        {
            var prefab = Resources.Load<GameObject>("Art/StoneCover");
            if (prefab == null) return;
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (!renderer.name.StartsWith("Wing-LowCover", System.StringComparison.Ordinal) || renderer.name.Contains("-CAP") || renderer.name.Contains("-INLAY")) continue;
                if ((renderer.bounds.size - new Vector3(2, 1.2f, 1)).sqrMagnitude > .001f) continue;
                var instance = Instantiate(prefab, renderer.bounds.center, Quaternion.identity);
                instance.name = renderer.name + " / Authored stone";
                coverInstances.Add(instance);
                foreach (var original in renderers)
                    if (original.name == renderer.name || original.name.StartsWith(renderer.name + "-", System.StringComparison.Ordinal))
                    { hiddenCover[original] = original.enabled; original.enabled = false; }
            }
        }

        void OnDestroy()
        {
            foreach (var entry in hiddenCover) if (entry.Key != null) entry.Key.enabled = entry.Value;
            foreach (var instance in coverInstances) if (instance != null) Destroy(instance);
            foreach (var renderer in surfaces)
            {
                if (renderer == null) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && originals.TryGetValue(materials[i], out var source)) materials[i] = source;
                renderer.sharedMaterials = materials;
            }
            foreach (var material in replacements.Values)
                if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }
    }
}
