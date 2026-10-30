using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class ArtBuildRestoreTests
    {
        [Test]
        public void TemporaryPipelineAndIdentityRestoreExactSourceFiles()
        {
            var pipeline = QualitySettings.renderPipeline;
            byte[] quality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset");
            byte[] identity = File.ReadAllBytes("Assets/Resources/BuildIdentity.txt");
            var builder = TypeCache.GetTypesDerivedFrom<UnityEditor.Build.IProcessSceneWithReport>().Single(type => type.FullName == "RailArena.Editor.ArtSliceBuilder");
            var restore = builder.GetMethod("RestoreBuildInputs");
            try
            {
                QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
                AssetDatabase.SaveAssets();
                File.WriteAllText("Assets/Resources/BuildIdentity.txt", "temporary-build-identity\n");
                restore.Invoke(null, new object[] { pipeline, identity, quality });
                Assert.That(QualitySettings.renderPipeline, Is.EqualTo(pipeline));
                CollectionAssert.AreEqual(quality, File.ReadAllBytes("ProjectSettings/QualitySettings.asset"));
                CollectionAssert.AreEqual(identity, File.ReadAllBytes("Assets/Resources/BuildIdentity.txt"));
            }
            finally { restore.Invoke(null, new object[] { pipeline, identity, quality }); }
        }
    }
}
