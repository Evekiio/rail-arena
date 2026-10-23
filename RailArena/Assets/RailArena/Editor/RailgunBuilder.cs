using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    /// <summary>Reproducible builder for the approved 1m precision carbine behind the shared weapon entry point.</summary>
    public static class RailgunBuilder
    {
        public const string Root="Assets/RailArena/Art/PrecisionCarbine";
        [MenuItem("Rail Arena/Build production carbine")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string name in new[]{"BaseColor","Normal","MetallicSmoothness","Emission"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+name+".png");
                if(importer==null)throw new InvalidOperationException("Generate production carbine textures first.");
                importer.textureType=name=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=name=="BaseColor"||name=="Emission";
                importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var surface=Material("Surface",false);var energy=Material("Energy",true);
            var root=new GameObject("Precision Carbine");
            try
            {
                var visual=root.AddComponent<RailgunPresentation>();var lods=new LOD[3];
                var emitters=new List<Renderer>();var rear=new List<Renderer>();var triggers=new List<Transform>();
                for(int level=0;level<3;level++)
                {
                    string path=Root+"/Carbine_LOD"+level+".fbx";
                    var importer=AssetImporter.GetAtPath(path) as ModelImporter;
                    if(importer==null)throw new InvalidOperationException("Run tools/build_carbine_production.py first.");
                    importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.None;
                    importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;
                    importer.importCameras=importer.importLights=false;importer.SaveAndReimport();
                    var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
                    model.name="Carbine LOD"+level;
                    var renderers=model.GetComponentsInChildren<MeshRenderer>();
                    foreach(var renderer in renderers)
                    {
                        bool emissive=renderer.name.EndsWith("Energy");
                        renderer.sharedMaterials=Enumerable.Repeat(emissive?energy:surface,renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount).ToArray();
                        if(emissive)emitters.Add(renderer);
                        if(new[]{"RF_01_","RF_02_","RF_03_"}.Any(renderer.name.StartsWith))rear.Add(renderer);
                        if(renderer.name.Contains("_trigger_"))triggers.Add(renderer.transform);
                    }
                    lods[level]=new LOD(level==0?.24f:level==1?.10f:.01f,renderers);
                }
                Transform Anchor(string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(root.transform,false);t.localPosition=position;return t;}
                visual.Muzzle=Anchor("Muzzle",new Vector3(0,.002f,.501f));
                visual.RightGrip=Anchor("Right grip",Vector3.zero);visual.LeftGrip=Anchor("Left support",Vector3.zero);
                visual.Aim=Anchor("Aim",new Vector3(0,.115f,-.013f));
                visual.Shoulder=Anchor("Shoulder",new Vector3(0,-.025f,-.497f));
                visual.Actuators=Array.Empty<Transform>();visual.Triggers=triggers.ToArray();visual.EnergySurfaces=emitters.ToArray();visual.RearSurfaces=rear.ToArray();
                HandGripAuthoring.Apply(visual);
                var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Weapons/Railgun.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            ContenderBuilder.UpdateWeapon();
            FirstPersonArmsBuilder.Build();
            AssetDatabase.SaveAssets();
        }
        static Material Material(string name,bool emission)
        {
            string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",Texture("BaseColor"));
            mat.SetTexture("_BumpMap",Texture("Normal"));mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness",1);mat.SetFloat("_SmoothnessTextureChannel",0);
            if(emission){mat.SetTexture("_EmissionMap",Texture("Emission"));mat.SetColor("_EmissionColor",Color.white);mat.EnableKeyword("_EMISSION");}
            EditorUtility.SetDirty(mat);return mat;
        }
        static Texture2D Texture(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+name+".png");
    }
}
