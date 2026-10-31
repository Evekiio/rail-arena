using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class FirstPersonAssemblyExport
    {
        [Serializable] public sealed class MatrixRecord { public float[] values; }
        [Serializable] public sealed class Indices { public int[] values; }
        [Serializable] public sealed class Geometry { public Vector3[] vertices, normals; public Vector2[] uv; public Indices[] submeshes; }
        [Serializable] public sealed class Weight { public int[] bones; public float[] weights; }
        [Serializable] public sealed class Surface { public string name, source, baseMap; public Color color; }
        [Serializable] public sealed class Rigid { public string name, source; public Geometry geometry; public MatrixRecord toRoot; public int[] materials; }
        [Serializable] public sealed class Skin
        {
            public string identity, source; public Geometry geometry; public string[] bones; public int[] parents;
            public Weight[] weights; public MatrixRecord[] inverseBindsRoot; public MatrixRecord meshToRoot; public int material;
        }
        [Serializable] public sealed class Marker { public string name; public MatrixRecord matrix; }
        [Serializable] public sealed class Pose
        {
            public string identity, mode, nativeImage; public MatrixRecord[] bonesToRoot; public Vector3[] bakedRoot;
            public MatrixRecord cameraToRoot; public float verticalFov, nearClip, farClip, leftWristError, rightWristError, maximumNativeLinearSkinError; public Marker[] markers;
        }
        [Serializable] public sealed class Package
        {
            public string schema = "rail-arena-first-person-assembly/1";
            public string scope = "Held hip/ADS assembly references. Exact installed arm topology, weights, calibrated inverse binds, posed bone matrices and baked verification vertices. Rigid rifle parts are installed LOD0, with the physical first-person optic opening. This does not author new motion or certify the full first-person action library. Unity cel shading remains the visual authority.";
            public string axes = "Unity weapon-root metres. Row-major 4x4 matrices. Blender assembly conversion is (x,z,y); reference camera looks along native +Z.";
            public Surface[] materials; public Rigid[] rifle; public Skin[] skins; public Pose[] poses;
        }
        [Serializable] public sealed class CaptureRecord { public string source, sourceSha256, decompressedSha256; public int appearances, poses, rigidParts; }
        static string Digest(byte[] data) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
        byte[] quality; string pipelinePath;
        readonly List<Surface> surfaces = new();
        readonly Dictionary<Material, int> materialIds = new();
        static MatrixRecord Matrix(Matrix4x4 value) => new() { values = Enumerable.Range(0, 16).Select(i => value[i / 4, i % 4]).ToArray() };
        int MaterialId(Material value)
        {
            if (materialIds.TryGetValue(value, out int id)) return id;
            string texture = value.HasProperty("_BaseMap") ? AssetDatabase.GetAssetPath(value.GetTexture("_BaseMap")) : "";
            id = surfaces.Count; materialIds.Add(value, id);
            surfaces.Add(new Surface { name = value.name, source = AssetDatabase.GetAssetPath(value),
                baseMap = !string.IsNullOrEmpty(texture) && File.Exists(texture) ? "RailArena/" + texture : null,
                color = value.HasProperty("_BaseColor") ? value.GetColor("_BaseColor") : Color.white });
            return id;
        }
        static Geometry Read(Mesh mesh)
        {
            using var snapshot = MeshUtility.AcquireReadOnlyMeshData(mesh); var data = snapshot[0];
            using var vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            using var normals = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            using var uv = new NativeArray<Vector2>(data.vertexCount, Allocator.Temp);
            data.GetVertices(vertices); data.GetNormals(normals); data.GetUVs(0, uv);
            var sections = new List<Indices>();
            for (int i = 0; i < data.subMeshCount; i++)
            {
                Assert.That(data.GetSubMesh(i).topology, Is.EqualTo(MeshTopology.Triangles));
                using var indices = new NativeArray<int>(data.GetSubMesh(i).indexCount, Allocator.Temp);
                data.GetIndices(indices, i); sections.Add(new Indices { values = indices.ToArray() });
            }
            return new Geometry { vertices = vertices.ToArray(), normals = normals.ToArray(), uv = uv.ToArray(), submeshes = sections.ToArray() };
        }
        Rigid[] Rifle(RailgunPresentation weapon)
        {
            var excluded = new HashSet<Renderer>();
            foreach (var group in weapon.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                foreach (var lod in lods.Skip(1)) foreach (var renderer in lod.renderers) excluded.Add(renderer);
            }
            return weapon.GetComponentsInChildren<MeshFilter>(true).Where(filter =>
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && !renderer.forceRenderingOff &&
                    renderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly && !excluded.Contains(renderer);
            }).Select(filter => new Rigid { name = filter.name, source = AssetDatabase.GetAssetPath(filter.sharedMesh),
                geometry = Read(filter.sharedMesh), toRoot = Matrix(weapon.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix),
                materials = filter.GetComponent<MeshRenderer>().sharedMaterials.Select(MaterialId).ToArray() }).ToArray();
        }
        Skin Arm(FirstPersonArms arms, RailgunPresentation weapon, string identity)
        {
            var renderer = arms.Surface; var mesh = renderer.sharedMesh;
            var toRoot = weapon.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Assert.That(mesh.bindposes.Length, Is.EqualTo(renderer.bones.Length));
            var weights = mesh.boneWeights.Select(weight => new Weight {
                bones = new[] { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 },
                weights = new[] { weight.weight0, weight.weight1, weight.weight2, weight.weight3 } }).ToArray();
            Assert.That(weights.Length, Is.EqualTo(mesh.vertexCount));
            foreach (var weight in weights) Assert.That(weight.weights.Sum(), Is.EqualTo(1).Within(.0001f));
            return new Skin { identity = identity, source = AssetDatabase.GetAssetPath(mesh), geometry = Read(mesh),
                bones = renderer.bones.Select(bone => bone.name).ToArray(),
                parents = renderer.bones.Select(bone => { var parent = bone.parent; while (parent != null && Array.IndexOf(renderer.bones, parent) < 0) parent = parent.parent; return Array.IndexOf(renderer.bones, parent); }).ToArray(),
                weights = weights, meshToRoot = Matrix(toRoot), inverseBindsRoot = mesh.bindposes.Select(bind => Matrix(bind * toRoot.inverse)).ToArray(), material = MaterialId(renderer.sharedMaterial) };
        }
        static Pose CapturePose(FirstPersonArms arms, RailgunPresentation weapon, Camera camera, string identity, string mode)
        {
            var baked = new Mesh();
            try
            {
                // Compensate the imported 100x renderer scale before applying its full matrix.
                arms.Surface.BakeMesh(baked, true);
                var root = weapon.transform.worldToLocalMatrix;
                var meshToRoot = root * arms.Surface.transform.localToWorldMatrix;
                var sourceMesh = arms.Surface.sharedMesh;
                var sourceVertices = sourceMesh.vertices; var sourceWeights = sourceMesh.boneWeights;
                var matrices = arms.Surface.bones.Select((bone, index) => root * bone.localToWorldMatrix * sourceMesh.bindposes[index]).ToArray();
                var bakedVertices = baked.vertices; float maximumLinearError = 0;
                for (int index = 0; index < sourceVertices.Length; index++)
                {
                    var weight = sourceWeights[index]; var point = sourceVertices[index];
                    var expected = matrices[weight.boneIndex0].MultiplyPoint3x4(point) * weight.weight0 + matrices[weight.boneIndex1].MultiplyPoint3x4(point) * weight.weight1 +
                        matrices[weight.boneIndex2].MultiplyPoint3x4(point) * weight.weight2 + matrices[weight.boneIndex3].MultiplyPoint3x4(point) * weight.weight3;
                    maximumLinearError = Mathf.Max(maximumLinearError, Vector3.Distance(expected, meshToRoot.MultiplyPoint3x4(bakedVertices[index])));
                }
                Assert.That(maximumLinearError, Is.LessThan(.00005f), "Native compensated bake must match linear four-weight skin in weapon-root metres");
                float left = Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, weapon.LeftGrip.position);
                float right = Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, weapon.RightGrip.position);
                Assert.That(Mathf.Max(left, right), Is.LessThan(.001f), "Assembly references require wrist contacts within 1 mm");
                return new Pose { identity = identity, mode = mode, nativeImage = identity + "-" + mode + ".png",
                    bonesToRoot = arms.Surface.bones.Select(bone => Matrix(root * bone.localToWorldMatrix)).ToArray(),
                    bakedRoot = baked.vertices.Select(meshToRoot.MultiplyPoint3x4).ToArray(), cameraToRoot = Matrix(root * camera.transform.localToWorldMatrix),
                    maximumNativeLinearSkinError = maximumLinearError, verticalFov = camera.fieldOfView, nearClip = camera.nearClipPlane, farClip = camera.farClipPlane, leftWristError = left, rightWristError = right,
                    markers = new[] { ("RightGrip", weapon.RightGrip), ("LeftGrip", weapon.LeftGrip), ("SightAxis", weapon.Aim), ("Muzzle", weapon.Muzzle), ("Shoulder", weapon.Shoulder) }
                        .Where(pair => pair.Item2 != null).Select(pair => new Marker { name = pair.Item1, matrix = Matrix(root * pair.Item2.localToWorldMatrix) }).ToArray() };
            }
            finally { Object.Destroy(baked); }
        }
        [UnityTest] public IEnumerator ExportHeldAssemblyWithSkinProof()
        {
            quality = File.ReadAllBytes("ProjectSettings/QualitySettings.asset"); pipelinePath = AssetDatabase.GetAssetPath(QualitySettings.renderPipeline);
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/RailArena/Art/ArtPass/Atmosphere/SkyGardensPipeline.asset");
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity"); yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance(); session.enabled = false; session.View.enabled = false;
            foreach (var actor in session.Actors) { actor.Controller.enabled = false; actor.Presentation.enabled = false; }
            var slice = new GameObject("First-person assembly export").AddComponent<ArtSlicePresentation>(); yield return null; yield return null;
            var camera = session.View; var arms = camera.GetComponentInChildren<FirstPersonArms>(true);
            var weapon = arms.GetComponentInParent<RailgunPresentation>(true); weapon.gameObject.SetActive(true); yield return null;
            var art = arms.GetComponent<ContenderArtPresentation>(); var characters = Resources.Load<ContenderArtSet>("Art/Contenders");
            Assert.That(characters.variants.Length, Is.EqualTo(4));
            var rootPosition = session.Human.transform.position; var velocity = session.Human.Velocity; int tick = session.Match.Tick, rounds = session.Human.Magazine.Rounds;
            float capsule = session.Human.Controller.height;
            string folder = Path.GetFullPath("../ArtSource/Original/ArtPass-v1/FirstPerson/Assembly-v1/RuntimeSource");
            Assert.That(Directory.Exists(folder), Is.False, "Preserve earlier source exports"); Directory.CreateDirectory(folder);
            var skins = new List<Skin>(); var poses = new List<Pose>(); Rigid[] rifle = null;
            var target = new RenderTexture(1280, 720, 24); target.Create(); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.transform.SetPositionAndRotation(session.Human.Eye, Quaternion.Euler(session.Human.Command.Pitch, session.Human.Command.Yaw, 0));
                foreach (var variant in characters.variants)
                {
                    art.ApplyArms(arms, variant);
                    foreach (bool ads in new[] { false, true })
                    {
                        var sight = new AimDownSights(); sight.Step(ads, 1);
                        weapon.transform.localPosition = sight.Position(weapon.transform.InverseTransformPoint(weapon.Aim.position), Vector3.zero, 0);
                        weapon.transform.localRotation = Quaternion.identity;
                        camera.fieldOfView = ads ? session.Aim.ViewFov(true) : session.Aim.FieldOfView;
                        yield return null; yield return null;
                        if (!ads) skins.Add(Arm(arms, weapon, variant.identity));
                        if (rifle == null) rifle = Rifle(weapon);
                        var pose = CapturePose(arms, weapon, camera, variant.identity, ads ? "ADS" : "Hip"); poses.Add(pose);
                        Assert.That(session.Human.transform.position, Is.EqualTo(rootPosition)); Assert.That(session.Human.Velocity, Is.EqualTo(velocity));
                        Assert.That(session.Human.Controller.height, Is.EqualTo(capsule)); Assert.That(session.Match.Tick, Is.EqualTo(tick)); Assert.That(session.Human.Magazine.Rounds, Is.EqualTo(rounds));
                        if (ads)
                        {
                            var axis = camera.transform.InverseTransformPoint(weapon.Aim.position);
                            Assert.That(new Vector2(axis.x, axis.y).magnitude, Is.LessThan(.0001f)); Assert.That(axis.z, Is.EqualTo(AimDownSights.EyeRelief).Within(.0001f));
                        }
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        var previous = RenderTexture.active;
                        try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); File.WriteAllBytes(Path.Combine(folder, pose.nativeImage), pixels.EncodeToPNG()); }
                        finally { RenderTexture.active = previous; }
                    }
                }
                Assert.That(rifle.Length, Is.GreaterThan(5));
                byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Package { materials = surfaces.ToArray(), rifle = rifle, skins = skins.ToArray(), poses = poses.ToArray() }));
                string sourcePath = Path.Combine(folder, "assembly-source.json.gz");
                using (var file = File.Create(sourcePath)) using (var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal)) gzip.Write(payload, 0, payload.Length);
                var capture = new CaptureRecord { source = "assembly-source.json.gz", sourceSha256 = Digest(File.ReadAllBytes(sourcePath)), decompressedSha256 = Digest(payload), appearances = skins.Count, poses = poses.Count, rigidParts = rifle.Length };
                File.WriteAllText(Path.Combine(folder, "capture.json"), JsonUtility.ToJson(capture, true));
                TestContext.WriteLine("FIRST_PERSON_ASSEMBLY_SOURCE_SHA256 " + capture.sourceSha256);
                TestContext.WriteLine("FIRST_PERSON_ASSEMBLY_EXPORTED four calibrated skins / eight held poses / LOD0 rifle / unchanged simulation");
            }
            finally { target.Release(); Object.Destroy(target); Object.Destroy(pixels); }
            Object.Destroy(slice); yield return null; yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (quality != null) { QualitySettings.renderPipeline = string.IsNullOrEmpty(pipelinePath) ? null : AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath); File.WriteAllBytes("ProjectSettings/QualitySettings.asset", quality); AssetDatabase.Refresh(); }
        }
    }
}
