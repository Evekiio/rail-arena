using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    public static class VfxScriptReferenceAudit
    {
        public static void ReimportUrpCandidates()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Main camera missing");
            var target = new RenderTexture(64, 64, 24);
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                if (GraphicsSettings.currentRenderPipeline == null) throw new InvalidOperationException("No active scriptable render pipeline");
                foreach (string path in new[] { "Assets/Validation/TraversalVfxCandidate/TraversalBurstLarge.vfx", "Assets/Validation/TraversalVfxCandidate/TraversalBurstDepthCollision.vfx" })
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    Debug.Log("VFX_URP_REIMPORT " + path);
                }
            }
            finally { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        }

        public static void Inspect()
        {
            string[] paths = {
                "Assets/Validation/TraversalVfxCandidate/TraversalBurstDynamicPlane.vfx",
                "Assets/Validation/TraversalVfxCandidate/TraversalBurstDirectionalHybrid.vfx",
                "Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Burst.vfx"
            };
            foreach (string path in paths)
            {
                string diskPath = path;
                if (path.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                    if (package == null) throw new IOException("Package not found: " + path);
                    diskPath = Path.Combine(package.resolvedPath, path.Substring(package.assetPath.Length + 1));
                }
                if (!File.Exists(diskPath)) throw new IOException("Missing VFX graph: " + path);
                var guids = Regex.Matches(File.ReadAllText(diskPath), @"m_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]{32})")
                    .Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToArray();
                int missing = 0, noClass = 0;
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath)) { missing++; Debug.Log($"VFX_SCRIPT_UNRESOLVED graph={path} guid={guid}"); continue; }
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                    if (script == null || script.GetClass() == null) { noClass++; Debug.Log($"VFX_SCRIPT_NO_CLASS graph={path} guid={guid} path={assetPath}"); }
                }
                Debug.Log($"VFX_SCRIPT_AUDIT graph={path} distinct={guids.Length} unresolved={missing} noClass={noClass}");
            }
        }
    }
}
