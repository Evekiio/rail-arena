using System;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class SkyAtmosphereArtBuilder
    {
        public static void Prepare()
        {
            var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Cloud sea.mat");
            var shader = Resources.Load<Shader>("Effects/SkyGardenClouds");
            if (original == null || shader == null) throw new InvalidOperationException("Sky source/shader missing");
            const string path = "Assets/Resources/Art/SkyGardenClouds.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(original); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader; EditorUtility.SetDirty(material);
            var profile = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            profile.CloudSkybox = material; EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
            Debug.Log("SKY_CLOUD_ART_READY shaped lower-hemisphere cloud banks; existing lighting palettes retained");
        }
    }
}
