using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class CarbineProductionTests
    {
        private bool hadCharacterSelection;
        private int characterSelection;

        [SetUp]
        public void UseCanonicalRosterForProductionReview()
        {
            hadCharacterSelection=PlayerPrefs.HasKey("offlineCharacter");
            characterSelection=PlayerPrefs.GetInt("offlineCharacter");
            PlayerPrefs.SetInt("offlineCharacter",0);
        }

        [TearDown]
        public void RestoreCharacterSelection()
        {
            if(hadCharacterSelection)PlayerPrefs.SetInt("offlineCharacter",characterSelection);
            else PlayerPrefs.DeleteKey("offlineCharacter");
            PlayerPrefs.Save();
        }

        [Serializable] sealed class Review
        {
            public System.Collections.Generic.List<string> contacts=new System.Collections.Generic.List<string>();
            public string unityVersion;public int captures;public float maximumWristError,maximumShoulderError;
            public string scope="Installed production prefabs, normal Animator/IK loop; controlled offline presentation, not human acceptance or performance qualification";
        }
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtSource/Original/PrecisionCarbine-Production-v1"));
        [Test]
        public void ClickIsConsumedOnceAndBlockedClicksNeverQueue()
        {
            var input=new WeaponClickLatch();
            input.Sample(true,true);input.Sample(false,true);
            Assert.That(input.Consume(),Is.True);
            for(int tick=0;tick<100;tick++){input.Sample(false,true);Assert.That(input.Consume(),Is.False);}
            input.Sample(true,false);input.Sample(false,true);Assert.That(input.Consume(),Is.False);
            input.Sample(true,true);input.Reset();Assert.That(input.Consume(),Is.False);
        }
        [Test]
        public void InstalledWeaponHasThreeBoundedLodsSurfaceAtlasAndForwardMuzzle()
        {
            var gun=Resources.Load<GameObject>("Weapons/Railgun");var weapon=gun.GetComponent<RailgunPresentation>();
            Assert.That(gun.transform.Find("Carbine LOD0"),Is.Not.Null);
            var lods=gun.GetComponent<LODGroup>().GetLODs();Assert.That(lods.Length,Is.EqualTo(3));
            int[] budgets={12000,6000,3000};
            var materials=lods.SelectMany(l=>l.renderers).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
            Assert.That(materials.Length,Is.LessThanOrEqualTo(3));
            foreach(var material in materials)
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
                    Assert.That(material.GetTexture(property).width,Is.EqualTo(2048));
            for(int i=0;i<3;i++)
            {
                var meshes=lods[i].renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).ToArray();
                Assert.That(meshes.Sum(m=>m.triangles.Length/3),Is.LessThanOrEqualTo(budgets[i]));
                Assert.That(meshes.All(m=>m.uv.Length==m.vertexCount),Is.True);
                var vertices=lods[i].renderers.SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>gun.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
                Assert.That(vertices.Max(v=>v.z)-vertices.Min(v=>v.z),Is.EqualTo(1).Within(.002f));
                Assert.That(vertices.Max(v=>v.x)-vertices.Min(v=>v.x),Is.LessThan(.101f));
            }
            Assert.That(weapon.Muzzle.localPosition,Is.EqualTo(new Vector3(0,.002f,.501f)));
            Assert.That(weapon.Aim,Is.Not.Null);Assert.That(weapon.Shoulder,Is.Not.Null);
            Assert.That(weapon.Triggers.Length,Is.EqualTo(3));Assert.That(weapon.EnergySurfaces.Length,Is.GreaterThan(0));
        }
        [UnityTest]
        public IEnumerator CaptureInstalledAppearanceHandsShoulderZoomRecoilAndLods()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/Foundry.unity");
            yield return new EnterPlayMode();
            var session=Object.FindFirstObjectByType<ArenaSession>();
            session.StartSession(true,3,1,TargetScenario.Combat);session.enabled=false;
            session.GetComponent<ArenaHud>().enabled=false;
            foreach(var actor in session.Actors){actor.Controller.enabled=false;actor.gameObject.SetActive(false);}
            var review=new Review{unityVersion=Application.unityVersion};Directory.CreateDirectory(Path.Combine(Output,"Unity"));
            var camera=new GameObject("Production capture").AddComponent<Camera>();camera.enabled=false;
            camera.nearClipPlane=.025f;camera.farClipPlane=30;camera.backgroundColor=new Color(.10f,.14f,.17f);camera.clearFlags=CameraClearFlags.SolidColor;
            var key=new GameObject("Production review key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.4f;key.transform.rotation=Quaternion.Euler(35,-35,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;
            // Session-owned view weapon is hidden; captures use the exact installed prefab and normal arm IK.
            session.View.gameObject.SetActive(false);
            for(int i=0;i<4;i++)
            {
                var actor=session.Actors[i];var palette=actor.Presentation.BodyRenderers[0].sharedMaterial;
                camera.cullingMask=1<<11;camera.transform.SetPositionAndRotation(new Vector3(0,1.66f,0),Quaternion.identity);
                var gun=Object.Instantiate(Resources.Load<GameObject>("Weapons/Railgun"),camera.transform,false);
                gun.transform.localPosition=FirstPersonWeaponMotion.RestPosition;
                var weapon=gun.GetComponent<RailgunPresentation>();gun.GetComponent<LODGroup>().ForceLOD(0);
                var arms=FirstPersonArms.CreateOnWeapon(weapon);ContenderPresentation.ApplyPalette(arms.Surface,palette);
                foreach(var t in gun.GetComponentsInChildren<Transform>())t.gameObject.layer=11;
                yield return null;yield return null;
                for(int state=0;state<4;state++)
                {
                    float recoil=state==2?1:0;weapon.SetCharge(recoil);arms.SetRecoil(recoil);
                    gun.transform.localPosition=FirstPersonWeaponMotion.RestPosition-Vector3.forward*(recoil*RailgunPresentation.ViewRecoilTravel);
                    camera.fieldOfView=state==1?2*Mathf.Atan(Mathf.Tan(50*Mathf.Deg2Rad)/1.5f)*Mathf.Rad2Deg:100;
                    yield return null;yield return null;
                    foreach(var pair in new[]{(HumanBodyBones.RightHand,weapon.RightGrip),(HumanBodyBones.LeftHand,weapon.LeftGrip)})
                    {
                        float error=Vector3.Distance(arms.Animator.GetBoneTransform(pair.Item1).position,pair.Item2.position);
                        review.maximumWristError=Mathf.Max(review.maximumWristError,error);
                        if(state==0)review.contacts.Add($"FP {i} {pair.Item1} error={error:F4} wrist={arms.Animator.GetBoneTransform(pair.Item1).position:F3} target={pair.Item2.position:F3} shoulderL={arms.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position:F3}");
                    }
                    Write(camera,$"FP-{i}-{new[]{"Normal","Zoom","Fire","Reset"}[state]}");review.captures++;
                }
                Object.DestroyImmediate(gun);
                actor.gameObject.SetActive(true);actor.transform.position=Vector3.zero;actor.Command=default;
                actor.Presentation.Animator.speed=1;
                camera.cullingMask=(1<<8)|(1<<10);camera.fieldOfView=44;
                camera.transform.position=new Vector3(2.6f,1.3f,2.2f);camera.transform.LookAt(new Vector3(0,1,.3f));
                yield return null;yield return null;
                var p=actor.Presentation;var shoulder=p.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position+Vector3.right*.015f+Vector3.up*.02f-Vector3.forward*.04f;
                review.maximumShoulderError=Mathf.Max(review.maximumShoulderError,Vector3.Distance(shoulder,p.Railgun.Shoulder.position));
                foreach(var pair in new[]{(HumanBodyBones.RightHand,p.RightGrip),(HumanBodyBones.LeftHand,p.LeftGrip)})
                {
                    float error=Vector3.Distance(p.Animator.GetBoneTransform(pair.Item1).position,pair.Item2.position);
                    review.maximumWristError=Mathf.Max(review.maximumWristError,error);
                    review.contacts.Add($"TP {i} {pair.Item1} error={error:F4} wrist={p.Animator.GetBoneTransform(pair.Item1).position:F3} target={pair.Item2.position:F3} shoulderL={p.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position:F3} shoulderR={p.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position:F3}");
                }
                for(int lod=0;lod<3;lod++)
                {
                    p.Railgun.GetComponent<LODGroup>().ForceLOD(lod);yield return null;
                    Write(camera,$"TP-{i}-LOD{lod}");review.captures++;
                }
                camera.transform.position=new Vector3(2.8f,1.15f,.25f);camera.transform.LookAt(new Vector3(0,1,.25f));
                p.Railgun.GetComponent<LODGroup>().ForceLOD(0);Write(camera,$"TP-{i}-Side");review.captures++;
                actor.gameObject.SetActive(false);
            }
            File.WriteAllText(Path.Combine(Output,"Unity-Validation.json"),JsonUtility.ToJson(review,true));
            Assert.That(review.maximumWristError,Is.LessThan(.015f));Assert.That(review.maximumShoulderError,Is.LessThan(.002f));
            Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(key.gameObject);
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Restore(){if(Application.isPlaying)yield return new ExitPlayMode();}
        static void Write(Camera camera,string name)
        {
            var target=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{target.Create();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,"Unity",name+".png"),image.EncodeToPNG());}
            finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
