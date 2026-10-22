using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailArena.Simulation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RailArena.Editor
{
    public static class ContenderBuilder
    {
        private const string Root = "Assets/RailArena/Characters";
        private const string Model = "Assets/ThirdParty/Grab3D/Contender_LOD";
        private static Dictionary<string, AnimationClip> clips;

        [MenuItem("Rail Arena/Build animated contender")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/Resources/Characters");
            AssetDatabase.Refresh();
            ContenderImporter.ConfigureAnimationClips("Assets/ThirdParty/Quaternius/UAL1_Standard.fbx");
            clips = AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/Quaternius/UAL1_Standard.fbx")
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToDictionary(c => c.name.Split('|').Last());
            var skins = new[] { Skin("Ion", new Color(.87f, .96f, 1)), Skin("Cinder", new Color(1, .64f, .39f)),
                Skin("Violet", new Color(.77f, .65f, 1)), Skin("Lime", new Color(.77f, 1, .51f)) };
            var controller = Controller();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model + "0.fbx");
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "Armored contender";
            var animator = instance.GetComponent<Animator>();
            if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Contender requires a valid humanoid Avatar.");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var presentation = instance.AddComponent<ContenderPresentation>();
            presentation.Animator = animator;
            presentation.Skins = skins;
            var bones = instance.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
            var renderers = new List<Renderer>();
            var lods = new LOD[3];
            for (int level = 0; level < 3; level++)
            {
                SkinnedMeshRenderer renderer;
                if (level == 0) renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                else
                {
                    var imported = AssetDatabase.LoadAssetAtPath<GameObject>(Model + level + ".fbx").GetComponentInChildren<SkinnedMeshRenderer>();
                    var obj = new GameObject("Body LOD" + level);
                    obj.transform.SetParent(instance.transform, false);
                    renderer = obj.AddComponent<SkinnedMeshRenderer>();
                    renderer.sharedMesh = imported.sharedMesh;
                    renderer.bones = imported.bones.Select(b => bones[b.name]).ToArray();
                    renderer.rootBone = bones[imported.rootBone.name];
                    renderer.localBounds = imported.localBounds;
                }
                renderer.sharedMaterial = skins[0];
                renderer.updateWhenOffscreen = true;
                renderers.Add(renderer);
                lods[level] = new LOD(new[] { .18f, .065f, .012f }[level], new Renderer[] { renderer });
            }
            presentation.BodyRenderers = renderers.ToArray();
            var lod = instance.AddComponent<LODGroup>();
            lod.SetLODs(lods);
            lod.RecalculateBounds();
            // Hurtboxes follow the same bones as the visible skinned mesh.
            Box(animator, HumanBodyBones.Head, HitRegion.Head, new Vector3(0, .075f, 0), new Vector3(.32f, .34f, .31f));
            Box(animator, HumanBodyBones.Chest, HitRegion.Body, new Vector3(0, .04f, 0), new Vector3(.47f, .43f, .29f));
            Box(animator, HumanBodyBones.Hips, HitRegion.Body, new Vector3(0, .04f, 0), new Vector3(.4f, .28f, .27f));
            Limb(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, .1f);
            Limb(animator, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, .085f);
            Limb(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, .1f);
            Limb(animator, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, .085f);
            Limb(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, .13f);
            Limb(animator, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, .11f);
            Limb(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, .13f);
            Limb(animator, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, .11f);
            Weapon(instance.transform, presentation);
            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Resources/Characters/Contender.prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            Debug.Log("Animated contender built with humanoid retargeting, three LODs and bone-following hitboxes.");
        }

        private static AnimatorController Controller()
        {
            string path = Root + "/Contender.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path)) AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Gait", AnimatorControllerParameterType.Float);
            controller.AddParameter("Vertical", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;
            var locomotion = machine.AddState("Locomotion");
            locomotion.iKOnFeet = true;
            locomotion.speedParameter = "Gait";
            locomotion.speedParameterActive = true;
            var tree = new BlendTree { name = "Velocity locomotion", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(Reverse(clips["Sprint_Loop"], "Backward run"), -9);
            tree.AddChild(Reverse(clips["Jog_Fwd_Loop"], "Backward jog"), -4);
            tree.AddChild(Reverse(clips["Walk_Loop"], "Backward walk"), -1.5f);
            tree.AddChild(clips["Idle_Loop"], 0);
            tree.AddChild(clips["Walk_Loop"], 1.5f);
            tree.AddChild(clips["Jog_Fwd_Loop"], 4);
            tree.AddChild(clips["Sprint_Loop"], 9);
            locomotion.motion = tree;
            machine.defaultState = locomotion;
            var jump = State(machine, "Jump", "Jump_Start", 3.5f);
            var air = State(machine, "Air", "Jump_Loop", 1);
            var land = State(machine, "Land", "Jump_Land", 3);
            var death = State(machine, "Death", "Death01", 1.3f);
            var takeoff = locomotion.AddTransition(jump);
            takeoff.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            takeoff.AddCondition(AnimatorConditionMode.Greater, 0, "Vertical");
            takeoff.duration = .06f;
            var fall = locomotion.AddTransition(air);
            fall.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
            fall.AddCondition(AnimatorConditionMode.Less, 0, "Vertical");
            fall.duration = .08f;
            Exit(jump, air, .65f, .08f);
            var touch = air.AddTransition(land);
            touch.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            touch.duration = .04f;
            var shortJump = jump.AddTransition(land);
            shortJump.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
            shortJump.duration = .04f;
            Exit(land, locomotion, .45f, .08f);
            var die = machine.AddAnyStateTransition(death);
            die.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            die.canTransitionToSelf = false;
            die.duration = .06f;
            var mask = new AvatarMask { name = "Upper body reactions" };
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            AssetDatabase.AddObjectToAsset(mask, controller);
            controller.AddLayer("Hit reaction");
            layers = controller.layers;
            layers[1].avatarMask = mask;
            layers[1].defaultWeight = .6f;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;
            var reactions = layers[1].stateMachine;
            var empty = reactions.AddState("No reaction");
            reactions.defaultState = empty;
            var hit = State(reactions, "Impact", "Hit_Chest", 1.3f);
            var impact = empty.AddTransition(hit);
            impact.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            impact.duration = .025f;
            Exit(hit, empty, .9f, .08f);
            return controller;
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, string clip, float speed)
        { var state = machine.AddState(name); state.motion = clips[clip]; state.speed = speed; return state; }
        private static void Exit(AnimatorState from, AnimatorState to, float time, float duration)
        { var transition = from.AddTransition(to); transition.hasExitTime = true; transition.exitTime = time; transition.duration = duration; }

        private static AnimationClip Reverse(AnimationClip source, string name)
        {
            var clip = new AnimationClip { name = name, frameRate = source.frameRate };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var original = AnimationUtility.GetEditorCurve(source, binding);
                var keys = original.keys.Reverse().Select(k => new Keyframe(source.length - k.time, k.value,
                    -k.outTangent, -k.inTangent, k.outWeight, k.inWeight) { weightedMode = k.weightedMode }).ToArray();
                AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keys));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            Save(clip, Root + "/" + name + ".anim");
            if (!clip.isHumanMotion) throw new InvalidOperationException("Reversed locomotion did not retain humanoid motion curves.");
            return clip;
        }

        private static void Box(Animator animator, HumanBodyBones bone, HitRegion region, Vector3 center, Vector3 size)
        {
            var obj = new GameObject(region + " hurtbox");
            obj.transform.SetParent(animator.GetBoneTransform(bone), false);
            obj.layer = 8;
            var box = obj.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            box.isTrigger = true;
            obj.AddComponent<Hitbox>().Region = region;
        }

        private static void Limb(Animator animator, HumanBodyBones start, HumanBodyBones end, float radius)
        {
            Transform first = animator.GetBoneTransform(start), last = animator.GetBoneTransform(end);
            Vector3 localEnd = first.InverseTransformPoint(last.position);
            var obj = new GameObject(start + " hurtbox");
            obj.layer = 8;
            obj.transform.SetParent(first, false);
            obj.transform.localPosition = localEnd / 2;
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, localEnd.normalized);
            var capsule = obj.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = localEnd.magnitude + radius;
            capsule.isTrigger = true;
            obj.AddComponent<Hitbox>().Region = HitRegion.Limb;
        }

        private static Material Skin(string name, Color tint)
        {
            const string textureRoot = "Assets/ThirdParty/Grab3D/Textures/sci-fi-soldier-rigged_";
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Contender " + name };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "basecolor.png"));
            material.SetColor("_BaseColor", tint);
            string normalPath = textureRoot + "normal.png";
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            if (normalImporter.textureType != TextureImporterType.NormalMap)
            { normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.SaveAndReimport(); }
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", PackedMetallic());
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Metallic", 1);
            material.SetFloat("_Smoothness", .75f);
            Save(material, Root + "/" + name + ".mat");
            return material;
        }

        private static void Weapon(Transform parent, ContenderPresentation presentation)
        {
            var root = new GameObject("Rail accelerator").transform;
            root.SetParent(parent, false);
            presentation.Weapon = root;
            Material alloy = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/Pale alloy.mat");
            Material dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/Blackened structure.mat");
            Material ion = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/Ion energy.mat");
            Part(root, "Receiver", new Vector3(0, 0, .05f), new Vector3(.16f, .16f, .48f), dark);
            Part(root, "Stock", new Vector3(0, -.025f, -.24f), new Vector3(.12f, .12f, .22f), alloy);
            Part(root, "Trigger grip", new Vector3(0, -.11f, -.1f), new Vector3(.075f, .19f, .1f), dark);
            Part(root, "Upper rail", new Vector3(0, .09f, .28f), new Vector3(.1f, .045f, .65f), alloy);
            Part(root, "Lower rail", new Vector3(0, -.07f, .28f), new Vector3(.1f, .045f, .65f), alloy);
            Part(root, "Core", new Vector3(0, .01f, .3f), new Vector3(.045f, .045f, .6f), ion);
            for (int i = 0; i < 6; i++)
                Part(root, "Cooling fin", new Vector3(0, 0, i * .055f), new Vector3(.21f, .2f, .018f), alloy);
            presentation.RightGrip = Marker(root, "Right grip", new Vector3(0, -.1f, -.09f));
            presentation.LeftGrip = Marker(root, "Left support", new Vector3(-.13f, -.08f, -.015f));
            presentation.RightGrip.localRotation = Quaternion.Euler(0, 0, 90);
            presentation.LeftGrip.localRotation = Quaternion.Euler(0, 0, -90);
            presentation.Muzzle = Marker(root, "Muzzle", new Vector3(0, .01f, .65f));
        }

        private static Transform Marker(Transform parent, string name, Vector3 point)
        { var marker = new GameObject(name).transform; marker.SetParent(parent, false); marker.localPosition = point; return marker; }
        private static void Part(Transform parent, string name, Vector3 point, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name; part.transform.SetParent(parent, false); part.transform.localPosition = point;
            part.transform.localScale = size; part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }
        private static void Save(UnityEngine.Object obj, string path)
        { if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)) AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(obj, path); }

        private static Texture2D PackedMetallic()
        {
            string output = Root + "/MetallicSmoothness.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(output);
            if (existing != null) return existing;
            const string prefix = "Assets/ThirdParty/Grab3D/Textures/sci-fi-soldier-rigged_metallic-roughness_";
            string[] paths = { prefix + "metallic.png", prefix + "roughness.png" };
            foreach (string path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = true;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }
            var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]);
            var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[1]);
            Color32[] pixels = metal.GetPixels32(), roughPixels = rough.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(pixels[i].r, 0, 0, (byte)(255 - roughPixels[i].r));
            var packed = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels32(pixels);
            packed.Apply();
            File.WriteAllBytes(output, packed.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(packed);
            foreach (string path in paths)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
            AssetDatabase.ImportAsset(output);
            var packedImporter = (TextureImporter)AssetImporter.GetAtPath(output);
            packedImporter.sRGBTexture = false;
            packedImporter.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(output);
        }
    }
}
