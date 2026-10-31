using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace RailArena.Tests
{
    public sealed class HandBindingReview
    {
        [Serializable] sealed class BoneRecord { public string name; public Vector3 sourceHeadMesh, targetHeadMesh; public float physicalHeadDifference; public float[] sourceBind, targetBind; }
        [Serializable] sealed class Report { public string scope="Read-only source versus installed calibration; distances use imported renderer scale. No gameplay or geometry writes."; public Vector3 meshScale; public BoneRecord[] bones; }
        static float[] Values(Matrix4x4 matrix)=>Enumerable.Range(0,16).Select(index=>matrix[index/4,index%4]).ToArray();
        [Test] public void PreserveGeometryAndAuthoredFingerBindings()
        {
            int fingers=0,bodyMatrices=0,streams=0;
            foreach(string identity in new[]{"Player","Bot01","Bot02","Bot03"})
            {
                string prefab=identity=="Player"?"Assets/Resources/Characters/Contender.prefab":"Assets/Resources/Characters/Stylized/"+identity+".prefab";
                var installed=AssetDatabase.LoadAssetAtPath<GameObject>(prefab).GetComponent<ContenderPresentation>();
                for(int level=0;level<3;level++)
                {
                    string prefix="Assets/RailArena/Art/ArtPass/Characters/"+identity+"_LOD"+level;
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(prefix+".fbx").GetComponentInChildren<SkinnedMeshRenderer>();
                    var target=AssetDatabase.LoadAssetAtPath<Mesh>(prefix+"-Calibrated.asset");
                    Assert.That(target.vertexCount,Is.EqualTo(source.sharedMesh.vertexCount));Assert.That(target.triangles,Is.EqualTo(source.sharedMesh.triangles));
                    using(var a=MeshUtility.AcquireReadOnlyMeshData(source.sharedMesh))
                    using(var b=MeshUtility.AcquireReadOnlyMeshData(target))
                    {
                        Assert.That(target.vertexBufferCount,Is.EqualTo(source.sharedMesh.vertexBufferCount));
                        for(int stream=0;stream<target.vertexBufferCount;stream++)
                        { Assert.That(a[0].GetVertexData<byte>(stream).ToArray().SequenceEqual(b[0].GetVertexData<byte>(stream).ToArray()),Is.True,identity+" LOD"+level+" vertex stream "+stream);streams++; }
                    }
                    var legacy=(SkinnedMeshRenderer)installed.BodyRenderers[level];
                    for(int index=0;index<source.bones.Length;index++)
                    {
                        string name=source.bones[index].name;bool finger=(name.StartsWith("LeftHand",StringComparison.Ordinal)&&name!="LeftHand")||(name.StartsWith("RightHand",StringComparison.Ordinal)&&name!="RightHand");
                        var expected=finger?source.sharedMesh.bindposes[index]:legacy.sharedMesh.bindposes[Array.FindIndex(legacy.bones,bone=>bone.name==name)];
                        Assert.That(target.bindposes[index],Is.EqualTo(expected),identity+" LOD"+level+" "+name);
                        if(finger)fingers++;else bodyMatrices++;
                    }
                    if(level==0)Assert.That(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/RailArena/Art/ArtPass/Characters/"+identity+"-Arms.asset").bindposes,Is.EqualTo(target.bindposes));
                }
            }
            Assert.That(fingers,Is.EqualTo(360));
            File.WriteAllText("../Validation/fps-hand-binding-contract.txt",$"PASS: {fingers} authored finger bind matrices; {bodyMatrices} unchanged legacy body matrices; {streams} byte-identical vertex streams; topology preserved; four arm skins share corrected LOD0 bind data.");
            TestContext.WriteLine(File.ReadAllText("../Validation/fps-hand-binding-contract.txt"));
        }
        [Test] public void InspectFingerBindings()
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RailArena/Art/ArtPass/Characters/Player_LOD0.fbx");
            var source=model.GetComponentInChildren<SkinnedMeshRenderer>();
            var target=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/RailArena/Art/ArtPass/Characters/Player_LOD0-Calibrated.asset");
            Assert.That(source.sharedMesh.vertexCount,Is.EqualTo(target.vertexCount));Assert.That(source.sharedMesh.vertices,Is.EqualTo(target.vertices));
            var frame=model.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
            var records=source.bones.Select((bone,index)=>{
                var before=source.sharedMesh.bindposes[index];var after=target.bindposes[index];
                Vector3 a=before.inverse.GetColumn(3),b=after.inverse.GetColumn(3);
                return new BoneRecord{name=bone.name,sourceHeadMesh=a,targetHeadMesh=b,physicalHeadDifference=frame.MultiplyVector(a-b).magnitude,sourceBind=Values(before),targetBind=Values(after)};
            }).ToArray();
            Directory.CreateDirectory("../Validation");File.WriteAllText("../Validation/fps-hand-bind-frames.json",JsonUtility.ToJson(new Report{meshScale=frame.lossyScale,bones=records},true));
            TestContext.WriteLine("FINGER_BIND_FRAMES_READ "+records.Length+" bones; raw geometry unchanged");
        }
    }
}
