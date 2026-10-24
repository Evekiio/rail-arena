using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace RailArena.Editor
{
    public static class ContenderPortraitBuilder
    {
        private const string Folder="Assets/Resources/UI/ContenderPortraits";
        [Serializable] private sealed class Record { public int character;public string prefab,dependencyHash,file; }
        [Serializable] private sealed class Manifest { public string unity;public Record[] portraits; }
        [MenuItem("Rail Arena/Render contender selection portraits")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Portrait authoring requires edit mode.");
            string previous=EditorSceneManager.GetActiveScene().path;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var records=new List<Record>();
            try
            {
                RenderSettings.ambientMode=AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=new Color(.56f,.68f,.75f);RenderSettings.ambientEquatorColor=new Color(.4f,.5f,.55f);RenderSettings.ambientGroundColor=new Color(.22f,.29f,.33f);
                var key=new GameObject("Portrait key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.7f;key.color=new Color(1,.95f,.88f);key.transform.rotation=Quaternion.Euler(35,150,0);key.shadows=LightShadows.None;
                var fill=new GameObject("Portrait fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.color=new Color(.75f,.88f,1);fill.transform.rotation=Quaternion.Euler(20,210,0);fill.shadows=LightShadows.None;
                var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.06f,.075f);camera.orthographic=true;camera.orthographicSize=.48f;camera.nearClipPlane=.02f;camera.farClipPlane=10;
                for(int i=0;i<OfflineRoster.CharacterCount;i++)
                {
                    string resource=OfflineRoster.PrefabPath(i);var prefab=Resources.Load<GameObject>(resource);
                    if(prefab==null)throw new InvalidOperationException("Missing contender "+resource);
                    var actor=Object.Instantiate(prefab);
                    try
                    {
                        var presentation=actor.GetComponent<ContenderPresentation>();presentation.Animator.enabled=false;
                        presentation.ApplySkin(i);presentation.Weapon.gameObject.SetActive(false);
                        actor.transform.rotation=Quaternion.Euler(0,-12,0);
                        foreach(var group in actor.GetComponentsInChildren<LODGroup>())group.ForceLOD(0);
                        var focus=presentation.Animator.GetBoneTransform(HumanBodyBones.Head).position-Vector3.up*.16f;
                        camera.transform.SetPositionAndRotation(focus+new Vector3(0,.04f,2),Quaternion.LookRotation(new Vector3(0,-.04f,-2)));
                        string path=Folder+"/Character-"+i+".png";
                        Capture(camera,path);
                        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.alphaSource=TextureImporterAlphaSource.None;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
                        string source=AssetDatabase.GetAssetPath(prefab);
                        records.Add(new Record {character=i,prefab=source,dependencyHash=AssetDatabase.GetAssetDependencyHash(source).ToString(),file=path});
                    }
                    finally {Object.DestroyImmediate(actor);}
                }
                AssetDatabase.SaveAssets();Directory.CreateDirectory("../Validation/CharacterPicker");
                File.WriteAllText("../Validation/CharacterPicker/Portraits.json",JsonUtility.ToJson(new Manifest {unity=Application.unityVersion,portraits=records.ToArray()},true));
            }
            finally {if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);}
        }
        private static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(512,640,24);var image=new Texture2D(512,640,TextureFormat.RGB24,false);var active=RenderTexture.active;
            try
            {
                target.Create();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,512,640),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally {RenderTexture.active=active;Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
