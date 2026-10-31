using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class ArtRenderBudgetReview
    {
        byte[] quality; RenderPipelineAsset pipeline;
        [Serializable] sealed class Surface
        {
            public string path, owner, mesh;
            public int triangles, materials, submeshes;
            public bool enabled, active, propertyBlock, forceRenderingOff;
            public string[] shaders;
        }
        [Serializable] sealed class Report
        {
            public string scope = "Installed renderer inventory including all LOD candidates, not submitted draw calls or GPU cost. Runtime profiler is the separate measured authority.";
            public Surface[] surfaces;
            public int rifleBatches, sourceShells, combinedRenderers, verifiedVertices;
            public bool restoredLodsAndRendererFlags;
        }
        static string PathOf(Transform item) => item.parent == null ? item.name : PathOf(item.parent)+"/"+item.name;
        sealed class VertexSample { public Vector3 position,normal,tangent; public float handedness; public Vector2 uv; public Material material; }
        static IEnumerable<VertexSample> Vertices(MeshRenderer renderer)
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            var points=mesh.vertices; var normals=mesh.normals; var uv=mesh.uv; var tangents=mesh.tangents;
            Assert.That(normals.Length,Is.EqualTo(points.Length)); Assert.That(uv.Length,Is.EqualTo(points.Length));
            Assert.That(tangents.Length,Is.EqualTo(points.Length));
            var normalMatrix=renderer.transform.localToWorldMatrix.inverse.transpose;
            for(int i=0;i<points.Length;i++)
                yield return new VertexSample { position=renderer.transform.TransformPoint(points[i]), normal=normalMatrix.MultiplyVector(normals[i]).normalized,
                    tangent=renderer.transform.TransformVector(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z)).normalized,
                    uv=uv[i],handedness=tangents[i].w,material=renderer.sharedMaterial };
        }
        static int VerifyBatch(RifleRenderBatch batch)
        {
            var weapon=batch.GetComponent<RailgunPresentation>(); var group=weapon.GetComponent<LODGroup>(); var originals=group.GetLODs();
            int verified=0;
            if(batch.SourceRenderers==0)
            {
                Assert.That(batch.BatchRenderers,Is.Zero);
                var shells=weapon.GetComponentsInChildren<MeshRenderer>(true).Where(renderer=>renderer.name.EndsWith("_shell_Surface",StringComparison.Ordinal));
                Assert.That(shells.All(renderer=>!renderer.enabled||renderer.forceRenderingOff||!renderer.gameObject.activeInHierarchy),Is.True,"Unbatched rifle must be hidden: "+PathOf(weapon.transform));
                return 0;
            }
            foreach(var lod in originals)
            {
                var combined=lod.renderers.OfType<MeshRenderer>().Where(renderer=>renderer.name.StartsWith("Rigid rifle shells",StringComparison.Ordinal)).ToArray();
                Assert.That(combined.Length,Is.GreaterThan(0));
                var parent=combined[0].transform.parent;
                var sources=parent.GetComponentsInChildren<MeshRenderer>(true).Where(renderer=>!renderer.enabled).ToArray();
                Assert.That(sources.All(renderer=>renderer.name.EndsWith("_shell_Surface",StringComparison.Ordinal)&&!renderer.name.StartsWith("RF_07_",StringComparison.Ordinal)&&!renderer.name.StartsWith("RF_11_",StringComparison.Ordinal)),Is.True);
                var before=sources.SelectMany(Vertices).ToArray(); var after=combined.SelectMany(Vertices).ToArray();
                Assert.That(after.Length,Is.EqualTo(before.Length));
                for(int index=0;index<before.Length;index++)
                {
                    Assert.That(Vector3.Distance(before[index].position,after[index].position),Is.LessThan(.00002f),"World vertex position");
                    Assert.That(Vector3.Distance(before[index].normal,after[index].normal),Is.LessThan(.00002f),"World normal");
                    Assert.That(Vector3.Distance(before[index].tangent,after[index].tangent),Is.LessThan(.00002f),"World tangent");
                    Assert.That(Vector2.Distance(before[index].uv,after[index].uv),Is.LessThan(.000001f),"UV");
                    Assert.That(after[index].handedness,Is.EqualTo(before[index].handedness));
                    Assert.That(after[index].material,Is.SameAs(before[index].material));
                }
                var expectedTriangles=new List<int>(); int vertexOffset=0;
                foreach(var source in sources)
                {
                    var mesh=source.GetComponent<MeshFilter>().sharedMesh;
                    expectedTriangles.AddRange(mesh.triangles.Select(index=>index+vertexOffset)); vertexOffset+=mesh.vertexCount;
                }
                var actualTriangles=new List<int>(); vertexOffset=0;
                foreach(var renderer in combined)
                {
                    var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                    actualTriangles.AddRange(mesh.triangles.Select(index=>index+vertexOffset)); vertexOffset+=mesh.vertexCount;
                }
                CollectionAssert.AreEqual(expectedTriangles,actualTriangles,"Triangle order/winding/index offsets");
                verified+=before.Length;
            }
            var excluded=weapon.GetComponentsInChildren<MeshRenderer>(true).Where(renderer=>renderer.name.StartsWith("RF_07_",StringComparison.Ordinal)||renderer.name.StartsWith("RF_11_",StringComparison.Ordinal)||renderer.name.Contains("_trigger_")||renderer.name.EndsWith("Energy",StringComparison.Ordinal)).ToArray();
            var snapshots=excluded.Select(renderer=>(renderer,renderer.enabled,renderer.GetComponent<MeshFilter>().sharedMesh,renderer.transform.localToWorldMatrix)).ToArray();
            batch.Restore();
            foreach(var item in snapshots){Assert.That(item.renderer.enabled,Is.EqualTo(item.Item2));Assert.That(item.renderer.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(item.Item3));Assert.That(item.renderer.transform.localToWorldMatrix,Is.EqualTo(item.Item4));}
            var restored=group.GetLODs(); Assert.That(restored.Length,Is.EqualTo(originals.Length));
            Assert.That(restored.SelectMany(lod=>lod.renderers).Any(renderer=>renderer.name.StartsWith("Rigid rifle shells",StringComparison.Ordinal)),Is.False);
            Assert.That(restored.SelectMany(lod=>lod.renderers).All(renderer=>renderer.enabled),Is.True);
            batch.Initialize(weapon); return verified;
        }
        [Serializable] sealed class ViewPose { public string id; public Vector3 position,euler; public float fov; public Matrix4x4 projection; }
        [Serializable] sealed class ViewReport { public string scope="Same-frame geometry/material camera comparisons; not native input, sound or performance acceptance."; public ViewPose[] views; }
        static void Capture(Camera camera,string path,RenderTexture target,Texture2D pixels)
        {
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
            var previous=RenderTexture.active;
            try { RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false); pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG()); }
            finally { RenderTexture.active=previous; }
        }
        static void CapturePairs(ArenaSession session,RifleRenderBatch[] batches)
        {
            const string folder="../Validation/ArtFrame-RifleBatch-Visual2";Directory.CreateDirectory(folder);
            var views=new List<ViewPose>();
            var cameraObject=new GameObject("Owned rifle batch comparison camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(session.View);camera.enabled=false;camera.aspect=1280f/720;camera.nearClipPlane=.02f;
            var target=new RenderTexture(1280,720,24);target.Create();var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var third=batches.First(batch=>batch.GetComponentInParent<ArenaActor>()!=null && batch.GetComponentInParent<ArenaActor>()!=session.Human);
            try
            {
                foreach(bool combined in new[]{false,true})
                {
                    foreach(var batch in batches){if(combined)batch.Initialize(batch.GetComponent<RailgunPresentation>());else batch.Restore();}
                    foreach(var batch in batches)batch.GetComponent<LODGroup>().ForceLOD(0);
                    camera.CopyFrom(session.View);camera.enabled=false;camera.aspect=1280f/720;camera.nearClipPlane=.02f;
                    camera.cullingMask=session.View.cullingMask;camera.transform.SetPositionAndRotation(session.View.transform.position,session.View.transform.rotation);
                    views.Add(new ViewPose{id="fps-"+(combined?"after":"before"),position=camera.transform.position,euler=camera.transform.eulerAngles,fov=camera.fieldOfView,projection=camera.projectionMatrix});
                    Capture(camera,folder+"/fps-"+(combined?"after":"before")+".png",target,pixels);
                    camera.cullingMask=~(1<<11);camera.fieldOfView=50;
                    var weapon=third.transform;
                    camera.transform.position=weapon.TransformPoint(new Vector3(1,.27f,.25f));camera.transform.LookAt(weapon.TransformPoint(new Vector3(0,0,.25f)));
                    for(int level=0;level<3;level++)
                    {
                        third.GetComponent<LODGroup>().ForceLOD(level);
                        views.Add(new ViewPose{id="third-lod"+level+"-"+(combined?"after":"before"),position=camera.transform.position,euler=camera.transform.eulerAngles,fov=camera.fieldOfView,projection=camera.projectionMatrix});
                        Capture(camera,folder+"/third-lod"+level+"-"+(combined?"after":"before")+".png",target,pixels);
                    }
                }
            }
            finally
            {
                File.WriteAllText(folder+"/camera-poses.json",JsonUtility.ToJson(new ViewReport{views=views.ToArray()},true));
                foreach(var batch in batches)if(batch!=null)batch.GetComponent<LODGroup>().ForceLOD(-1);
                target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(cameraObject);
            }
        }
        [UnityTest] public IEnumerator InventoryInstalledRenderers()
        {
            quality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset"); pipeline=QualitySettings.renderPipeline;
            QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>(); session.StartSession(true,3,1,TargetScenario.Combat);
            session.gameObject.AddComponent<ArtSlicePresentation>(); yield return null; yield return null;
            var surfaces=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(renderer=>
            {
                var skin=renderer as SkinnedMeshRenderer; var filter=renderer.GetComponent<MeshFilter>(); var mesh=skin!=null?skin.sharedMesh:filter!=null?filter.sharedMesh:null;
                var gun=renderer.GetComponentInParent<RailgunPresentation>(); var actor=renderer.GetComponentInParent<ArenaActor>();
                return new Surface { path=PathOf(renderer.transform), owner=gun!=null?"rifle":actor!=null?"character":renderer.GetComponentInParent<Canvas>()!=null?"UI":"environment/effects", mesh=mesh!=null?mesh.name:"none", triangles=mesh!=null?(int)(Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(long)mesh.GetIndexCount(i))/3):0, materials=renderer.sharedMaterials.Length,submeshes=mesh!=null?mesh.subMeshCount:0,enabled=renderer.enabled,active=renderer.gameObject.activeInHierarchy,propertyBlock=renderer.HasPropertyBlock(),forceRenderingOff=renderer.forceRenderingOff,shaders=renderer.sharedMaterials.Select(m=>m!=null?m.shader.name:"missing").ToArray() };
            }).OrderBy(item=>item.path).ToArray();
            Assert.That(surfaces.Any(item=>item.owner=="character" && item.path.Contains("Painted cel")),Is.True);
            var batches=Object.FindObjectsByType<RifleRenderBatch>(FindObjectsSortMode.None); Assert.That(batches.Length,Is.EqualTo(5));
            CapturePairs(session,batches);
            int count=batches.Sum(batch=>VerifyBatch(batch));
            Directory.CreateDirectory("../Validation/ArtFrame-RifleBatch-Candidate"); File.WriteAllText("../Validation/ArtFrame-RifleBatch-Candidate/renderer-inventory.json",JsonUtility.ToJson(new Report{surfaces=surfaces,rifleBatches=batches.Length,sourceShells=batches.Sum(batch=>batch.SourceRenderers),combinedRenderers=batches.Sum(batch=>batch.BatchRenderers),verifiedVertices=count,restoredLodsAndRendererFlags=true},true));
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(quality==null)yield break;
            QualitySettings.renderPipeline=pipeline;File.WriteAllBytes("ProjectSettings/QualitySettings.asset",quality);AssetDatabase.Refresh();
        }
    }
}

