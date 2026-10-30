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
            if (StylizedContenderBuilder.ModelsReady) { StylizedContenderBuilder.Build(); return; }
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
            presentation.Hands = instance.AddComponent<HandArticulation>();
            presentation.Hands.Configure(instance.transform);
            HandGripAuthoring.Apply(presentation.Hands);
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
                renderer.quality = SkinQuality.Bone4;
                renderer.updateWhenOffscreen = true;
                renderers.Add(renderer);
                lods[level] = new LOD(new[] { .18f, .065f, .012f }[level], new Renderer[] { renderer });
            }
            presentation.BodyRenderers = renderers.ToArray();
            var lod = instance.AddComponent<LODGroup>();
            lod.SetLODs(lods);
            lod.RecalculateBounds();
            Hurtboxes(animator);
            Weapon(instance.transform, presentation);
            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Resources/Characters/Contender.prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            FirstPersonArmsBuilder.Build();
            Debug.Log("Animated contender built with humanoid retargeting, three LODs and bone-following hitboxes.");
        }

        private static void Hurtboxes(Animator animator)
        {
            // Collider dimensions are metres, independent of the imported FBX bone units.
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
        }

        [MenuItem("Rail Arena/Update contender hurtboxes")]
        public static void UpdateHurtboxes()
        {
            const string path = "Assets/Resources/Characters/Contender.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var hitbox in root.GetComponentsInChildren<Hitbox>(true))
                    UnityEngine.Object.DestroyImmediate(hitbox.gameObject);
                Hurtboxes(root.GetComponent<Animator>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
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
            SetMetreScale(obj.transform);
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
            SetMetreScale(obj.transform);
            var capsule = obj.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = Vector3.Distance(first.position, last.position) + radius;
            capsule.isTrigger = true;
            obj.AddComponent<Hitbox>().Region = HitRegion.Limb;
        }

        private static void SetMetreScale(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            if (Mathf.Min(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) < .000001f)
                throw new InvalidOperationException("A hurtbox cannot inherit a zero bone scale.");
            transform.localScale = new Vector3(1 / Mathf.Abs(scale.x), 1 / Mathf.Abs(scale.y), 1 / Mathf.Abs(scale.z));
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Weapons/Railgun.prefab");
            if (prefab == null) throw new InvalidOperationException("Build the detailed railgun before regenerating contenders.");
            var root = UnityEngine.Object.Instantiate(prefab, parent, false).transform;
            presentation.Weapon = root;
            presentation.Railgun = root.GetComponent<RailgunPresentation>();
            presentation.RightGrip = presentation.Railgun.RightGrip;
            presentation.LeftGrip = presentation.Railgun.LeftGrip;
            presentation.Muzzle = presentation.Railgun.Muzzle;
        }

        [MenuItem("Rail Arena/Update contender weapon")]
        public static void UpdateWeapon()
        {
            foreach (string path in new[] { "Assets/Resources/Characters/Contender.prefab",
                "Assets/Resources/Characters/Stylized/Bot01.prefab", "Assets/Resources/Characters/Stylized/Bot02.prefab",
                "Assets/Resources/Characters/Stylized/Bot03.prefab" })
            {
                if (!System.IO.File.Exists(path)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var presentation = root.GetComponent<ContenderPresentation>();
                    if (presentation.Weapon != null) UnityEngine.Object.DestroyImmediate(presentation.Weapon.gameObject);
                    Weapon(root.transform, presentation);
                    HandGripAuthoring.Apply(presentation.Hands);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
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
