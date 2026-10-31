using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class BridgeStoneArtReview
    {
        byte[] priorQuality;
        RenderPipelineAsset priorPipeline;

        [UnityTest]
        public IEnumerator InstalledBridgePreservesGeometryAndReturnDevices()
        {
            priorQuality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset");
            priorPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 0, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            session.enabled = false; session.View.enabled = false;
            var sources = session.World.GetComponentsInChildren<MeshRenderer>().Where(renderer =>
                renderer.name == "Bridge" || renderer.name == "Bridge-Screen1" || renderer.name == "Bridge-Screen-1"
                || renderer.name == "Underpass-Pier1" || renderer.name == "Underpass-Pier-1").ToArray();
            Assert.That(sources.Length, Is.EqualTo(5));
            var meshes = sources.ToDictionary(renderer => renderer, renderer => renderer.GetComponent<MeshCollider>().sharedMesh);
            var matrices = sources.ToDictionary(renderer => renderer, renderer => renderer.localToWorldMatrix);
            var colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var devices = session.World.Portals.Select(gate => gate.transform).Concat(session.World.Pads.Select(pad => pad.transform))
                .ToDictionary(transform => transform, transform => transform.localToWorldMatrix);
            var art = new GameObject("Owned bridge art review").AddComponent<ArtSlicePresentation>();
            yield return null; yield return null;
            var groups = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(group => group.name.EndsWith(" / Authored bridge stone", System.StringComparison.Ordinal)).ToArray();
            Assert.That(groups.Length, Is.EqualTo(5));
            CollectionAssert.AreEquivalent(colliders, Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var device in devices) Assert.That(device.Key.localToWorldMatrix, Is.EqualTo(device.Value));
            foreach (var source in sources)
            {
                Assert.That(source.enabled, Is.False);
                Assert.That(source.localToWorldMatrix, Is.EqualTo(matrices[source]));
                Assert.That(source.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(meshes[source]));
                var group = groups.Single(item => item.name == source.name + " / Authored bridge stone");
                Assert.That(group.GetComponentsInChildren<Collider>().Length, Is.Zero);
                int previous = int.MaxValue;
                Assert.That(group.GetLODs().Length, Is.EqualTo(3));
                foreach (var lod in group.GetLODs())
                {
                    var renderer = lod.renderers.Single();
                    var filter = renderer.GetComponent<MeshFilter>();
                    int triangles = filter.sharedMesh.triangles.Length / 3;
                    Assert.That(triangles, Is.LessThan(previous)); previous = triangles;
                    Assert.That((renderer.bounds.size-source.bounds.size).sqrMagnitude, Is.LessThan(.000001f));
                    Assert.That((renderer.bounds.center-source.bounds.center).sqrMagnitude, Is.LessThan(.000001f));
                    var envelope = source.bounds; envelope.Expand(.0002f);
                    foreach (var vertex in filter.sharedMesh.vertices)
                        Assert.That(envelope.Contains(filter.transform.TransformPoint(vertex)), Is.True, source.name + " visual must remain inside the source envelope");
                    Assert.That(renderer.sharedMaterials.All(material => material != null && material.shader.name.Contains("Cel")), Is.True);
                }
            }
            string output = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Environment/BridgeStone/UnityReview");
            Directory.CreateDirectory(output);
            var camera = new GameObject("Owned bridge capture camera").AddComponent<Camera>();
            camera.enabled = false; camera.cullingMask = (1 << 0) | (1 << 9);
            camera.fieldOfView = 65; camera.nearClipPlane = .08f; camera.farClipPlane = 1000;
            var target = new RenderTexture(1920, 1080, 24); target.Create();
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            float priorTimeScale = Time.timeScale; Time.timeScale = 0;
            var oldRenderers = session.World.GetComponentsInChildren<MeshRenderer>().Where(renderer => sources.Any(source => renderer == source
                || renderer.name.StartsWith(source.name + "-CAP", System.StringComparison.Ordinal)
                || renderer.name.StartsWith(source.name + "-INLAY", System.StringComparison.Ordinal))).ToArray();
            try
            {
                foreach (bool before in new[] { true, false })
                {
                    foreach (var group in groups) { group.gameObject.SetActive(!before); group.ForceLOD(0); }
                    foreach (var renderer in oldRenderers) renderer.enabled = before;
                    foreach (var view in new[] {
                        ("bridge-overview", new Vector3(10,11,10), new Vector3(0,4,0)),
                        ("bridge-gameplay", new Vector3(-5,5.6f,-1.5f), new Vector3(4,4.9f,-1.7f)),
                        ("underpass", new Vector3(0,1.6f,-7), new Vector3(0,2,0)),
                        ("underside", new Vector3(7,-1,7), new Vector3(0,3.6f,0)) })
                    {
                        camera.transform.position = view.Item2; camera.transform.LookAt(view.Item3);
                        yield return null;
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        var previousTarget = RenderTexture.active;
                        try
                        {
                            RenderTexture.active = target; pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                            File.WriteAllBytes(Path.Combine(output, view.Item1 + (before ? "-before.png" : "-after.png")), pixels.EncodeToPNG());
                        }
                        finally { RenderTexture.active = previousTarget; }
                    }
                }
                Object.Destroy(art); yield return null; yield return null;
                foreach (var source in sources) Assert.That(source.enabled, Is.True);
                foreach (var group in groups) Assert.That(group == null, Is.True);
                foreach (var device in devices) Assert.That(device.Key.localToWorldMatrix, Is.EqualTo(device.Value));
            }
            finally
            {
                Time.timeScale = priorTimeScale; target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(camera.gameObject);
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (priorQuality == null) yield break;
            QualitySettings.renderPipeline = priorPipeline;
            File.WriteAllBytes("ProjectSettings/QualitySettings.asset", priorQuality); AssetDatabase.Refresh();
        }
    }
}
