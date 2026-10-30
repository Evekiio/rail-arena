using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkUvAudit
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bark.mat");
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.sharedMaterials.Contains(material)).ToArray();
            int missingUv = 0, missingTangent = 0, triangles = 0, degenerate = 0; float minArea = float.PositiveInfinity, maxArea = 0;
            foreach (var r in renderers)
            {
                var filter = r.GetComponent<MeshFilter>(); if (filter == null || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh; var uv = mesh.uv; var ids = mesh.triangles;
                if (uv.Length != mesh.vertexCount) { missingUv++; continue; }
                if (mesh.tangents.Length != mesh.vertexCount) missingTangent++;
                for (int i = 0; i < ids.Length; i += 3)
                {
                    var a = uv[ids[i]]; var b = uv[ids[i+1]]; var c = uv[ids[i+2]];
                    float area = Mathf.Abs((b.x-a.x)*(c.y-a.y) - (b.y-a.y)*(c.x-a.x)); triangles++;
                    if (area < .000001f) degenerate++; else { minArea = Mathf.Min(minArea, area); maxArea = Mathf.Max(maxArea, area); }
                }
            }
            Debug.Log("SKY_BARK_UV renderers=" + renderers.Length + " missingUv=" + missingUv + " missingTangents=" + missingTangent + " triangles=" + triangles + " degenerate=" + degenerate + " minUvArea=" + minArea + " maxUvArea=" + maxArea);
            if (renderers.Length == 0 || missingUv > 0 || missingTangent > 0) throw new Exception("Bark mesh lacks UVs or tangents");
        }
    }
}
