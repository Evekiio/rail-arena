using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class SkyNatureArtBuilder
    {
        [Serializable] sealed class Palette { public float[] LeafShade, LeafJade, LeafLight, LeafSun; }
        [Serializable] sealed class Source { public Palette paletteSrgb; }
        public static void Prepare()
        {
            const string root = "Assets/RailArena/Art/ArtPass/Nature/";
            var source = JsonUtility.FromJson<Source>(File.ReadAllText("../ArtSource/Original/ArtPass-v1/Nature/Canopy/production.json"));
            string[] names = { "LeafShade", "LeafJade", "LeafLight", "LeafSun" };
            float[][] colors = { source.paletteSrgb.LeafShade, source.paletteSrgb.LeafJade, source.paletteSrgb.LeafLight, source.paletteSrgb.LeafSun };
            var profile = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            var materials = new Material[4];
            var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    string path = root + names[i] + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) { material = new Material(profile.SurfaceShader); AssetDatabase.CreateAsset(material, path); }
                    profile.Configure(material, lit);
                    material.SetColor("_BaseColor", new Color(colors[i][0], colors[i][1], colors[i][2]).linear);
                    material.SetFloat("_OcclusionStrength", 0); material.enableInstancing = true;
                    materials[i] = material; EditorUtility.SetDirty(material);
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + "SkyGarden-Canopy-v1.fbx");
                if (model == null) throw new InvalidOperationException("Original canopy export is missing");
                var instance = UnityEngine.Object.Instantiate(model);
                try
                {
                    var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers)
                    {
                        bounds.Encapsulate(renderer.bounds);
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials[Array.IndexOf(names, m.name)]).ToArray();
                        renderer.gameObject.AddComponent<SkyGardenMotion>();
                    }
                    if ((bounds.size - Vector3.one).sqrMagnitude > .0001f || bounds.center.sqrMagnitude > .0001f) throw new InvalidOperationException("Canopy metre scale/pivot mismatch");
                    if (instance.GetComponentsInChildren<Collider>().Length != 0) throw new InvalidOperationException("Leaf art must not add collision");
                    PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Resources/Art/Canopy.prefab");
                    Debug.Log("SKY_NATURE_IMPORT unit envelope; centered pivot; 4 cel palettes; 0 colliders");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            finally { UnityEngine.Object.DestroyImmediate(lit); }
            AssetDatabase.SaveAssets();
        }
    }
}
