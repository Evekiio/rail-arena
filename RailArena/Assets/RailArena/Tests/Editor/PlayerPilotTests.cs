using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class PlayerPilotTests
    {
        private const string Path = "Assets/RailArena/Art/PlayerPilot/Player-Pilot.prefab";

        [Test]
        public void PlayerPilotIsSeparateAndRetainsRigAndPaintableLods()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset, Is.Not.EqualTo(Resources.Load<GameObject>("Characters/Contender")));
            var root = Object.Instantiate(asset);
            try
            {
                var animator = root.GetComponent<Animator>();
                Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
                Assert.That(animator.applyRootMotion, Is.False);
                var p = root.GetComponent<ContenderPresentation>();
                Assert.That(p.enabled, Is.False, "Review prefab must not accidentally run gameplay presentation.");
                Assert.That(root.GetComponentsInChildren<Hitbox>(true).Length, Is.EqualTo(11));
                Assert.That(p.Hands.Joints.Length, Is.EqualTo(30));
                int previous = int.MaxValue;
                foreach (var lod in root.GetComponent<LODGroup>().GetLODs())
                {
                    var skin = (SkinnedMeshRenderer)lod.renderers.Single();
                    Assert.That(skin.sharedMesh.subMeshCount, Is.EqualTo(1));
                    Assert.That(skin.sharedMesh.triangles.Length, Is.LessThan(previous));
                    previous = skin.sharedMesh.triangles.Length;
                    Assert.That(skin.sharedMesh.uv.Distinct().Count(), Is.GreaterThan(500), "Real UV islands must survive export.");
                    Assert.That(skin.bones.All(b => b != null), Is.True);
                    foreach (var w in skin.sharedMesh.boneWeights)
                        Assert.That(w.weight0+w.weight1+w.weight2+w.weight3, Is.EqualTo(1).Within(.001f));
                    Assert.That(skin.sharedMaterial.GetTexture("_MetallicGlossMap"), Is.Not.Null);
                    Assert.That(skin.sharedMaterial.GetTexture("_EmissionMap"), Is.Not.Null);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void PlayerPilotSupportsAnimationAndCarbineWristTargets()
        {
            // Calls the same independent geometry/contact audit used by batch import.
            var builder = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("RailArena.Editor.PlayerPilotBuilder"))
                .First(t => t != null);
            builder.GetMethod("Validate").Invoke(null, null);
        }
    }
}
