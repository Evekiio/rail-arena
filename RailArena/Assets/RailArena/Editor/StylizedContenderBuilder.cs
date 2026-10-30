using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Replaces presentation meshes while retaining the calibrated animation and gameplay rig.</summary>
    public static class StylizedContenderBuilder
    {
        public const string ArtRoot = "Assets/RailArena/Art/StylizedContenders";
        private static readonly string[] Names = { "Player", "Bot01", "Bot02", "Bot03" };
        public static bool ModelsReady => Names.All(name => File.Exists($"{ArtRoot}/{name}_LOD0.fbx"));

        [MenuItem("Rail Arena/Build approved stylized contenders")]
        public static void Build()
        {
            if (!ModelsReady) throw new InvalidOperationException("Generate all four stylized contenders with tools/build_stylized_contenders.py first.");
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/Resources/Characters/Stylized");
            // Build bots before replacing the shared player template.
            foreach (string name in new[] { "Bot01", "Bot02", "Bot03", "Player" }) BuildCharacter(name);
            FirstPersonArmsBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("STYLIZED_CONTENDERS_BUILT: four original meshes, shared calibrated humanoid rig, three LODs and first-person arms.");
        }

        private static Material Palette(string name)
        {
            string path = $"{ArtRoot}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name + " palette" };
                AssetDatabase.CreateAsset(material, path);
            }
            string texturePath = $"{ArtRoot}/{name}-Palette.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", .08f);
            material.SetFloat("_Smoothness", .35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildCharacter(string name)
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Resources/Characters/Contender.prefab");
            try
            {
                root.name = "Stylized " + name;
                var presentation = root.GetComponent<ContenderPresentation>();
                var bones = root.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name)
                    .ToDictionary(group => group.Key, group => group.First());
                var previous = presentation.BodyRenderers;
                var material = Palette(name);
                var renderers = new List<Renderer>();
                var lods = new LOD[3];
                for (int level = 0; level < 3; level++)
                {
                    string path = $"{ArtRoot}/{name}_LOD{level}.fbx";
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = false;
                    importer.isReadable = true;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.SaveAndReimport();
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var avatar = model.GetComponent<Animator>()?.avatar;
                    if (avatar == null || !avatar.isHuman || !avatar.isValid)
                        throw new InvalidOperationException(name + " requires a valid imported humanoid Avatar.");
                    var imported = model.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (imported == null || imported.sharedMesh.subMeshCount != 1)
                        throw new InvalidOperationException(name + " requires one palette-mapped skin per LOD.");
                    var body = new GameObject(name + " body LOD" + level);
                    body.transform.SetParent(root.transform, false);
                    var local = model.transform.worldToLocalMatrix * imported.transform.localToWorldMatrix;
                    body.transform.localPosition = local.GetColumn(3);
                    body.transform.localRotation = local.rotation;
                    body.transform.localScale = local.lossyScale;
                    var renderer = body.AddComponent<SkinnedMeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.bones = imported.bones.Select(b => bones[b.name]).ToArray();
                    renderer.rootBone = bones[imported.rootBone.name];
                    // The retained legacy rig has different FBX bone scales. Rebind in its rest space.
                    var mesh = Object.Instantiate(imported.sharedMesh);
                    mesh.name = name + " calibrated skin LOD" + level;
                    mesh.bindposes = renderer.bones.Select(bone =>
                        bone.worldToLocalMatrix * renderer.transform.localToWorldMatrix).ToArray();
                    string meshPath = $"{ArtRoot}/{name}_LOD{level}-Skin.asset";
                    var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, meshPath); savedMesh = mesh; }
                    else { EditorUtility.CopySerialized(mesh, savedMesh); Object.DestroyImmediate(mesh); }
                    renderer.sharedMesh = savedMesh;
                    renderer.localBounds = imported.localBounds;
                    renderer.quality = SkinQuality.Bone4;
                    renderer.updateWhenOffscreen = true;
                    renderers.Add(renderer);
                    lods[level] = new LOD(new[] { .18f, .065f, .012f }[level], new Renderer[] { renderer });
                }
                presentation.BodyRenderers = renderers.ToArray();
                presentation.Skins = new[] { material };
                var group = root.GetComponent<LODGroup>();
                group.SetLODs(lods);
                group.RecalculateBounds();
                foreach (var renderer in previous)
                {
                    if (renderer.transform == root.transform) throw new InvalidOperationException("Cannot remove a renderer on the rig root.");
                    Object.DestroyImmediate(renderer.gameObject);
                }
                string output = name == "Player" ? "Assets/Resources/Characters/Contender.prefab" :
                    $"Assets/Resources/Characters/Stylized/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, output);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
