using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX.SDF;

namespace RailArena.Editor
{
    public static class RampSdfAssetCandidate
    {
        [Serializable] private sealed class Metadata
        {
            public string sourceGuid;
            public string sourceDependencyHash;
            public string generatorDependencyHash;
            public string unityVersion;
            public string vfxPackageVersion;
            public float padding;
            public int signPasses;
            public float threshold;
            public float sdfOffset;
            public Vector3 center;
            public Vector3 size;
            public int maxResolution;
        }
        public static void Create()
        {
            const string source = "Assets/RailArena/Generated/SkyGardensArt/R1.asset";
            const string target = "Assets/Validation/TraversalVfxCandidate/R1Sdf64V3.asset";
            const string metadataPath = "Assets/Validation/TraversalVfxCandidate/R1Sdf64V3.json";
            if (AssetDatabase.LoadAssetAtPath<Texture3D>(target) != null || File.Exists(metadataPath)) throw new InvalidOperationException("Candidate already exists");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(source); if (mesh == null) throw new InvalidOperationException("Ramp mesh not found");
            var bounds = mesh.bounds;
            using (var baker = new MeshToSDFBaker(bounds.size + Vector3.one * .8f, bounds.center, 64, mesh, 1, .5f, 0f))
            {
                baker.BakeSDF(); var sourceTexture = baker.SdfTexture;
                var request = AsyncGPUReadback.Request(sourceTexture, 0, 0, sourceTexture.width, 0, sourceTexture.height, 0, sourceTexture.volumeDepth);
                request.WaitForCompletion(); if (request.hasError) throw new InvalidOperationException("SDF readback failed");
                if (request.layerCount != sourceTexture.volumeDepth) throw new InvalidOperationException("Unexpected SDF slice count");
                int sliceLength = sourceTexture.width * sourceTexture.height; var voxels = new ushort[sliceLength * sourceTexture.volumeDepth];
                for (int z = 0; z < request.layerCount; z++)
                {
                    var slice = request.GetData<ushort>(z); if (slice.Length != sliceLength) throw new InvalidOperationException("Unexpected SDF slice format");
                    for (int i = 0; i < sliceLength; i++) voxels[z * sliceLength + i] = slice[i];
                }
                var texture = new Texture3D(sourceTexture.width, sourceTexture.height, sourceTexture.volumeDepth, TextureFormat.RHalf, false)
                { name = "R1 SDF 64 experimental", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                texture.SetPixelData(voxels, 0); texture.Apply(false, false);
                AssetDatabase.CreateAsset(texture, target);
                var metadata = new Metadata { sourceGuid = AssetDatabase.AssetPathToGUID(source), sourceDependencyHash = AssetDatabase.GetAssetDependencyHash(source).ToString(), generatorDependencyHash = AssetDatabase.GetAssetDependencyHash("Assets/RailArena/Editor/RampSdfAssetCandidate.cs").ToString(), unityVersion = Application.unityVersion, vfxPackageVersion = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MeshToSDFBaker).Assembly).version, padding = .8f, signPasses = 1, threshold = .5f, sdfOffset = 0f, center = bounds.center, size = baker.GetActualBoxSize(), maxResolution = 64 };
                File.WriteAllText(metadataPath, JsonUtility.ToJson(metadata, true)); AssetDatabase.ImportAsset(metadataPath); AssetDatabase.SaveAssets();
                Debug.Log("TRAVERSAL_RAMP_SDF_ASSET " + target + " size=" + metadata.size + " guid=" + metadata.sourceGuid);
            }
        }
    }
}



