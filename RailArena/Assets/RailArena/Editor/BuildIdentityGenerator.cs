using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RailArena.Editor
{
    /// <summary>Same source, assets, dependencies and project settings produce the same network identity.</summary>
    public sealed class BuildIdentityGenerator : IPreprocessBuildWithReport
    {
        private const string Output = "Assets/Resources/BuildIdentity.txt";
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report) => Generate();

        [MenuItem("Rail Arena/Generate build identity")]
        public static void GenerateMenu() => Generate();

        public static string Generate()
        {
            AssetDatabase.SaveAssets();
            var files = new[] { "Assets", "Packages", "ProjectSettings" }
                .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                .Select(path => path.Replace('\\', '/'))
                .Where(Include).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            // Include the wire contract and target in addition to source-controlled inputs.
            hash.AppendData(Encoding.UTF8.GetBytes("RailArena/UDP/" + Networking.DuelProtocol.Version + "/Windows64\n"));
            var buffer = new byte[65536];
            foreach (string path in files)
            {
                byte[] name = Encoding.UTF8.GetBytes(path);
                hash.AppendData(BitConverter.GetBytes(name.Length));
                hash.AppendData(name);
                using var input = File.OpenRead(path);
                int headerSize = input.Read(buffer, 0, 5);
                input.Position = 0;
                bool yaml = headerSize == 5 && Encoding.ASCII.GetString(buffer, 0, 5) == "%YAML";
                string extension = Path.GetExtension(path).ToLowerInvariant();
                bool text = yaml || new[] { ".cs", ".json", ".asmdef", ".txt", ".meta", ".shader", ".hlsl", ".uss", ".uxml", ".inputactions" }.Contains(extension);
                if (text)
                {
                    string normalized = File.ReadAllText(path, new UTF8Encoding(false, true)).Replace("\r\n", "\n").Replace("\r", "\n");
                    byte[] content = Encoding.UTF8.GetBytes(normalized);
                    hash.AppendData(BitConverter.GetBytes((long)content.Length));
                    hash.AppendData(content);
                    continue;
                }
                hash.AppendData(BitConverter.GetBytes(input.Length));
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0) hash.AppendData(buffer, 0, count);
            }
            string identity = BitConverter.ToString(hash.GetHashAndReset()).Replace("-", "").ToLowerInvariant();
            Directory.CreateDirectory("Assets/Resources");
            if (!File.Exists(Output) || File.ReadAllText(Output) != identity + "\n")
                File.WriteAllText(Output, identity + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(Output, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return identity;
        }

        private static bool Include(string path)
        {
            if (path.StartsWith(Output, StringComparison.Ordinal) || path.StartsWith("Assets/Validation", StringComparison.Ordinal)) return false;
            if (path.Contains("/Editor/") || path.Contains("/Tests/") || path.EndsWith("/Editor.meta", StringComparison.Ordinal) ||
                path.EndsWith("/Tests.meta", StringComparison.Ordinal)) return false;
            // Package caches and lockfiles outside these two manifests are not build inputs.
            if (path.StartsWith("Packages/", StringComparison.Ordinal))
                return path == "Packages/manifest.json" || path == "Packages/packages-lock.json";
            return true;
        }
    }
}
