using System.Collections;
using System.IO;
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
    public sealed class SummitStoneArtReview
    {
        byte[] priorQuality;
        RenderPipelineAsset priorPipeline;
        [UnityTest]
        public IEnumerator InstalledSummitPreservesJumpSurfacesAndReturns()
        {
            priorQuality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset"); priorPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity"); yield return new EnterPlayMode();
            var session = Object.FindAnyObjectByType<ArenaSession>(); session.StartSession(true,0,1,TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance(); session.enabled = false; session.View.enabled = false;
            var sources = session.World.GetComponentsInChildren<MeshRenderer>().Where(renderer => renderer.name == "SUMMIT-DECK"
                || renderer.name == "SUMMIT-CENTRAL-COVER" || renderer.name.StartsWith("SUMMIT-COLUMN-",System.StringComparison.Ordinal)).ToArray();
            Assert.That(sources.Length,Is.EqualTo(6));
            var meshes = sources.ToDictionary(renderer => renderer,renderer => renderer.GetComponent<MeshCollider>().sharedMesh);
            var vertices = sources.ToDictionary(renderer => renderer,renderer => meshes[renderer].vertices);
            var triangles = sources.ToDictionary(renderer => renderer,renderer => meshes[renderer].triangles);
            var matrices = sources.ToDictionary(renderer => renderer,renderer => renderer.localToWorldMatrix);
            var colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var devices = session.World.Pads.Select(pad => pad.transform).Concat(session.World.Portals.Select(gate => gate.transform))
                .ToDictionary(transform => transform,transform => transform.localToWorldMatrix);
            var destinations = session.World.Pads.ToDictionary(pad => pad,pad => pad.Destination);
            var art = new GameObject("Owned summit stone review").AddComponent<ArtSlicePresentation>(); yield return null; yield return null;
            var groups = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None).Where(group => group.name.EndsWith(" / Authored summit stone",System.StringComparison.Ordinal)).ToArray();
            Assert.That(groups.Length,Is.EqualTo(6));
            CollectionAssert.AreEquivalent(colliders,Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None));
            foreach (var device in devices) Assert.That(device.Key.localToWorldMatrix,Is.EqualTo(device.Value));
            foreach (var destination in destinations) Assert.That(destination.Key.Destination,Is.EqualTo(destination.Value));
            foreach (var source in sources)
            {
                var collider = source.GetComponent<MeshCollider>(); var mesh = meshes[source];
                Assert.That(collider.sharedMesh,Is.SameAs(mesh)); Assert.That(source.localToWorldMatrix,Is.EqualTo(matrices[source]));
                CollectionAssert.AreEqual(vertices[source],mesh.vertices); CollectionAssert.AreEqual(triangles[source],mesh.triangles);
                Assert.That(source.enabled,Is.False);
                var topLocal = new Vector3(mesh.bounds.center.x,mesh.bounds.max.y,mesh.bounds.center.z);
                var top = source.transform.TransformPoint(topLocal);
                var points = mesh.vertices.Where(vertex => Mathf.Abs(vertex.y-mesh.bounds.max.y)<.00001f)
                    .Select(vertex => Vector3.Scale(vertex-topLocal,source.transform.lossyScale))
                    .Select(vertex => new Vector2(vertex.x,vertex.z)).Distinct().OrderBy(point => Mathf.Atan2(point.y,point.x)).ToArray();
                Assert.That(points.Length,Is.GreaterThanOrEqualTo(4));
                var group = groups.Single(item => item.name == source.name+" / Authored summit stone");
                Assert.That(group.GetComponentsInChildren<Collider>().Length,Is.Zero); Assert.That(group.GetLODs().Length,Is.EqualTo(3));
                int previous = int.MaxValue;
                foreach (var lod in group.GetLODs())
                {
                    var renderer = lod.renderers.Single(); var filter = renderer.GetComponent<MeshFilter>();
                    int count = filter.sharedMesh.triangles.Length/3; Assert.That(count,Is.LessThan(previous)); previous = count;
                    foreach (var vertex in filter.sharedMesh.vertices)
                    {
                        var relative = filter.transform.TransformPoint(vertex)-top;
                        Assert.That(relative.y,Is.InRange(-collider.bounds.size.y-.0001f,.0001f));
                        var point = new Vector2(relative.x,relative.z);
                        for (int index=0;index<points.Length;index++)
                        {
                            var edge=points[(index+1)%points.Length]-points[index]; var delta=point-points[index];
                            Assert.That(edge.x*delta.y-edge.y*delta.x,Is.GreaterThanOrEqualTo(-.0001f),source.name+" visual outside original convex hull");
                        }
                    }
                    Assert.That(renderer.sharedMaterials.All(material => material!=null&&material.shader.name.Contains("Cel")),Is.True);
                }
            }
            string output=Path.GetFullPath("../ArtSource/Original/ArtPass-v1/Environment/SummitStone/UnityReview"); Directory.CreateDirectory(output);
            var camera=new GameObject("Owned summit comparison camera").AddComponent<Camera>(); camera.enabled=false;
            camera.cullingMask=(1<<0)|(1<<9); camera.fieldOfView=65; camera.nearClipPlane=.08f; camera.farClipPlane=1000;
            var target=new RenderTexture(1920,1080,24); target.Create(); var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            float priorTimeScale=Time.timeScale; Time.timeScale=0;
            try
            {
                foreach (bool before in new[]{true,false})
                {
                    foreach(var group in groups){group.gameObject.SetActive(!before);group.ForceLOD(0);} foreach(var source in sources)source.enabled=before;
                    foreach(var view in new[]{
                        ("summit-overview",new Vector3(12,31,12),new Vector3(0,22,0)),
                        ("summit-gameplay",new Vector3(1.5f,23.6f,-4.5f),new Vector3(0,23,4)),
                        ("summit-underside",new Vector3(10,16,10),new Vector3(0,21.7f,0)),
                        ("summit-takeoff",new Vector3(4,23.6f,3.7f),new Vector3(16,14,16))})
                    {
                        camera.transform.position=view.Item2; camera.transform.LookAt(view.Item3); yield return null;
                        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target}); var old=RenderTexture.active;
                        try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,view.Item1+(before?"-before.png":"-after.png")),pixels.EncodeToPNG());}
                        finally{RenderTexture.active=old;}
                    }
                }
                Object.Destroy(art); yield return null; yield return null;
                foreach(var source in sources){Assert.That(source.enabled,Is.True);Assert.That(source.GetComponent<MeshCollider>().sharedMesh,Is.SameAs(meshes[source]));}
                foreach(var group in groups)Assert.That(group==null,Is.True);
                foreach(var destination in destinations)Assert.That(destination.Key.Destination,Is.EqualTo(destination.Value));
            }
            finally{Time.timeScale=priorTimeScale;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(camera.gameObject);}
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(Application.isPlaying)yield return new ExitPlayMode();
            if(priorQuality==null)yield break;
            QualitySettings.renderPipeline=priorPipeline;File.WriteAllBytes("ProjectSettings/QualitySettings.asset",priorQuality);AssetDatabase.Refresh();
        }
    }
}
