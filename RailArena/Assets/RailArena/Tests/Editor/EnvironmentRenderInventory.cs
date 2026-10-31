using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class EnvironmentRenderInventory
    {
        [Serializable] sealed class MaterialRecord
        {
            public int queue;
            public string id, name, shader;
            public bool instancing;
        }
        [Serializable] sealed class Surface
        {
            public string path, mesh, meshId, reason;
            public int vertices, triangles, submeshes, layer, lightmap;
            public uint renderingLayerMask;
            public bool enabled, active, forcedOff, propertyBlock, readable, staticBatch, gameObjectStatic;
            public string[] ancestorScripts;
            public string shadows, probes;
            public Vector3 center, size;
            public MaterialRecord[] materials;
        }
        [Serializable] sealed class Report
        {
            public string scope = "Installed source after Review23. All renderer/LOD candidates, not submitted draw calls, occlusion, GPU cost or proof of safe batching. Static candidates exclude known moving/device/actor/LOD/property-block/non-readable/non-cel surfaces; ancestor scripts and lighting state require review.";
            public Surface[] surfaces;
        }
        static string PathOf(Transform item) => item.parent == null ? item.name : PathOf(item.parent) + "/" + item.name;

        static Surface Read(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : (renderer as SkinnedMeshRenderer)?.sharedMesh;
            var materials = renderer.sharedMaterials;
            string reason = !renderer.gameObject.activeInHierarchy ? "inactive" : !renderer.enabled ? "disabled" : renderer.forceRenderingOff ? "forced-off" :
                renderer is not MeshRenderer ? "non-rigid-renderer" : mesh == null ? "no-mesh" :
                renderer.GetComponentInParent<ArenaActor>(true) != null ? "actor" :
                renderer.GetComponentInParent<RailgunPresentation>(true) != null ? "weapon" :
                renderer.GetComponentInParent<SkyGardenMotion>(true) != null ? "wind/water-motion" :
                renderer.GetComponentInParent<TraversalPresentation>(true) != null ? "traversal-effect" :
                renderer.GetComponentInParent<HealthPickupVisual>(true) != null ? "pickup-state" :
                renderer.GetComponentInParent<PortalGate>(true) != null ? "portal" :
                renderer.GetComponentInParent<JumpPad>(true) != null ? "pad" :
                renderer.GetComponentInParent<LODGroup>(true) != null ? "lod" :
                renderer.HasPropertyBlock() ? "property-block" : !mesh.isReadable ? "not-readable" :
                materials.Length != mesh.subMeshCount ? "material-slot-mismatch" :
                materials.Any(material => material == null || material.shader.name != "RailArena/CelSurface" || material.renderQueue > 2500) ? "non-opaque-cel" :
                renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly ? "shadow-only" :
                renderer.lightmapIndex >= 0 ? "lightmapped" : "static-candidate";
            return new Surface {
                path = PathOf(renderer.transform), mesh = mesh != null ? mesh.name : "", meshId = mesh != null ? mesh.GetEntityId().ToString() : "",
                vertices = mesh != null ? mesh.vertexCount : 0, triangles = mesh != null ? Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3) : 0,
                submeshes = mesh != null ? mesh.subMeshCount : 0, layer = renderer.gameObject.layer, renderingLayerMask = renderer.renderingLayerMask,
                lightmap = renderer.lightmapIndex, shadows = renderer.shadowCastingMode.ToString(), probes = renderer.lightProbeUsage.ToString(),
                enabled = renderer.enabled, active = renderer.gameObject.activeInHierarchy, forcedOff = renderer.forceRenderingOff,
                propertyBlock = renderer.HasPropertyBlock(), readable = mesh != null && mesh.isReadable, staticBatch = renderer.isPartOfStaticBatch,
                gameObjectStatic = renderer.gameObject.isStatic, center = renderer.bounds.center, size = renderer.bounds.size, reason = reason,
                ancestorScripts = renderer.GetComponentsInParent<MonoBehaviour>(true).Select(component => component.GetType().FullName).Distinct().ToArray(),
                materials = materials.Select(material => material == null ? new MaterialRecord() : new MaterialRecord {
                    id = material.GetEntityId().ToString(), name = material.name, shader = material.shader.name, queue = material.renderQueue, instancing = material.enableInstancing }).ToArray()
            };
        }

        [UnityTest]
        public IEnumerator CaptureInstalledRendererAndMaterialInventory()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Environment rendering inventory").AddComponent<ArtSlicePresentation>();
            yield return null;
            yield return null;
            var report = new Report { surfaces = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(Read).OrderBy(surface => surface.path).ToArray() };
            Assert.That(report.surfaces.Length, Is.GreaterThan(500));
            Assert.That(report.surfaces.Any(surface => surface.path.Contains("Authored garden nature")), Is.True);
            string folder = Path.GetFullPath("../Validation/EnvironmentRenderInventory-01");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "renderer-inventory.json"), JsonUtility.ToJson(report, true));
            Debug.Log("ENVIRONMENT_RENDER_INVENTORY_CAPTURED " + report.surfaces.Length + " renderer/LOD candidates; " + report.surfaces.Count(surface => surface.reason == "static-candidate") + " static candidates, not submitted draw calls");
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
