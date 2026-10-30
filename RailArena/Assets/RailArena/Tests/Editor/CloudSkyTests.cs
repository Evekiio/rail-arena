using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CloudSkyTests
    {
        [Test]
        public void SkyRendersFiniteContinuousAndStationaryAtWrapAndPoles()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            var sky=RenderSettings.skybox;
            Assert.That(sky.shader.name,Is.EqualTo("Rail Arena/Foundry Cloud Sky"));
            Assert.That(sky.shader.isSupported,Is.True);Assert.That(ShaderUtil.ShaderHasError(sky.shader),Is.False);
            var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
            float fov=camera.fieldOfView,far=camera.farClipPlane;
            var target=new RenderTexture(16,16,24,RenderTextureFormat.ARGBFloat);
            var pixels=new Texture2D(16,16,TextureFormat.RGBAFloat,false,true);
            var active=RenderTexture.active;
            try
            {
                target.Create();camera.transform.position=new Vector3(0,300,0);camera.fieldOfView=.1f;camera.farClipPlane=20;
                Color Sample(Vector3 direction)
                {
                    camera.transform.rotation=Quaternion.LookRotation(direction,Mathf.Abs(direction.normalized.y)>.9f?Vector3.forward:Vector3.up);
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});
                    RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,16,16),0,0);pixels.Apply();
                    foreach(var color in pixels.GetPixels())
                    {
                        Assert.That(float.IsFinite(color.r)&&float.IsFinite(color.g)&&float.IsFinite(color.b),Is.True);
                        Assert.That(color.maxColorComponent,Is.InRange(.05f,1.3f));
                    }
                    return pixels.GetPixel(8,8);
                }
                float Difference(Color a,Color b)=>Mathf.Max(Mathf.Abs(a.r-b.r),Mathf.Abs(a.g-b.g),Mathf.Abs(a.b-b.b));
                Assert.That(Difference(Sample(new Vector3(-1,0,.0001f)),Sample(new Vector3(-1,0,-.0001f))),Is.LessThan(.015f));
                Assert.That(Difference(Sample(new Vector3(.0001f,1,0)),Sample(new Vector3(-.0001f,1,0))),Is.LessThan(.015f));
                Assert.That(Difference(Sample(new Vector3(.0001f,-1,0)),Sample(new Vector3(-.0001f,-1,0))),Is.LessThan(.015f));
                var view=new Vector3(.8f,-.12f,.5f);
                Assert.That(Difference(Sample(view),Sample(view)),Is.LessThan(.00001f));
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;camera.farClipPlane=far;
                RenderTexture.active=active;Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target);
            }
        }
    }
}
