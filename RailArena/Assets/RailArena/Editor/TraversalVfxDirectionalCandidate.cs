using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace RailArena.Editor
{
    public static class TraversalVfxDirectionalCandidate
    {
        public static void Create()
        {
            CreateGraph("TraversalBurstCyanVisible.vfx", "TraversalBurstDirectionalFreeV3.vfx");
            CreateGraph("TraversalBurstDepthCollision.vfx", "TraversalBurstDirectionalDepthV3.vfx");
        }
        private static void CreateGraph(string source, string target)
        {
            const string folder = "Assets/Validation/TraversalVfxCandidate/";
            if (AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + target) != null) throw new InvalidOperationException("Candidate already exists: " + target);
            if (!AssetDatabase.CopyAsset(folder + source, folder + target)) throw new InvalidOperationException("Copy failed: " + target);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(folder + target);
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");
            var objectExtensions = assembly.GetType("UnityEditor.VFX.VisualEffectObjectExtensions", true);
            var resourceExtensions = assembly.GetType("UnityEditor.VFX.VisualEffectResourceExtensions", true);
            var resource = objectExtensions.GetMethod("GetResource", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { asset });
            var graph = resourceExtensions.GetMethod("GetGraph", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            var children = (IEnumerable)graph.GetType().GetProperty("children").GetValue(graph);
            var init = children.Cast<object>().Single(x => x.GetType().Name == "VFXBasicInitialize");
            var blockType = assembly.GetType("UnityEditor.VFX.Block.SetAttribute", true);
            foreach (var item in new[] { ("position", "_Position", "UnityEditor.VFX.Position", "position", Vector3.zero), ("velocity", "_Velocity", "UnityEditor.VFX.Vector", "vector", new Vector3(0, -4, 0)) })
            {
                var block = ScriptableObject.CreateInstance(blockType);
                blockType.GetMethods().Single(m => m.Name == "SetSettingValue" && m.GetParameters().Length == 2).Invoke(block, new object[] { "attribute", item.Item1 });
                init.GetType().GetMethods().Single(m => m.Name == "AddChild" && m.GetParameters().Length == 3).Invoke(init, new object[] { block, -1, true });
                var slots = (IEnumerable)blockType.GetProperty("inputSlots").GetValue(block);
                var slot = slots.Cast<object>().Single();
                if (slot == null) throw new InvalidOperationException("Missing slot " + item.Item2);
                var valueType = assembly.GetType(item.Item3, true); var value = Activator.CreateInstance(valueType); valueType.GetField(item.Item4).SetValue(value, item.Item5);
                slot.GetType().GetProperty("value").SetValue(slot, value);
            }
            resourceExtensions.GetMethod("WriteAssetWithSubAssets", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            AssetDatabase.ImportAsset(folder + target, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TRAVERSAL_DIRECTIONAL_CREATED " + target);
        }
    }
}


