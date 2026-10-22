using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RailArena.Editor
{
    public static class BotTuningBuilder
    {
        private const string Path = "Assets/Resources/BotTuning.asset";

        [MenuItem("Rail Arena/Create bot tuning if missing")]
        public static void EnsureAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BotTuning>(Path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BotTuning>();
                AssetDatabase.CreateAsset(asset, Path);
                AssetDatabase.SaveAssets();
            }
            asset.Validate();
        }
    }

    public sealed class BotTuningBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var asset = AssetDatabase.LoadAssetAtPath<BotTuning>("Assets/Resources/BotTuning.asset");
            if (asset == null) throw new BuildFailedException("Create Assets/Resources/BotTuning.asset before building Rail Arena.");
            try { asset.Validate(); }
            catch (System.ArgumentException e) { throw new BuildFailedException("Invalid bot tuning: " + e.Message); }
        }
    }
}
