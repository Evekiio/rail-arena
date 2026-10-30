using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class PortalViewTests
    {
        [UnityTest]
        public IEnumerator DestinationViewsAreLiveAndPickupsAnimateWithoutMovingAuthority()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/TwinPlatforms.unity");
            yield return new EnterPlayMode(); yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>(); session.enabled=false;
            var camera=session.View;
            var capture=typeof(TwinPlatformsTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic);
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube); marker.transform.localScale=Vector3.one*2;
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); marker.GetComponent<Renderer>().sharedMaterial=material;
            foreach(var gate in session.World.Portals)
            {
                var view=gate.GetComponent<PortalView>();
                camera.transform.position=gate.transform.position+gate.transform.forward*6+Vector3.up*1.7f;
                camera.transform.rotation=Quaternion.LookRotation(-gate.transform.forward);
                marker.transform.position=gate.Exit.transform.position+gate.Exit.transform.forward*8+Vector3.up*1.7f;
                Assert.That(camera.usePhysicalProperties, Is.True);
                foreach (float aspect in new[] { 4f/3f, 16f/9f, 21f/9f })
                {
                    camera.aspect = aspect;
                    foreach (float fov in new[] { 70f, 100f, 110f })
                    {
                        camera.focalLength = Camera.FieldOfViewToFocalLength(fov, camera.sensorSize.y);
                        Assert.That(view.RenderView(camera), Is.True);
                        var expected = camera.projectionMatrix; var actual = view.DestinationCamera.projectionMatrix;
                        Assert.That(actual.m00, Is.EqualTo(expected.m00).Within(.0001f));
                        Assert.That(actual.m11, Is.EqualTo(expected.m11).Within(.0001f));
                        Assert.That(actual.m02, Is.EqualTo(expected.m02).Within(.0001f));
                        Assert.That(actual.m12, Is.EqualTo(expected.m12).Within(.0001f));
                    }
                }
                camera.ResetAspect();
                foreach(var color in new[]{Color.green,Color.red})
                {
                    material.color=color; Assert.That(view.RenderView(camera),Is.True);
                    var texture=new Texture2D(view.Texture.width,view.Texture.height,TextureFormat.RGB24,false);
                    var old=RenderTexture.active; RenderTexture.active=view.Texture;
                    texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0); texture.Apply(); RenderTexture.active=old;
                    Color actual=texture.GetPixel(texture.width/2,texture.height/2); Object.DestroyImmediate(texture);
                    Assert.That(color==Color.green?actual.g:actual.r,Is.GreaterThan(.6f),"Destination marker must appear through the matching exit");
                    Assert.That(color==Color.green?actual.r:actual.g,Is.LessThan(.2f));
                }
                marker.SetActive(false); view.RenderView(camera); capture.Invoke(null,new object[]{camera,"PortalView-"+gate.name}); marker.SetActive(true);
                Assert.That(ShaderUtil.ShaderHasError(gate.Presentation.Surface.sharedMaterial.shader),Is.False);
                Assert.That(gate.Presentation.Surface.forceRenderingOff,Is.False);
                camera.transform.position=gate.transform.position-gate.transform.forward*2+Vector3.up*1.7f;
                Assert.That(view.RenderView(camera),Is.False,"Back-facing portals should not spend extra render work");
            }
            Object.Destroy(marker); Object.Destroy(material);
            foreach(var pickup in session.World.Pickups)
            {
                var visual=pickup.Presentation; Vector3 position=pickup.transform.position;
                Quaternion rotation=visual.FloatingRoot.localRotation;
                for(int i=0;i<20;i++) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
                Assert.That(Quaternion.Angle(rotation,visual.FloatingRoot.localRotation),Is.GreaterThan(.1f));
                Assert.That(pickup.transform.position,Is.EqualTo(position));
                pickup.SetAvailable(false); Assert.That(visual.FloatingRoot.gameObject.activeSelf,Is.False); Assert.That(visual.Glow.enabled,Is.False);
                pickup.SetAvailable(true); Assert.That(visual.FloatingRoot.gameObject.activeSelf,Is.True);
                Assert.That(visual.FloatingRoot.GetComponentsInChildren<Collider>().Length,Is.Zero);
                camera.transform.position=position+new Vector3(-3,1.3f,-3); camera.transform.LookAt(visual.FloatingRoot.position);
                capture.Invoke(null,new object[]{camera,"HealthUpgrade-"+pickup.name});
            }
            session.StartSession(true,1,1,TargetScenario.Stationary); session.SetPaused(true);
            foreach(var actor in session.Actors) actor.Bot=false;
            while(!session.Match.IsActive) session.Match.Advance();
            var health=session.World.Pickups[0]; session.Human.Health=50;
            session.Human.Place(health.transform.position-Vector3.up*.9f); session.Human.Command=default;
            var advance=typeof(ArenaSession).GetMethod("AdvanceValidationTick",BindingFlags.Instance|BindingFlags.NonPublic);
            advance.Invoke(session,null);
            Assert.That(session.Human.Health,Is.GreaterThan(50)); Assert.That(health.Presentation.Available,Is.False);
            session.Human.Place(new Vector3(-44,.08f,14));
            for(int i=0;i<1201;i++) advance.Invoke(session,null);
            Assert.That(health.Presentation.Available,Is.True); Assert.That(health.Presentation.FloatingRoot.gameObject.activeSelf,Is.True);
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Restore() { if(Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
