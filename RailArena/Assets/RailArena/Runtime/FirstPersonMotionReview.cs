#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Controlled idle/recoil/charge review, not gameplay footage or firing-cadence acceptance.</summary>
    public sealed class FirstPersonMotionReview : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public int frames = 96;
            public int fps = 24;
            public float maximumLeftWristError, maximumRightWristError;
            public string scope = "Controlled in-engine idle/recoil/charge presentation, not native input or gameplay";
        }

        public static string Capture()
        {
            var session = Object.FindFirstObjectByType<ArenaSession>();
            if (!Application.isPlaying || session == null || session.Running)
                throw new InvalidOperationException("Enter Play mode at the main menu before recording view arms.");
            var recorder = new GameObject("View-arm motion recorder").AddComponent<FirstPersonMotionReview>();
            recorder.StartCoroutine(recorder.Record(session));
            return "Recording 96 frames through the normal animation loop; wait for ViewArmsMotion/complete.txt.";
        }

        private IEnumerator Record(ArenaSession session)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/ViewArmsMotion"));
            Directory.CreateDirectory(folder); File.Delete(Path.Combine(folder, "complete.txt"));
            int previousRate = Time.captureFramerate; Time.captureFramerate = 24;
            var cameraObject = new GameObject("View-arm review camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.CopyFrom(session.View);
            camera.enabled = false; camera.fieldOfView = 100;
            camera.transform.SetPositionAndRotation(new Vector3(-16, 1.66f, -19), Quaternion.Euler(0, 90, 0));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var gun = Object.Instantiate(Resources.Load<GameObject>("Weapons/Railgun"), camera.transform, false);
            gun.transform.localPosition = new Vector3(.33f, -.3f, .62f);
            var weapon = gun.GetComponent<RailgunPresentation>();
            gun.GetComponent<LODGroup>().ForceLOD(0);
            var arms = FirstPersonArms.CreateOnWeapon(weapon);
            foreach (var part in gun.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 11;
            var render = new RenderTexture(1280, 720, 24); render.Create();
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active; camera.targetTexture = render;
            var report = new Report();
            try
            {
                yield return null; yield return null;
                for (int frame = 0; frame < 96; frame++)
                {
                    int age = frame < 24 ? 1000 : frame < 60 ? frame - 24 : frame - 60;
                    float recoil = Mathf.Max(0, 1 - age / 24f * 5);
                    gun.transform.localPosition = new Vector3(.33f, -.3f, .62f - recoil * RailgunPresentation.ViewRecoilTravel);
                    weapon.SetCharge(Mathf.Clamp01(1 - age / 36f));
                    yield return new WaitForEndOfFrame();
                    report.maximumLeftWristError = Mathf.Max(report.maximumLeftWristError,
                        Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, weapon.LeftGrip.position));
                    report.maximumRightWristError = Mathf.Max(report.maximumRightWristError,
                        Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, weapon.RightGrip.position));
                    camera.Render(); RenderTexture.active = render;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder, $"frame-{frame:0000}.png"), pixels.EncodeToPNG());
                }
                File.WriteAllText(Path.Combine(folder, "review.json"), JsonUtility.ToJson(report, true));
                File.WriteAllText(Path.Combine(folder, "complete.txt"), "96 controlled presentation frames captured.");
            }
            finally
            {
                RenderTexture.active = previousTarget; camera.targetTexture = null; render.Release();
                Object.DestroyImmediate(render); Object.DestroyImmediate(pixels); Object.DestroyImmediate(cameraObject);
                Time.captureFramerate = previousRate; Object.Destroy(gameObject);
            }
        }
    }
}
#endif
