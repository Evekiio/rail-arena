using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    /// <summary>Headless, disposable comparison of the audited carbine candidate.</summary>
    public static class CarbineCandidateReview
    {
        private const string Candidate = "Assets/Validation/CarbineCandidate/";
        private const string Current = "Assets/RailArena/Art/PrecisionCarbine/";

        public static void Run()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("A graphics device is required for comparison renders.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var filename in new[] { "AO.png", "AO-white-background.png" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Candidate + filename);
                if (importer == null) throw new InvalidOperationException("Candidate AO is missing: " + filename);
                importer.sRGBTexture = false;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            for (var level = 0; level < 3; level++)
            {
                var path = Candidate + "Carbine_LOD" + level + ".fbx";
                var modelImporter = (ModelImporter)AssetImporter.GetAtPath(path);
                modelImporter.importAnimation = false;
                modelImporter.animationType = ModelImporterAnimationType.None;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                modelImporter.isReadable = true;
                modelImporter.importCameras = modelImporter.importLights = false;
                modelImporter.SaveAndReimport();
                CompareModels(Current + "Carbine_LOD" + level + ".fbx", path);
            }
            // Opening an empty scene unloads unused assets; load the review materials afterward.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>(Candidate + "AO.png");
            var neutralAo = AssetDatabase.LoadAssetAtPath<Texture2D>(Candidate + "AO-white-background.png");
            var sourceSurface = AssetDatabase.LoadAssetAtPath<Material>(Current + "Surface.mat");
            var energy = AssetDatabase.LoadAssetAtPath<Material>(Current + "Energy.mat");
            if (ao == null || neutralAo == null || sourceSurface == null || energy == null)
                throw new InvalidOperationException("Required material or texture missing.");
            var surface = new Material(sourceSurface);
            try
            {
                surface.SetTexture("_OcclusionMap", ao);
                surface.SetFloat("_OcclusionStrength", 1f);
                surface.EnableKeyword("_OCCLUSIONMAP");
                if (surface.GetTexture("_OcclusionMap") != ao || !surface.IsKeywordEnabled("_OCCLUSIONMAP"))
                    throw new InvalidOperationException("AO material binding failed.");
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white * .55f;
                var light = new GameObject("Review light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = .8f;
                light.transform.rotation = Quaternion.Euler(35, -40, 0);
                var camera = new GameObject("Review camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f, .045f, .06f);
                camera.orthographic = true;
                camera.orthographicSize = .36f;
                camera.transform.position = new Vector3(1.1f, .55f, -.55f);
                camera.transform.LookAt(new Vector3(0, 0, 0));
                Render(Current + "Carbine_LOD0.fbx", sourceSurface, energy, camera, "Carbine-current-v2.png");
                Render(Candidate + "Carbine_LOD0.fbx", surface, energy, camera, "Carbine-AO-candidate-v2.png");
                surface.SetTexture("_OcclusionMap", neutralAo);
                Render(Candidate + "Carbine_LOD0.fbx", surface, energy, camera, "Carbine-AO-neutral-v2.png");
                Debug.Log("CARBINE_CANDIDATE_REVIEW_OK");
            }
            finally { UnityEngine.Object.DestroyImmediate(surface); }
        }

        private static void CompareModels(string originalPath, string candidatePath)
        {
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(originalPath);
            var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);
            if (original == null || candidate == null) throw new InvalidOperationException("Missing model");
            var before = original.GetComponentsInChildren<MeshFilter>().OrderBy(x => x.name).ToArray();
            var after = candidate.GetComponentsInChildren<MeshFilter>().OrderBy(x => x.name).ToArray();
            if (before.Length != after.Length) throw new InvalidOperationException("Mesh count differs");
            for (var i = 0; i < before.Length; i++)
            {
                var a = before[i]; var b = after[i];
                if (a.name != b.name || a.sharedMesh.triangles.Length != b.sharedMesh.triangles.Length ||
                    Vector3.Distance(a.transform.localPosition, b.transform.localPosition) > .0001f ||
                    Vector3.Distance(a.sharedMesh.bounds.size, b.sharedMesh.bounds.size) > .0001f)
                    throw new InvalidOperationException("Model structure differs at " + a.name + " / " + b.name);
                // UV seams can change Unity's imported vertex count without changing the mesh silhouette.
                if (a.sharedMesh.vertexCount != b.sharedMesh.vertexCount)
                    Debug.Log("CARBINE_CANDIDATE_VERTEX_SPLITS " + a.name + " original=" +
                        a.sharedMesh.vertexCount + " candidate=" + b.sharedMesh.vertexCount);
            }
            Debug.Log("CARBINE_CANDIDATE_STRUCTURE_OK " + candidatePath + " meshes=" + after.Length);
        }

        private static void Render(string path, Material surface, Material energy, Camera camera, string filename)
        {
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var target = new RenderTexture(1600, 900, 24);
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterials = Enumerable.Repeat(renderer.name.EndsWith("Energy") ? energy : surface,
                        renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount).ToArray();
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                var folder = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/CarbineRepackedAudited");
                File.WriteAllBytes(Path.Combine(folder, filename), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(model);
            }
        }
    }
}
