using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class TraversalVfxSdfDependencyTests
    {
        [Test]
        public void MeshContentChangeInvalidatesDependencyHash()
        {
            const string source = "Assets/RailArena/Generated/SkyGardensArt/R1.asset";
            string copy = "Assets/Validation/TraversalVfxCandidate/HashProbe-" + System.Guid.NewGuid().ToString("N") + ".asset";
            Assert.That(AssetDatabase.CopyAsset(source, copy), Is.True);
            try
            {
                var before = AssetDatabase.GetAssetDependencyHash(copy);
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(copy); var vertices = mesh.vertices;
                vertices[0] += new Vector3(.125f, 0, 0); mesh.vertices = vertices; mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(copy, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var after = AssetDatabase.GetAssetDependencyHash(copy);
                Debug.Log("TRAVERSAL_SDF_HASH_CHANGED " + (before != after));
                Assert.That(after, Is.Not.EqualTo(before), "Changing mesh contents must change its dependency hash");
            }
            finally { AssetDatabase.DeleteAsset(copy); }
        }
    }
}
