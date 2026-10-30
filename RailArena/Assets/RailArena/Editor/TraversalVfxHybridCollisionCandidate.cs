using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace RailArena.Editor
{
    /// <summary>Creates a disposable collision-graph experiment with the installed VFX editor API.</summary>
    public static class TraversalVfxHybridCollisionCandidate
    {
        private const string Source = "Assets/Validation/TraversalVfxCandidate/TraversalBurstDynamicPlaneV3.vfx";
        private const string Target = "Assets/Validation/TraversalVfxCandidate/TraversalBurstHybridCollision.vfx";

        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Target) != null) throw new InvalidOperationException("Collision candidate already exists");
            if (!AssetDatabase.CopyAsset(Source, Target)) throw new InvalidOperationException("Could not copy the candidate graph");
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Target);
            if (asset == null) throw new InvalidOperationException("Copied graph was not imported");
            var editorAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");
            var objectExtensions = editorAssembly.GetType("UnityEditor.VFX.VisualEffectObjectExtensions", true);
            var resourceExtensions = editorAssembly.GetType("UnityEditor.VFX.VisualEffectResourceExtensions", true);
            var resource = objectExtensions.GetMethod("GetResource", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { asset });
            var graph = resourceExtensions.GetMethod("GetGraph", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            var children = (IEnumerable)graph.GetType().GetProperty("children").GetValue(graph);
            var update = children.Cast<object>().Single(item => item.GetType().Name == "VFXBasicUpdate");
            var collisionType = editorAssembly.GetType("UnityEditor.VFX.Block.CollisionDepth", true);
            var collision = ScriptableObject.CreateInstance(collisionType);
            var add = update.GetType().GetMethods().Single(m => m.Name == "AddChild" && m.GetParameters().Length == 3);
            add.Invoke(update, new object[] { collision, -1, true });
            resourceExtensions.GetMethod("WriteAssetWithSubAssets", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            AssetDatabase.ImportAsset(Target, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Target) == null) throw new InvalidOperationException("Collision candidate did not import");
            Debug.Log("TRAVERSAL_VFX_HYBRID_COLLISION_CANDIDATE_CREATED " + Target);
        }
    }
}


