using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class PadShaderGraphAudit
    {
        public static void BuildPlayerAudit()
        {
            const string output = "../Builds/SkyGardens-PadGraph-Audit-2026-10-30/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Audit output already exists");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Audit build failed: " + report.summary.result);
            Debug.Log("PAD_GRAPH_AUDIT_BUILD " + Path.GetFullPath(output));
        }

        public static void BuildPlaytest()
        {
            const string output = "../Builds/SkyGardens-PadGraph-2026-10-30/RailArena.exe";
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new IOException("Build output already exists");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/Effects/PadEnergy.shadergraph");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Production pad graph is unavailable");
            ArenaBuilder.BuildWindowsPlaytestTo(output);
            Debug.Log("PAD_GRAPH_PLAYTEST_BUILD " + Path.GetFullPath(output));
        }

        public static void BuildPrototype() => BuildGraph("Assets/Validation/TraversalShaderGraph/PadEnergyPrototypeV3.shadergraph", "RailArena.PadEnergy.v1");
        public static void BuildEnhancedPrototype() => BuildGraph("Assets/Validation/TraversalShaderGraph/PadEnergyEnhanced.shadergraph", "RailArena.PadEnergy.Enhanced.v1");
        private static void BuildGraph(string output, string providerKey)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            const string template = "Library/PackageCache/com.unity.shadergraph@8b7e693cdbfe/GraphTemplates/Cross Pipeline/Unlit Simple.shadergraph";
            if (File.Exists(output)) throw new IOException("Prototype graph already exists");
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type Find(string name) => assemblies.Select(a => a.GetType(name)).FirstOrDefault(t => t != null) ?? throw new InvalidOperationException("Missing type: " + name);
            var graphType = Find("UnityEditor.ShaderGraph.GraphData");
            var nodeBaseType = Find("UnityEditor.ShaderGraph.AbstractMaterialNode");
            var providerType = Find("UnityEditor.ShaderGraph.ProviderSystem.ProviderNode");
            var jsonType = Find("UnityEditor.ShaderGraph.Serialization.MultiJson");
            var libraryType = Find("Unity.GraphAuthoring.Editor.ProviderSystem.ProviderLibrary");
            var functionType = Find("Unity.GraphAuthoring.Editor.ProviderSystem.IShaderFunction");
            object[] args = { null };
            if (!(bool)libraryType.GetMethod("TryGetInstance", flags).Invoke(null, args)) throw new InvalidOperationException("Provider library unavailable");
            var query = libraryType.GetMethod("ProvidersByVersion", flags).MakeGenericMethod(functionType);
            var providers = ((IEnumerable)query.Invoke(args[0], new object[] { providerKey })).Cast<object>().ToArray();
            if (providers.Length != 1) throw new InvalidOperationException("Pad provider missing or ambiguous");
            var graph = Activator.CreateInstance(graphType);
            var deserialize = jsonType.GetMethods(flags).Single(m => m.Name == "Deserialize" && m.IsGenericMethodDefinition).MakeGenericMethod(graphType);
            deserialize.Invoke(null, new object[] { graph, File.ReadAllText(template), null, false });
            var nodes = ((IEnumerable)graphType.GetMethod("GetNodes", flags).MakeGenericMethod(nodeBaseType).Invoke(graph, null)).Cast<object>();
            var nameProperty = nodeBaseType.GetProperty("name", flags);
            var colorBlock = nodes.Single(n => (string)nameProperty.GetValue(n) == "SurfaceDescription.BaseColor");
            var node = Activator.CreateInstance(providerType);
            providerType.GetMethod("InitializeFromProvider", flags).Invoke(node, new[] { providers[0] });
            graphType.GetMethod("AddNode", flags, null, new[] { nodeBaseType, typeof(bool) }, null).Invoke(graph, new[] { node, (object)false });
            var getSlot = nodeBaseType.GetMethod("GetSlotReference", flags);
            graphType.GetMethod("Connect", flags).Invoke(graph, new[] { getSlot.Invoke(node, new object[] { 0 }), getSlot.Invoke(colorBlock, new object[] { 0 }) });
            var position = Activator.CreateInstance(Find("UnityEditor.ShaderGraph.PositionNode"));
            var spaceField = position.GetType().BaseType.GetField("m_Space", flags);
            spaceField.SetValue(position, Enum.Parse(spaceField.FieldType, "Object"));
            var swizzle = Activator.CreateInstance(Find("UnityEditor.ShaderGraph.SwizzleNode"));
            swizzle.GetType().GetProperty("maskInput", flags).SetValue(swizzle, "xz");
            var addNode = graphType.GetMethod("AddNode", flags, null, new[] { nodeBaseType, typeof(bool) }, null);
            foreach (var extra in new[] { position, swizzle }) addNode.Invoke(graph, new[] { extra, (object)false });
            var connect = graphType.GetMethod("Connect", flags);
            connect.Invoke(graph, new[] { getSlot.Invoke(position, new object[] { 0 }), getSlot.Invoke(swizzle, new object[] { 0 }) });
            connect.Invoke(graph, new[] { getSlot.Invoke(swizzle, new object[] { 1 }), getSlot.Invoke(node, new object[] { 1 }) });
            var shaderInputType = Find("UnityEditor.ShaderGraph.Internal.ShaderInput");
            var propertyNodeType = Find("UnityEditor.ShaderGraph.PropertyNode");
            var addInput = graphType.GetMethod("AddGraphInput", flags);
            foreach (var entry in new[] { ("_Clock", 2), ("_Burst", 3), ("_Calm", 4), ("_Charge", 5), ("_Tint", 6) })
            {
                var propertyType = Find(entry.Item1 == "_Tint" ? "UnityEditor.ShaderGraph.Internal.ColorShaderProperty" : "UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty");
                var property = Activator.CreateInstance(propertyType, true);
                shaderInputType.GetProperty("displayName", flags).SetValue(property, entry.Item1.Substring(1));
                shaderInputType.GetProperty("overrideReferenceName", flags).SetValue(property, entry.Item1);
                if (entry.Item1 == "_Tint") propertyType.GetProperty("value", flags).SetValue(property, new Color(.06f, .65f, .61f, 1));
                addInput.Invoke(graph, new[] { property, (object)(-1) });
                var propertyNode = Activator.CreateInstance(propertyNodeType);
                propertyNodeType.GetProperty("property", flags).SetValue(propertyNode, property);
                addNode.Invoke(graph, new[] { propertyNode, (object)false });
                connect.Invoke(graph, new[] { getSlot.Invoke(propertyNode, new object[] { 0 }), getSlot.Invoke(node, new object[] { entry.Item2 }) });
            }
            var serialized = (string)jsonType.GetMethod("Serialize", flags).Invoke(null, new[] { graph });
            File.WriteAllText(output, serialized);
            AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(output);
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Prototype graph did not import cleanly");
            Debug.Log("PAD_SHADER_GRAPH_PROTOTYPE " + output);
        }

        public static void VerifyProvider()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type Find(string name) => assemblies.Select(a => a.GetType(name)).FirstOrDefault(t => t != null) ?? throw new InvalidOperationException("Missing type: " + name);
            var libraryType = Find("Unity.GraphAuthoring.Editor.ProviderSystem.ProviderLibrary");
            var functionType = Find("Unity.GraphAuthoring.Editor.ProviderSystem.IShaderFunction");
            var nodeType = Find("UnityEditor.ShaderGraph.ProviderSystem.ProviderNode");
            object[] args = { null };
            bool available = (bool)libraryType.GetMethod("TryGetInstance", flags).Invoke(null, args);
            if (!available || args[0] == null) throw new InvalidOperationException("Shader Graph provider library is unavailable");
            var query = libraryType.GetMethod("ProvidersByVersion", flags).MakeGenericMethod(functionType);
            var providers = ((IEnumerable)query.Invoke(args[0], new object[] { "RailArena.PadEnergy.v1" })).Cast<object>().ToArray();
            if (providers.Length != 1) throw new InvalidOperationException("Expected one pad node provider, found " + providers.Length);
            var node = Activator.CreateInstance(nodeType);
            nodeType.GetMethod("InitializeFromProvider", flags).Invoke(node, new[] { providers[0] });
            var nodeName = nodeType.GetProperty("name", flags).GetValue(node) as string;
            if (nodeName != "Jump Pad Energy") throw new InvalidOperationException("Unexpected node name: " + nodeName);
            Debug.Log("PAD_SHADER_GRAPH_PROVIDER_OK key=RailArena.PadEnergy.v1 name=" + nodeName);
        }
    }
}
