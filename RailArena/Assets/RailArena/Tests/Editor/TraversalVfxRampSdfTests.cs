using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.VFX.SDF;

namespace RailArena.Tests
{
    public sealed class TraversalVfxRampSdfTests
    {
        [UnityTest]
        public IEnumerator BakeActualRampAndReadSignedDistances()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); Assert.That(session, Is.Not.Null);
            var mesh = session.World.GetComponentsInChildren<MeshCollider>().Single(c => c.name == "R1").sharedMesh;
            var bounds = mesh.bounds; var size = bounds.size + Vector3.one * .8f;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            using (var baker = new MeshToSDFBaker(size, bounds.center, 64, mesh))
            {
                baker.BakeSDF(); long submittedMs = timer.ElapsedMilliseconds; var texture = baker.SdfTexture; Assert.That(texture, Is.Not.Null);
                Assert.That(texture.IsCreated(), Is.True); Assert.That(texture.dimension, Is.EqualTo(TextureDimension.Tex3D));
                var request = AsyncGPUReadback.Request(texture, 0, 0, texture.width, 0, texture.height, 0, texture.volumeDepth, TextureFormat.RFloat);
                double deadline = UnityEditor.EditorApplication.timeSinceStartup + 30;
                while (!request.done && UnityEditor.EditorApplication.timeSinceStartup < deadline) { UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
                Assert.That(request.done, Is.True, "SDF readback timed out"); Assert.That(request.hasError, Is.False, "SDF readback failed");
                long readbackMs = timer.ElapsedMilliseconds; float min = float.PositiveInfinity, max = float.NegativeInfinity; int voxels = 0;
                for (int layer = 0; layer < request.layerCount; layer++) { var values = request.GetData<float>(layer); voxels += values.Length; for (int i = 0; i < values.Length; i++) { min = Mathf.Min(min, values[i]); max = Mathf.Max(max, values[i]); } }
                Debug.Log("TRAVERSAL_RAMP_SDF_COST submitMs=" + submittedMs + " readbackWallMs=" + readbackMs + " format=" + texture.graphicsFormat + " bytesPerVoxel=" + UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockSize(texture.graphicsFormat));
                Debug.Log("TRAVERSAL_RAMP_SDF size=" + baker.GetActualBoxSize() + " texture=" + texture.width + "x" + texture.height + "x" + texture.volumeDepth + " voxels=" + voxels + " layers=" + request.layerCount + " min=" + min + " max=" + max);
                int midX = texture.width / 2, midY = texture.height / 2, midZ = texture.volumeDepth / 2;
                float centerDistance = request.GetData<float>(midZ)[midY * texture.width + midX];
                float cornerDistance = request.GetData<float>(0)[0];
                Debug.Log("TRAVERSAL_RAMP_SDF_SAMPLES center=" + centerDistance + " corner=" + cornerDistance);
                Assert.That(centerDistance, Is.LessThan(0), "Center of the flat ramp section should be inside");
                Assert.That(cornerDistance, Is.GreaterThan(0), "Padded box corner should be outside");
                Assert.That(min, Is.LessThan(0), "Expected negative values inside the mesh"); Assert.That(max, Is.GreaterThan(0), "Expected positive values outside the mesh");
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}




