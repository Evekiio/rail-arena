using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace RailArena.Editor
{
    public static class TraversalVfxBoundedCandidate
    {
        public static void Create()
        {
            const string source = "Assets/Validation/TraversalVfxCandidate/TraversalBurstDirectionalDepthV3.vfx";
            const string target = "Assets/Validation/TraversalVfxCandidate/TraversalBurstDirectionalBounded.vfx";
            if (AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(target) != null) throw new InvalidOperationException("Candidate exists");
            if (!AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException("Copy failed");
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(target);
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");
            var objects = assembly.GetType("UnityEditor.VFX.VisualEffectObjectExtensions", true);
            var resources = assembly.GetType("UnityEditor.VFX.VisualEffectResourceExtensions", true);
            var resource = objects.GetMethod("GetResource", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { asset });
            var graph = resources.GetMethod("GetGraph", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            var children = (IEnumerable)graph.GetType().GetProperty("children").GetValue(graph);
            var update = children.Cast<object>().Single(x => x.GetType().Name == "VFXBasicUpdate");
            var collisionType = assembly.GetType("UnityEditor.VFX.Block.CollisionShape", true);
            var collision = ScriptableObject.CreateInstance(collisionType);
            update.GetType().GetMethods().Single(m => m.Name == "AddChild" && m.GetParameters().Length == 3).Invoke(update, new object[] { collision, -1, true });
            var shapeType = assembly.GetType("UnityEditor.VFX.Block.CollisionShapeBase+Type", true);
            collisionType.GetMethods().Single(m => m.Name == "SetSettingValue" && m.GetParameters().Length == 2).Invoke(collision, new object[] { "shape", Enum.Parse(shapeType, "OrientedBox") });
            var parameterType = assembly.GetType("UnityEditor.VFX.VFXParameter", true);
            foreach (var entry in new[] { ("BoxCenter", "box_center", new Vector3(0, -2, 0)), ("BoxSize", "box_size", new Vector3(2, .3f, 2)) })
            {
                var parameter = ScriptableObject.CreateInstance(parameterType); parameterType.GetMethod("Init").Invoke(parameter, new object[] { typeof(Vector3) });
                var set = parameterType.GetMethods().Single(m => m.Name == "SetSettingValue" && m.GetParameters().Length == 2);
                set.Invoke(parameter, new object[] { "m_ExposedName", entry.Item1 }); set.Invoke(parameter, new object[] { "m_Exposed", true });
                parameterType.GetProperty("value").SetValue(parameter, entry.Item3);
                graph.GetType().GetMethods().Single(m => m.Name == "AddChild" && m.GetParameters().Length == 3).Invoke(graph, new object[] { parameter, -1, true });
                var input = collisionType.GetMethod("GetSlotByPath").Invoke(collision, new object[] { true, entry.Item2 });
                var output = parameterType.GetMethod("GetSlotByPath").Invoke(parameter, new object[] { false, "o" });
                if (input == null || output == null) throw new InvalidOperationException("Missing slot " + entry.Item2);
                if (!(bool)input.GetType().GetMethod("Link", BindingFlags.Public | BindingFlags.Instance).Invoke(input, new object[] { output, true })) throw new InvalidOperationException("Link failed " + entry.Item2);
            }
            resources.GetMethod("WriteAssetWithSubAssets", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TRAVERSAL_BOUNDED_CREATED " + target);
        }
    }
}
