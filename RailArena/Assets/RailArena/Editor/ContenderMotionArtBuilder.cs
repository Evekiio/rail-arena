using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class ContenderMotionArtBuilder
    {
        public static void Prepare()
        {
            PrepareRetreatCandidates();
            var original = Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            foreach (string name in new[] { "Bot01", "Bot02", "Bot03" })
                if (Resources.Load<GameObject>("Characters/Stylized/" + name).GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController != original)
                    throw new InvalidOperationException("Shared retreat override requires the existing common controller");
            const string path = "Assets/Resources/Art/ContenderRetreat.overrideController";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (controller == null) { controller = new AnimatorOverrideController(original); AssetDatabase.CreateAsset(controller, path); }
            controller.runtimeAnimatorController = original;
            var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>(); controller.GetOverrides(overrides);
            var names = new System.Collections.Generic.Dictionary<string, string> { { "Backward walk", "RetreatWalk" }, { "Backward jog", "RetreatJog" }, { "Backward run", "RetreatRun" } };
            int changed = 0;
            for (int index = 0; index < overrides.Count; index++)
                if (names.TryGetValue(overrides[index].Key.name, out var name))
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/RailArena/Art/ArtPass/Animation/" + name + ".fbx").OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                    overrides[index] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[index].Key, clip); changed++;
                }
            if (changed != 3) throw new InvalidOperationException("Exactly three reversed locomotion clips must be replaced");
            controller.ApplyOverrides(overrides); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            Debug.Log("AUTHORED_RETREAT_CONTROLLER_READY three dedicated clips; original controller and forward actions preserved");
        }
        [Serializable] sealed class ClipEvidence
        {
            public string name, path;
            public bool humanoid, validAvatar;
            public float length, frameRate, humanScale;
            public int curves;
        }
        [Serializable] sealed class ImportEvidence
        {
            public string scope = "Original retreat loops imported on the existing humanoid skeleton. Installation and skinned contact are verified separately by the installed-avatar/playback records.";
            public ClipEvidence[] clips;
        }
        public static void PrepareRetreatCandidates()
        {
            var source = (ModelImporter)AssetImporter.GetAtPath("Assets/RailArena/Art/StylizedContenders/Player_LOD0.fbx");
            var evidence = new System.Collections.Generic.List<ClipEvidence>();
            foreach (string name in new[] { "RetreatWalk", "RetreatJog", "RetreatRun" })
            {
                string path = "Assets/RailArena/Art/ArtPass/Animation/" + name + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Motion export missing: " + path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.preserveHierarchy = true;
                importer.humanDescription = source.humanDescription;
                importer.importAnimation = true; importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.SaveAndReimport();
                var settings = importer.defaultClipAnimations;
                if (settings.Length != 1) throw new InvalidOperationException("One authored take is required per motion export: " + name);
                settings[0].name = name; settings[0].loopTime = true; settings[0].loopPose = true;
                settings[0].lockRootRotation = true; settings[0].lockRootPositionXZ = true; settings[0].lockRootHeightY = true;
                settings[0].keepOriginalOrientation = true; settings[0].keepOriginalPositionXZ = true; settings[0].keepOriginalPositionY = true;
                importer.clipAnimations = settings; importer.SaveAndReimport();
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Single();
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                if (!avatar.isHuman || !avatar.isValid || !clip.isHumanMotion) throw new InvalidOperationException("Motion must import as valid humanoid data: " + name);
                evidence.Add(new ClipEvidence { name = name, path = path, humanoid = clip.isHumanMotion, validAvatar = avatar.isValid,
                    humanScale = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Animator>().humanScale, length = clip.length, frameRate = clip.frameRate, curves = AnimationUtility.GetCurveBindings(clip).Length });
            }
            AssetDatabase.SaveAssets(); Directory.CreateDirectory("../Validation");
            File.WriteAllText("../Validation/motion-retreat-import.json", JsonUtility.ToJson(new ImportEvidence { clips = evidence.ToArray() }, true));
            Debug.Log("RETREAT_HUMANOID_IMPORT_VERIFIED three original loop candidates; live controller unchanged");
        }
    }
}
