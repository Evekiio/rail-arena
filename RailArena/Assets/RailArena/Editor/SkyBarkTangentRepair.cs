using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkTangentRepair
    {
        public static void Apply()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bark.mat");
            if (material == null) throw new InvalidOperationException("Bark material missing");
            var meshes = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Contains(material))
                .Select(r => r.GetComponent<MeshFilter>()?.sharedMesh).Where(m => m != null).Distinct().ToArray();
            if (meshes.Length == 0 || meshes.Any(m => m.uv.Length != m.vertexCount || m.tangents.Length != 0)) throw new InvalidOperationException("Unexpected bark mesh state");
            var backup = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/BarkBeforeTangents");
            if (Directory.Exists(backup)) throw new IOException("Bark backup already exists"); Directory.CreateDirectory(backup);
            foreach (var mesh in meshes)
            {
                var path = AssetDatabase.GetAssetPath(mesh); if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Mesh is not an asset");
                File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), false);
            }
            foreach (var mesh in meshes) { mesh.RecalculateTangents(); if (mesh.tangents.Length != mesh.vertexCount) throw new InvalidOperationException("Tangent calculation failed for " + mesh.name); EditorUtility.SetDirty(mesh); }
            AssetDatabase.SaveAssets(); Debug.Log("SKY_BARK_TANGENTS_APPLIED meshes=" + meshes.Length + " backup=" + backup);
        }
    }
}
