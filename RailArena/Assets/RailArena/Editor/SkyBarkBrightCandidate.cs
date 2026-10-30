using System;
using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class SkyBarkBrightCandidate
    {
        public static void Create()
        {
            const string source = "Assets/Validation/SkyBarkCandidate/BarkCandidate.mat";
            const string target = "Assets/Validation/SkyBarkCandidate/BarkBrightCandidate.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(target) != null) throw new InvalidOperationException("Candidate exists");
            if (!AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Copy failed");
            var material = AssetDatabase.LoadAssetAtPath<Material>(target); var color = material.GetColor("_BaseColor");
            material.SetColor("_BaseColor", new Color(Mathf.Min(1, color.r * 1.6f), Mathf.Min(1, color.g * 1.6f), Mathf.Min(1, color.b * 1.6f), color.a));
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets(); Debug.Log("SKY_BARK_BRIGHT_CANDIDATE " + target);
        }
    }
}
