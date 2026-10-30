using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace RailArena.Editor
{
    /// <summary>Produces an isolated graph with an exposed collision-plane position.</summary>
    public static class TraversalVfxDynamicPlaneCandidate
    {
        private const string Source = "Assets/Validation/TraversalVfxCandidate/TraversalBurstDynamicPlaneV2.vfx";
        private const string Target = "Assets/Validation/TraversalVfxCandidate/TraversalBurstDynamicPlaneV3.vfx";

        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Target) != null) throw new InvalidOperationException("Dynamic candidate already exists");
            if (!AssetDatabase.CopyAsset(Source, Target)) throw new InvalidOperationException("Could not copy collision candidate");
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Target);
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Unity.VisualEffectGraph.Editor");
            var objectExtensions = assembly.GetType("UnityEditor.VFX.VisualEffectObjectExtensions", true);
            var resourceExtensions = assembly.GetType("UnityEditor.VFX.VisualEffectResourceExtensions", true);
            var resource = objectExtensions.GetMethod("GetResource", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { asset });
            var graph = resourceExtensions.GetMethod("GetGraph", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            var children = (IEnumerable)graph.GetType().GetProperty("children").GetValue(graph);
            var update = children.Cast<object>().Single(item => item.GetType().Name == "VFXBasicUpdate");
            var modelType = assembly.GetType("UnityEditor.VFX.VFXModel", true);
            var blocks = (IEnumerable)modelType.GetProperty("children").GetValue(update);
            var collision = blocks.Cast<object>().Single(item => item.GetType().Name == "CollisionShape");
            var parameterType = assembly.GetType("UnityEditor.VFX.VFXParameter", true);
            var parameter = ScriptableObject.CreateInstance(parameterType);
            parameterType.GetMethod("Init").Invoke(parameter, new object[] { typeof(Vector3) });
            var set = parameterType.GetMethods().Single(m => m.Name == "SetSettingValue" && m.GetParameters().Length == 2);
            set.Invoke(parameter, new object[] { "m_ExposedName", "CollisionNormal" });
            set.Invoke(parameter, new object[] { "m_Exposed", true });
            parameterType.GetProperty("value").SetValue(parameter, Vector3.up);
            graph.GetType().GetMethods().Single(m => m.Name == "AddChild" && m.GetParameters().Length == 3)
                .Invoke(graph, new object[] { parameter, -1, true });
            var input = collision.GetType().GetMethod("GetSlotByPath").Invoke(collision, new object[] { true, "Plane_normal" });
            var output = parameterType.GetMethod("GetSlotByPath").Invoke(parameter, new object[] { false, "o" });
            if (input == null || output == null) throw new InvalidOperationException("Could not locate plane or parameter slot");
            var link = input.GetType().GetMethod("Link", BindingFlags.Public | BindingFlags.Instance);
            if (!(bool)link.Invoke(input, new object[] { output, true })) throw new InvalidOperationException("Could not link collision normal");
            resourceExtensions.GetMethod("WriteAssetWithSubAssets", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { resource });
            AssetDatabase.ImportAsset(Target, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("TRAVERSAL_VFX_DYNAMIC_PLANE_CREATED " + Target);
        }
    }
}
