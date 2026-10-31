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
            PrepareStrafeCandidates();
            PrepareReactionCandidates();
            PrepareCrouchCandidates();
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
            Debug.Log("AUTHORED_LOCOMOTION_READY twelve cardinal gaits, five grounded crouch motions, three airborne states and two combat reactions; legacy controller preserved");
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
            foreach (string parameter in new[] { "MotionX", "MotionZ" })
                if (!graph.parameters.Any(item => item.name == parameter)) graph.AddParameter(parameter, AnimatorControllerParameterType.Float);
            var legacyTree = (BlendTree)((AnimatorController)original).layers[0].stateMachine.states.Single(child => child.state.name == "Locomotion").state.motion;
            var tree = (BlendTree)states["Locomotion"].motion;
            var children = legacyTree.children.Select(child => new ChildMotion
            { motion = child.motion, position = new Vector2(0, child.threshold), timeScale = 1 }).ToList();
            foreach (var direction in new[] { ("Left", -1), ("Right", 1) })
            foreach (var gait in new[] { ("Walk", 1.5f), ("Jog", 4f), ("Run", 9f) })
                children.Add(new ChildMotion { motion = AuthoredClip("Strafe" + direction.Item1 + gait.Item1), position = new Vector2(direction.Item2 * gait.Item2, 0), timeScale = 1 });
            tree.name = "Authored directional locomotion"; tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "MotionX"; tree.blendParameterY = "MotionZ"; tree.useAutomaticThresholds = false; tree.children = children.ToArray();
            states["Locomotion"].speed = 1; states["Locomotion"].speedParameterActive = false;
            EditorUtility.SetDirty(tree); EditorUtility.SetDirty(states["Locomotion"]);
            PrepareCrouchState(graph, states);
            foreach (var pair in new[] { ("Jump", "Takeoff"), ("Air", "AirborneHold"), ("Land", "LandingRecover") })
            {
                var state = states[pair.Item1];
                state.motion = AuthoredClip(pair.Item2);
                state.speed = 1; state.speedParameterActive = false;
                EditorUtility.SetDirty(state);
            }
            states["Death"].motion = AuthoredClip("DeathFall"); states["Death"].speed = 1; states["Death"].speedParameterActive = false; states["Death"].tag = "AuthoredDeath";
            EditorUtility.SetDirty(states["Death"]);
            var impact = graph.layers[1].stateMachine.states.Single(child => child.state.name == "Impact").state;
            impact.motion = AuthoredClip("HitResponse"); impact.speed = 1; impact.speedParameterActive = false;
            foreach (var transition in impact.transitions)
            { transition.hasExitTime = true; transition.exitTime = .95f; transition.duration = .04f; EditorUtility.SetDirty(transition); }
            EditorUtility.SetDirty(impact);
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

        static void PrepareCrouchState(AnimatorController graph, System.Collections.Generic.Dictionary<string,AnimatorState> states)
        {
            if(!graph.parameters.Any(parameter=>parameter.name=="AuthoredCrouch"))graph.AddParameter("AuthoredCrouch",AnimatorControllerParameterType.Bool);
            var machine=graph.layers[0].stateMachine;
            if(!states.TryGetValue("Crouch",out var state))state=machine.AddState("Crouch");
            var tree=state.motion as BlendTree;
            if(tree==null){tree=new BlendTree();AssetDatabase.AddObjectToAsset(tree,graph);}
            tree.name="Authored crouch directions";tree.blendType=BlendTreeType.FreeformDirectional2D;tree.blendParameter="MotionX";tree.blendParameterY="MotionZ";tree.useAutomaticThresholds=false;
            tree.children=new[]{new ChildMotion{motion=AuthoredClip("CrouchIdle"),position=Vector2.zero,timeScale=1},
                new ChildMotion{motion=AuthoredClip("CrouchForward"),position=new Vector2(0,3.6f),timeScale=1},new ChildMotion{motion=AuthoredClip("CrouchBackward"),position=new Vector2(0,-3.6f),timeScale=1},
                new ChildMotion{motion=AuthoredClip("CrouchLeft"),position=new Vector2(-3.6f,0),timeScale=1},new ChildMotion{motion=AuthoredClip("CrouchRight"),position=new Vector2(3.6f,0),timeScale=1}};
            state.motion=tree;state.speed=1;state.speedParameterActive=false;state.iKOnFeet=true;state.tag="AuthoredCrouch";
            var enter=machine.anyStateTransitions.SingleOrDefault(item=>item.name=="Art grounded crouch");
            if(enter==null)enter=machine.AddAnyStateTransition(state);
            enter.name="Art grounded crouch";enter.conditions=new[]{new AnimatorCondition{mode=AnimatorConditionMode.If,parameter="AuthoredCrouch"},new AnimatorCondition{mode=AnimatorConditionMode.If,parameter="Grounded"},new AnimatorCondition{mode=AnimatorConditionMode.IfNot,parameter="Dead"}};
            enter.hasExitTime=false;enter.duration=0;enter.canTransitionToSelf=false;
            CrouchTransition(state,states["Locomotion"],"Art crouch stand",0,new AnimatorCondition{mode=AnimatorConditionMode.IfNot,parameter="AuthoredCrouch"},new AnimatorCondition{mode=AnimatorConditionMode.If,parameter="Grounded"});
            CrouchTransition(state,states["Jump"],"Art crouch jump",.06f,new AnimatorCondition{mode=AnimatorConditionMode.IfNot,parameter="Grounded"},new AnimatorCondition{mode=AnimatorConditionMode.Greater,parameter="Vertical",threshold=0});
            CrouchTransition(state,states["Air"],"Art crouch fall",.06f,new AnimatorCondition{mode=AnimatorConditionMode.IfNot,parameter="Grounded"},new AnimatorCondition{mode=AnimatorConditionMode.Less,parameter="Vertical",threshold=.0001f});
            EditorUtility.SetDirty(tree);EditorUtility.SetDirty(state);EditorUtility.SetDirty(enter);
        }
        static void CrouchTransition(AnimatorState source,AnimatorState destination,string name,float duration,params AnimatorCondition[] conditions)
        {
            var transition=source.transitions.SingleOrDefault(item=>item.name==name)??source.transitions.SingleOrDefault(item=>item.destinationState==destination&&string.IsNullOrEmpty(item.name));
            if(transition==null)transition=source.AddTransition(destination);
            transition.name=name;transition.hasExitTime=false;transition.duration=duration;transition.conditions=conditions;EditorUtility.SetDirty(transition);
        }
        public static void AuditGraphRepeatability()
        {
            var original=Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            PrepareArtStateGraph(original);AssetDatabase.SaveAssets();
            const string path="Assets/Resources/Art/ContenderMotion.controller";byte[] before=File.ReadAllBytes(path);
            PrepareArtStateGraph(original);AssetDatabase.SaveAssets();byte[] after=File.ReadAllBytes(path);
            if(!before.SequenceEqual(after))throw new InvalidOperationException("Art graph preparation must preserve serialized identities across repeated runs");
            Directory.CreateDirectory("../Validation");
            using(var sha=System.Security.Cryptography.SHA256.Create())
                File.WriteAllText("../Validation/motion-crouch-graph-repeatability.json","{\"identical\":true,\"controllerSha256\":\""+BitConverter.ToString(sha.ComputeHash(after)).Replace("-","").ToLowerInvariant()+"\"}\n");
            Debug.Log("ART_GRAPH_REPEATABILITY_VERIFIED");
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
            public string scope = "Original authored motion imported on the existing humanoid skeleton. Installation and skinned contact are verified separately by the installed-avatar/playback records.";
            public ClipEvidence[] clips;
        }
        public static void PrepareRetreatCandidates()
            => ImportClips(new[] { "RetreatWalk", "RetreatJog", "RetreatRun" }, "motion-retreat-import.json");

        public static void PrepareAirborneCandidates()
            => ImportClips(new[] { "Takeoff", "AirborneHold", "LandingRecover" }, "motion-airborne-import.json");

        public static void PrepareStrafeCandidates()
            => ImportClips(new[] { "StrafeLeftWalk", "StrafeLeftJog", "StrafeLeftRun", "StrafeRightWalk", "StrafeRightJog", "StrafeRightRun" }, "motion-strafe-import.json");

        public static void PrepareReactionCandidates()
            => ImportClips(new[] { "HitResponse", "DeathFall" }, "motion-reactions-import.json");

        public static void PrepareCrouchCandidates()
            => ImportClips(new[] { "CrouchIdle", "CrouchForward", "CrouchBackward", "CrouchLeft", "CrouchRight", "CrouchEnter", "CrouchExit" }, "motion-crouch-import.json");

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
                settings[0].name = name; settings[0].loopTime = name.StartsWith("Retreat", StringComparison.Ordinal) || name.StartsWith("Advance", StringComparison.Ordinal) || name.StartsWith("Strafe", StringComparison.Ordinal) || name == "AirborneHold" ||
                    (name.StartsWith("Crouch", StringComparison.Ordinal) && name != "CrouchEnter" && name != "CrouchExit"); settings[0].loopPose = settings[0].loopTime;
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
