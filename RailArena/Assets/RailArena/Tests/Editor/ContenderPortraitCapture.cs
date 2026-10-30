using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class ContenderPortraitCapture
    {
        [UnityTest]
        public IEnumerator RenderInstalledContenderPortraits()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.Graphics.MotionBlur = false; session.Graphics.Apply(session.View);
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            session.enabled = false;
            new GameObject("Contender art portrait set").AddComponent<ArtSlicePresentation>();
            yield return null; yield return null;
            Assert.That(session.Actors.Count, Is.EqualTo(4));
            var camera = new GameObject("Owned portrait camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f,.08f,.105f).linear;
            camera.cullingMask = 1 << 30; camera.fieldOfView = 38; camera.aspect = .8f; camera.nearClipPlane = .05f; camera.farClipPlane = 20;
            var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.antialiasingQuality = AntialiasingQuality.High;
            string folder = "Assets/Resources/UI/ArtPassPortraits"; Directory.CreateDirectory(folder);
            foreach (var actor in session.Actors)
            {
                actor.Controller.enabled = false; actor.Presentation.enabled = false;
                actor.Presentation.Weapon.gameObject.SetActive(false);
                actor.transform.SetPositionAndRotation(new Vector3(1000 + actor.Id * 8, 0, 1000), Quaternion.identity);
                actor.Presentation.transform.localRotation = Quaternion.identity;
                var animator = actor.Presentation.Animator;
                animator.SetFloat("Speed",0); animator.SetFloat("Gait",1); animator.SetBool("Grounded",true); animator.SetBool("Dead",false);
                animator.Play("Locomotion",0,.15f); animator.Update(0); ArmContactPose.AlignBody(animator); animator.enabled = false;
            }
            foreach (var actor in session.Actors)
            {
                foreach (var item in actor.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
                camera.transform.position = actor.transform.position + new Vector3(.45f,1.25f,2.7f);
                camera.transform.LookAt(actor.transform.position + Vector3.up * 1.1f);
                yield return null;
                var target = new RenderTexture(800,1000,24); var pixels = new Texture2D(800,1000,TextureFormat.RGB24,false);
                var old = RenderTexture.active;
                try
                {
                    target.Create(); camera.targetTexture = target;
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; pixels.ReadPixels(new Rect(0,0,800,1000),0,0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder,"Character-"+actor.CharacterId+".png"),pixels.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = null; RenderTexture.active = old; target.Release();
                    Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
                }
                foreach (var item in actor.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = actor == session.Human ? 10 : 8;
            }
            Debug.Log("CONTENDER_UI_PORTRAITS_RENDERED 4 x 800x1000 from installed refined geometry/materials; temporary studio pose");
            Object.Destroy(camera.gameObject);
            yield return new ExitPlayMode();
        }
    }
}
