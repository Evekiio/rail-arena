using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CombatReactionArtReview
    {
        sealed class SkinSurface
        {
            readonly Transform[] bones; readonly Vector3[] vertices; readonly BoneWeight[] weights;
            readonly Matrix4x4[] bindposes, matrices;
            public SkinSurface(SkinnedMeshRenderer skin)
            { bones = skin.bones; vertices = skin.sharedMesh.vertices; weights = skin.sharedMesh.boneWeights; bindposes = skin.sharedMesh.bindposes; matrices = new Matrix4x4[bones.Length]; Assert.That(vertices.Length, Is.GreaterThan(500)); }
            public Vector2 Heights()
            {
                for (int i = 0; i < bones.Length; i++) matrices[i] = bones[i].localToWorldMatrix * bindposes[i];
                float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var v = vertices[i]; var w = weights[i];
                    float y = (matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3).y;
                    minimum = Mathf.Min(minimum, y); maximum = Mathf.Max(maximum, y);
                }
                return new Vector2(minimum, maximum);
            }
        }
        [UnityTest] public IEnumerator InstalledReactionsRetainContactsDeathAndRespawnLifecycle() => Capture(false);
        [UnityTest] public IEnumerator CaptureSharedLegacyDeathReference() => Capture(true);
        IEnumerator Capture(bool baseline)
        {
            var graph = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Art/ContenderMotion.controller");
            var death = graph.layers[0].stateMachine.states.Single(child => child.state.name == "Death").state;
            var impact = graph.layers[1].stateMachine.states.Single(child => child.state.name == "Impact").state;
            Assert.That(death.motion.name, Is.EqualTo("DeathFall")); Assert.That(impact.motion.name, Is.EqualTo("HitResponse"));
            Assert.That(death.speed, Is.EqualTo(1)); Assert.That(impact.speed, Is.EqualTo(1));
            var legacy = (AnimatorController)Resources.Load<GameObject>("Characters/Contender").GetComponent<ContenderPresentation>().Animator.runtimeAnimatorController;
            Assert.That(legacy.layers[0].stateMachine.states.Single(child => child.state.name == "Death").state.speed, Is.EqualTo(1.3f));
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity"); yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>(); typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary); while (!session.Match.IsActive) session.Match.Advance();
            new GameObject("Installed reaction review").AddComponent<ArtSlicePresentation>(); yield return null; yield return null;
            session.enabled = false; session.View.enabled = false;
            var surfaces = new Dictionary<int, SkinSurface>(); var seenImpact = new HashSet<int>(); var seenReturn = new HashSet<int>();
            foreach (var actor in session.Actors)
            {
                actor.Controller.enabled = false; actor.Presentation.enabled = false; actor.Command = default; actor.Slide = default;
                actor.transform.SetPositionAndRotation(new Vector3((actor.Id - 1.5f) * 1.4f, actor.Controller.skinWidth, 12), Quaternion.identity);
                foreach (var item in actor.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
                var animator = actor.Presentation.Animator; animator.Rebind(); animator.speed = 1; animator.keepAnimatorStateOnDisable = true; animator.enabled = false;
                surfaces.Add(actor.Id, new SkinSurface((SkinnedMeshRenderer)actor.Presentation.BodyRenderers[0]));
                if (baseline) actor.Presentation.SetMotionController(legacy);
                else Assert.That(actor.Presentation.CorpseContactSamples, Is.GreaterThan(20));
            }
            var collision = GameObject.CreatePrimitive(PrimitiveType.Cube); collision.name = "Owned corpse support"; collision.layer = 0;
            collision.transform.position = new Vector3(0, -.05f, 12); collision.transform.localScale = new Vector3(8, .1f, 6); collision.GetComponent<Renderer>().forceRenderingOff = true;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Owned contact reference"; floor.layer = 30;
            floor.transform.position = collision.transform.position; floor.transform.localScale = collision.transform.localScale; Object.DestroyImmediate(floor.GetComponent<Collider>());
            var material = new Material(Resources.Load<Material>("Art/StoneCover-Stone")); floor.GetComponent<Renderer>().sharedMaterial = material; Physics.SyncTransforms();
            var camera = new GameObject("Owned reaction review camera").AddComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .18f, .22f); camera.orthographic = true; camera.orthographicSize = 1.65f; camera.aspect = 16f / 9;
            camera.transform.position = new Vector3(3.2f, 2.3f, 17.5f); camera.transform.LookAt(new Vector3(0, .65f, 12));
            var fill = new GameObject("Owned reaction fill").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .5f; fill.shadows = LightShadows.None; fill.cullingMask = 1 << 30; fill.transform.rotation = Quaternion.Euler(25, 200, 0);
            var target = new RenderTexture(1280, 720, 24); target.Create(); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            string folder = Path.GetFullPath(baseline ? "../Validation/ReactionLegacyReference" : Environment.GetEnvironmentVariable("RA_REACTION_REVIEW_OUTPUT") ?? "../Validation/ReactionPlayback"); Directory.CreateDirectory(folder);
            var csv = new StringBuilder("frame,characterId,alive,baseState,reactionState,minimumSkinY,maximumSkinY,rootY,modelY,hipsY,headY,rightAxisDot,reactionWeight,handsEnabled,weaponVisible\n");
            var messages = new List<string>(); int errors = 0; void Check(bool condition, string message) { if (!condition) { errors++; if (messages.Count < 20) messages.Add(message); } }
            int oldRate = Time.captureFramerate; Time.captureFramerate = 60;
            try
            {
                for (int frame = 0; frame < (baseline ? 120 : 300); frame++)
                {
                    foreach (var actor in session.Actors)
                    {
                        var presentation = actor.Presentation; var animator = presentation.Animator;
                        if (frame == (baseline ? 0 : 120)) { actor.Health = 0; presentation.SetAlive(false); presentation.SetAlive(false); presentation.Hit(); }
                        if (!baseline && frame == 120 + CombatRules.RespawnTicks) { var position = actor.transform.position; actor.Place(position); actor.Controller.enabled = false; actor.Health = 100; actor.Life++; presentation.SetAlive(true); }
                        actor.Velocity = !baseline && frame < 120 ? Vector3.right * 4 : Vector3.zero; actor.GroundedOverride = true; actor.AirborneTicks = 0;
                        presentation.ApplyMotionState(1f / 60); if (!baseline && (frame == 12 || frame == 72 || frame == 119)) presentation.Hit();
                        animator.Update(1f / 60); presentation.ApplyCombatPose();
                        if (actor.Alive) presentation.Hands.Pose(0); else presentation.ApplyCorpsePose(1f / 60);
                        if (baseline) { presentation.Hands.enabled = true; presentation.Hands.Pose(0); }
                        var baseState = animator.GetCurrentAnimatorStateInfo(0); var reactionState = animator.GetCurrentAnimatorStateInfo(1);
                        if (frame < 120 && reactionState.IsName("Impact")) seenImpact.Add(actor.Id);
                        if (frame > 50 && frame < 70 && reactionState.IsName("No reaction")) seenReturn.Add(actor.Id);
                        var height = surfaces[actor.Id].Heights();
                        var right = Vector3.ProjectOnPlane(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position, Vector3.up).normalized;
                        float dot = Vector3.Dot(right, presentation.transform.right);
                        string label = $"frame={frame} character={actor.CharacterId}";
                        if (!baseline)
                        {
                        Check(height.x >= -.01f, label + " skin penetrates floor: " + height.x);
                        if (frame >= 210 && frame < 240) { Check(height.x <= .06f, label + " settled corpse floats: " + height.x); Check(height.y < .8f, label + " corpse must finish lying down"); }
                        if (frame >= 125 && frame < 240) Check(baseState.IsName("Death"), label + " must use death state");
                        if (frame >= 120 && frame < 132) Check(dot > .5f, label + " death must not reverse the body facing");
                        Check(actor.transform.position == new Vector3((actor.Id - 1.5f) * 1.4f, actor.Controller.skinWidth, 12), label + " animation moved authoritative root");
                        Check(presentation.Weapon.gameObject.activeSelf == actor.Alive, label + " weapon visibility");
                        Check(presentation.Hands.enabled == actor.Alive, label + " grip posing lifecycle");
                        Check(Mathf.Abs(animator.GetLayerWeight(1) - (actor.Alive ? .6f : 0)) < .001f, label + " reaction suppression/reset");
                        Check(presentation.GetComponentsInChildren<Hitbox>(true).All(box => box.GetComponent<Collider>().enabled == actor.Alive), label + " hitbox lifecycle");
                        if (frame >= 245) Check(!baseState.IsName("Death"), label + " respawn must leave death");
                        }
                        csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5:F6},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6},{11:F6},{12:F6},{13},{14}\n", frame, actor.CharacterId, actor.Alive, baseState.IsName("Death") ? "Death" : "Locomotion", reactionState.IsName("Impact") ? "Impact" : "No reaction", height.x, height.y, actor.transform.position.y, presentation.transform.position.y, animator.GetBoneTransform(HumanBodyBones.Hips).position.y, animator.GetBoneTransform(HumanBodyBones.Head).position.y, dot, animator.GetLayerWeight(1), presentation.Hands.enabled, presentation.Weapon.gameObject.activeSelf);
                    }
                    yield return null;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target }); var previous = RenderTexture.active;
                    try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); File.WriteAllBytes(Path.Combine(folder, "frame-" + frame.ToString("D4") + ".png"), pixels.EncodeToPNG()); }
                    finally { RenderTexture.active = previous; }
                }
                if (baseline) yield break;
                Assert.That(seenImpact.Count, Is.EqualTo(4)); Assert.That(seenReturn.Count, Is.EqualTo(4));
                Assert.That(errors, Is.EqualTo(0), string.Join("\n", messages));
                var profile = new StringBuilder("characterId,vertices,contactSamples,iterations,meanMs,p95Ms\n");
                foreach (var actor in session.Actors)
                {
                    actor.Health = 0; actor.Presentation.SetAlive(false); var animator = actor.Presentation.Animator;
                    animator.Play("Death", 0, .9999f); animator.Update(0);
                    for (int i = 0; i < 20; i++) actor.Presentation.ApplyCorpsePose(1f / 60);
                    var durations = new double[1000]; double total = 0;
                    for (int i = 0; i < durations.Length; i++)
                    {
                        long start = System.Diagnostics.Stopwatch.GetTimestamp(); actor.Presentation.ApplyCorpsePose(1f / 60);
                        durations[i] = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000d / System.Diagnostics.Stopwatch.Frequency; total += durations[i];
                    }
                    Array.Sort(durations); var heights = surfaces[actor.Id].Heights(); Assert.That(heights.x, Is.InRange(-.01f, .06f));
                    profile.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:F6},{5:F6}\n", actor.CharacterId, ((SkinnedMeshRenderer)actor.Presentation.BodyRenderers[0]).sharedMesh.vertexCount,
                        actor.Presentation.CorpseContactSamples, durations.Length, total / durations.Length, durations[950]);
                }
                File.WriteAllText(Path.Combine(folder, "ContactProfile.csv"), profile.ToString());
            }
            finally
            {
                File.WriteAllText(Path.Combine(folder, "Playback.csv"), csv.ToString()); Time.captureFramerate = oldRate;
                target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(camera.gameObject); Object.Destroy(collision); Object.Destroy(floor); Object.Destroy(material); Object.Destroy(fill.gameObject);
            }
            yield return new ExitPlayMode();
        }
        [UnityTearDown] public IEnumerator Cleanup() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
