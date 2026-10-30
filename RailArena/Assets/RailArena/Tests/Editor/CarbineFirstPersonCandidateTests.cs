using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CarbineFirstPersonCandidateTests
    {
        [UnityTest]
        public IEnumerator CompareCandidateOnActualThirdPersonLods()
        {
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Validation/CarbineCandidate/AO-white-background.png");
            var surface = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Art/PrecisionCarbine/Surface.mat");
            Assert.That(ao, Is.Not.Null); Assert.That(surface, Is.Not.Null);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            var session = Object.FindFirstObjectByType<ArenaSession>();
            session.StartSession(true, 1, 1, TargetScenario.Stationary);
            for (int i = 0; i < 3; i++) yield return null;
            session.enabled = false;
            var actor = session.Actors[1]; actor.Controller.enabled = false; actor.transform.position = Vector3.zero;
            actor.Presentation.Animator.speed = 0;
            var weapon = actor.Presentation.Railgun; var lodGroup = weapon.GetComponent<LODGroup>();
            var cameraObject = new GameObject("Carbine LOD audit camera"); var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.cullingMask = 1 << 8; camera.fieldOfView = 44;
            camera.transform.position = new Vector3(2.6f, 1.3f, 2.2f); camera.transform.LookAt(new Vector3(0, 1, .3f));
            var material = new Material(surface); material.SetTexture("_OcclusionMap", ao); material.SetFloat("_OcclusionStrength", 1); material.EnableKeyword("_OCCLUSIONMAP");
            var target = new RenderTexture(1280, 720, 24); var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); var oldActive = RenderTexture.active;
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/CarbineThirdPersonComparison-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
            try
            {
                target.Create();
                for (int level = 0; level < 3; level++)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Validation/CarbineCandidate/Carbine_LOD{level}.fbx");
                    Assert.That(model, Is.Not.Null);
                    var candidates = model.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.name, f => f.sharedMesh);
                    var filters = weapon.transform.Find("Carbine LOD" + level).GetComponentsInChildren<MeshFilter>(true);
                    Assert.That(filters.All(f => candidates.ContainsKey(f.name)), Is.True);
                    var originals = filters.Select(f => f.sharedMesh).ToArray(); var renderers = filters.Select(f => f.GetComponent<MeshRenderer>()).ToArray();
                    var oldMaterials = renderers.Select(r => r.sharedMaterials).ToArray();
                    Assert.That(oldMaterials.Sum(m => m.Count(x => x == surface)), Is.GreaterThan(0));
                    lodGroup.ForceLOD(level);
                    try
                    {
                        foreach (bool useCandidate in new[] { false, true })
                        {
                            for (int i = 0; i < filters.Length; i++)
                            {
                                filters[i].sharedMesh = useCandidate ? candidates[filters[i].name] : originals[i];
                                renderers[i].sharedMaterials = useCandidate ? oldMaterials[i].Select(m => m == surface ? material : m).ToArray() : oldMaterials[i];
                            }
                            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                            File.WriteAllBytes(Path.Combine(output, $"lod{level}-{(useCandidate ? "candidate" : "original")}.png"), image.EncodeToPNG());
                        }
                    }
                    finally { for (int i = 0; i < filters.Length; i++) { filters[i].sharedMesh = originals[i]; renderers[i].sharedMaterials = oldMaterials[i]; } }
                }
                Debug.Log("CARBINE_THIRD_PERSON_COMPARISON " + output);
            }
            finally
            {
                lodGroup.ForceLOD(-1); session.enabled = true; RenderTexture.active = oldActive;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); Object.DestroyImmediate(material); Object.DestroyImmediate(cameraObject);
            }
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CompareCandidateOnActualFirstPersonWeapon()
        {
            var candidateModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Validation/CarbineCandidate/Carbine_LOD0.fbx");
            var ao = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Validation/CarbineCandidate/AO-white-background.png");
            var surface = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Art/PrecisionCarbine/Surface.mat");
            Assert.That(candidateModel, Is.Not.Null); Assert.That(ao, Is.Not.Null); Assert.That(surface, Is.Not.Null);
            var candidateMeshes = candidateModel.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.name, f => f.sharedMesh);
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode();
            var session = Object.FindFirstObjectByType<ArenaSession>();
            session.StartSession(true, 0, 1, TargetScenario.Stationary);
            for (int i = 0; i < 4; i++) yield return null;
            session.enabled = false;
            var weapon = (Transform)typeof(ArenaSession).GetField("weapon", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            Assert.That(weapon, Is.Not.Null); Assert.That(weapon.gameObject.activeInHierarchy, Is.True);
            var lod = weapon.Find("Carbine LOD0"); Assert.That(lod, Is.Not.Null);
            var filters = lod.GetComponentsInChildren<MeshFilter>(true).Where(f => !f.name.StartsWith("RF_07_", StringComparison.Ordinal)).ToArray();
            Assert.That(filters.Length, Is.GreaterThan(0));
            Assert.That(filters.All(f => candidateMeshes.ContainsKey(f.name)), Is.True, "Candidate mesh names differ from the installed weapon.");
            var originals = filters.Select(f => f.sharedMesh).ToArray();
            var renderers = filters.Select(f => f.GetComponent<MeshRenderer>()).ToArray();
            var oldMaterials = renderers.Select(r => r.sharedMaterials).ToArray();
            int surfaceBindings = oldMaterials.Sum(materials => materials.Count(m => m == surface));
            Assert.That(surfaceBindings, Is.GreaterThan(0), "No installed surface materials would receive candidate AO.");
            var material = new Material(surface); material.SetTexture("_OcclusionMap", ao); material.SetFloat("_OcclusionStrength", 1); material.EnableKeyword("_OCCLUSIONMAP");
            var target = new RenderTexture(1280, 720, 24); var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active; var oldPosition = weapon.localPosition; var oldRotation = weapon.localRotation;
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/CarbineFirstPersonComparison-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
            try
            {
                target.Create();
                var sight = weapon.InverseTransformPoint(weapon.GetComponent<RailgunPresentation>().Aim.position);
                var ads = new AimDownSights(); ads.Step(true, 1);
                foreach (var view in new[] { ("hip", FirstPersonWeaponMotion.RestPosition), ("ads", ads.Position(sight, Vector3.zero, 0)) })
                {
                    weapon.localPosition = view.Item2; weapon.localRotation = Quaternion.identity;
                    foreach (bool useCandidate in new[] { false, true })
                    {
                        for (int i = 0; i < filters.Length; i++)
                        {
                            filters[i].sharedMesh = useCandidate ? candidateMeshes[filters[i].name] : originals[i];
                            renderers[i].sharedMaterials = useCandidate ? oldMaterials[i].Select(m => m == surface ? material : m).ToArray() : oldMaterials[i];
                        }
                        RenderPipeline.SubmitRenderRequest(session.View, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                        File.WriteAllBytes(Path.Combine(output, view.Item1 + (useCandidate ? "-candidate.png" : "-original.png")), image.EncodeToPNG());
                    }
                }
                Debug.Log($"CARBINE_FIRST_PERSON_COMPARISON meshes={filters.Length} surfaceBindings={surfaceBindings} opticExcluded=true output={output}");
            }
            finally
            {
                for (int i = 0; i < filters.Length; i++) { filters[i].sharedMesh = originals[i]; renderers[i].sharedMaterials = oldMaterials[i]; }
                weapon.localPosition = oldPosition; weapon.localRotation = oldRotation; session.enabled = true;
                RenderTexture.active = oldActive; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); Object.DestroyImmediate(material);
            }
            yield return new ExitPlayMode();
        }
    }
}
