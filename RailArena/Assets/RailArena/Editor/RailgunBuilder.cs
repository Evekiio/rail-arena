using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class RailgunBuilder
    {
        private const string Root = "Assets/RailArena/Art/Railgun";

        [MenuItem("Rail Arena/Build detailed railgun")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Weapons");
            AssetDatabase.Refresh();
            var materials = new Dictionary<string, Material>
            {
                ["Gunmetal"] = Surface("Gunmetal", new Color(.045f, .065f, .085f), .85f, .72f),
                ["Titanium"] = Surface("Titanium", new Color(.39f, .46f, .52f), .85f, .76f),
                ["Ceramic"] = Surface("Ceramic", new Color(.71f, .75f, .72f), .15f, .66f),
                ["Copper"] = Surface("Copper", new Color(.56f, .19f, .055f), .8f, .73f),
                ["Rubber"] = Surface("Rubber", new Color(.014f, .022f, .028f), .05f, .38f),
                ["Energy"] = Surface("Energy", new Color(.015f, .65f, 1), .25f, .78f, true)
            };
            var root = new GameObject("RA-01 Induction Accelerator");
            try
            {
                var visual = root.AddComponent<RailgunPresentation>();
                var lods = new LOD[2];
                var actuators = new List<Transform>();
                var energy = new List<Renderer>();
                for (int level = 0; level < 2; level++)
                {
                    string path = Root + "/Railgun_LOD" + level + ".fbx";
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) throw new InvalidOperationException("Generate the railgun FBX sources with tools/build_railgun.py first.");
                    importer.importAnimation = false;
                    importer.animationType = ModelImporterAnimationType.None;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.importCameras = importer.importLights = false;
                    importer.SaveAndReimport();
                    var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                    model.name = "Detail LOD" + level;
                    var renderers = model.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in renderers)
                    {
                        string surface = materials.Keys.FirstOrDefault(key => renderer.name.Contains("_" + key));
                        if (surface == null) throw new InvalidOperationException("Unknown railgun surface: " + renderer.name);
                        renderer.sharedMaterials = Enumerable.Repeat(materials[surface], renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount).ToArray();
                        if (surface == "Energy") energy.Add(renderer);
                        if (renderer.name.StartsWith("Actuator")) actuators.Add(renderer.transform);
                    }
                    lods[level] = new LOD(level == 0 ? .18f : .01f, renderers);
                }
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(root.transform, false); muzzle.localPosition = new Vector3(0, .015f, .742f);
                visual.Muzzle = muzzle; visual.Actuators = actuators.ToArray(); visual.EnergySurfaces = energy.ToArray();
                var group = root.AddComponent<LODGroup>(); group.SetLODs(lods); group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Weapons/Railgun.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            ContenderBuilder.UpdateWeapon();
            AssetDatabase.SaveAssets();
        }

        private static Material Surface(string name, Color color, float metallic, float smoothness, bool emission = false)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            { material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
            if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 2.5f); }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
