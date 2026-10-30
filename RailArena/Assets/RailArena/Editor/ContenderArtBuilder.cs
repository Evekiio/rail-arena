using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class ContenderArtBuilder
    {
        const string Root = "Assets/RailArena/Art/ArtPass/Characters/";
        public static void Prepare()
        {
            var cel = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            const string outlinePath = "Assets/Resources/Art/CharacterContour.mat";
            var outline = AssetDatabase.LoadAssetAtPath<Material>(outlinePath);
            if (outline == null) { outline = new Material(Resources.Load<Shader>("Effects/CelOutline")); AssetDatabase.CreateAsset(outline, outlinePath); }
            outline.SetColor("_OutlineColor", new Color(.12f,.16f,.19f).linear); outline.SetFloat("_OutlinePixels", 1.25f);
            outline.SetFloat("_FadeStart", 12); outline.SetFloat("_FadeEnd", 38); EditorUtility.SetDirty(outline);
            var variants = new List<ContenderArtSet.Variant>();
            foreach (string name in new[] { "Player", "Bot01", "Bot02", "Bot03" })
            {
                var lods = new List<ContenderArtSet.SkinMesh>();
                SkinnedMeshRenderer first = null;
                for (int level = 0; level < 3; level++)
                {
                    string path = Root + name + "_LOD" + level + ".fbx";
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) throw new InvalidOperationException("Refined contender export missing: " + path);
                    importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.Generic;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.isReadable = true;
                    importer.importCameras = importer.importLights = false; importer.SaveAndReimport();
                    var renderer = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<SkinnedMeshRenderer>();
                    if (level == 0) first = renderer;
                    var old = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RailArena/Art/StylizedContenders/" + name + "_LOD" + level + ".fbx").GetComponentInChildren<SkinnedMeshRenderer>();
                    ValidateBindPoses(old, renderer);
                    // The installed avatar uses calibrated legacy FBX bone scales, not the source FBX bind space.
                    string prefabPath = name == "Player" ? "Assets/Resources/Characters/Contender.prefab" : "Assets/Resources/Characters/Stylized/" + name + ".prefab";
                    var installed = (SkinnedMeshRenderer)AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<ContenderPresentation>().BodyRenderers[level];
                    var installedPoses = installed.sharedMesh.bindposes;
                    var calibrated = UnityEngine.Object.Instantiate(renderer.sharedMesh);
                    calibrated.name = name + " art-pass calibrated LOD" + level;
                    calibrated.bindposes = renderer.bones.Select(b => installedPoses[Array.FindIndex(installed.bones, previous => previous.name == b.name)]).ToArray();
                    string calibratedPath = Root + name + "_LOD" + level + "-Calibrated.asset";
                    var saved = AssetDatabase.LoadAssetAtPath<Mesh>(calibratedPath);
                    if (saved == null) { AssetDatabase.CreateAsset(calibrated, calibratedPath); saved = calibrated; }
                    else { EditorUtility.CopySerialized(calibrated, saved); UnityEngine.Object.DestroyImmediate(calibrated); }
                    lods.Add(new ContenderArtSet.SkinMesh { mesh = saved, bones = renderer.bones.Select(b => b.name).ToArray() });
                }
                string texturePath = Root + name + "-Painted.png";
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                textureImporter.sRGBTexture = true; textureImporter.mipmapEnabled = true; textureImporter.maxTextureSize = 2048;
                textureImporter.wrapMode = TextureWrapMode.Clamp; textureImporter.SaveAndReimport();
                string materialPath = Root + name + "-Cel.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(cel.SurfaceShader); AssetDatabase.CreateAsset(material, materialPath); }
                using (var source = new DisposableMaterial()) cel.Configure(material, source.Value);
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_OcclusionStrength", 0); material.enableInstancing = true; EditorUtility.SetDirty(material);
                material.SetColor("_ShadowTint", new Color(.79f,.78f,.77f).linear);
                material.SetColor("_MidTint", new Color(.93f,.94f,.95f).linear);
                var armBones = new HashSet<int>();
                for (int i = 0; i < first.bones.Length; i++)
                    if (first.bones[i].name.Contains("ForeArm") || first.bones[i].name.Contains("Hand")) armBones.Add(i);
                var arms = FirstPersonArmsBuilder.ExtractArms(lods[0].mesh, armBones);
                string armPath = Root + name + "-Arms.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(armPath);
                if (existing == null) AssetDatabase.CreateAsset(arms, armPath);
                else { EditorUtility.CopySerialized(arms, existing); UnityEngine.Object.DestroyImmediate(arms); arms = existing; }
                variants.Add(new ContenderArtSet.Variant { identity = name, lods = lods.ToArray(), surface = material, outline = outline,
                    arms = new ContenderArtSet.SkinMesh { mesh = arms, bones = first.bones.Select(b => b.name).ToArray() } });
                Debug.Log("CONTENDER_ART_IMPORT " + name + " rig bind poses verified; 3 LODs; arm extraction " + arms.vertexCount);
            }
            const string profilePath = "Assets/Resources/Art/Contenders.asset";
            var set = AssetDatabase.LoadAssetAtPath<ContenderArtSet>(profilePath);
            if (set == null) { set = ScriptableObject.CreateInstance<ContenderArtSet>(); AssetDatabase.CreateAsset(set, profilePath); }
            set.variants = variants.ToArray();
            set.locomotionController = Resources.Load<RuntimeAnimatorController>("Art/ContenderRetreat");
            if (set.locomotionController == null) throw new InvalidOperationException("Authored retreat controller is missing");
            EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
        }

        sealed class DisposableMaterial : IDisposable
        {
            public Material Value = new(Shader.Find("Universal Render Pipeline/Lit"));
            public void Dispose() => UnityEngine.Object.DestroyImmediate(Value);
        }

        static void ValidateBindPoses(SkinnedMeshRenderer original, SkinnedMeshRenderer refined)
        {
            var lookup = original.bones.Select((bone, index) => (bone.name, index)).ToDictionary(v => v.name, v => v.index);
            var old = original.sharedMesh.bindposes; var current = refined.sharedMesh.bindposes;
            for (int i = 0; i < refined.bones.Length; i++)
            {
                if (!lookup.TryGetValue(refined.bones[i].name, out int index)) throw new InvalidOperationException("Unmatched skin bone " + refined.bones[i].name);
                for (int element = 0; element < 16; element++)
                    if (Mathf.Abs(old[index][element] - current[i][element]) > .0005f) throw new InvalidOperationException("Changed bind pose for " + refined.bones[i].name);
            }
        }
    }
}
