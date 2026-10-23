#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Controlled view-weapon review, not gameplay footage or native-input acceptance.</summary>
    public sealed class FirstPersonMotionReview : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public int frames = 192;
            public int fps = 24;
            public float maximumLeftWristError, maximumRightWristError;
            public float maximumOffsetMetres, maximumRotationDegrees;
            public float maximumFingerRotationDegrees;
            public string scope = "Controlled equip/run/airborne/landing/look/recoil presentation, not native input or gameplay";
        }

        public static string Capture()
        {
            var session = Object.FindFirstObjectByType<ArenaSession>();
            if (!Application.isPlaying || session == null || session.Running)
                throw new InvalidOperationException("Enter Play mode at the main menu before recording view arms.");
            var recorder = new GameObject("View-arm motion recorder").AddComponent<FirstPersonMotionReview>();
            recorder.StartCoroutine(recorder.Record(session));
            return "Recording 192 frames through the normal animation loop; wait for ViewHandMotion/complete.txt.";
        }

        private IEnumerator Record(ArenaSession session)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/ViewHandMotion"));
            Directory.CreateDirectory(folder); File.Delete(Path.Combine(folder, "complete.txt"));
            int previousRate = Time.captureFramerate; Time.captureFramerate = 24;
            var cameraObject = new GameObject("View-arm review camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.CopyFrom(session.View);
            camera.enabled = false; camera.fieldOfView = 100;
            camera.transform.SetPositionAndRotation(new Vector3(-16, 1.66f, -19), Quaternion.Euler(0, 90, 0));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var gun = Object.Instantiate(Resources.Load<GameObject>("Weapons/Railgun"), camera.transform, false);
            gun.transform.localPosition = FirstPersonWeaponMotion.RestPosition;
            var weapon = gun.GetComponent<RailgunPresentation>();
            gun.GetComponent<LODGroup>().ForceLOD(0);
            var arms = FirstPersonArms.CreateOnWeapon(weapon);
            foreach (var part in gun.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 11;
            var render = new RenderTexture(1280, 720, 24); render.Create();
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active; camera.targetTexture = render;
            var report = new Report();
            var motion = new FirstPersonWeaponMotion();
            motion.Reset(true);
            try
            {
                yield return null; yield return null;
                for (int frame = 0; frame < report.frames; frame++)
                {
                    bool airborne = frame >= 72 && frame < 96;
                    var velocity = frame >= 24 && frame < 96 ? new Vector3(10, airborne ? -18 : -1, 0) : Vector3.zero;
                    var look = frame >= 120 && frame < 144 ? new Vector2(Mathf.Sin((frame - 120) / 24f * Mathf.PI * 2) * 8, 0) : Vector2.zero;
                    int age = frame < 144 ? 1000 : frame < 180 ? frame - 144 : frame - 180;
                    float recoil = Mathf.Max(0, 1 - age / 24f * 5);
                    arms.SetRecoil(recoil);
                    motion.Step(1f / report.fps, velocity, !airborne, look, recoil);
                    gun.transform.localPosition = FirstPersonWeaponMotion.RestPosition + motion.Offset - Vector3.forward * (recoil * RailgunPresentation.ViewRecoilTravel);
                    gun.transform.localRotation = motion.Rotation;
                    report.maximumOffsetMetres = Mathf.Max(report.maximumOffsetMetres, motion.Offset.magnitude);
                    report.maximumRotationDegrees = Mathf.Max(report.maximumRotationDegrees, Quaternion.Angle(Quaternion.identity, motion.Rotation));
                    weapon.SetCharge(Mathf.Clamp01(1 - age / 36f));
                    yield return new WaitForEndOfFrame();
                    report.maximumLeftWristError = Mathf.Max(report.maximumLeftWristError,
                        Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, weapon.LeftGrip.position));
                    report.maximumRightWristError = Mathf.Max(report.maximumRightWristError,
                        Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, weapon.RightGrip.position));
                    for (int i = 0; i < arms.Hands.Joints.Length; i++)
                        report.maximumFingerRotationDegrees = Mathf.Max(report.maximumFingerRotationDegrees,
                            Quaternion.Angle(arms.Hands.GripRotations[i], arms.Hands.Joints[i].localRotation));
                    camera.Render(); RenderTexture.active = render;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder, $"frame-{frame:0000}.png"), pixels.EncodeToPNG());
                }
                yield return HandCloseups(arms, weapon, camera, render, pixels, folder);
                File.WriteAllText(Path.Combine(folder, "review.json"), JsonUtility.ToJson(report, true));
                File.WriteAllText(Path.Combine(folder, "complete.txt"), "192 controlled presentation frames captured.");
            }
            finally
            {
                RenderTexture.active = previousTarget; camera.targetTexture = null; render.Release();
                Object.DestroyImmediate(render); Object.DestroyImmediate(pixels); Object.DestroyImmediate(cameraObject);
                Time.captureFramerate = previousRate; Object.Destroy(gameObject);
            }
        }

        private static IEnumerator HandCloseups(FirstPersonArms arms, RailgunPresentation weapon, Camera source,
            RenderTexture target, Texture2D pixels, string folder)
        {
            var root = new GameObject("Hand contact review camera");
            var camera = root.AddComponent<Camera>(); camera.CopyFrom(source); camera.enabled = false;
            camera.fieldOfView = 50; camera.nearClipPlane = .01f; camera.targetTexture = target;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            try
            {
                foreach (bool left in new[] { false, true })
                    foreach (float pull in new[] { 0f, 1f })
                    {
                        arms.SetRecoil(pull);
                        yield return new WaitForEndOfFrame();
                        var hand = arms.Animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                        var center = hand.position + weapon.transform.forward * .035f;
                        camera.transform.position = center + weapon.transform.right * (left ? -.28f : .28f) +
                            weapon.transform.up * .09f - weapon.transform.forward * .1f;
                        camera.transform.LookAt(center);
                        camera.Render(); RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                        File.WriteAllBytes(Path.Combine(folder, $"{(left ? "Left" : "Right")}-{(pull > 0 ? "Pull" : "Grip")}.png"), pixels.EncodeToPNG());
                    }
            }
            finally { camera.targetTexture = null; Object.DestroyImmediate(root); }
        }
    }
}
#endif
