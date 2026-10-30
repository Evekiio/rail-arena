using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RailArena.Editor
{
    public static class ContenderMotionArtBuilder
    {
        public static void Prepare()
        {
            PrepareRetreatCandidates();
            PrepareAirborneCandidates();
            ImportClips(new[] { "AdvanceWalk", "AdvanceJog", "AdvanceRun" }, "motion-advance-import.json");
            var original = Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            foreach (string name in new[] { "Bot01", "Bot02", "Bot03" })
                if (Resources.Load<GameObject>("Characters/Stylized/" + name).GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController != original)
                    throw new InvalidOperationException("Shared retreat override requires the existing common controller");
            const string path = "Assets/Resources/Art/ContenderRetreat.overrideController";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (controller == null) { controller = new AnimatorOverrideController(original); AssetDatabase.CreateAsset(controller, path); }
            controller.runtimeAnimatorController = PrepareArtStateGraph(original);
            var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>(); controller.GetOverrides(overrides);
            var names = new System.Collections.Generic.Dictionary<string, string> { { "Backward walk", "RetreatWalk" }, { "Backward jog", "RetreatJog" }, { "Backward run", "RetreatRun" }, { "Walk_Loop", "AdvanceWalk" }, { "Jog_Fwd_Loop", "AdvanceJog" }, { "Sprint_Loop", "AdvanceRun" } };
            int changed = 0;
            for (int index = 0; index < overrides.Count; index++)
                if (names.TryGetValue(overrides[index].Key.name.Split('|').Last(), out var name))
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/RailArena/Art/ArtPass/Animation/" + name + ".fbx").OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                    overrides[index] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[index].Key, clip); changed++;
                }
            if (changed != 6) throw new InvalidOperationException("Exactly six forward and backward locomotion clips must be replaced");
            controller.ApplyOverrides(overrides); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
            Debug.Log("AUTHORED_LOCOMOTION_READY six dedicated gaits plus airborne states; legacy controller preserved");
        }
        static AnimatorController PrepareArtStateGraph(RuntimeAnimatorController original)
        {
            const string path = "Assets/Resources/Art/ContenderMotion.controller";
            var graph = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (graph == null)
            {
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), path))
                    throw new InvalidOperationException("Art locomotion must use an independent copy of the existing graph");
                graph = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            if (graph == original) throw new InvalidOperationException("Legacy controller must remain unchanged");
            var states = graph.layers[0].stateMachine.states.ToDictionary(child => child.state.name, child => child.state);
            foreach (var pair in new[] { ("Jump", "Takeoff"), ("Air", "AirborneHold"), ("Land", "LandingRecover") })
            {
                var state = states[pair.Item1];
                state.motion = AuthoredClip(pair.Item2);
                state.speed = 1; state.speedParameterActive = false;
                EditorUtility.SetDirty(state);
            }
            foreach (var transition in states["Jump"].transitions)
                if (transition.destinationState == states["Air"] && transition.hasExitTime)
                { transition.exitTime = .9f; EditorUtility.SetDirty(transition); }
            foreach (var transition in states["Land"].transitions)
                if (transition.destinationState == states["Locomotion"] && transition.hasExitTime)
                { transition.exitTime = .92f; EditorUtility.SetDirty(transition); }
            foreach (bool backwards in new[] { false, true })
            {
                string name = backwards ? "Art retreat recovery" : "Art forward recovery";
                var transition = states["Land"].transitions.SingleOrDefault(item => item.name == name);
                if (transition == null)
                {
                    transition = states["Land"].AddTransition(states["Locomotion"]); transition.name = name;
                    transition.AddCondition(backwards ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, backwards ? -.5f : .5f, "Speed");
                }
                transition.hasExitTime = true; transition.exitTime = .25f; transition.duration = .08f;
                EditorUtility.SetDirty(transition);
            }
            EditorUtility.SetDirty(graph);
            return graph;
        }
        static AnimationClip AuthoredClip(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/RailArena/Art/ArtPass/Animation/" + name + ".fbx")
            .OfType<AnimationClip>().Single(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
        [Serializable] sealed class ClipEvidence
        {
            public string name, path;
            public bool humanoid, validAvatar;
            public float length, frameRate, humanScale;
            public int curves;
        }
        [Serializable] sealed class ImportEvidence
        {
            public string scope = "Original authored motion imported on the existing humanoid skeleton. Installation and skinned contact are verified separately by the installed-avatar/playback records.";
            public ClipEvidence[] clips;
        }
        public static void PrepareRetreatCandidates()
            => ImportClips(new[] { "RetreatWalk", "RetreatJog", "RetreatRun" }, "motion-retreat-import.json");

        public static void PrepareAirborneCandidates()
            => ImportClips(new[] { "Takeoff", "AirborneHold", "LandingRecover" }, "motion-airborne-import.json");

        static void ImportClips(string[] names, string evidenceFile)
        {
            var source = (ModelImporter)AssetImporter.GetAtPath("Assets/RailArena/Art/StylizedContenders/Player_LOD0.fbx");
            var evidence = new System.Collections.Generic.List<ClipEvidence>();
            foreach (string name in names)
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
                settings[0].name = name; settings[0].loopTime = name.StartsWith("Retreat", StringComparison.Ordinal) || name.StartsWith("Advance", StringComparison.Ordinal) || name == "AirborneHold"; settings[0].loopPose = settings[0].loopTime;
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
            File.WriteAllText("../Validation/" + evidenceFile, JsonUtility.ToJson(new ImportEvidence { clips = evidence.ToArray() }, true));
            Debug.Log("AUTHORED_HUMANOID_IMPORT_VERIFIED " + string.Join(", ", names));
        }
    }
}
