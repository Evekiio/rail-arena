using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class ArenaStoneArtReview
    {
        byte[] priorQuality;
        RenderPipelineAsset priorPipeline;
        [Serializable] sealed class Geometry { public Vector3[] vertices; }
        [Serializable] sealed class SourcePart { public string name, family; public Vector3 size; public Geometry canonicalCollision; }
        [Serializable] sealed class SourceReport { public SourcePart[] parts; }
        [Serializable] sealed class Production { public string master, masterSha256, sourceSha256; }
        [Serializable] sealed class Evidence
        {
            public string scope = "Installed Editor art profile; unchanged collider identities/geometry, navigation, devices, exact visual hulls and cleanup. Matching camera comparisons and forced LOD views; not Windows performance or native input acceptance.";
            public string masterSha256, sourceSha256;
            public int placements, uniqueLodAssets, lodMeshInstances, originalFamilyColliders, preservedSceneColliders, floorRays, materialRegionProbes;
            public bool physicsPreserved, navigationPreserved, devicesPreserved, cleanupPassed;
            public View[] views;
        }
        [Serializable] sealed class View { public string name; public Vector3 position, euler; public int lod; }
        sealed class Surface
        {
            public readonly Vector3[] points;
            public readonly int[][] triangles;
            public readonly Material[] materials;
            public Surface(Renderer renderer)
            {
                var filter=renderer.GetComponent<MeshFilter>(); var mesh=filter.sharedMesh;
                points=mesh.vertices.Select(filter.transform.TransformPoint).ToArray();
                triangles=Enumerable.Range(0,mesh.subMeshCount).Select(submesh=>mesh.GetTriangles(submesh)).ToArray();
                materials=renderer.sharedMaterials;
            }
            public Material At(Vector3 origin)
            {
                float closest=float.PositiveInfinity; Material result=null;
                for(int submesh=0;submesh<triangles.Length;submesh++)
                for(int index=0;index<triangles[submesh].Length;index+=3)
                {
                    var indices=triangles[submesh]; Vector3 a=points[indices[index]],b=points[indices[index+1]],c=points[indices[index+2]];
                    Vector3 edge=b-a,other=c-a,p=Vector3.Cross(Vector3.down,other);
                    float determinant=Vector3.Dot(edge,p);
                    if(Mathf.Abs(determinant)<.0000001f)continue;
                    Vector3 delta=origin-a; float inverse=1/determinant;
                    float u=Vector3.Dot(delta,p)*inverse;
                    if(u<-.00001f||u>1.00001f)continue;
                    Vector3 q=Vector3.Cross(delta,edge); float v=Vector3.Dot(Vector3.down,q)*inverse;
                    if(v<-.00001f||u+v>1.00001f)continue;
                    float distance=Vector3.Dot(other,q)*inverse;
                    if(distance<0||distance>=closest)continue;
                    closest=distance;result=materials[submesh];
                }
                return result;
            }
        }

        static float RampHeight(SourcePart source, float x, bool top)
        {
            var profile = source.canonicalCollision.vertices.GroupBy(point => Mathf.Round(point.x*100000)/100000)
                .OrderBy(group => group.Key).Select(group => new Vector2(group.Key, top ? group.Max(point => point.y) : group.Min(point => point.y))).ToArray();
            for (int index=1; index<profile.Length; index++)
                if (x <= profile[index].x+.00002f)
                    return Mathf.Lerp(profile[index-1].y,profile[index].y,Mathf.InverseLerp(profile[index-1].x,profile[index].x,x));
            return profile.Last().y;
        }

        [UnityTest]
        public IEnumerator InstalledArenaPreservesCollisionRoutesAndDevices()
        {
            priorQuality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset");
            const string artFolder="../ArtSource/Original/ArtPass-v1/Environment/ArenaStone";
            var production=JsonUtility.FromJson<Production>(File.ReadAllText(artFolder+"/production.json"));
            using(var hash=SHA256.Create())
            {
                Assert.That(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(artFolder+"/"+production.master))).Replace("-","").ToLowerInvariant(),Is.EqualTo(production.masterSha256));
                Assert.That(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(artFolder+"/arena-source.json"))).Replace("-","").ToLowerInvariant(),Is.EqualTo(production.sourceSha256));
            }
            priorPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            var report = JsonUtility.FromJson<SourceReport>(File.ReadAllText("../ArtSource/Original/ArtPass-v1/Environment/ArenaStone/arena-source.json"));
            var set = Resources.Load<ArenaStoneSet>("Art/LowerArenaStone");
            Assert.That(set.parts.Length, Is.EqualTo(19));
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            session.StartSession(true,0,1,TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            session.enabled=false; session.View.enabled=false;
            var renderers = session.World.GetComponentsInChildren<MeshRenderer>(true);
            var sources = set.parts.ToDictionary(part => part.sourceName, part => renderers.Single(renderer => renderer.name==part.sourceName));
            var originals = renderers.Where(renderer => sources.Keys.Any(name => renderer.name==name || renderer.name.StartsWith(name+"-CAP",StringComparison.Ordinal)
                || renderer.name.StartsWith(name+"-INLAY",StringComparison.Ordinal))).ToDictionary(renderer => renderer,renderer => renderer.enabled);
            var collision = sources.ToDictionary(entry => entry.Key,entry => entry.Value.GetComponent<MeshCollider>());
            var meshes = collision.ToDictionary(entry => entry.Key,entry => entry.Value.sharedMesh);
            var vertices = meshes.ToDictionary(entry => entry.Key,entry => entry.Value.vertices);
            var triangles = meshes.ToDictionary(entry => entry.Key,entry => entry.Value.triangles);
            var matrices = sources.ToDictionary(entry => entry.Key,entry => entry.Value.localToWorldMatrix);
            var colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var devices = session.World.Portals.Select(gate => gate.transform).Concat(session.World.Pads.Select(pad => pad.transform)).ToDictionary(item => item,item => item.localToWorldMatrix);
            var destinations = session.World.Pads.Select(pad => pad.Destination).ToArray();
            var nodes = session.World.Nodes.Select(node => node.position).ToArray();
            var links = session.World.Links.ToArray();
            var art = new GameObject("Owned lower arena stone review").AddComponent<ArtSlicePresentation>();
            yield return null; yield return null;
            var groups = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(group => group.name.EndsWith(" / Authored arena stone",StringComparison.Ordinal)).ToArray();
            Assert.That(groups.Length,Is.EqualTo(19));
            CollectionAssert.AreEquivalent(colliders,Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None));
            CollectionAssert.AreEqual(nodes,session.World.Nodes.Select(node => node.position).ToArray());
            CollectionAssert.AreEqual(links,session.World.Links);
            CollectionAssert.AreEqual(destinations,session.World.Pads.Select(pad => pad.Destination).ToArray());
            foreach (var device in devices) Assert.That(device.Key.localToWorldMatrix,Is.EqualTo(device.Value));
            int rays=0,materialProbes=0;
            foreach (var entry in sources)
            {
                string name=entry.Key; var source=entry.Value; var part=report.parts.Single(item => item.name==name);
                var binding=set.parts.Single(item => item.sourceName==name);
                var center=source.transform.TransformPoint(meshes[name].bounds.center);
                Assert.That(source.enabled,Is.False);
                Assert.That(source.localToWorldMatrix,Is.EqualTo(matrices[name]));
                Assert.That(collision[name].sharedMesh,Is.SameAs(meshes[name]));
                CollectionAssert.AreEqual(vertices[name],meshes[name].vertices);
                CollectionAssert.AreEqual(triangles[name],meshes[name].triangles);
                foreach (var vertex in vertices[name])
                {
                    Vector3 canonical=Quaternion.Inverse(binding.rotation)*(source.transform.TransformPoint(vertex)-center);
                    Assert.That(part.canonicalCollision.vertices.Min(point => Vector3.Distance(point,canonical)),Is.LessThan(.00002f));
                }
                var group=groups.Single(item => item.name==name+" / Authored arena stone");
                Assert.That(group.GetComponentsInChildren<Collider>().Length,Is.Zero);
                Assert.That(group.GetLODs().Length,Is.EqualTo(3));
                int previous=int.MaxValue;
                foreach (var lod in group.GetLODs())
                {
                    var renderer=lod.renderers.Single(); var filter=renderer.GetComponent<MeshFilter>();
                    int count=filter.sharedMesh.triangles.Length/3;
                    Assert.That(count,Is.LessThan(previous)); previous=count;
                    Assert.That(Vector3.Distance(renderer.bounds.center,center),Is.LessThan(.0001f));
                    Assert.That(Vector3.Distance(renderer.bounds.size,part.size),Is.LessThan(.0001f));
                    Assert.That(renderer.sharedMaterials.All(material => material!=null&&material.shader.name.Contains("Cel")),Is.True);
                    foreach (var vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 canonical=Quaternion.Inverse(binding.rotation)*(filter.transform.TransformPoint(vertex)-center);
                        Assert.That(Mathf.Abs(canonical.x),Is.LessThanOrEqualTo(part.size.x/2+.0001f));
                        Assert.That(Mathf.Abs(canonical.z),Is.LessThanOrEqualTo(part.size.z/2+.0001f));
                        float low=part.family=="Ramp"?RampHeight(part,canonical.x,false):-part.size.y/2;
                        float high=part.family=="Ramp"?RampHeight(part,canonical.x,true):part.size.y/2;
                        Assert.That(canonical.y,Is.InRange(low-.0001f,high+.0001f),name+" imported visual must remain inside the exact source profile");
                    }
                }
                foreach (float x in part.family=="Ramp"?new[]{-4.75f,0,4.75f}:new[]{0f})
                {
                    float top=part.family=="Ramp"?RampHeight(part,x,true):part.size.y/2;
                    Vector3 expected=center+binding.rotation*new Vector3(x,top,0);
                    Assert.That(Physics.Raycast(expected+Vector3.up*.05f,Vector3.down,out var hit,.15f,(1<<0)|(1<<9),QueryTriggerInteraction.Ignore),Is.True);
                    Assert.That(hit.collider,Is.SameAs(collision[name]));
                    Assert.That(Mathf.Abs(hit.point.y-expected.y),Is.LessThan(.001f)); rays++;
                }
                var surfaces=group.GetLODs().Select(lod=>new Surface(lod.renderers.Single())).ToArray();
                int probesBefore=materialProbes;
                for(int submesh=0;submesh<surfaces[0].materials.Length;submesh++)
                {
                    var expected=surfaces[0].materials[submesh];
                    if(expected.name!="DeckStone"&&expected.name!="DeckTeal")continue;
                    var indices=surfaces[0].triangles[submesh];
                    for(int index=0;index<indices.Length;index+=3)
                    {
                        Vector3 a=surfaces[0].points[indices[index]],b=surfaces[0].points[indices[index+1]],c=surfaces[0].points[indices[index+2]];
                        if(Mathf.Abs(Vector3.Cross(b-a,c-a).normalized.y)<.9f)continue;
                        Vector3 point=(a+b+c)/3,canonical=Quaternion.Inverse(binding.rotation)*(point-center);
                        float top=part.family=="Ramp"?RampHeight(part,canonical.x,true):part.size.y/2;
                        if(Mathf.Abs(canonical.y-top)>.001f)continue;
                        for(int level=1;level<3;level++)
                        {
                            Assert.That(surfaces[level].At(point+Vector3.up*.04f),Is.SameAs(expected),name+" LOD"+level+" must retain paving/arrow material at "+point);
                            materialProbes++;
                        }
                    }
                }
                Assert.That(materialProbes,Is.GreaterThan(probesBefore),name+" needs actual top-surface appearance probes");
            }
            var camera=new GameObject("Owned arena stone camera").AddComponent<Camera>();
            camera.enabled=false; camera.cullingMask=(1<<0)|(1<<9); camera.fieldOfView=65; camera.nearClipPlane=.08f; camera.farClipPlane=1000;
            var target=new RenderTexture(1920,1080,24); target.Create();
            var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            string output=Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Environment/ArenaStone/UnityReview"); Directory.CreateDirectory(output);
            var views=new[] {
                ("arena-overview",new Vector3(37,24,-38),new Vector3(0,2,0)),
                ("outer-gameplay",new Vector3(-25,1.68f,9),new Vector3(-25,.8f,1)),
                ("ramp-west",new Vector3(-23,4.5f,14),new Vector3(-17,1.9f,8)),
                ("ramp-east",new Vector3(23,4.5f,-14),new Vector3(17,1.9f,-8)),
                ("underpass-gameplay",new Vector3(0,1.6f,-11),new Vector3(0,1,2)),
                ("outer-underside",new Vector3(32,-5,17),new Vector3(24,-.2f,10)),
                ("corner-detail",new Vector3(-29,1.2f,17),new Vector3(-24,0,15)) };
            var captured=new System.Collections.Generic.List<View>();
            float priorTime=Time.timeScale; Time.timeScale=0;
            try
            {
                foreach (bool before in new[]{true,false})
                {
                    foreach (var group in groups) { group.gameObject.SetActive(!before); group.ForceLOD(0); }
                    foreach (var original in originals) original.Key.enabled=before&&original.Value;
                    foreach (var view in views)
                    {
                        camera.transform.position=view.Item2; camera.transform.LookAt(view.Item3);
                        yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
                        var previous=RenderTexture.active;
                        try { RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply(); File.WriteAllBytes(Path.Combine(output,view.Item1+(before?"-before.png":"-after.png")),pixels.EncodeToPNG()); }
                        finally { RenderTexture.active=previous; }
                        captured.Add(new View { name=view.Item1+(before?"-before":"-after"),position=camera.transform.position,euler=camera.transform.eulerAngles,lod=before?-1:0 });
                    }
                }
                foreach (int level in new[]{1,2})
                {
                    foreach (var group in groups) group.ForceLOD(level);
                    camera.transform.position=views.Last().Item2; camera.transform.LookAt(views.Last().Item3);
                    yield return null;
                    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination=target });
                    var previous=RenderTexture.active;
                    try { RenderTexture.active=target; pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply(); File.WriteAllBytes(Path.Combine(output,"corner-detail-lod"+level+".png"),pixels.EncodeToPNG()); }
                    finally { RenderTexture.active=previous; }
                    captured.Add(new View { name="corner-detail-lod"+level,position=camera.transform.position,euler=camera.transform.eulerAngles,lod=level });
                }
                Object.Destroy(art); yield return null; yield return null;
                foreach (var original in originals) Assert.That(original.Key.enabled,Is.EqualTo(original.Value));
                foreach (var group in groups) Assert.That(group==null,Is.True);
                foreach (var device in devices) Assert.That(device.Key.localToWorldMatrix,Is.EqualTo(device.Value));
                CollectionAssert.AreEquivalent(colliders,Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None));
                File.WriteAllText(Path.Combine(output,"installed-review.json"),JsonUtility.ToJson(new Evidence { placements=19,uniqueLodAssets=21,lodMeshInstances=57,originalFamilyColliders=19,preservedSceneColliders=colliders.Length,
                    masterSha256=production.masterSha256,sourceSha256=production.sourceSha256,materialRegionProbes=materialProbes,
                    floorRays=rays,physicsPreserved=true,navigationPreserved=true,devicesPreserved=true,cleanupPassed=true,views=captured.ToArray() },true));
            }
            finally { Time.timeScale=priorTime; target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(camera.gameObject); }
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (priorQuality==null) yield break;
            QualitySettings.renderPipeline=priorPipeline;
            File.WriteAllBytes("ProjectSettings/QualitySettings.asset",priorQuality); AssetDatabase.Refresh();
        }
    }
}
