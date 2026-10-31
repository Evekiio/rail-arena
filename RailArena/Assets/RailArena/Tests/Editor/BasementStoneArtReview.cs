using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class BasementStoneArtReview
    {
        [Serializable] sealed class View { public string id; public Vector3 position,euler; public float fieldOfView; }
        [Serializable] sealed class MeshCheck { public string deck;public int lod,triangles;public float minimumY,maximumY,minimumHullCross; }
        [Serializable] sealed class Report { public string scope="Installed eight stone deck visuals under the production art pipeline. Exact source colliders, transforms and return-device positions preserved; matching camera comparisons isolate deck presentation. Offscreen world cameras exclude HUD/native input/audio."; public string sourceMasterSha256; public int decks,colliders; public View[] views; public MeshCheck[] meshes; }
        [Serializable] sealed class MasterRecord { public string master,masterSha256; }
        sealed class Source
        {
            public Transform deck;public MeshCollider collider;public Mesh mesh;public Matrix4x4 matrix;public MeshRenderer renderer;public Material material;public Vector3[] vertices;public int[] triangles;
        }
        string priorPipelinePath;byte[] priorQuality;
        [UnityTest]
        public IEnumerator VerifyAndCaptureInstalledDecks()
        {
            priorPipelinePath=AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);priorQuality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");
            QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,0,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();session.enabled=false;session.View.enabled=false;
            var routes=session.World.GetComponentsInChildren<BasementRoute>().OrderBy(route=>route.name).ToArray();Assert.That(routes.Length,Is.EqualTo(4));
            var sources=routes.SelectMany(route=>new[]{route.Staging,route.Landing}).Select(deck=>new Source{deck=deck,collider=deck.GetComponent<MeshCollider>(),mesh=deck.GetComponent<MeshCollider>().sharedMesh,matrix=deck.localToWorldMatrix,renderer=deck.GetComponent<MeshRenderer>(),material=deck.GetComponent<MeshRenderer>().sharedMaterial}).ToArray();
            foreach(var source in sources){source.vertices=source.mesh.vertices;source.triangles=source.mesh.triangles;}
            var colliders=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var devices=session.World.Pads.Select(pad=>pad.transform).Concat(session.World.Portals.Select(portal=>portal.transform)).ToDictionary(item=>item,item=>item.localToWorldMatrix);
            var markers=routes.Select(route=>route.transform.Find("Landing marker").GetComponent<MeshRenderer>()).ToArray();
            var art=new GameObject("Measured basement stone art review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            var roots=Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(group=>group.name.Contains(" authored stone")).ToArray();Assert.That(roots.Length,Is.EqualTo(8));
            foreach(var marker in markers)Assert.That(marker.enabled,Is.False);
            CollectionAssert.AreEquivalent(colliders,Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None),"Presentation must not add or remove collision");
            foreach(var pair in devices)Assert.That(pair.Key.localToWorldMatrix,Is.EqualTo(pair.Value),"Return-device transforms must remain exact");
            var meshChecks=new List<MeshCheck>();
            foreach(var source in sources)
            {
                Assert.That(source.collider.sharedMesh,Is.SameAs(source.mesh));Assert.That(source.deck.localToWorldMatrix,Is.EqualTo(source.matrix));Assert.That(source.renderer.enabled,Is.False);
                CollectionAssert.AreEqual(source.vertices,source.mesh.vertices);CollectionAssert.AreEqual(source.triangles,source.mesh.triangles);
                var top=source.deck.TransformPoint(new Vector3(source.mesh.bounds.center.x,source.mesh.bounds.max.y,source.mesh.bounds.center.z));
                var group=roots.Single(item=>Vector3.Distance(item.transform.position,top)<.001f);Assert.That(group.GetComponentsInChildren<Collider>().Length,Is.Zero);
                var points=source.mesh.vertices.Where(vertex=>Mathf.Abs(vertex.y-source.mesh.bounds.max.y)<.00001f&&new Vector2(vertex.x,vertex.z).sqrMagnitude>.1f).Select(vertex=>Vector3.Scale(vertex-new Vector3(0,source.mesh.bounds.max.y,0),source.deck.lossyScale)).Select(vertex=>new Vector2(vertex.x,vertex.z)).Distinct().OrderBy(point=>Mathf.Atan2(point.y,point.x)).ToArray();Assert.That(points.Length,Is.EqualTo(20));
                int previous=int.MaxValue;var lods=group.GetLODs();Assert.That(lods.Length,Is.EqualTo(3));
                int level=0;
                foreach(var lod in lods)
                {
                    Assert.That(lod.renderers.Length,Is.EqualTo(1));var filter=lod.renderers[0].GetComponent<MeshFilter>();int triangles=filter.sharedMesh.triangles.Length/3;Assert.That(triangles,Is.LessThan(previous));previous=triangles;
                    var check=new MeshCheck{deck=source.deck.parent.name+"/"+source.deck.name,lod=level++,triangles=triangles,minimumY=float.PositiveInfinity,maximumY=float.NegativeInfinity,minimumHullCross=float.PositiveInfinity};
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        var relative=filter.transform.TransformPoint(vertex)-top;check.minimumY=Mathf.Min(check.minimumY,relative.y);check.maximumY=Mathf.Max(check.maximumY,relative.y);var point=new Vector2(relative.x,relative.z);
                        for(int index=0;index<points.Length;index++)
                        { var edge=points[(index+1)%points.Length]-points[index];var delta=point-points[index];check.minimumHullCross=Mathf.Min(check.minimumHullCross,edge.x*delta.y-edge.y*delta.x); }
                    }
                    Assert.That(check.minimumY,Is.GreaterThanOrEqualTo(-source.renderer.bounds.size.y-.001f));Assert.That(check.maximumY,Is.LessThanOrEqualTo(.001f));Assert.That(check.minimumHullCross,Is.GreaterThanOrEqualTo(-.001f),"All visual vertices must stay inside the unchanged collision footprint");meshChecks.Add(check);
                }
                Assert.That(previous,Is.LessThan(1000),"Distant stone LOD must remain inexpensive");
            }
            var camera=new GameObject("Owned deck comparison camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=(1<<0)|(1<<9);camera.fieldOfView=55;camera.nearClipPlane=.08f;camera.farClipPlane=1000;
            var target=new RenderTexture(1920,1080,24);target.Create();var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);var views=new List<View>();
            string folder=Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/UnityReview");Directory.CreateDirectory(folder);
            float priorScale=Time.timeScale;Time.timeScale=0;
            var landing=sources.First(source=>source.deck==routes[0].Landing);var staging=sources.First(source=>source.deck==routes[0].Staging);
            var landingTop=landing.deck.position+Vector3.up*.3f;var stagingTop=staging.deck.position+Vector3.up*.3f;
            try
            {
                foreach(bool before in new[]{false,true})
                {
                    foreach(var group in roots)group.gameObject.SetActive(!before);foreach(var source in sources)source.renderer.enabled=before;foreach(var marker in markers)marker.enabled=before;
                    foreach(var view in new[]{("basement-top",landingTop+new Vector3(-7,6,8),landingTop+Vector3.down*.15f),("staging-gameplay",stagingTop+new Vector3(-.5f,1.58f,-1.6f),stagingTop+new Vector3(2,.15f,2)),("basement-underside",landingTop+new Vector3(7,-3.4f,7),landingTop+Vector3.down*.3f)})
                    {
                        camera.transform.position=view.Item2;camera.transform.LookAt(view.Item3);yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var old=RenderTexture.active;
                        string id=view.Item1+(before?"-before":"-after");
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,id+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=old;}
                        views.Add(new View{id=id,position=camera.transform.position,euler=camera.transform.eulerAngles,fieldOfView=camera.fieldOfView});
                    }
                }
                foreach(var group in roots)group.gameObject.SetActive(true);foreach(var source in sources)source.renderer.enabled=false;foreach(var marker in markers)marker.enabled=false;
                var landingGroup=roots.Single(group=>Vector3.Distance(group.transform.position,landingTop)<.001f);
                for(int level=0;level<3;level++)
                {
                    landingGroup.ForceLOD(level);camera.transform.position=landingTop+new Vector3(-7,6,8);camera.transform.LookAt(landingTop+Vector3.down*.15f);yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var old=RenderTexture.active;string id="basement-lod"+level;
                    try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,id+".png"),pixels.EncodeToPNG());}finally{RenderTexture.active=old;}
                    views.Add(new View{id=id,position=camera.transform.position,euler=camera.transform.eulerAngles,fieldOfView=camera.fieldOfView});
                }
                landingGroup.ForceLOD(-1);
                Object.Destroy(art);yield return null;yield return null;
                foreach(var source in sources){Assert.That(source.renderer.enabled,Is.True);Assert.That(source.renderer.sharedMaterial,Is.SameAs(source.material));Assert.That(source.collider.sharedMesh,Is.SameAs(source.mesh));}
                foreach(var group in roots)Assert.That(group==null,Is.True,"Owned stone visuals must be removed when the art profile is removed");
                foreach(var marker in markers)Assert.That(marker.enabled,Is.True);
                const string sourceFolder="../ArtSource/Original/ArtPass-v1/Environment/BasementDecks/";
                var master=JsonUtility.FromJson<MasterRecord>(File.ReadAllText(sourceFolder+"production.json"));
                using(var sha=System.Security.Cryptography.SHA256.Create())
                    Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(sourceFolder+master.master))).Replace("-","").ToLowerInvariant(),Is.EqualTo(master.masterSha256));
                File.WriteAllText(Path.Combine(folder,"index.json"),JsonUtility.ToJson(new Report{sourceMasterSha256=master.masterSha256,decks=8,colliders=colliders.Length,views=views.ToArray(),meshes=meshChecks.ToArray()},true));
                TestContext.WriteLine("BASEMENT_DECK_MASTER_NATIVE_VERIFIED "+master.masterSha256);
            }
            finally{Time.timeScale=priorScale;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(camera.gameObject);}
            yield return new ExitPlayMode();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(priorQuality!=null)
            { QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(priorPipelinePath);File.WriteAllBytes("ProjectSettings/QualitySettings.asset",priorQuality);AssetDatabase.Refresh(); }
        }
    }
}
