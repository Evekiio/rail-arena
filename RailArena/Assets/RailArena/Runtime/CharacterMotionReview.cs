#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RailArena.Editor
{
    public sealed class CharacterMotionReview : MonoBehaviour
    {
        /// <summary>Controlled, fixed-step visual proof. Not a gameplay or input acceptance test.</summary>
        public static string Capture()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode and start a one-bot stationary practice session first.");
            var session = UnityEngine.Object.FindFirstObjectByType<ArenaSession>();
            if (!session.Running || session.Actors.Count != 2) throw new InvalidOperationException("The motion review requires one bot.");
            var recorder = new GameObject("Controlled motion recorder").AddComponent<CharacterMotionReview>();
            recorder.StartCoroutine(recorder.Record(session));
            return "Recording through the normal player loop; wait for CharacterMotion/complete.txt.";
        }

        private IEnumerator Record(ArenaSession session)
        {
            var actor = session.Actors[1];
            var animator = actor.Presentation.Animator;
            int previousCaptureRate = Time.captureFramerate;
            Time.captureFramerate = 24;
            actor.Presentation.ReviewPlayback = true;
            bool wasPaused = session.Paused;
            Vector3 previousPosition = actor.transform.position;
            int previousHealth = actor.Health;
            session.SetPaused(true);
            actor.Place(new Vector3(0, .09f, -18));
            actor.Model.localRotation = Quaternion.identity;
            actor.Command = default;
            actor.Controller.enabled = false;
            actor.Model.GetComponent<LODGroup>().ForceLOD(0);
            var cameraObject = new GameObject("Motion review camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << 8;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .045f, .06f);
            camera.fieldOfView = 40;
            camera.transform.position = new Vector3(2.7f, 1.5f, -13.5f);
            camera.transform.LookAt(new Vector3(0, 1.3f, -18));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var fillObject = new GameObject("Motion review fill");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 12;
            fill.intensity = 8;
            fill.transform.position = new Vector3(1, 3, -15);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Motion review ground";
            floor.layer = 8;
            floor.transform.position = new Vector3(0, -.05f, -18);
            floor.transform.localScale = new Vector3(3, .1f, 3);
            floor.GetComponent<Renderer>().sharedMaterial = session.PlayerMaterial;
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            var render = new RenderTexture(640, 800, 24);
            render.Create();
            var pixels = new Texture2D(640, 800, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = render;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/CharacterMotion"));
            Directory.CreateDirectory(folder);
            File.Delete(Path.Combine(folder, "complete.txt"));
            int index = 0;
            try
            {
                foreach (string phase in new[] { "Idle", "Run", "Backwards", "Jump", "Death" })
                {
                    int frames = phase == "Idle" ? 24 : phase == "Death" ? 48 : 36;
                    actor.Health = phase == "Death" ? 0 : 100;
                    actor.Presentation.SetAlive(phase != "Death");
                    animator.Rebind();
                    animator.speed = 1;
                    animator.SetBool("Dead", phase == "Death");
                    animator.SetBool("Grounded", phase != "Jump");
                    animator.SetFloat("Gait", 1);
                    animator.SetFloat("Speed", phase == "Run" ? 9 : phase == "Backwards" ? -9 : 0);
                    foreach(var parameter in animator.parameters)
                    {
                        if(parameter.name=="MotionX")animator.SetFloat(parameter.nameHash,0);
                        if(parameter.name=="MotionZ")animator.SetFloat(parameter.nameHash,phase=="Run"?9:phase=="Backwards"?-9:0);
                    }
                    animator.Play(phase == "Death" ? "Base Layer.Death" : phase == "Jump" ? "Base Layer.Air" : "Base Layer.Locomotion", 0, 0);
                    for (int frame = 0; frame < frames; frame++)
                    {
                        actor.transform.position = new Vector3(0, .09f + (phase == "Jump" ? Mathf.Sin(frame / (float)(frames - 1) * Mathf.PI) * .8f : 0), -18);
                        yield return new WaitForEndOfFrame();
                        camera.Render();
                        RenderTexture.active = render;
                        pixels.ReadPixels(new Rect(0, 0, 640, 800), 0, 0);
                        pixels.Apply();
                        File.WriteAllBytes(Path.Combine(folder, $"frame-{index++:0000}.png"), pixels.EncodeToPNG());
                    }
                }
                File.WriteAllText(Path.Combine(folder, "README.txt"), "Controlled in-engine LOD0 pose review at 24 fps: idle 0-23, run 24-59, backwards 60-95, jump 96-131, death 132-179. The jump root arc is staged for inspection. This is not gameplay footage or native input validation.");
                File.WriteAllText(Path.Combine(folder, "complete.txt"), "180 frames captured through Unity's normal animation loop.");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
                UnityEngine.Object.DestroyImmediate(floor);
                actor.Health = previousHealth;
                actor.Presentation.ReviewPlayback = false;
                actor.SetAlive(previousHealth > 0);
                actor.Place(previousPosition);
                actor.Model.GetComponent<LODGroup>().ForceLOD(-1);
                animator.speed = 1;
                session.SetPaused(wasPaused);
                Time.captureFramerate = previousCaptureRate;
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}
#endif
