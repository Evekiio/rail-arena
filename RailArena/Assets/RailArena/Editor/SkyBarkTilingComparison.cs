using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkTilingComparison
    {
        public static void Capture()
        {
            const string folder = "Assets/Validation/SkyBarkNatural/";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(folder + "TilingDiagnostic.shader");
            var original = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "BarkAlbedo.png");
            var candidate = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "BarkSeamCandidate.png");
            if (shader == null || original == null || candidate == null) throw new InvalidOperationException("Missing diagnostic inputs");
            var material = new Material(shader); var target = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32); target.Create();
            var image = new Texture2D(1024, 1024, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/BarkTilingComparison"); Directory.CreateDirectory(output);
            try
            {
                foreach (var entry in new[] { ("original", original), ("candidate", candidate) })
                {
                    entry.Item2.wrapMode = TextureWrapMode.Repeat; Graphics.Blit(entry.Item2, target, material);
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); image.Apply();
                    string path = Path.Combine(output, entry.Item1 + ".png"); if (File.Exists(path)) throw new IOException("Capture exists"); File.WriteAllBytes(path, image.EncodeToPNG());
                }
                Debug.Log("SKY_BARK_TILING_COMPARISON " + output);
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
