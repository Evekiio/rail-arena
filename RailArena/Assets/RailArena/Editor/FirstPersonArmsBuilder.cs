using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    public static class FirstPersonArmsBuilder
    {
        private const string Root = "Assets/RailArena/Characters";
        private const string MeshPath = Root + "/View arms.asset";

        [MenuItem("Rail Arena/Build first-person arms")]
        public static void Build()
        {
            bool stylized = StylizedContenderBuilder.ModelsReady;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(stylized ?
                "Assets/Resources/Characters/Contender.prefab" : "Assets/ThirdParty/Grab3D/Contender_LOD0.fbx");
            var root = Object.Instantiate(source);
            try
            {
                root.name = "First person armored arms";
                var animator = root.GetComponent<Animator>();
                var renderer = root.GetComponentInChildren<SkinnedMeshRenderer>();
                if (stylized)
                {
                    var presentation = root.GetComponent<ContenderPresentation>();
                    renderer = (SkinnedMeshRenderer)presentation.BodyRenderers[0];
                    Object.DestroyImmediate(presentation.Weapon.gameObject);
                    foreach (var other in presentation.BodyRenderers.Skip(1)) Object.DestroyImmediate(other.gameObject);
                    foreach (var hitbox in root.GetComponentsInChildren<Hitbox>(true)) Object.DestroyImmediate(hitbox.gameObject);
                    Object.DestroyImmediate(root.GetComponent<LODGroup>());
                    Object.DestroyImmediate(presentation);
                }
                var armBones = new HashSet<int>();
                foreach (var id in new[] { HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                    HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand })
                {
                    int index = Array.IndexOf(renderer.bones, animator.GetBoneTransform(id));
                    if (index < 0) throw new InvalidOperationException("The source mesh lacks an arm skin bone: " + id);
                    armBones.Add(index);
                }
                foreach (var id in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
                {
                    var hand = animator.GetBoneTransform(id);
                    for (int i = 0; i < renderer.bones.Length; i++)
                        if (renderer.bones[i].IsChildOf(hand)) armBones.Add(i);
                }
                Mesh mesh = ExtractArms(renderer.sharedMesh, armBones);
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                if (old == null) AssetDatabase.CreateAsset(mesh, MeshPath);
                else
                {
                    old.Clear(); old.indexFormat = mesh.indexFormat;
                    old.vertices = mesh.vertices; old.normals = mesh.normals; old.tangents = mesh.tangents; old.uv = mesh.uv;
                    old.boneWeights = mesh.boneWeights; old.bindposes = mesh.bindposes; old.triangles = mesh.triangles;
                    old.bounds = mesh.bounds; old.UploadMeshData(false); EditorUtility.SetDirty(old); Object.DestroyImmediate(mesh);
                }
                renderer.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(stylized ?
                    StylizedContenderBuilder.ArtRoot + "/Player.mat" : Root + "/Ion.mat");
                renderer.localBounds = renderer.sharedMesh.bounds;
                renderer.updateWhenOffscreen = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                animator.runtimeAnimatorController = Controller();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var view = root.AddComponent<FirstPersonArms>();
                view.Animator = animator; view.Surface = renderer;
                renderer.quality = SkinQuality.Bone4;
                view.Hands = root.GetComponent<HandArticulation>();
                if (view.Hands == null) view.Hands = root.AddComponent<HandArticulation>();
                view.Hands.Configure(root.transform);
                HandGripAuthoring.Apply(view.Hands);
                // Relative to the railgun: keeps the shoulder cuts below the first-person frame.
                root.transform.localPosition = new Vector3(-.20f, -1.44f, -.17f);
                foreach (var part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 11;
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Characters/ViewArms.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static RuntimeAnimatorController Controller()
        {
            string path = Root + "/View arms.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var layer = controller.layers[0]; layer.iKPass = true; controller.layers = new[] { layer };
            var state = layer.stateMachine.states.FirstOrDefault(s => s.state.name == "Ready").state;
            if (state == null) state = layer.stateMachine.AddState("Ready");
            var body = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/Contender.controller");
            state.motion = body.animationClips.First(c => c.name.EndsWith("Idle_Loop"));
            layer.stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        public static Mesh ExtractArms(Mesh source, HashSet<int> bones)
        {
            var sourceWeights = source.boneWeights;
            bool Arm(int index)
            {
                var w = sourceWeights[index];
                float sum = (bones.Contains(w.boneIndex0) ? w.weight0 : 0) + (bones.Contains(w.boneIndex1) ? w.weight1 : 0) +
                    (bones.Contains(w.boneIndex2) ? w.weight2 : 0) + (bones.Contains(w.boneIndex3) ? w.weight3 : 0);
                return sum >= .6f;
            }
            var selected = new List<int>();
            var indices = source.triangles;
            for (int i = 0; i < indices.Length; i += 3)
                if (Arm(indices[i]) && Arm(indices[i + 1]) && Arm(indices[i + 2]))
                    selected.AddRange(new[] { indices[i], indices[i + 1], indices[i + 2] });
            if (selected.Count < 300) throw new InvalidOperationException("Arm extraction did not retain sufficient source geometry.");
            var used = selected.Distinct().ToArray();
            var map = new Dictionary<int, int>();
            for (int i = 0; i < used.Length; i++) map[used[i]] = i;
            var positions = source.vertices; var normals = source.normals; var tangents = source.tangents; var uv = source.uv;
            var mesh = new Mesh { name = "Contender arm-only presentation", indexFormat = IndexFormat.UInt32,
                vertices = used.Select(i => positions[i]).ToArray(), normals = used.Select(i => normals[i]).ToArray(),
                tangents = tangents.Length == positions.Length ? used.Select(i => tangents[i]).ToArray() : Array.Empty<Vector4>(),
                uv = used.Select(i => uv[i]).ToArray(),
                boneWeights = used.Select(i => sourceWeights[i]).ToArray(), bindposes = source.bindposes,
                triangles = selected.Select(i => map[i]).ToArray() };
            mesh.RecalculateBounds(); return mesh;
        }
    }
}
