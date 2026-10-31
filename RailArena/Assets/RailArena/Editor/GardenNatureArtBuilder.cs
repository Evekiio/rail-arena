using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RailArena.Editor
{
    public static class GardenNatureArtBuilder
    {
        public const string Folder="../ArtSource/Original/ArtPass-v1/Nature/GardenNature";
        [Serializable] public sealed class Part
        {
            public string sourceName,family,meshAsset,meshGuid,vertexSha256,indexSha256;
            public Vector3 center,size,frameCenter,frameSize;
            public int vertices,triangles,colliders;
            public bool wind;
        }
        [Serializable] public sealed class Source
        {
            public string scene="Assets/RailArena/Generated/SkyGardensBasement.unity",sceneSha256;
            public string scope="Read-only installed planting authority. Bounds are world-space metres. Canonical unit families map into these original visual envelopes; existing collision/navigation/traversal remain authoritative.";
            public string axes="Blender (x,y,z) -> Unity (x,z,-y). Unit visual family pivots use bounds centre.";
            public Part[] parts;
        }
        static string Digest(byte[] data)
        {
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(data)).Replace("-","").ToLowerInvariant();
        }
        static string Family(string name)
        {
            if(name.StartsWith("TREE-",StringComparison.Ordinal))return name.EndsWith("-BARK",StringComparison.Ordinal)?"Trunk":"Crown";
            if(name.StartsWith("SHRUB-",StringComparison.Ordinal))return "Shrub";
            if(name.StartsWith("GRASS-",StringComparison.Ordinal))return "Grass";
            if(name.StartsWith("IVY-",StringComparison.Ordinal))return name.EndsWith("-STEMS",StringComparison.Ordinal)?"VineStem":"VineLeaf";
            return null;
        }
        [Serializable] sealed class Paint { public string texture,sha256; }
        [Serializable] sealed class Module { public string family; }
        [Serializable] sealed class Export { public string family,export,exportSha256; public int level,triangles; public Vector3[] canonicalVerticesBlender; }
        [Serializable] sealed class Binding { public string sourceName,module; public Vector3 center,size; public bool wind; }
        [Serializable] sealed class Production { public string master,masterSha256,sourceSha256; public Paint paint; public Module[] modules; public Export[] lods; public Binding[] bindings; }
        [Serializable] sealed class ImportView { public string family; public int level,triangles,materials; public Vector3 axisEuler; public bool importedXReflectionBaked; public string canonicalMesh; public float maximumCanonicalVertexErrorMetres; }
        [Serializable] sealed class ImportReport { public string masterSha256,sourceSha256,scope="Native model import and canonical unit-frame geometry only; no installed gameplay or artistic acceptance."; public ImportView[] models; public int bindings; }
        static float MaximumDeviation(Vector3[] actual,Vector3[] expected)
        {
            float maximum=0;
            foreach(var point in actual)
            {
                float nearest=float.PositiveInfinity;
                foreach(var candidate in expected)nearest=Mathf.Min(nearest,(point-candidate).sqrMagnitude);
                maximum=Mathf.Max(maximum,nearest);
            }
            return Mathf.Sqrt(maximum);
        }
        static float GeometryError(MeshFilter filter,Vector3[] expected)
        {
            var actual=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();
            return Mathf.Max(MaximumDeviation(actual,expected),MaximumDeviation(expected,actual));
        }
        public static void Prepare()
        {
            var spec=JsonUtility.FromJson<Production>(File.ReadAllText(Folder+"/production.json"));
            if(spec.modules.Length!=11||spec.lods.Length!=33||spec.bindings==null||spec.bindings.Length!=24)throw new InvalidOperationException("Compose the complete nature source/reference/binding package first");
            if(Digest(File.ReadAllBytes(Folder+"/"+spec.master))!=spec.masterSha256||Digest(File.ReadAllBytes(Folder+"/nature-source.json"))!=spec.sourceSha256||Digest(File.ReadAllBytes(Folder+"/"+spec.paint.texture))!=spec.paint.sha256)throw new InvalidOperationException("Nature canonical source/master/paint hash mismatch");
            const string assets="Assets/RailArena/Art/ArtPass/Nature/GardenNature/";
            string texturePath=assets+spec.paint.texture;File.Copy(Folder+"/"+spec.paint.texture,texturePath,true);AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
            var textureImporter=(TextureImporter)AssetImporter.GetAtPath(texturePath);textureImporter.textureType=TextureImporterType.Default;textureImporter.sRGBTexture=true;textureImporter.alphaSource=TextureImporterAlphaSource.None;textureImporter.mipmapEnabled=true;textureImporter.wrapMode=TextureWrapMode.Clamp;textureImporter.filterMode=FilterMode.Trilinear;textureImporter.anisoLevel=2;textureImporter.maxTextureSize=512;textureImporter.textureCompression=TextureImporterCompression.Uncompressed;textureImporter.SaveAndReimport();
            var profile=Resources.Load<CelArtProfile>("Art/SkyGardensCel");if(profile==null)throw new InvalidOperationException("Prepare the shared cel art profile first");
            string materialPath=assets+"GardenPaint.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(profile.SurfaceShader);AssetDatabase.CreateAsset(material,materialPath);}
            var template=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try{profile.Configure(material,template);}finally{Object.DestroyImmediate(template);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",Color.white);material.SetFloat("_OcclusionStrength",0);material.enableInstancing=true;EditorUtility.SetDirty(material);
            var prefabs=new System.Collections.Generic.Dictionary<string,GameObject>();var results=new System.Collections.Generic.List<ImportView>();
            foreach(var module in spec.modules)
            {
                var root=new GameObject("Garden "+module.family);int previous=int.MaxValue;
                try
                {
                    var lods=new LOD[3];
                    for(int level=0;level<3;level++)
                    {
                        var export=spec.lods.Single(item=>item.family==module.family&&item.level==level);string path=export.export.Substring("RailArena/".Length);
                        if(Digest(File.ReadAllBytes(path))!=export.exportSha256)throw new InvalidOperationException("Nature export hash mismatch: "+path);
                        var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.addCollider=false;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
                        var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        Mesh canonical;
                        try
                        {
                            var filters=model.GetComponentsInChildren<MeshFilter>(true);if(filters.Length!=1)throw new InvalidOperationException("Expected one opaque nature mesh per level");var filter=filters[0];
                            var imported=filter.sharedMesh;canonical=Object.Instantiate(imported);canonical.name=module.family+" LOD"+level+" / canonical Blender frame";
                            // Unity's FBX handedness conversion reflects X relative to the declared (x,z,-y) reference.
                            // Bake the correction into an owned mesh, preserving the untouched FBX source and positive prefab scale.
                            var matrix=Matrix4x4.Scale(new Vector3(-1,1,1))*filter.transform.localToWorldMatrix;
                            canonical.vertices=imported.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                            var normalMatrix=matrix.inverse.transpose;canonical.normals=imported.normals.Select(normal=>normalMatrix.MultiplyVector(normal).normalized).ToArray();
                            var tangents=imported.tangents;
                            for(int index=0;index<tangents.Length;index++)
                            {
                                var direction=matrix.MultiplyVector(new Vector3(tangents[index].x,tangents[index].y,tangents[index].z)).normalized;
                                tangents[index]=new Vector4(direction.x,direction.y,direction.z,tangents[index].w*(matrix.determinant<0?-1:1));
                            }
                            canonical.tangents=tangents;
                            for(int submesh=0;submesh<canonical.subMeshCount;submesh++)
                            {
                                var indices=imported.GetTriangles(submesh);
                                if(matrix.determinant<0)for(int index=0;index<indices.Length;index+=3){int swap=indices[index+1];indices[index+1]=indices[index+2];indices[index+2]=swap;}
                                canonical.SetTriangles(indices,submesh,false);
                            }
                            canonical.RecalculateBounds();
                        }
                        finally{Object.DestroyImmediate(model);}
                        var expected=export.canonicalVerticesBlender.Select(point=>new Vector3(point.x,point.z,-point.y)).ToArray();var actual=canonical.vertices;
                        float error=Mathf.Max(MaximumDeviation(actual,expected),MaximumDeviation(expected,actual));
                        if(error>.00002f){Object.DestroyImmediate(canonical);throw new InvalidOperationException("Native nature vertices differ from canonical handedness/frame: "+module.family+" LOD"+level+" error="+error);}
                        int triangles=canonical.triangles.Length/3;
                        if(triangles!=export.triangles||triangles>=previous||canonical.subMeshCount!=1){Object.DestroyImmediate(canonical);throw new InvalidOperationException("Nature triangle/material budget differs: "+module.family+" LOD"+level);}
                        if(actual.Any(point=>Mathf.Max(Mathf.Abs(point.x),Mathf.Abs(point.y),Mathf.Abs(point.z))>.50002f)){Object.DestroyImmediate(canonical);throw new InvalidOperationException("Nature geometry escaped its original reference frame");}
                        string canonicalPath=assets+module.family+"-LOD"+level+"-Canonical.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(canonicalPath);
                        if(existing==null)AssetDatabase.CreateAsset(canonical,canonicalPath);else{EditorUtility.CopySerialized(canonical,existing);Object.DestroyImmediate(canonical);canonical=existing;}
                        var visual=new GameObject(module.family+" LOD"+level);visual.transform.SetParent(root.transform,false);visual.AddComponent<MeshFilter>().sharedMesh=canonical;var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                        previous=triangles;lods[level]=new LOD(level==0?.24f:level==1?.10f:.001f,new Renderer[]{renderer});
                        results.Add(new ImportView{family=module.family,level=level,triangles=triangles,materials=1,axisEuler=Vector3.zero,importedXReflectionBaked=true,canonicalMesh=canonicalPath,maximumCanonicalVertexErrorMetres=error});
                    }
                    if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Nature must not add collision");
                    var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();prefabs.Add(module.family,PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Art/Garden"+module.family+".prefab"));
                }
                finally{Object.DestroyImmediate(root);}
            }
            const string setPath="Assets/Resources/Art/GardenNature.asset";var set=AssetDatabase.LoadAssetAtPath<GardenNatureSet>(setPath);
            if(set==null){set=ScriptableObject.CreateInstance<GardenNatureSet>();AssetDatabase.CreateAsset(set,setPath);}
            set.parts=spec.bindings.Select(binding=>new GardenNatureSet.Part{sourceName=binding.sourceName,module=binding.module,prefab=prefabs[binding.module],center=binding.center,size=binding.size,wind=binding.wind}).ToArray();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("../Validation");File.WriteAllText("../Validation/garden-nature-import.json",JsonUtility.ToJson(new ImportReport{masterSha256=spec.masterSha256,sourceSha256=spec.sourceSha256,models=results.ToArray(),bindings=24},true));
            Debug.Log("GARDEN_NATURE_NATIVE_READY 11 modules / 33 reducing LODs / one painted material each / 24 bindings / no colliders");
        }
        [Serializable] sealed class AxisProbe { public Vector3 rootPosition,rootEuler,rootScale,meshPosition,meshEuler,meshScale,actualMin,actualMax,expectedMin,expectedMax,bestPrefix; public float bestError; }
        public static void AuditImportedAxes()
        {
            var spec=JsonUtility.FromJson<Production>(File.ReadAllText(Folder+"/production.json"));var export=spec.lods.Single(item=>item.family=="Trunk"&&item.level==0);
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(export.export.Substring("RailArena/".Length)));
            try
            {
                var filter=model.GetComponentsInChildren<MeshFilter>(true).Single();var original=model.transform.localRotation;
                var actual=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();var expected=export.canonicalVerticesBlender.Select(point=>new Vector3(point.x,point.z,-point.y)).ToArray();
                var report=new AxisProbe{rootPosition=model.transform.localPosition,rootEuler=model.transform.localEulerAngles,rootScale=model.transform.localScale,meshPosition=filter.transform.localPosition,meshEuler=filter.transform.localEulerAngles,meshScale=filter.transform.localScale,actualMin=new Vector3(actual.Min(p=>p.x),actual.Min(p=>p.y),actual.Min(p=>p.z)),actualMax=new Vector3(actual.Max(p=>p.x),actual.Max(p=>p.y),actual.Max(p=>p.z)),expectedMin=new Vector3(expected.Min(p=>p.x),expected.Min(p=>p.y),expected.Min(p=>p.z)),expectedMax=new Vector3(expected.Max(p=>p.x),expected.Max(p=>p.y),expected.Max(p=>p.z)),bestError=float.PositiveInfinity};
                foreach(int x in new[]{0,90,180,270})foreach(int y in new[]{0,90,180,270})foreach(int z in new[]{0,90,180,270})
                {
                    var prefix=Quaternion.Euler(x,y,z);model.transform.localRotation=prefix*original;float error=GeometryError(filter,expected);
                    if(error<report.bestError){report.bestError=error;report.bestPrefix=new Vector3(x,y,z);}
                }
                File.WriteAllText("../Validation/garden-nature-native-axes.json",JsonUtility.ToJson(report,true));Debug.Log("GARDEN_NATURE_AXES "+JsonUtility.ToJson(report));
            }
            finally{Object.DestroyImmediate(model);}
        }
        public static void Audit()
        {
            const string scene="Assets/RailArena/Generated/SkyGardensBasement.unity";byte[] before=File.ReadAllBytes(scene);
            EditorSceneManager.OpenScene(scene);var world=Object.FindAnyObjectByType<ArenaSession>().World;
            var parts=world.GetComponentsInChildren<MeshRenderer>(true).Where(renderer=>Family(renderer.name)!=null).OrderBy(renderer=>renderer.name,StringComparer.Ordinal).Select(renderer=>
            {
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;string path=AssetDatabase.GetAssetPath(mesh);
                var vertices=mesh.vertices;var indices=mesh.triangles;
                byte[] vertexBytes=new byte[vertices.Length*12];for(int i=0;i<vertices.Length;i++){Buffer.BlockCopy(BitConverter.GetBytes(vertices[i].x),0,vertexBytes,i*12,4);Buffer.BlockCopy(BitConverter.GetBytes(vertices[i].y),0,vertexBytes,i*12+4,4);Buffer.BlockCopy(BitConverter.GetBytes(vertices[i].z),0,vertexBytes,i*12+8,4);}
                byte[] indexBytes=new byte[indices.Length*4];Buffer.BlockCopy(indices,0,indexBytes,0,indexBytes.Length);
                return new Part{sourceName=renderer.name,family=Family(renderer.name),center=renderer.bounds.center,size=renderer.bounds.size,frameCenter=renderer.bounds.center,frameSize=renderer.bounds.size,meshAsset=path,meshGuid=AssetDatabase.AssetPathToGUID(path),vertexSha256=Digest(vertexBytes),indexSha256=Digest(indexBytes),vertices=vertices.Length,triangles=indices.Length/3,colliders=renderer.GetComponents<Collider>().Length,wind=renderer.GetComponent<SkyGardenMotion>()!=null};
            }).ToArray();
            foreach(var pair in parts.Where(part=>part.sourceName.StartsWith("IVY-",StringComparison.Ordinal)).GroupBy(part=>part.sourceName.Split('-')[1]))
            {
                var bounds=new Bounds(pair.First().center,pair.First().size);
                foreach(var part in pair)bounds.Encapsulate(new Bounds(part.center,part.size));
                foreach(var part in pair){part.frameCenter=bounds.center;part.frameSize=bounds.size;}
            }
            if(parts.Length!=24||parts.GroupBy(part=>part.family).Any(group=>group.Count()!=4))throw new InvalidOperationException("Expected four installed plantings with six visual families");
            if(!before.SequenceEqual(File.ReadAllBytes(scene)))throw new InvalidOperationException("Read-only nature audit changed saved scene bytes");
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/nature-source.json",JsonUtility.ToJson(new Source{sceneSha256=Digest(before),parts=parts},true));
            Debug.Log("GARDEN_NATURE_SOURCE_AUDITED 24 renderings / six families / four installed sites; original scene unchanged");
        }
    }
}
