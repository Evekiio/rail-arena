#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    /// <summary>Opt-in accelerated standalone scenarios, using real controllers, hurtboxes and authority.</summary>
    public sealed class MechanicsValidation : MonoBehaviour
    {
        [Serializable]
        private sealed class Check
        {
            public string name;
            public int tick;
            public bool passed;
            public string observed;
        }

        [Serializable]
        private sealed class Report
        {
            public string fingerprint;
            public string clock = "Accelerated integer ticks through normal authority; not real-time or native input acceptance";
            public float elapsedSeconds;
            public int errors;
            public string failure;
            public Check[] checks;
            public bool passed;
        }

        private ArenaSession session;
        private readonly List<Check> checks = new();
        private int errors;
        private ArenaActor Shooter => session.Actors[0];
        private ArenaActor Target => session.Actors[1];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--validate-mechanics") >= 0)
                new GameObject("Standalone mechanics validation").AddComponent<MechanicsValidation>();
        }

        private void OnEnable() => Application.logMessageReceived += Log;
        private void OnDisable() => Application.logMessageReceived -= Log;
        private void Log(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }

        private IEnumerator Start()
        {
            yield return null;
            var started = Time.realtimeSinceStartup;
            session = FindFirstObjectByType<ArenaSession>();
            string failure = null;
            var run = Run();
            while (true)
            {
                object current = null;
                bool more;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception e) { failure = e.ToString(); break; }
                if (!more) break;
                yield return current;
            }
            (run as IDisposable)?.Dispose();
            var report = new Report
            {
                fingerprint = Networking.BuildIdentity.Fingerprint,
                elapsedSeconds = Time.realtimeSinceStartup - started,
                errors = errors, failure = failure, checks = checks.ToArray(),
                passed = failure == null && errors == 0 && checks.Count >= 20
            };
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--validation-output");
            string output = index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) :
                Path.Combine(Application.persistentDataPath, "mechanics-validation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }

        private void Setup(bool practice)
        {
            session.AutomatedHuman = false;
            session.StartSession(practice, 1, 1, TargetScenario.Stationary);
            session.SetPaused(true);
            foreach (var actor in session.Actors) actor.Bot = false;
            PlaceForShots();
        }

        private void PlaceForShots()
        {
            // Sky Gardens has a wall across the original Foundry firing lane.
            bool skyGardens = session.gameObject.scene.name.StartsWith("SkyGardens", StringComparison.Ordinal);
            float shooterZ = skyGardens ? -12f : -22f;
            Shooter.Place(new Vector3(0, .08f, shooterZ));
            Target.Place(new Vector3(0, .08f, shooterZ + 6f));
            Shooter.Command = Target.Command = default;
            Physics.SyncTransforms();
        }

        private void Advance(int ticks)
        {
            for (int i = 0; i < ticks; i++) session.AdvanceValidationTick();
        }

        private void Expect(string name, bool passed, string observed)
        {
            checks.Add(new Check { name = name, tick = session.Match.Tick, passed = passed, observed = observed });
            if (!passed) throw new InvalidOperationException(name + ": " + observed);
        }

        private void Aim(HitRegion region)
        {
            Physics.SyncTransforms();
            foreach (var hitbox in Target.GetComponentsInChildren<Hitbox>())
            {
                if (hitbox.Region != region) continue;
                var collider = hitbox.GetComponent<Collider>();
                Vector3 point = collider.bounds.center;
                var direction = (point - Shooter.Eye).normalized;
                // Confirm that the requested region is the first live shape, including world occlusion.
                if (!Physics.Raycast(Shooter.Eye, direction, out var hit, 180, (1 << 8) | 1, QueryTriggerInteraction.Collide) ||
                    hit.collider.GetComponent<Hitbox>() != hitbox) continue;
                Shooter.Command = new MovementCommand
                {
                    Yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg,
                    Pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg,
                    Fire = true
                };
                return;
            }
            throw new InvalidOperationException("No unobstructed actual hurtbox found for " + region);
        }

        private void Fire(HitRegion region)
        {
            int shots = session.ShotsFired;
            Aim(region);
            Advance(1);
            Shooter.Command = default;
            if (session.ShotsFired != shots + 1) throw new InvalidOperationException("Arranged shot was not fired.");
        }

        private static bool PickupVisible(HealthPickup pickup)
        {
            if (pickup.Presentation != null) return pickup.Presentation.Available;
            if (pickup.Visuals.Length == 0) throw new InvalidOperationException("Pickup has no visual representation to validate.");
            return pickup.Visuals[0].enabled;
        }
        private IEnumerator Run()
        {
            Setup(false);
            yield return null; yield return null;
            Aim(HitRegion.Head);
            Advance(179);
            Expect("Countdown blocks fire and damage through tick 179", session.ShotsFired == 0 && Target.Health == 100 &&
                session.Match.Phase == MatchPhase.Countdown, $"shots={session.ShotsFired}, health={Target.Health}, phase={session.Match.Phase}");
            Advance(1);
            Shooter.Command = default;
            int life = Target.Life;
            Expect("Tick 180 activates combat and lethal headshot scores once", Target.Health == 0 && session.ShotsFired == 1 &&
                session.Match.Scores[0] == 1 && session.Match.Phase == MatchPhase.Playing,
                $"health={Target.Health}, shots={session.ShotsFired}, score={session.Match.Scores[0]}");
            Expect("Authority confirms lethal hit with elimination feedback", session.Feedback.ConfirmedHits == 1 &&
                session.Feedback.LastHitWasKill, $"hits={session.Feedback.ConfirmedHits}, lethal={session.Feedback.LastHitWasKill}");
            Target.Command = new MovementCommand { Fire = true };
            Advance(119);
            Expect("Dead actor cannot fire or respawn before 120 ticks", !Target.Alive && Target.Life == life && session.ShotsFired == 1,
                $"alive={Target.Alive}, life={Target.Life}, shots={session.ShotsFired}");
            Advance(1);
            Expect("Respawn at exact 120-tick boundary restores 100 health and new life", Target.Health == 100 && Target.Life == life + 1 &&
                Target.Controller.enabled && Target.NextShotTick == session.Match.Tick,
                $"health={Target.Health}, life={Target.Life}, nextShot={Target.NextShotTick}");

            PlaceForShots();
            Fire(HitRegion.Body);
            int first = session.Match.Tick;
            int firstInterval = Shooter.Magazine.LastShotInterval;
            int count = session.ShotsFired;
            Expect("First body shot deals 50", Target.Health == 50, "health=" + Target.Health);
            Expect("Nonlethal damage confirms a hit without elimination", session.Feedback.ConfirmedHits == 2 &&
                !session.Feedback.LastHitWasKill, $"hits={session.Feedback.ConfirmedHits}, lethal={session.Feedback.LastHitWasKill}");
            Aim(HitRegion.Body);
            Advance(firstInterval - 1);
            Expect("Shot intent cannot discharge before the scheduled boundary", session.ShotsFired == count && Target.Health == 50,
                $"shots={session.ShotsFired}, health={Target.Health}");
            Advance(1); Shooter.Command = default;
            Expect("Second body shot fires on its scheduled boundary and kills", Target.Health == 0 && session.ShotsFired == count + 1 &&
                session.Match.Tick - first == firstInterval, $"health={Target.Health}, shots={session.ShotsFired}, interval={session.Match.Tick - first}");
            int beforeBurst = session.ShotsFired;
            Shooter.Command = new MovementCommand { Fire = true, Yaw = 90 };
            Advance(60);
            Expect("Held automatic fire produces four shots across sixty ticks from this cadence phase", session.ShotsFired == beforeBurst + 4,
                $"shots={session.ShotsFired - beforeBurst}");
            Shooter.Command = default; beforeBurst = session.ShotsFired; Advance(12);
            Expect("Released trigger stops automatic fire", session.ShotsFired == beforeBurst, "shots=" + session.ShotsFired);
            Advance(120); PlaceForShots();
            Fire(HitRegion.Limb);
            Expect("First limb shot deals 34", Target.Health == 66, "health=" + Target.Health);
            Advance(CombatRules.ShotIntervalTicks - 1); Fire(HitRegion.Limb);
            Expect("Second limb shot leaves 32 health", Target.Health == 32, "health=" + Target.Health);
            Advance(CombatRules.ShotIntervalTicks - 1); Fire(HitRegion.Limb);
            Expect("Third limb shot kills without negative health", Target.Health == 0, "health=" + Target.Health);
            Advance(120); PlaceForShots();
            Fire(HitRegion.Body);
            Advance(CombatRules.ShotIntervalTicks - 1); Fire(HitRegion.Limb);
            Expect("Mixed body and limb damage leaves 16 health", Target.Health == 16, "health=" + Target.Health);
            Advance(CombatRules.ShotIntervalTicks - 1); Fire(HitRegion.Limb);
            Expect("Mixed hit sequence kills and records one kill", Target.Health == 0 && session.Match.Scores[0] == 4,
                $"health={Target.Health}, score={session.Match.Scores[0]}");
            Advance(120);

            var pickup = session.World.Pickups[0];
            var location = pickup.transform.position - Vector3.up * .92f;
            Shooter.Place(location); Target.Place(location);
            Shooter.Health = Target.Health = 100;
            Advance(1);
            Expect("Full-health actors leave pickup available", pickup.AvailableTick == 0 && PickupVisible(pickup),
                "availableTick=" + pickup.AvailableTick);
            Shooter.Health = Target.Health = 50;
            Advance(1);
            int collected = session.Match.Tick;
            Expect("Contested pickup heals exactly one actor by 25", Shooter.Health == 75 && Target.Health == 50 &&
                pickup.AvailableTick == collected + 1200 && !PickupVisible(pickup),
                $"healths={Shooter.Health}/{Target.Health}, availableTick={pickup.AvailableTick}");
            Advance(1199);
            Expect("Pickup remains absent for next 1199 ticks", Shooter.Health == 75 && Target.Health == 50 && !PickupVisible(pickup),
                $"healths={Shooter.Health}/{Target.Health}, visible={PickupVisible(pickup)}");
            Advance(1);
            Expect("Pickup returns and can be recollected at exact 1200 ticks", Shooter.Health == 100 && Target.Health == 50 &&
                pickup.AvailableTick == collected + 2400, $"healths={Shooter.Health}/{Target.Health}, availableTick={pickup.AvailableTick}");
            pickup.AvailableTick = 0; Shooter.Health = 90;
            Advance(1);
            Expect("Pickup caps 90 health at 100", Shooter.Health == 100, "health=" + Shooter.Health);
            PlaceForShots(); Shooter.Health = 40;
            Advance(600);
            Expect("No passive health regeneration over 600 ticks", Shooter.Health == 40, "health=" + Shooter.Health);

            int oldScore = session.Match.Scores[1]; life = Target.Life;
            Target.Place(new Vector3(0, -21, 0));
            Advance(1);
            Expect("Void death subtracts one point and disables actor", Target.Health == 0 && !Target.Controller.enabled &&
                session.Match.Scores[1] == oldScore - 1 && session.VoidDeaths == 1,
                $"health={Target.Health}, score={session.Match.Scores[1]}, voids={session.VoidDeaths}");
            Advance(119);
            Expect("Void victim remains dead until respawn boundary", Target.Health == 0 && Target.Life == life, "life=" + Target.Life);
            Advance(1);
            Expect("Void victim respawns at 120 ticks", Target.Health == 100 && Target.Life == life + 1, "life=" + Target.Life);

            // Run the complete integer clock, without staging a private MatchState or shortening its limit.
            Setup(false);
            yield return null; yield return null;
            Advance(36179);
            Expect("Timed match remains playing one tick before ten-minute boundary", session.Match.Phase == MatchPhase.Playing &&
                session.Match.RemainingTicks == 1, $"phase={session.Match.Phase}, remaining={session.Match.RemainingTicks}");
            Advance(1);
            Expect("Tied leaders enter sudden death at tick 36180", session.Match.Phase == MatchPhase.SuddenDeath && session.Match.Tick == 36180,
                "phase=" + session.Match.Phase);
            Fire(HitRegion.Head);
            Expect("Next tied-leader kill resolves sudden death", session.Match.Phase == MatchPhase.Results && session.Match.Winner == 0 &&
                session.Match.Scores[0] == 1, $"phase={session.Match.Phase}, winner={session.Match.Winner}, score={session.Match.Scores[0]}");
            int end = session.Match.Tick;
            Advance(120);
            Expect("Results stop authority clock and respawn", session.Match.Tick == end && !Target.Alive,
                $"tick={session.Match.Tick}, targetAlive={Target.Alive}");

            Setup(false);
            yield return null; yield return null;
            Advance(180); Fire(HitRegion.Head);
            Advance(36180 - session.Match.Tick);
            Expect("Unique leader wins at normal time limit", session.Match.Phase == MatchPhase.Results && session.Match.Winner == 0 &&
                session.Match.Tick == 36180, $"phase={session.Match.Phase}, winner={session.Match.Winner}");

            Setup(true);
            yield return null; yield return null;
            Advance(36780);
            Expect("Practice stays active beyond timed-match limit", session.Match.Phase == MatchPhase.Playing && session.Practice,
                $"phase={session.Match.Phase}, practice={session.Practice}");
            Fire(HitRegion.Head); Advance(120);
            Expect("Practice still respawns at 120 ticks after time limit", Target.Health == 100 && Target.Life == 2,
                $"health={Target.Health}, life={Target.Life}");

            session.StartSession(true, 0, 1, TargetScenario.Stationary);
            session.SetPaused(true);
            yield return null; yield return null;
            Expect("Solo practice creates only the human with no network", session.Actors.Count == 1 && session.BotCount == 0 &&
                session.Practice && !session.IsOnline, $"actors={session.Actors.Count}, bots={session.BotCount}, online={session.IsOnline}");
            Advance(180);
            Vector3 before = Shooter.transform.position;
            Shooter.Command = new MovementCommand { Move = Vector2.up, Yaw = 90, Fire = true };
            Advance(30); Shooter.Command = default;
            Expect("Solo authority moves and fires three rounds in half a second", Vector3.Distance(before, Shooter.transform.position) > 1 &&
                session.ShotsFired == 3 && Shooter.Health == 100,
                $"travel={Vector3.Distance(before, Shooter.transform.position)}, shots={session.ShotsFired}, health={Shooter.Health}");
            Shooter.Place(new Vector3(0, .08f, session.gameObject.scene.name.StartsWith("SkyGardens", StringComparison.Ordinal) ? -12f : -22f));
            Advance(36180);
            Expect("Solo practice stays active beyond match limit", session.Match.Phase == MatchPhase.Playing && session.Actors.Count == 1,
                $"phase={session.Match.Phase}, actors={session.Actors.Count}");
            Shooter.Place(new Vector3(0, -21, 0));
            Advance(1);
            Expect("Solo void death still scores minus one", !Shooter.Alive && session.Match.Scores[0] == -1,
                $"health={Shooter.Health}, score={session.Match.Scores[0]}");
            Advance(119);
            bool stillDead = !Shooter.Alive;
            Advance(1);
            Expect("Solo void respawn preserves exact 120-tick boundary", stillDead && Shooter.Health == 100 && Shooter.Life == 2,
                $"deadBeforeBoundary={stillDead}, health={Shooter.Health}, life={Shooter.Life}");
            session.StartSession(true, 1, 2, TargetScenario.Moving);
            session.SetPaused(true);
            yield return null; yield return null;
            Expect("Solo can restart into configured bot practice", session.Actors.Count == 2 && session.BotCount == 1 &&
                session.Difficulty == 2 && session.Scenario == TargetScenario.Moving,
                $"actors={session.Actors.Count}, difficulty={session.Difficulty}, scenario={session.Scenario}");
            session.StartSession(false, 0, 1, TargetScenario.Combat);
            session.SetPaused(true);
            yield return null; yield return null;
            Expect("Timed bot match retains at least one bot", session.Actors.Count == 2 && session.BotCount == 1 && !session.Practice,
                $"actors={session.Actors.Count}, bots={session.BotCount}, practice={session.Practice}");
        }
    }
}
#endif




