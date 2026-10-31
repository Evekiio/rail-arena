using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class StoneSubmissionArtReview
    {
        [Serializable] sealed class ImageEvidence
        {
            public string view;
            public Vector3 position, euler;
            public float fov;
            public double meanAbsoluteRgb;
            public int pixelsOverTen, maximumChannelDifference;
        }
        [Serializable] sealed class Report
        {
            public string scope = "Opt-in research only. All source triangle corners preserve local positions/normals/tangents/UVs and palette roles. Matching frozen native cameras check colour/normal/shadow presentation. All collider definitions/poses and LOD-group identities remain unchanged. Not a performance or artistic acceptance claim.";
            public int sourceModels, triangleCorners, sceneColliders, renderers, originalMaterialSlots, derivedMaterialSlots;
            public bool restored;
            public ImageEvidence[] images;
        }
        byte[] quality;
        RenderPipelineAsset pipeline;
        float timeScale;
        bool ownsTime;

        static int Verify(StoneSubmissionArt.Part part)
        {
            using var snapshot = MeshUtility.AcquireReadOnlyMeshData(part.source);
            var data = snapshot[0];
            using var positions = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            using var normals = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            using var tangents = new NativeArray<Vector4>(data.vertexCount, Allocator.Temp);
            data.GetVertices(positions); data.GetNormals(normals); data.GetTangents(tangents);
            var vertices = part.derived.vertices; var outputNormals = part.derived.normals; var outputTangents = part.derived.tangents;
            var colors = part.derived.colors; var properties = new List<Vector2>(); part.derived.GetUVs(3, properties);
            var output = part.derived.triangles;
            Assert.That(part.derived.subMeshCount, Is.EqualTo(1));
            Assert.That(part.derived.bounds, Is.EqualTo(part.source.bounds));
            var sourceUvs = new Dictionary<int, Vector4[]>(); var outputUvs = new Dictionary<int, List<Vector4>>();
            for (int channel = 0; channel < 8; channel++)
            {
                if (channel == 3 || !data.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0 + channel))) continue;
                using var values = new NativeArray<Vector4>(data.vertexCount, Allocator.Temp); data.GetUVs(channel, values);
                sourceUvs[channel] = values.ToArray(); outputUvs[channel] = new List<Vector4>(); part.derived.GetUVs(channel, outputUvs[channel]);
            }
            int cursor = 0;
            float positionError = 0, normalError = 0, tangentError = 0, uvError = 0, colorError = 0, propertyError = 0;
            for (int slot = 0; slot < data.subMeshCount; slot++)
            {
                using var indices = new NativeArray<int>(data.GetSubMesh(slot).indexCount, Allocator.Temp); data.GetIndices(indices, slot, true);
                var material = part.sourceMaterials[slot]; var tint = material.GetColor("_BaseColor");
                if (QualitySettings.activeColorSpace == ColorSpace.Linear) tint = tint.linear;
                var expected = new Vector2(material.IsKeywordEnabled("_NORMALMAP") ? 1 : 0, material.GetFloat("_HighlightStrength"));
                foreach (int index in indices)
                {
                    int target = output[cursor++];
                    positionError = Mathf.Max(positionError, Vector3.Distance(positions[index], vertices[target]));
                    normalError = Mathf.Max(normalError, Vector3.Distance(normals[index], outputNormals[target]));
                    tangentError = Mathf.Max(tangentError, Vector4.Distance(tangents[index], outputTangents[target]));
                    colorError = Mathf.Max(colorError, Vector4.Distance(tint, colors[target]));
                    propertyError = Mathf.Max(propertyError, Vector2.Distance(expected, properties[target]));
                    foreach (var pair in sourceUvs) uvError = Mathf.Max(uvError, Vector4.Distance(pair.Value[index], outputUvs[pair.Key][target]));
                }
            }
            Assert.That(cursor, Is.EqualTo(output.Length));
            Assert.That(Mathf.Max(positionError, normalError, tangentError, uvError, colorError, propertyError), Is.LessThan(.000001f), part.source.name);
            return cursor;
        }

        static Color32[] Capture(Camera camera, string path, RenderTexture target, Texture2D pixels, PortalView[] portals)
        {
            foreach (var portal in portals) portal.RenderView(camera);
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG()); return pixels.GetPixels32();
            }
            finally { RenderTexture.active = previous; }
        }

        [UnityTest]
        public IEnumerator PreserveGeometryLightingCollisionAndLods()
        {
            var art = Resources.Load<StoneSubmissionArt>("Art/StoneSubmissionResearch");
            Assert.That(art, Is.Not.Null); Assert.That(art.parts.Length, Is.GreaterThan(10));
            int corners = art.parts.Sum(Verify);
            quality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset"); pipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true, 3, 1, TargetScenario.Stationary); while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Stone submission native review").AddComponent<ArtSlicePresentation>(); yield return null; yield return null;
            session.enabled = false; session.View.enabled = false;
            foreach (var actor in session.Actors) { actor.Controller.enabled = false; actor.Presentation.enabled = false; actor.Presentation.Animator.enabled = false; }
            timeScale = Time.timeScale; ownsTime = true; Time.timeScale = 0; yield return null;
            var colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToDictionary(item => item, item => (EditorJsonUtility.ToJson(item), item.transform.localToWorldMatrix));
            var lods = Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToDictionary(item => item, item => EditorJsonUtility.ToJson(item));
            var camera = new GameObject("Owned stone comparison camera").AddComponent<Camera>(); camera.enabled = false;
            camera.fieldOfView = 100; camera.aspect = 16f / 9; camera.nearClipPlane = .05f; camera.farClipPlane = 400; camera.cullingMask = ~(1 << 11);
            var target = new RenderTexture(1920, 1080, 24); target.Create(); var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var candidate = new GameObject("Owned stone submission research").AddComponent<StoneSubmissionPresentation>();
            var portals = Object.FindObjectsByType<PortalView>(FindObjectsSortMode.None); var gate = session.World.Portals[0];
            var poses = new[] { ("combat", new Vector3(0, 16, -28), new Vector3(0, 5, 0)), ("summit", new Vector3(0, 37, -12), new Vector3(0, 26, 0)),
                ("basement", new Vector3(40, -14, 40), new Vector3(50, -20, 50)), ("foliage-waterfall", new Vector3(-22, 7, 8), new Vector3(-31, -10, 16)),
                ("portal", gate.transform.position + gate.transform.forward * 3 + Vector3.up * 1.5f, gate.transform.position + Vector3.up * 1.1f) };
            string folder = Path.GetFullPath("../Validation/StoneSubmissionNative-03"); Directory.CreateDirectory(folder);
            var images = new List<ImageEvidence>(); int rendererCount = 0, slots = 0;
            try
            {
                foreach (var pose in poses)
                {
                    camera.transform.position = pose.Item2; camera.transform.LookAt(pose.Item3);
                    var before = Capture(camera, Path.Combine(folder, pose.Item1 + "-before.png"), target, pixels, portals);
                    candidate.Apply(); rendererCount = candidate.ReplacedRenderers; slots = candidate.OriginalMaterialSlots;
                    Assert.That(rendererCount, Is.GreaterThan(20));
                    var after = Capture(camera, Path.Combine(folder, pose.Item1 + "-after.png"), target, pixels, portals);
                    long sum = 0; int maximum = 0, overTen = 0;
                    for (int index = 0; index < before.Length; index++)
                    {
                        int red = Math.Abs(before[index].r - after[index].r), green = Math.Abs(before[index].g - after[index].g), blue = Math.Abs(before[index].b - after[index].b);
                        int delta = Math.Max(red, Math.Max(green, blue)); sum += red + green + blue; maximum = Math.Max(maximum, delta); if (delta > 10) overTen++;
                    }
                    images.Add(new ImageEvidence { view = pose.Item1, position = camera.transform.position, euler = camera.transform.eulerAngles, fov = camera.fieldOfView,
                        meanAbsoluteRgb = sum / (double)(before.Length * 3), maximumChannelDifference = maximum, pixelsOverTen = overTen });
                    foreach (var pair in colliders) { Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value.Item1)); Assert.That(pair.Key.transform.localToWorldMatrix, Is.EqualTo(pair.Value.Item2)); }
                    foreach (var pair in lods) Assert.That(EditorJsonUtility.ToJson(pair.Key), Is.EqualTo(pair.Value));
                    candidate.Restore(); Assert.That(candidate.ReplacedRenderers, Is.Zero);
                }
                File.WriteAllText(Path.Combine(folder, "review.json"), JsonUtility.ToJson(new Report { sourceModels = art.parts.Length, triangleCorners = corners,
                    sceneColliders = colliders.Count, renderers = rendererCount, originalMaterialSlots = slots, derivedMaterialSlots = rendererCount, restored = true, images = images.ToArray() }, true));
                Assert.That(images.All(image => image.meanAbsoluteRgb < .1 && image.pixelsOverTen < 100), Is.True, string.Join("\n", images.Select(image => image.view + ": " + image.meanAbsoluteRgb + " / " + image.pixelsOverTen)));
            }
            finally { candidate.Restore(); Object.Destroy(candidate.gameObject); target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (ownsTime) { Time.timeScale = timeScale; ownsTime = false; }
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (quality != null) { QualitySettings.renderPipeline = pipeline; File.WriteAllBytes("ProjectSettings/QualitySettings.asset", quality); AssetDatabase.Refresh(); }
        }
    }
}
