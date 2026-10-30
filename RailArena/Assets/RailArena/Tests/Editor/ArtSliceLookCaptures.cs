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
        [Serializable] class CaptureReport { public string scope = "Cel material prototype in serialized Sky Gardens basement scene, PC pipeline. Not a completed Gate 2 slice. Conditional review-build rifle/water/atmosphere overrides are not applied. HUD is composed from actual uGUI geometry; not native input or player-build parity acceptance."; public View[] views; }
        readonly List<View> views = new List<View>();
        string folder;
        Camera camera;
        Canvas canvas;

        [UnityTest]
        public IEnumerator CaptureCelLookPrototype()
        {
            folder = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Gate2/look-prototype"); Directory.CreateDirectory(folder);
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
                actor.transform.position = new Vector3(1000 + i * 5, 0, 1000);
            }
            File.WriteAllText(Path.Combine(folder, "index.json"), JsonUtility.ToJson(new CaptureReport { views = views.ToArray() }, true));
            camera.cullingMask = originalMask;
            Debug.Log("ART_SLICE_LOOK_CAPTURES " + views.Count);
            yield return new ExitPlayMode();
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
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
