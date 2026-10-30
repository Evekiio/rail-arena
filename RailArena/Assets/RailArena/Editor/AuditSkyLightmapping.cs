using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class AuditSkyLightmapping
    {
        [Serializable] private sealed class Report
        {
            public string scene; public int renderers, meshFilters, meshesWithUv2, contributors, realtimeLights, bakedLights, mixedLights;
            public bool lightingSettingsAssigned; public string lightmapper;
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var world=UnityEngine.Object.FindAnyObjectByType<ArenaSession>().World;
            var filters=world.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh!=null).ToArray();
            var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            bool assigned=Lightmapping.TryGetLightingSettings(out var settings);
            var report=new Report { scene=SkyGardensBuilder.ScenePath,renderers=world.GetComponentsInChildren<MeshRenderer>(true).Length,
                meshFilters=filters.Length,meshesWithUv2=filters.Count(f=>f.sharedMesh.uv2.Length==f.sharedMesh.vertexCount),
                contributors=filters.Count(f=>(GameObjectUtility.GetStaticEditorFlags(f.gameObject)&StaticEditorFlags.ContributeGI)!=0),
                realtimeLights=lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Realtime),bakedLights=lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Baked),mixedLights=lights.Count(l=>l.lightmapBakeType==LightmapBakeType.Mixed),
                lightingSettingsAssigned=assigned,lightmapper=assigned?settings.lightmapper.ToString():"none assigned" };
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--lighting-audit-output");if(i<0||i+1>=args.Length)throw new ArgumentException("Supply --lighting-audit-output");
            File.WriteAllText(args[i+1],JsonUtility.ToJson(report,true));Debug.Log("SKY_LIGHTMAPPING_AUDIT_COMPLETE");
        }
    }
}
