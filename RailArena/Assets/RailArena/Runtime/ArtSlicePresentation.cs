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
        readonly List<RifleReloadPresentation> rifleActions = new();
        readonly List<ContenderArtPresentation> contenderArt = new();
        ContenderArtSet characters;
        readonly Dictionary<HealthPickupVisual, Renderer> pickupMarkers = new();
        RifleSurfaceArt rifleSurface;
        readonly Dictionary<MeshFilter, Mesh> originalRifleMeshes = new();
        Material originalSkybox;
        ArenaWayfindingArt wayfinding;
        Material opticSurface;

        void Start()
        {
            session = FindFirstObjectByType<ArenaSession>();
            if (Profile == null) Profile = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            if (Profile == null || Profile.SurfaceShader == null) { Debug.LogError("Art slice cel profile is missing", this); enabled = false; return; }
            characters = Resources.Load<ContenderArtSet>("Art/Contenders");
            rifleSurface = Resources.Load<RifleSurfaceArt>("Art/RifleSurface");
            if (characters == null) throw new System.InvalidOperationException("Refined contender art set is missing");
            if (rifleSurface == null || Profile.WaterShader == null) throw new System.InvalidOperationException("Integrated rifle/water art is missing");
            originalSkybox = RenderSettings.skybox;
            if (Profile.CloudSkybox != null) RenderSettings.skybox = Profile.CloudSkybox;
            InstallStoneCover();
            InstallCanopies();
            InstallMedicalCores();
            wayfinding = gameObject.AddComponent<ArenaWayfindingArt>(); wayfinding.Initialize(session.World);
            foreach (var arms in session.View.GetComponentsInChildren<FirstPersonArms>(true))
            {
                BindRifle(arms.GetComponentInParent<RailgunPresentation>(true), null);
                BindCharacter(arms.gameObject).ApplyArms(arms, characters.variants[session.Human != null ? session.Human.CharacterId : session.SelectedOfflineCharacter]);
            }
            foreach (var actor in session.Actors)
                BindCharacter(actor.gameObject).ApplyBody(actor, characters.variants[actor.CharacterId]);
            Apply(FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        void LateUpdate()
        {
            if (session == null) return;
            var first = session.Actors.Count > 0 ? session.Actors[0] : null;
            if (actorCount == session.Actors.Count && firstActor == first) return;
            actorCount = session.Actors.Count; firstActor = first;
            foreach (var actor in session.Actors)
            {
                BindCharacter(actor.gameObject).ApplyBody(actor, characters.variants[actor.CharacterId]);
                Apply(actor.GetComponentsInChildren<Renderer>(true));
                BindRifle(actor.Presentation.Railgun, actor);
            }
            foreach (var arms in session.View.GetComponentsInChildren<FirstPersonArms>(true))
                BindCharacter(arms.gameObject).ApplyArms(arms, characters.variants[session.Human != null ? session.Human.CharacterId : session.SelectedOfflineCharacter]);
            Apply(session.View.GetComponentsInChildren<Renderer>(true));
        }

        ContenderArtPresentation BindCharacter(GameObject target)
        {
            var art = target.GetComponent<ContenderArtPresentation>();
            if (art == null) { art = target.AddComponent<ContenderArtPresentation>(); contenderArt.Add(art); }
            return art;
        }

        void BindRifle(RailgunPresentation weapon, ArenaActor owner)
        {
            if (weapon.GetComponent<RifleReloadPresentation>() != null) return;
            for (int level = 0; level < rifleSurface.models.Length; level++)
            {
                var lod = weapon.transform.Find("Carbine LOD" + level);
                if (lod == null) throw new System.InvalidOperationException("Carbine LOD is missing");
                var meshes = new Dictionary<string, Mesh>();
                foreach (var filter in rifleSurface.models[level].GetComponentsInChildren<MeshFilter>(true)) meshes[filter.name] = filter.sharedMesh;
                foreach (var filter in lod.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.name.StartsWith("RF_07_", System.StringComparison.Ordinal)) continue;
                    if (!meshes.TryGetValue(filter.name, out var mesh)) continue;
                    originalRifleMeshes.Add(filter, filter.sharedMesh); filter.sharedMesh = mesh;
                }
            }
            var action = weapon.gameObject.AddComponent<RifleReloadPresentation>();
            rifleActions.Add(action); action.Initialize(weapon, session, owner);
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
                    // Portal surfaces, particles and transparent specialists retain their shaders.
                    if (source == null) continue;
                    bool water = source.shader.name == "RailArena/SkyWater";
                    if (!water && (source.shader.name != "Universal Render Pipeline/Lit" || source.GetFloat("_Surface") > .5f)) continue;
                    // Optic geometry retains its original UVs and open first-person mesh, matching the prior review.
                    if (source == rifleSurface.sourceSurface && renderer.name.StartsWith("RF_07_", System.StringComparison.Ordinal))
                    {
                        if (opticSurface == null)
                        {
                            opticSurface = new Material(source) { name = "Cel optic / original UVs" };
                            Profile.Configure(opticSurface, source); originals.Add(opticSurface, source);
                        }
                        materials[i] = opticSurface; changed = true; continue;
                    }
                    if (!replacements.TryGetValue(source, out var material))
                    {
                        material = new Material(source) { name = source.name + " / Cel slice" };
                        if (water) material.shader = Profile.WaterShader;
                        else
                        {
                            Profile.Configure(material, source);
                            if (source == rifleSurface.sourceSurface)
                            { material.SetTexture("_OcclusionMap", rifleSurface.cavityAtlas); material.SetFloat("_OcclusionStrength", 1); }
                        }
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

        void InstallCanopies()
        {
            var prefab = Resources.Load<GameObject>("Art/Canopy");
            if (prefab == null) throw new System.InvalidOperationException("Measured garden canopy is missing");
            foreach (var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!renderer.name.StartsWith("TREE-", System.StringComparison.Ordinal) || !renderer.name.EndsWith("-LEAVES", System.StringComparison.Ordinal)) continue;
                var instance = Instantiate(prefab, renderer.bounds.center, Quaternion.identity);
                instance.name = renderer.name + " / Layered canopy";
                instance.transform.localScale = renderer.bounds.size;
                coverInstances.Add(instance); hiddenCover[renderer] = renderer.enabled; renderer.enabled = false;
            }
        }

        void InstallMedicalCores()
        {
            var prefab = Resources.Load<GameObject>("Art/MedicalCore");
            if (prefab == null) throw new System.InvalidOperationException("Measured medical core is missing");
            foreach (var pickup in session.World.Pickups)
            {
                var visual = pickup.Presentation;
                if (visual.FloatingRoot == null) throw new System.InvalidOperationException("Pickup float animation root is missing");
                foreach (var renderer in visual.FloatingRoot.GetComponentsInChildren<Renderer>(true))
                { hiddenCover[renderer] = renderer.enabled; renderer.enabled = false; }
                var instance = Instantiate(prefab, visual.FloatingRoot, false);
                instance.name = "Authored medical medallion";
                coverInstances.Add(instance); pickupMarkers.Add(visual, visual.Marker);
                visual.Marker = instance.GetComponentInChildren<MeshRenderer>(true);
                foreach (var pedestal in pickup.GetComponentsInChildren<Renderer>(true))
                    if (pedestal.name == "Faceted pedestal")
                    {
                        float currentBob = Mathf.Sin(visual.AnimationTime * 1.8f) * .14f;
                        float lift = pedestal.bounds.max.y + .17f - (visual.Marker.bounds.min.y - currentBob);
                        instance.transform.localPosition += visual.FloatingRoot.InverseTransformVector(Vector3.up * lift);
                        break;
                    }
                visual.Marker.enabled = visual.Available;
            }
        }

        void OnDestroy()
        {
            if (Profile != null && RenderSettings.skybox == Profile.CloudSkybox) RenderSettings.skybox = originalSkybox;
            if (wayfinding != null) Destroy(wayfinding);
            foreach (var action in rifleActions) if (action != null) Destroy(action);
            foreach (var entry in originalRifleMeshes) if (entry.Key != null) entry.Key.sharedMesh = entry.Value;
            foreach (var character in contenderArt) if (character != null) Destroy(character);
            foreach (var entry in pickupMarkers) if (entry.Key != null) entry.Key.Marker = entry.Value;
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
            if (opticSurface != null)
                if (Application.isPlaying) Destroy(opticSurface); else DestroyImmediate(opticSurface);
        }
    }
}
