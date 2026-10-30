using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class ArtSliceLookCaptures
    {
        [Serializable] class View { public string id, path, note; public Vector3 position, euler; }
        [Serializable] class CaptureReport { public string scope = "Integrated art in serialized Sky Gardens basement scene using the production atmosphere pipeline, authored characters, rifle reload, stone, foliage, medical core and water. HUD is composed from actual uGUI geometry; native input, sound and final player acceptance remain open."; public View[] views; }
        RenderPipelineAsset priorPipeline;
        readonly List<View> views = new List<View>();
        string folder;
        Camera camera;
        Canvas canvas;

        [UnityTest]
        public IEnumerator CaptureCelLookPrototype()
        {
            folder = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Gate2/look-prototype"); Directory.CreateDirectory(folder);
            priorPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null; yield return null;
            new GameObject("Cel look prototype").AddComponent<ArtSlicePresentation>();
            yield return null; yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); camera = session.View; canvas = Object.FindAnyObjectByType<Canvas>();
            // Static art comparisons need stable detail. Change the temporary session, never saved preferences.
            session.Graphics.MotionBlur = false;
            session.Graphics.Apply(camera);
            var supportCheck = new Material(Resources.Load<CelArtProfile>("Art/SkyGardensCel").SurfaceShader);
            try
            {
                Assert.That(supportCheck.FindPass("ShadowCaster"), Is.GreaterThanOrEqualTo(0));
                Assert.That(supportCheck.FindPass("DepthNormals"), Is.GreaterThanOrEqualTo(0));
            }
            finally { Object.DestroyImmediate(supportCheck); }
            int authoredCoverCount = 0;
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name.EndsWith(" / Authored stone", StringComparison.Ordinal))
                { authoredCoverCount++; Assert.That(root.GetComponentsInChildren<Collider>(), Is.Empty); }
            Assert.That(authoredCoverCount, Is.EqualTo(4));
            // Change only the temporary session value, never the user's saved character preference.
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            yield return Capture("menu", "Actual current menu components; offscreen uGUI composition", true);
            session.StartSession(true, 3, 1, TargetScenario.Stationary); while (!session.Match.IsActive) session.Match.Advance();
            yield return null; yield return null; session.enabled = false;
            yield return Capture("fps-hud", "Current first-person rifle, hands and combat HUD", true);
            int originalMask = camera.cullingMask;
            camera.cullingMask = (originalMask | (1 << 8)) & ~(1 << 11);
            foreach (var pose in new[] {
                ("garden-bridge", new Vector3(25, 3, -14), new Vector3(0, 5, 0)),
                ("summit", new Vector3(14, 31, -16), new Vector3(0, 23, 0)),
                ("basement", new Vector3(43, -3, 43), new Vector3(32, -9, 32)),
                ("waterfall", new Vector3(-35, -6, 23), new Vector3(-31, -10, 16)) })
            {
                camera.transform.position = pose.Item2; camera.transform.LookAt(pose.Item3);
                yield return Capture(pose.Item1, "Fixed Editor baseline camera; visibility and gameplay acceptance are not inferred", false);
            }
            var gate = session.World.Portals[0]; camera.transform.position = gate.transform.position + gate.transform.forward * 3 + Vector3.up * 1.5f; camera.transform.LookAt(gate.transform.position + Vector3.up * 1.1f);
            gate.GetComponent<PortalView>().RenderView(camera);
            yield return Capture("portal", "Current live portal/device appearance", false);
            var pad = session.World.Pads[0]; camera.transform.position = pad.transform.position + new Vector3(3, 3, -4); camera.transform.LookAt(pad.transform.position);
            yield return Capture("pad", "Current jump-pad appearance", false);
            var pickup = session.World.Pickups[0]; camera.transform.position = pickup.transform.position + new Vector3(2, 1, -3); camera.transform.LookAt(pickup.transform.position);
            yield return Capture("health", "Current animated health pickup appearance, sampled frame", false);
            int availableTick = pickup.AvailableTick;
            pickup.SetAvailable(false);
            Assert.That(pickup.Presentation.FloatingRoot.gameObject.activeSelf, Is.False);
            Assert.That(pickup.Presentation.Marker.enabled, Is.False);
            yield return Capture("health-depleted", "Depleted pickup keeps its pedestal and status while the medical core disappears", false);
            pickup.SetAvailable(true);
            Assert.That(pickup.AvailableTick, Is.EqualTo(availableTick));
            camera.focalLength = camera.sensorSize.y / (2 * Mathf.Tan(55 * Mathf.Deg2Rad / 2));
            camera.transform.position = new Vector3(25.5f, 3.3f, -11); camera.transform.LookAt(new Vector3(29.5f, 3.2f, -16));
            yield return Capture("garden-canopy", "Opaque layered foliage inside the original leaf envelope; original tree collision retained", false);
            camera.focalLength = camera.sensorSize.y / (2 * Mathf.Tan(55 * Mathf.Deg2Rad / 2));
            camera.transform.position = new Vector3(24, 3.6f, 7.2f); camera.transform.LookAt(new Vector3(21, 2.7f, 11));
            yield return Capture("stone-after", "Original Blender cover in its unchanged collision footprint; cel material prototype", false);
            Object.Destroy(Object.FindAnyObjectByType<ArtSlicePresentation>()); yield return null; yield return null;
            yield return Capture("stone-before", "Identical camera and scene after removing only slice presentation", false);
            new GameObject("Cel look prototype").AddComponent<ArtSlicePresentation>(); yield return null; yield return null;
            var oldPositions = new Vector3[session.Actors.Count];
            camera.cullingMask |= 1 << 10;
            camera.focalLength = camera.sensorSize.y / (2 * Mathf.Tan(38 * Mathf.Deg2Rad / 2));
            for (int i = 0; i < session.Actors.Count; i++) { oldPositions[i] = session.Actors[i].transform.position; session.Actors[i].Controller.enabled = false; session.Actors[i].transform.position = new Vector3(1000 + i * 5, 0, 1000); }
            for (int i = 0; i < session.Actors.Count; i++)
            {
                var actor = session.Actors[i]; actor.transform.position = new Vector3(0, 0, 12); actor.transform.rotation = Quaternion.identity;
                camera.transform.position = new Vector3(2.2f, 1.5f, 15.2f); camera.transform.LookAt(new Vector3(0, 1, 12));
                yield return Capture(i == 0 ? "player" : "bot0" + i, "Staged current imported character and weapon in the existing arena; not a natural gameplay pose", false);
                bool presentationEnabled = actor.Presentation.enabled, animatorEnabled = actor.Presentation.Animator.enabled;
                actor.Presentation.enabled = false; actor.Presentation.Animator.enabled = false;
                yield return null;
                Vector3 face = HeadCenter(actor);
                var facing = Quaternion.Euler(0, actor.Command.Yaw, 0);
                camera.transform.position = face + facing * new Vector3(.4f, .06f, 1.2f); camera.transform.LookAt(face);
                camera.focalLength = camera.sensorSize.y / (2 * Mathf.Tan(22 * Mathf.Deg2Rad / 2));
                bool weaponVisible = actor.Presentation.Weapon.gameObject.activeSelf; actor.Presentation.Weapon.gameObject.SetActive(false);
                yield return Capture((i == 0 ? "player" : "bot0" + i) + "-face", "Current refined face, paint and selective silhouette contour; final human motion/likeness review pending", false);
                actor.Presentation.Weapon.gameObject.SetActive(weaponVisible);
                actor.Presentation.enabled = presentationEnabled; actor.Presentation.Animator.enabled = animatorEnabled;
                camera.focalLength = camera.sensorSize.y / (2 * Mathf.Tan(38 * Mathf.Deg2Rad / 2));
                actor.transform.position = new Vector3(1000 + i * 5, 0, 1000);
            }
            File.WriteAllText(Path.Combine(folder, "index.json"), JsonUtility.ToJson(new CaptureReport { views = views.ToArray() }, true));
            camera.cullingMask = originalMask;
            Debug.Log("ART_SLICE_LOOK_CAPTURES " + views.Count);
            yield return new ExitPlayMode();
        }

        static Vector3 HeadCenter(ArenaActor actor)
        {
            var skin = (SkinnedMeshRenderer)actor.Presentation.BodyRenderers[0];
            int head = Array.IndexOf(skin.bones, actor.Presentation.Animator.GetBoneTransform(HumanBodyBones.Head));
            Assert.That(head, Is.GreaterThanOrEqualTo(0));
                var vertices = skin.sharedMesh.vertices; var weights = skin.sharedMesh.boneWeights;
                var bindposes = skin.sharedMesh.bindposes;
                var matrices = new Matrix4x4[skin.bones.Length];
                for (int i = 0; i < matrices.Length; i++) matrices[i] = skin.bones[i].localToWorldMatrix * bindposes[i];
                var bounds = new Bounds(); bool first = true;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = weights[i];
                    float influence = (w.boneIndex0 == head ? w.weight0 : 0) + (w.boneIndex1 == head ? w.weight1 : 0) +
                        (w.boneIndex2 == head ? w.weight2 : 0) + (w.boneIndex3 == head ? w.weight3 : 0);
                    if (influence < .5f) continue;
                    var world = matrices[w.boneIndex0].MultiplyPoint3x4(vertices[i]) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(vertices[i]) * w.weight1 +
                        matrices[w.boneIndex2].MultiplyPoint3x4(vertices[i]) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(vertices[i]) * w.weight3;
                    if (first) { bounds = new Bounds(world, Vector3.zero); first = false; } else bounds.Encapsulate(world);
                }
                Assert.That(first, Is.False);
                Assert.That(Vector3.Distance(bounds.center, skin.bones[head].position), Is.LessThan(.4f), "Head reference must be in world metres");
                return bounds.center;
        }
        IEnumerator Capture(string identity, string note, bool ui)
        {
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
            try
            {
                target.Create(); camera.targetTexture = target;
                if (ui) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .2f; }
                yield return null; Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                if (ui) HeadlessUiReview.Draw(canvas, camera, target, false);
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder, identity + ".png"), pixels.EncodeToPNG());
                views.Add(new View { id = identity, path = "ArtSource/Original/ArtPass-v1/Gate2/look-prototype/" + identity + ".png", note = note, position = camera.transform.position, euler = camera.transform.eulerAngles });
            }
            finally
            {
                camera.targetTexture = oldTarget; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                RenderTexture.active = oldActive; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            }
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); QualitySettings.renderPipeline = priorPipeline; }
    }
}
