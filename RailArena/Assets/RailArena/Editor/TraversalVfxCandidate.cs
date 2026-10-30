using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace RailArena.Editor
{
    /// <summary>Creates an isolated VFX Graph candidate from Unity's burst template.</summary>
    public static class TraversalVfxCandidate
    {
        private const string Folder = "Assets/Validation/TraversalVfxCandidate";
        private const string Graph = Folder + "/TraversalBurst.vfx";

        public static void Prepare()
        {
            if (AssetDatabase.IsValidFolder(Folder)) throw new InvalidOperationException("Candidate already exists");
            AssetDatabase.CreateFolder("Assets/Validation", "TraversalVfxCandidate");
            const string template = "Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Burst.vfx";
            if (!AssetDatabase.CopyAsset(template, Graph)) throw new InvalidOperationException("Unable to copy Unity VFX burst template");
            AssetDatabase.ImportAsset(Graph, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Graph);
            if (graph == null) throw new InvalidOperationException("Candidate did not import as a VisualEffectAsset");
            var names = new List<string>();
            var root = new GameObject("Temporary VFX inspection");
            try { var effect = root.AddComponent<VisualEffect>(); effect.visualEffectAsset = graph; effect.GetParticleSystemNames(names); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            if (names.Count == 0) throw new InvalidOperationException("Candidate has no particle system");
            Debug.Log("TRAVERSAL_VFX_CANDIDATE_IMPORTED systems=" + string.Join(",", names));
            foreach (var item in AssetDatabase.LoadAllAssetsAtPath(Graph).Where(item => item != null))
                Debug.Log("TRAVERSAL_VFX_SUBASSET " + item.GetType().FullName + " " + item.name);
        }
    }
}
