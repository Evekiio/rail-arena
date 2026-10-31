using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class FirstPersonReadyArtReview
    {
        [UnityTest]
        public IEnumerator AuthoredReadyPreservesContactsAdsAndSimulation()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardensBasement.unity");
            yield return new EnterPlayMode();
            yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            typeof(ArenaSession).GetProperty("SelectedOfflineCharacter").SetValue(session, 0);
            session.StartSession(true, 3, 1, TargetScenario.Stationary);
            while (!session.Match.IsActive) session.Match.Advance();
            session.enabled = false;
            var slice = new GameObject("Authored first-person ready review").AddComponent<ArtSlicePresentation>();
            yield return null;
            yield return null;
            var art = Resources.Load<FirstPersonEquipArt>("Art/FirstPersonEquip");
            Assert.That(session.EquipArt, Is.SameAs(art));
            Assert.That(art.Duration, Is.EqualTo(.3f).Within(.000001f));
            var arms = session.View.GetComponentInChildren<FirstPersonArms>(true);
            var weapon = arms.GetComponentInParent<RailgunPresentation>(true);
            Assert.That(weapon, Is.Not.Null);
            Assert.That(weapon.Aim, Is.Not.Null);
            weapon.gameObject.SetActive(true);
            yield return null;
            var armArt = arms.GetComponent<ContenderArtPresentation>();
            var characters = Resources.Load<ContenderArtSet>("Art/Contenders");
            foreach (var actor in session.Actors) { actor.Controller.enabled = false; actor.Presentation.enabled = false; }
            var actorRoot = session.Human.transform.position;
            var velocity = session.Human.Velocity;
            float height = session.Human.Controller.height;
            int tick = session.Match.Tick, rounds = session.Human.Magazine.Rounds, nextShot = session.Human.NextShotTick;
            var camera = session.View;
            camera.enabled = false;
            camera.transform.SetPositionAndRotation(session.Human.Eye, Quaternion.Euler(session.Human.Command.Pitch, session.Human.Command.Yaw, 0));
            float previousFov = camera.fieldOfView;
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            string folder = Path.GetFullPath("../Validation/FirstPersonReadyEyePlayback");
            Directory.CreateDirectory(folder);
            var csv = new StringBuilder("frame,character,authored,mode,sourceTime,leftWristError,rightWristError,offsetMetres,rotationDegrees,sightError,simulationTick,rounds\n");
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 60;
            try
            {
                foreach (bool authored in new[] { false, true })
                {
                    string path = Path.Combine(folder, authored ? "after" : "before");
                    Directory.CreateDirectory(path);
                    int output = 0;
                    for (int character = 0; character < characters.variants.Length; character++)
                    {
                        armArt.ApplyArms(arms, characters.variants[character]);
                        for (int mode = 0; mode < 3; mode++)
                        {
                            var motion = new FirstPersonWeaponMotion { EquipArt = authored ? art : null };
                            motion.Reset(true);
                            var sight = new AimDownSights();
                            if (mode == 1) sight.Step(true, 1);
                            for (int frame = 0; frame < 30; frame++, output++)
                            {
                                motion.Step(1f / 60, Vector3.zero, true, Vector2.zero, 0);
                                if (mode == 2) sight.Step(true, 1f / 60);
                                camera.fieldOfView = Mathf.Lerp(session.Aim.FieldOfView, session.Aim.ViewFov(true), sight.Weight);
                                Vector3 sightLocal = weapon.transform.InverseTransformPoint(weapon.Aim.position);
                                weapon.transform.localPosition = sight.Position(sightLocal, motion.Offset, 0);
                                weapon.transform.localRotation = sight.Rotation(motion.Rotation);
                                yield return null;
                                float left = Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, weapon.LeftGrip.position);
                                float right = Vector3.Distance(arms.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, weapon.RightGrip.position);
                                Assert.That(Mathf.Max(left, right), Is.LessThan(.025f), $"character {character} mode {mode} frame {frame}");
                                var axis = camera.transform.InverseTransformPoint(weapon.Aim.position);
                                float sightError = new Vector2(axis.x, axis.y).magnitude;
                                if (sight.Weight >= .99999f)
                                {
                                    Assert.That(sightError, Is.LessThan(.0001f));
                                    Assert.That(axis.z, Is.EqualTo(AimDownSights.EyeRelief).Within(.0001f));
                                }
                                Assert.That(session.Human.transform.position, Is.EqualTo(actorRoot));
                                Assert.That(session.Human.Velocity, Is.EqualTo(velocity));
                                Assert.That(session.Human.Controller.height, Is.EqualTo(height));
                                Assert.That(session.Match.Tick, Is.EqualTo(tick));
                                Assert.That(session.Human.Magazine.Rounds, Is.EqualTo(rounds));
                                Assert.That(session.Human.NextShotTick, Is.EqualTo(nextShot));
                                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                                var previous = RenderTexture.active;
                                try
                                {
                                    RenderTexture.active = target;
                                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                                    pixels.Apply();
                                    File.WriteAllBytes(Path.Combine(path, $"frame-{output:D4}.png"), pixels.EncodeToPNG());
                                }
                                finally { RenderTexture.active = previous; }
                                csv.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9:F6},{10},{11}\n", output, character, authored, mode, (frame + 1) / 60f, left, right, motion.Offset.magnitude, Quaternion.Angle(Quaternion.identity, motion.Rotation), sightError, tick, rounds);
                            }
                            motion.Reset(false);
                            motion.Step(1f / 60, Vector3.zero, true, Vector2.zero, 0);
                            Assert.That(motion.Offset.magnitude, Is.LessThan(.000001f), "Disabled/dead/pause reset must clear the ready action");
                            Assert.That(Quaternion.Angle(motion.Rotation, Quaternion.identity), Is.LessThan(.001f));
                        }
                    }
                }
                File.WriteAllText(Path.Combine(folder, "Playback.csv"), csv.ToString());
                Object.Destroy(slice);
                yield return null;
                Assert.That(session.EquipArt, Is.Null, "Removing the art profile restores the prior equip track");
            }
            finally
            {
                Time.captureFramerate = previousRate;
                camera.fieldOfView = previousFov;
                target.Release();
                Object.Destroy(target);
                Object.Destroy(pixels);
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
