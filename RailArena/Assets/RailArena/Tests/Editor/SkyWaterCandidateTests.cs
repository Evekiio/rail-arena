using System;
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
    public sealed class SkyWaterCandidateTests
    {
        [UnityTest]
        public IEnumerator FindGroundedWaterfallSightlines()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            yield return null;
            Physics.SyncTransforms();
            var target = new Vector3(-31.1f, -2f, 16);
            var matches = new System.Collections.Generic.List<Vector3>();
            for (int x = -32; x <= 32; x += 2) for (int z = -22; z <= 22; z += 2)
            {
                if (!Physics.Raycast(new Vector3(x, 35, z), Vector3.down, out var ground, 36, 1 << 0, QueryTriggerInteraction.Ignore) || ground.normal.y < .8f) continue;
                Vector3 eye = ground.point + Vector3.up * 1.6f;
                if (Vector3.Distance(eye, target) < 2 || Physics.Linecast(eye, target, 1 << 0, QueryTriggerInteraction.Ignore)) continue;
                matches.Add(eye);
            }
            foreach (var eye in matches.OrderBy(p => Vector3.Distance(p, target)).Take(10)) Debug.Log($"SKY_WATER_GROUNDED_SIGHTLINE eye={eye:F3} distance={Vector3.Distance(eye, target):F2}");
            Debug.Log("SKY_WATER_GROUNDED_SIGHTLINES count=" + matches.Count);
            var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-water.mat");
            var foam = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foam.mat");
            var surfaces = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterial == water || r.sharedMaterial == foam).ToArray();
            var enabled = surfaces.Select(r => r.enabled).ToArray();
            var camera = Camera.main; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var targetTexture = new RenderTexture(128, 72, 24); var pixels = new Texture2D(128, 72, TextureFormat.RGB24, false); var oldActive = RenderTexture.active;
            var visible = new System.Collections.Generic.List<(Vector3 eye, int changed)>();
            try
            {
                targetTexture.Create();
                foreach (var eye in matches)
                {
                    camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
                    Color32[] Capture(bool show)
                    {
                        for (int i = 0; i < surfaces.Length; i++) surfaces[i].enabled = show && enabled[i];
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = targetTexture });
                        RenderTexture.active = targetTexture; pixels.ReadPixels(new Rect(0, 0, 128, 72), 0, 0); pixels.Apply();
                        return pixels.GetPixels32();
                    }
                    var withWater = Capture(true); var withoutWater = Capture(false); int changed = 0;
                    for (int y = 18; y < 54; y++) for (int x = 32; x < 96; x++)
                    {
                        int i = y * 128 + x;
                        int difference = Math.Abs(withWater[i].r - withoutWater[i].r) + Math.Abs(withWater[i].g - withoutWater[i].g) + Math.Abs(withWater[i].b - withoutWater[i].b);
                        if (difference > 8) changed++;
                    }
                    if (changed > 0) visible.Add((eye, changed));
                }
                foreach (var item in visible.OrderByDescending(v => v.changed).Take(10)) Debug.Log($"SKY_WATER_VISIBLE_SIGHTLINE eye={item.eye:F3} changed_pixels={item.changed}");
                Debug.Log("SKY_WATER_VISIBLE_SIGHTLINES count=" + visible.Count);
            }
            finally
            {
                for (int i = 0; i < surfaces.Length; i++) surfaces[i].enabled = enabled[i];
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation); RenderTexture.active = oldActive;
                targetTexture.Release(); Object.DestroyImmediate(targetTexture); Object.DestroyImmediate(pixels);
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CompareWaterInArena()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Validation/SkyWaterCandidate/LightResponsiveWater.shader");
            var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-water.mat");
            var foam = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-foam.mat");
            Assert.That(shader, Is.Not.Null); Assert.That(water, Is.Not.Null); Assert.That(foam, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            for (int i = 0; i < 3; i++) yield return null;
            var surfaces = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterial == water || r.sharedMaterial == foam).ToArray();
            Assert.That(surfaces.Length, Is.GreaterThan(0));
            var originals = surfaces.Select(r => r.sharedMaterial).ToArray();
            Material Candidate(Material source)
            {
                var result = new Material(shader); result.SetColor("_BaseColor", source.GetColor("_BaseColor")); result.SetFloat("_Foam", source.GetFloat("_Foam")); return result;
            }
            var newWater = Candidate(water); var newFoam = Candidate(foam);
            var camera = Camera.main; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var target = new RenderTexture(960, 540, 24); var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/SkyWaterComparison-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
            try
            {
                target.Create();
                foreach (var view in new[] {
                    ("exterior", new Vector3(-34, -1.5f, 16), new Vector3(-31, -1, 16)),
                    ("grounded-near", new Vector3(-28, 1.6f, 12), new Vector3(-31.1f, -2, 16)),
                    ("grounded-far", new Vector3(-4, 1.6f, 6), new Vector3(-31.1f, -2, 16)) })
                {
                    camera.transform.SetPositionAndRotation(view.Item2, Quaternion.LookRotation(view.Item3 - view.Item2));
                    foreach (bool useCandidate in new[] { false, true })
                    {
                        for (int i = 0; i < surfaces.Length; i++) surfaces[i].sharedMaterial = useCandidate ? (originals[i] == water ? newWater : newFoam) : originals[i];
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                        File.WriteAllBytes(Path.Combine(output, view.Item1 + (useCandidate ? "-candidate.png" : "-original.png")), pixels.EncodeToPNG());
                    }
                }
                Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
                Debug.Log($"SKY_WATER_COMPARISON surfaces={surfaces.Length} output={output}");
            }
            finally
            {
                for (int i = 0; i < surfaces.Length; i++) surfaces[i].sharedMaterial = originals[i];
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation); RenderTexture.active = previous;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); Object.DestroyImmediate(newWater); Object.DestroyImmediate(newFoam);
            }
            yield return new ExitPlayMode();
        }
    }
}
