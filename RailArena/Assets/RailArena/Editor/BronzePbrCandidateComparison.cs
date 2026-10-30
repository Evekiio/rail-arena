using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    public static class BronzePbrCandidateComparison
    {
        public static void Capture()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bronze.mat");
            var all = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var renderers = all.OfType<MeshRenderer>().Where(r => r.sharedMaterials.Contains(material)).ToArray();
            MeshRenderer renderer = null; Vector3 center = default, normal = default; float maxArea = 0;
            foreach (var candidate in renderers)
            {
                var mesh = candidate.GetComponent<MeshFilter>().sharedMesh; var vertices = mesh.vertices; var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = candidate.transform.TransformPoint(vertices[triangles[i]]); var b = candidate.transform.TransformPoint(vertices[triangles[i + 1]]); var c = candidate.transform.TransformPoint(vertices[triangles[i + 2]]);
                    var cross = Vector3.Cross(b - a, c - a); float area = cross.magnitude;
                    if (area <= maxArea) continue;
                    renderer = candidate; maxArea = area; center = (a + b + c) / 3; normal = cross.normalized;
                }
            }
            if (renderer == null || maxArea <= 0) throw new Exception("No bronze triangle to capture");
            var states = all.Select(r => r.enabled).ToArray(); var previousMaterials = renderer.sharedMaterials;
            var comparisonMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Validation/BronzePbrCandidate/BronzeCandidate.mat"); if (comparisonMaterial == null) throw new Exception("Candidate missing");
            var lightObject = new GameObject("Bronze diagnostic light");
            var diagnosticLight = lightObject.AddComponent<Light>(); diagnosticLight.type = LightType.Directional; diagnosticLight.intensity = 2;
            var tangent = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .9f ? Vector3.right : Vector3.up).normalized;
            lightObject.transform.rotation = Quaternion.LookRotation((-normal + tangent * .6f).normalized);
            var camera = Camera.main; var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var old = RenderTexture.active; var previous = camera.targetTexture;
            try
            {
                for (int i = 0; i < all.Length; i++) all[i].enabled = all[i] == renderer;

                target.Create(); camera.targetTexture = target; camera.transform.position = center + normal * Mathf.Clamp(Mathf.Sqrt(maxArea) * 1.5f, .5f, 5); camera.transform.LookAt(center);
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/BronzePbrCandidateComparison")); Directory.CreateDirectory(output);
                foreach (bool enabled in new[] { false, true })
                {
                    renderer.sharedMaterials = previousMaterials.Select(m => m == material && enabled ? comparisonMaterial : m).ToArray();
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, enabled ? "Candidate.png" : "Original.png"), image.EncodeToPNG());
                }
                Debug.Log("BRONZE_PBR_COMPARISON renderer=" + renderer.name + " output=" + output);
            }
            finally { for (int i = 0; i < all.Length; i++) all[i].enabled = states[i]; renderer.sharedMaterials = previousMaterials; camera.targetTexture = previous; RenderTexture.active = old; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(lightObject); }
        }
    }
}

