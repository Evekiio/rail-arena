using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class GardenNatureArtReview
    {
        byte[] quality; RenderPipelineAsset pipeline; float priorTime; bool ownsPause;
        [Serializable] sealed class Production { public string master,masterSha256,sourceSha256; }
        [Serializable] sealed class View { public string name; public Vector3 position,euler; public float fov; public int lod; public bool legacy; }
        [Serializable] sealed class Evidence
        {
            public string scope="Installed Editor nature at original audited frames; controlled resting-wind legacy recreation from unchanged Review20 assets, matched cameras, all LODs, collider/navigation/device preservation and cleanup. Overlay HUD, native input/audio, all sightlines and artistic acceptance remain open.";
            public int motionFrames;public float motionSeconds,maximumWindAngle,maximumRootPositionDrift,maximumRootScaleDrift;public string masterSha256,sourceSha256;public int placements,lodMeshInstances,canonicalModels,originalSceneColliders,verifiedWorldVertices,normalFaces,windRoles;public bool physicsPreserved,colliderDefinitionsPreserved,navigationPreserved,devicesPreserved,cleanupPassed; public int invariantWorldAndControllerMatrices,natureOnlyColliderPoseChecks;public View[] views;
        }
        static void Rest(SkyGardenMotion motion)
        {
            motion.enabled=false;var field=typeof(SkyGardenMotion).GetField("rest",BindingFlags.Instance|BindingFlags.NonPublic);
            motion.transform.localRotation=(Quaternion)field.GetValue(motion);
        }
        static void Capture(Camera camera,string path,RenderTexture target,Texture2D pixels,PortalView[] portals)
        {
            foreach(var portal in portals)portal.RenderView(camera);
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});var previous=RenderTexture.active;
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
        }
        [UnityTest] public IEnumerator InstalledNaturePreservesTheArenaAndCleansUp()
        {
            quality=File.ReadAllBytes("ProjectSettings/QualitySettings.asset");pipeline=QualitySettings.renderPipeline;
            QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            const string folder="../ArtSource/Original/ArtPass-v1/Nature/GardenNature";var production=JsonUtility.FromJson<Production>(File.ReadAllText(folder+"/production.json"));
            using(var hash=SHA256.Create())
            {
                Assert.That(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(folder+"/"+production.master))).Replace("-","").ToLowerInvariant(),Is.EqualTo(production.masterSha256));
                Assert.That(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(folder+"/nature-source.json"))).Replace("-","").ToLowerInvariant(),Is.EqualTo(production.sourceSha256));
            }
            var set=Resources.Load<GardenNatureSet>("Art/GardenNature");Assert.That(set.parts.Length,Is.EqualTo(24));
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");yield return new EnterPlayMode();yield return null;
            var session=Object.FindAnyObjectByType<ArenaSession>();session.StartSession(true,0,1,TargetScenario.Stationary);while(!session.Match.IsActive)session.Match.Advance();yield return null;yield return null;session.enabled=false;session.View.enabled=false;priorTime=Time.timeScale;Time.timeScale=0;ownsPause=true;
            var names=new HashSet<string>(set.parts.Select(part=>part.sourceName));var sources=session.World.GetComponentsInChildren<MeshRenderer>(true).Where(renderer=>names.Contains(renderer.name)).ToDictionary(renderer=>renderer.name);
            Assert.That(sources.Count,Is.EqualTo(24));var rendererFlags=sources.Values.ToDictionary(renderer=>renderer,renderer=>renderer.enabled);
            var sourceMeshes=sources.Values.ToDictionary(renderer=>renderer,renderer=>renderer.GetComponent<MeshFilter>().sharedMesh);
            var sourceVertices=sourceMeshes.ToDictionary(pair=>pair.Key,pair=>pair.Value.vertices);var sourceIndices=sourceMeshes.ToDictionary(pair=>pair.Key,pair=>pair.Value.triangles);
            var sourcePositions=sources.Values.ToDictionary(renderer=>renderer,renderer=>renderer.transform.position);var sourceScales=sources.Values.ToDictionary(renderer=>renderer,renderer=>renderer.transform.localScale);
            var motions=sources.Values.Select(renderer=>renderer.GetComponent<SkyGardenMotion>()).Where(motion=>motion!=null).ToDictionary(motion=>motion,motion=>motion.enabled);
            var colliders=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None);var colliderMatrices=colliders.Where(collider=>collider is CharacterController||collider.transform.IsChildOf(session.World.transform)).ToDictionary(collider=>collider,collider=>collider.transform.localToWorldMatrix);var colliderDefinitions=colliders.ToDictionary(collider=>collider,collider=>EditorJsonUtility.ToJson(collider));
            var colliderMeshes=colliders.OfType<MeshCollider>().ToDictionary(collider=>collider,collider=>collider.sharedMesh);var colliderVertices=colliderMeshes.ToDictionary(pair=>pair.Key,pair=>pair.Value.vertices);var colliderIndices=colliderMeshes.ToDictionary(pair=>pair.Key,pair=>pair.Value.triangles);
            var devices=session.World.Portals.Select(gate=>gate.transform).Concat(session.World.Pads.Select(pad=>pad.transform)).ToDictionary(item=>item,item=>item.localToWorldMatrix);var destinations=session.World.Pads.Select(pad=>pad.Destination).ToArray();var nodes=session.World.Nodes.Select(node=>node.position).ToArray();var links=session.World.Links.ToArray();
            var art=new GameObject("Owned garden nature review").AddComponent<ArtSlicePresentation>();yield return null;yield return null;
            var groups=Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(group=>group.name.EndsWith(" / Authored garden nature",StringComparison.Ordinal)).ToArray();Assert.That(groups.Length,Is.EqualTo(24));
            foreach(var motion in motions.Keys){Assert.That(motion.enabled,Is.False);Rest(motion);}
            foreach(var group in groups)foreach(var motion in group.GetComponentsInChildren<SkyGardenMotion>(true))Rest(motion);
            int vertices=0,faces=0;var uniqueMeshes=new HashSet<Mesh>();
            foreach(var group in groups)
            {
                string name=group.name.Split(new[]{" / "},StringSplitOptions.None)[0];var binding=set.parts.Single(part=>part.sourceName==name);Assert.That(group.transform.position,Is.EqualTo(binding.center));Assert.That(group.transform.localScale,Is.EqualTo(binding.size));Assert.That(group.GetComponentsInChildren<Collider>(true).Length,Is.Zero);
                Assert.That(group.GetComponents<SkyGardenMotion>().Length,Is.EqualTo(binding.wind?1:0));var lods=group.GetLODs();Assert.That(lods.Length,Is.EqualTo(3));int previous=int.MaxValue;
                foreach(var lod in lods)
                {
                    Assert.That(lod.renderers.Length,Is.EqualTo(1));var renderer=(MeshRenderer)lod.renderers[0];var filter=renderer.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;uniqueMeshes.Add(mesh);Assert.That(mesh.subMeshCount,Is.EqualTo(1));Assert.That(renderer.sharedMaterials.Length,Is.EqualTo(1));Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("RailArena/CelSurface"));Assert.That(renderer.sharedMaterial.GetTexture("_BaseMap"),Is.Not.Null);
                    var points=mesh.vertices;var normals=mesh.normals;var indices=mesh.triangles;Assert.That(normals.Length,Is.EqualTo(points.Length));Assert.That(indices.Length/3,Is.LessThan(previous));previous=indices.Length/3;
                    foreach(var point in points){var world=filter.transform.TransformPoint(point);var relative=world-binding.center;Assert.That(Mathf.Abs(relative.x),Is.LessThanOrEqualTo(binding.size.x*.5001f));Assert.That(Mathf.Abs(relative.y),Is.LessThanOrEqualTo(binding.size.y*.5001f));Assert.That(Mathf.Abs(relative.z),Is.LessThanOrEqualTo(binding.size.z*.5001f));vertices++;}
                    for(int index=0;index<indices.Length;index+=3)
                    {
                        int a=indices[index],b=indices[index+1],c=indices[index+2];var normal=Vector3.Cross(points[b]-points[a],points[c]-points[a]);
                        if(normal.sqrMagnitude<.000000000001f)continue;
                        Assert.That(Vector3.Dot(normal.normalized,(normals[a]+normals[b]+normals[c]).normalized),Is.GreaterThan(.98f),"Reflected triangle winding and normals disagree: "+name);faces++;
                    }
                }
            }
            Assert.That(uniqueMeshes.Count,Is.EqualTo(33));
            var legacy=new List<GameObject>();foreach(var binding in set.parts.Where(part=>part.module.StartsWith("Crown",StringComparison.Ordinal)))
            {
                var source=sources[binding.sourceName];var instance=Object.Instantiate(Resources.Load<GameObject>("Art/Canopy"),source.bounds.center,Quaternion.identity);instance.name="Owned legacy canopy comparison";instance.transform.localScale=source.bounds.size;foreach(var motion in instance.GetComponentsInChildren<SkyGardenMotion>(true))Rest(motion);legacy.Add(instance);
            }
            var cameraObject=new GameObject("Owned nature comparison camera");var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=55;camera.aspect=16f/9;camera.nearClipPlane=.05f;camera.farClipPlane=400;camera.cullingMask=~(1<<11);camera.enabled=false;
            var texture=new RenderTexture(1920,1080,24);texture.Create();var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);var portals=Object.FindObjectsByType<PortalView>(FindObjectsSortMode.None);var captures=new List<View>();string output=Path.GetFullPath(folder+"/UnityReview");Directory.CreateDirectory(output);
            try
            {
                var poses=new List<(string name,Vector3 position,Vector3 target)>();
                foreach(var binding in set.parts.Where(part=>part.module=="Trunk").OrderBy(part=>part.sourceName,StringComparer.Ordinal))poses.Add((binding.sourceName.ToLowerInvariant()+"-close",binding.center+new Vector3(5,1.2f,6),binding.center+Vector3.up*.15f));
                var first=set.parts.Single(part=>part.sourceName=="TREE-1-BARK");var vine=set.parts.Single(part=>part.sourceName=="IVY-1-LEAVES");poses.Add(("ground-rosettes",new Vector3(first.center.x+2.6f,1.4f,first.center.z+2.8f),new Vector3(first.center.x,.5f,first.center.z)));poses.Add(("hanging-vines",vine.center+new Vector3(3,-.5f,4),vine.center));poses.Add(("garden-overview",new Vector3(-38,9,26),new Vector3(0,2,0)));
                var nearestTree=set.parts.Where(part=>part.module=="Trunk").OrderBy(part=>(part.center-session.View.transform.position).sqrMagnitude).First();session.View.transform.LookAt(nearestTree.center);
                var natureColliderMatrices=colliders.ToDictionary(collider=>collider,collider=>collider.transform.localToWorldMatrix);int naturePoseChecks=0;
                foreach(bool before in new[]{true,false})
                {
                    foreach(var group in groups){group.gameObject.SetActive(!before);group.ForceLOD(0);}foreach(var instance in legacy)instance.SetActive(before);
                    foreach(var pair in sources)pair.Value.enabled=before&&!pair.Key.EndsWith("-LEAVES",StringComparison.Ordinal)&&rendererFlags[pair.Value]||before&&pair.Key.StartsWith("IVY-",StringComparison.Ordinal)&&rendererFlags[pair.Value];
                    foreach(var pose in poses)
                    {
                        camera.transform.position=pose.position;camera.transform.LookAt(pose.target);Capture(camera,Path.Combine(output,pose.name+(before?"-before":"-after")+".png"),texture,pixels,portals);captures.Add(new View{name=pose.name,position=camera.transform.position,euler=camera.transform.eulerAngles,fov=camera.fieldOfView,lod=0,legacy=before});
                    }
                    Capture(session.View,Path.Combine(output,"fps-garden"+(before?"-before":"-after")+".png"),texture,pixels,portals);captures.Add(new View{name="fps-garden",position=session.View.transform.position,euler=session.View.transform.eulerAngles,fov=session.View.fieldOfView,lod=0,legacy=before});
                    foreach(var pair in natureColliderMatrices){Assert.That(pair.Key.transform.localToWorldMatrix,Is.EqualTo(pair.Value),"Nature-only toggle moved collider: "+pair.Key.name);naturePoseChecks++;}
                }
                File.WriteAllText(Path.Combine(output,"capture-poses.json"),JsonUtility.ToJson(new Evidence{masterSha256=production.masterSha256,sourceSha256=production.sourceSha256,views=captures.ToArray()},true));
                camera.transform.position=poses[0].position;camera.transform.LookAt(poses[0].target);
                foreach(int level in new[]{1,2}){foreach(var group in groups)group.ForceLOD(level);Capture(camera,Path.Combine(output,"tree-detail-lod"+level+".png"),texture,pixels,portals);captures.Add(new View{name="tree-detail-lod"+level,position=camera.transform.position,euler=camera.transform.eulerAngles,fov=camera.fieldOfView,lod=level,legacy=false});}
                string windFolder=Path.GetFullPath("../Validation/GardenNature-Wind-02");Assert.That(Directory.Exists(windFolder),Is.False,"Preserve prior motion frames");Directory.CreateDirectory(windFolder);
                var windTexture=new RenderTexture(1280,720,24);windTexture.Create();var windPixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
                var positionBaseline=groups.ToDictionary(group=>group,group=>group.transform.position);var scaleBaseline=groups.ToDictionary(group=>group,group=>group.transform.localScale);
                var moving=groups.SelectMany(group=>group.GetComponents<SkyGardenMotion>()).ToArray();var staticGroups=groups.Where(group=>group.GetComponent<SkyGardenMotion>()==null).ToArray();
                var rotations=groups.ToDictionary(group=>group,group=>group.transform.localRotation);int captureRate=Time.captureFramerate;int fixedTick=session.Match.Tick;
                float maxAngle=0,maxPosition=0,maxScale=0;var csv=new System.Text.StringBuilder("frame,seconds,matchTick,maxWindAngleDegrees,maxPositionDriftMetres,maxScaleDrift\n");
                try
                {
                    foreach(var group in groups)group.ForceLOD(0);foreach(var motion in moving)motion.enabled=true;
                    camera.transform.position=poses[0].position;camera.transform.LookAt(poses[0].target);Time.captureFramerate=30;Time.timeScale=1;
                    for(int frame=0;frame<240;frame++)
                    {
                        yield return null;Assert.That(session.Match.Tick,Is.EqualTo(fixedTick),"Visual reel must not advance authority");
                        foreach(var group in groups){maxPosition=Mathf.Max(maxPosition,Vector3.Distance(group.transform.position,positionBaseline[group]));maxScale=Mathf.Max(maxScale,Vector3.Distance(group.transform.localScale,scaleBaseline[group]));if(group.GetComponent<SkyGardenMotion>()!=null)maxAngle=Mathf.Max(maxAngle,Quaternion.Angle(group.transform.localRotation,rotations[group]));}
                        for(int site=1;site<=4;site++)
                        {
                            var stem=groups.Single(group=>group.name=="IVY-"+site+"-STEMS / Authored garden nature");var leaf=groups.Single(group=>group.name=="IVY-"+site+"-LEAVES / Authored garden nature");
                            Assert.That(stem.transform.position,Is.EqualTo(leaf.transform.position));Assert.That(stem.transform.localScale,Is.EqualTo(leaf.transform.localScale));Assert.That(Quaternion.Angle(stem.transform.localRotation,leaf.transform.localRotation),Is.LessThan(.001f),"Paired vine wind detached");
                        }
                        foreach(var group in staticGroups)Assert.That(Quaternion.Angle(group.transform.localRotation,rotations[group]),Is.LessThan(.001f));
                        Capture(camera,Path.Combine(windFolder,"frame-"+frame.ToString("0000")+".png"),windTexture,windPixels,portals);
                        csv.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F6},{2},{3:F6},{4:F9},{5:F9}\n",frame,(frame+1)/30f,session.Match.Tick,maxAngle,maxPosition,maxScale);
                    }
                    Assert.That(maxAngle,Is.GreaterThan(.1f),"Native wind must actually advance");Assert.That(maxPosition,Is.LessThan(.000001f));Assert.That(maxScale,Is.LessThan(.000001f));File.WriteAllText(Path.Combine(windFolder,"motion.csv"),csv.ToString());
                }
                finally{Time.captureFramerate=captureRate;Time.timeScale=0;foreach(var motion in moving)Rest(motion);windTexture.Release();Object.Destroy(windTexture);Object.Destroy(windPixels);}
                foreach(var instance in legacy){instance.SetActive(false);Object.Destroy(instance);}Object.Destroy(art);yield return null;yield return null;
                foreach(var pair in rendererFlags)Assert.That(pair.Key.enabled,Is.EqualTo(pair.Value));foreach(var pair in motions)Assert.That(pair.Key.enabled,Is.EqualTo(pair.Value));foreach(var group in groups)Assert.That(group==null,Is.True);
                foreach(var pair in sourceMeshes){Assert.That(pair.Key.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(pair.Value));CollectionAssert.AreEqual(sourceVertices[pair.Key],pair.Value.vertices);CollectionAssert.AreEqual(sourceIndices[pair.Key],pair.Value.triangles);Assert.That(pair.Key.transform.position,Is.EqualTo(sourcePositions[pair.Key]));Assert.That(pair.Key.transform.localScale,Is.EqualTo(sourceScales[pair.Key]));}
                CollectionAssert.AreEquivalent(colliders,Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None));foreach(var pair in colliderMatrices)Assert.That(pair.Key.transform.localToWorldMatrix,Is.EqualTo(pair.Value),"World/controller collider placement: "+pair.Key.name);foreach(var pair in colliderDefinitions)Assert.That(EditorJsonUtility.ToJson(pair.Key),Is.EqualTo(pair.Value),"Collider definition: "+pair.Key.name);foreach(var pair in colliderMeshes){Assert.That(pair.Key.sharedMesh,Is.SameAs(pair.Value));CollectionAssert.AreEqual(colliderVertices[pair.Key],pair.Value.vertices);CollectionAssert.AreEqual(colliderIndices[pair.Key],pair.Value.triangles);}
                CollectionAssert.AreEqual(nodes,session.World.Nodes.Select(node=>node.position).ToArray());CollectionAssert.AreEqual(links,session.World.Links);CollectionAssert.AreEqual(destinations,session.World.Pads.Select(pad=>pad.Destination).ToArray());foreach(var pair in devices)Assert.That(pair.Key.localToWorldMatrix,Is.EqualTo(pair.Value));
                File.WriteAllText(Path.Combine(output,"installed-review.json"),JsonUtility.ToJson(new Evidence{motionFrames=240,motionSeconds=8,maximumWindAngle=maxAngle,maximumRootPositionDrift=maxPosition,maximumRootScaleDrift=maxScale,masterSha256=production.masterSha256,sourceSha256=production.sourceSha256,placements=24,lodMeshInstances=72,canonicalModels=33,originalSceneColliders=colliders.Length,verifiedWorldVertices=vertices,normalFaces=faces,windRoles=set.parts.Count(part=>part.wind),physicsPreserved=true,colliderDefinitionsPreserved=true,invariantWorldAndControllerMatrices=colliderMatrices.Count,natureOnlyColliderPoseChecks=naturePoseChecks,navigationPreserved=true,devicesPreserved=true,cleanupPassed=true,views=captures.ToArray()},true));
            }
            finally{Time.timeScale=priorTime;ownsPause=false;texture.Release();Object.Destroy(texture);Object.Destroy(pixels);Object.Destroy(cameraObject);foreach(var instance in legacy)if(instance!=null)Object.Destroy(instance);}
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(ownsPause){Time.timeScale=priorTime;ownsPause=false;}if(Application.isPlaying)yield return new ExitPlayMode();if(quality==null)yield break;QualitySettings.renderPipeline=pipeline;File.WriteAllBytes("ProjectSettings/QualitySettings.asset",quality);AssetDatabase.Refresh();
        }
    }
}
