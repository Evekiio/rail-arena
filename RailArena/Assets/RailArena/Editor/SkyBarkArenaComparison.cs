using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    public static class SkyBarkArenaComparison
    {
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bark.mat");
            var candidate = AssetDatabase.LoadAssetAtPath<Material>("Assets/Validation/SkyBarkCandidate/BarkCandidate.mat");
            if (original == null || candidate == null) throw new InvalidOperationException("Bronze materials missing");
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Contains(original)).ToArray();
            var renderer = renderers.FirstOrDefault(r => r.name == "TREE-1-BARK"); if (renderer == null) throw new InvalidOperationException("Target renderer missing");
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh; var vertices = mesh.vertices; var triangles = mesh.triangles;
            Vector3 point = default, normal = default; float area = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = renderer.transform.TransformPoint(vertices[triangles[i]]); var b = renderer.transform.TransformPoint(vertices[triangles[i + 1]]); var c = renderer.transform.TransformPoint(vertices[triangles[i + 2]]);
                var cross = Vector3.Cross(b - a, c - a); if (cross.magnitude <= area) continue;
                area = cross.magnitude; point = (a + b + c) / 3; normal = cross.normalized;
            }
            var camera = Camera.main; var oldTarget = camera.targetTexture; var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var oldActive = RenderTexture.active; var originals = renderers.Select(r => r.sharedMaterials).ToArray();
            var target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/SkyBarkArenaComparison"); Directory.CreateDirectory(output);
            try
            {
                foreach (float distance in new[] { .75f, 2.5f }) foreach (bool useCandidate in new[] { false, true })
                {
                    for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterials = originals[i].Select(m => m == original && useCandidate ? candidate : m).ToArray();
                    camera.transform.position = point + normal * distance; camera.transform.LookAt(point);
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                    string path = Path.Combine(output, (useCandidate ? "candidate" : "original") + "-" + (int)(distance * 100) + ".png");
                    if (File.Exists(path)) throw new IOException("Capture already exists: " + path); File.WriteAllBytes(path, image.EncodeToPNG());
                }
                Debug.Log("SKY_BARK_COMPARISON " + output + " renderer=" + renderer.name);
            }
            finally
            {
                for (int i = 0; i < renderers.Length; i++) renderers[i].sharedMaterials = originals[i];
                camera.targetTexture = oldTarget; camera.transform.SetPositionAndRotation(oldPosition, oldRotation); RenderTexture.active = oldActive;
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}

