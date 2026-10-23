using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class StylizedContenderTests
    {
        [TestCase("Player", .36f, .19f, .095f)]
        [TestCase("Bot01", .32f, .155f, .065f)]
        [TestCase("Bot02", .52f, .34f, .21f)]
        [TestCase("Bot03", .105f, .043f, .023f)]
        public void PalettePreservesAuthoredLinearSkinColor(string name, float red, float green, float blue)
        {
            string path = "Assets/RailArena/Art/StylizedContenders/" + name + "-Palette.png";
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(path)).sRGBTexture, Is.True);
            // Read the PNG bytes independently of the imported GPU texture.
            var png = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(png, File.ReadAllBytes(path)), Is.True);
                Color decoded = png.GetPixel(224, 32).linear;
                Assert.That(decoded.r, Is.EqualTo(red).Within(.006f));
                Assert.That(decoded.g, Is.EqualTo(green).Within(.006f));
                Assert.That(decoded.b, Is.EqualTo(blue).Within(.006f));
            }
            finally { Object.DestroyImmediate(png); }
        }

        [TestCase("Characters/Contender")]
        [TestCase("Characters/Stylized/Bot01")]
        [TestCase("Characters/Stylized/Bot02")]
        [TestCase("Characters/Stylized/Bot03")]
        public void StylizedDesignHasWeightedLodsAndRetainsFunctionalRig(string path)
        {
            var root = Object.Instantiate(Resources.Load<GameObject>(path));
            var baked = new Mesh();
            try
            {
                Assert.That(root, Is.Not.Null, path);
                var presentation = root.GetComponent<ContenderPresentation>();
                presentation.ApplySkin(0);
                var properties = new MaterialPropertyBlock();
                presentation.BodyRenderers[0].GetPropertyBlock(properties);
                Assert.That(properties.GetTexture("_BaseMap"), Is.EqualTo(presentation.Skins[0].GetTexture("_BaseMap")));
                var animator = presentation.Animator;
                Assert.That(animator.avatar.isHuman && animator.avatar.isValid, Is.True);
                Assert.That(animator.applyRootMotion, Is.False);
                Assert.That(root.GetComponentsInChildren<Hitbox>(true).Length, Is.EqualTo(11));
                Assert.That(presentation.Hands.Joints.Length, Is.EqualTo(30));
                Assert.That(presentation.RightGrip, Is.EqualTo(presentation.Railgun.RightGrip));
                var lods = root.GetComponent<LODGroup>().GetLODs();
                Assert.That(lods.Length, Is.EqualTo(3));
                int previousTriangles = int.MaxValue;
                foreach (var lod in lods)
                {
                    Assert.That(lod.renderers.Length, Is.EqualTo(1));
                    var renderer = (SkinnedMeshRenderer)lod.renderers[0];
                    var mesh = renderer.sharedMesh;
                    Assert.That(mesh.subMeshCount, Is.EqualTo(1));
                    Assert.That(mesh.triangles.Length / 3, Is.LessThan(previousTriangles));
                    previousTriangles = mesh.triangles.Length / 3;
                    Assert.That(renderer.sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
                    Assert.That(mesh.uv.Select(uv => new Vector2(Mathf.Round(uv.x * 10000), Mathf.Round(uv.y * 10000))).Distinct().Count(),
                        Is.InRange(8, 16), "Palette coordinates must occupy the primary UV channel.");
                    Assert.That(renderer.bones.All(bone => bone != null), Is.True);
                    foreach (var weight in mesh.boneWeights)
                        Assert.That(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3,
                            Is.EqualTo(1).Within(.001f));
                }
                animator.Rebind();
                animator.SetBool("Grounded", true);
                animator.SetFloat("Speed", 8);
                animator.SetFloat("Gait", 1);
                animator.Play("Base Layer.Locomotion", 0, 0);
                for (int frame = 0; frame < 15; frame++)
                {
                    animator.Update(1f / 30);
                    var renderer = (SkinnedMeshRenderer)presentation.BodyRenderers[0];
                    // FBX renderer transforms carry a 100x unit scale; request true local-space vertices.
                    renderer.BakeMesh(baked, true);
                    Assert.That(baked.vertices.All(vertex =>
                        !float.IsNaN(vertex.x) && !float.IsNaN(vertex.y) && !float.IsNaN(vertex.z) &&
                        !float.IsInfinity(vertex.x) && !float.IsInfinity(vertex.y) && !float.IsInfinity(vertex.z)), Is.True);
                    var points = baked.vertices.Select(renderer.transform.TransformPoint).ToArray();
                    float height = points.Max(point => point.y) - points.Min(point => point.y);
                    Assert.That(height, Is.InRange(1.2f, 2.4f), path + " must remain metre-sized while animated.");
                }
            }
            finally { Object.DestroyImmediate(baked); Object.DestroyImmediate(root); }
        }

        [Test]
        public void PlayerAndBotsUseFourDistinctMeshesAndPalettes()
        {
            var presentations = new[] { "Characters/Contender", "Characters/Stylized/Bot01",
                "Characters/Stylized/Bot02", "Characters/Stylized/Bot03" }
                .Select(path => Resources.Load<GameObject>(path).GetComponent<ContenderPresentation>()).ToArray();
            Assert.That(presentations.Select(p => ((SkinnedMeshRenderer)p.BodyRenderers[0]).sharedMesh).Distinct().Count(), Is.EqualTo(4));
            Assert.That(presentations.Select(p => p.BodyRenderers[0].sharedMaterial.GetTexture("_BaseMap")).Distinct().Count(), Is.EqualTo(4));
        }
    }
}
