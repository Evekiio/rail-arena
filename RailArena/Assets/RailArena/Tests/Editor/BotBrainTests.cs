using System;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class BotBrainTests
    {
        private GameObject root;
        private ArenaSession session;
        private ArenaActor bot;
        private ArenaActor target;
        private static readonly Vector3 Origin = new(2000, 0, 2000);

        private void Setup(int difficulty)
        {
            root = new GameObject("Isolated bot decision fixture");
            var world = root.AddComponent<ArenaWorld>();
            Transform Point(string name, Vector3 position)
            {
                var point = new GameObject(name).transform;
                point.SetParent(root.transform); point.position = Origin + position;
                return point;
            }
            world.Nodes = new[] { Point("Start", Vector3.zero), Point("North", Vector3.forward * 10), Point("East", Vector3.right * 10) };
            world.Links = new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, 2), new Vector2Int(2, 0) };
            world.Spawns = new[] { Point("Target spawn", Vector3.forward * 8), Point("Bot spawn", Vector3.zero) };
            world.Pads = Array.Empty<JumpPad>();
            world.Pickups = Array.Empty<HealthPickup>();
            session = root.AddComponent<ArenaSession>();
            session.World = world;
            session.StartSession(false, 1, difficulty, TargetScenario.Combat);
            session.SetPaused(true);
            target = session.Actors[0]; bot = session.Actors[1];
            bot.Brain = new BotBrain(7191);
            while (!session.Match.IsActive) session.Match.Advance();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Cleanup()
        {
            if (session != null)
                foreach (var actor in session.Actors) if (actor != null) Object.DestroyImmediate(actor.gameObject);
            if (root != null) Object.DestroyImmediate(root);
        }

        [TestCase(0, 48, 5f)]
        [TestCase(1, 27, 2.5f)]
        [TestCase(2, 14, 1f)]
        public void DifficultyControlsReactionAndBoundedImperfectAim(int difficulty, int reactionTicks, float maximumError)
        {
            Setup(difficulty);
            int seen = session.Match.Tick;
            int firstFire = -1;
            float observedError = 0;
            for (int i = 0; i < 180; i++)
            {
                var command = bot.Brain.Think(session, bot);
                if (command.Fire && firstFire < 0) firstFire = session.Match.Tick;
                Vector3 direction = (target.Eye - Vector3.up * .35f - bot.Eye).normalized;
                float wantedPitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
                if (i >= reactionTicks)
                {
                    float yawError = Mathf.Abs(Mathf.DeltaAngle(command.Yaw, 0));
                    float pitchError = Mathf.Abs(command.Pitch - wantedPitch);
                    Assert.That(yawError, Is.LessThanOrEqualTo(maximumError + .001f));
                    Assert.That(pitchError, Is.LessThanOrEqualTo(maximumError + .001f));
                    observedError = Mathf.Max(observedError, yawError, pitchError);
                }
                session.Match.Advance();
            }
            Assert.That(firstFire - seen, Is.EqualTo(reactionTicks));
            Assert.That(observedError, Is.GreaterThan(maximumError * .5f), "Seeded aim must include nonzero, difficulty-scaled error.");
            Assert.That(bot.Health, Is.EqualTo(100));
            Assert.That(target.Health, Is.EqualTo(100), "Thinking alone cannot apply damage.");
        }

        [TestCase(0, 48)]
        [TestCase(1, 27)]
        [TestCase(2, 14)]
        public void OcclusionStopsFireAndReappearanceRequiresFreshReaction(int difficulty, int reactionTicks)
        {
            Setup(difficulty);
            MovementCommand command = default;
            for (int i = 0; i <= reactionTicks; i++)
            { command = bot.Brain.Think(session, bot); session.Match.Advance(); }
            Assert.That(command.Fire, Is.True);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.position = Origin + new Vector3(0, 1.5f, 4);
            wall.transform.localScale = new Vector3(20, 5, 1);
            target.Place(Origin + new Vector3(5, 0, 8));
            Physics.SyncTransforms();
            for (int i = 0; i < 120; i++)
            {
                command = bot.Brain.Think(session, bot);
                Assert.That(command.Fire, Is.False, "World geometry must immediately break target visibility.");
                session.Match.Advance();
            }
            // While hidden, navigation may change aim; it must not follow the hidden target's pitch.
            Assert.That(command.Pitch, Is.EqualTo(0));
            Object.DestroyImmediate(wall);
            Physics.SyncTransforms();
            for (int i = 0; i < reactionTicks; i++)
            {
                command = bot.Brain.Think(session, bot);
                Assert.That(command.Fire, Is.False, "Reacquisition must not retain the old reaction timer.");
                session.Match.Advance();
            }
            // Extra turn time is allowed here because the reappearing target is off-axis.
            bool fired = false;
            for (int i = 0; i < 90; i++)
            { fired |= bot.Brain.Think(session, bot).Fire; session.Match.Advance(); }
            Assert.That(fired, Is.True);
        }

        [Test]
        public void LowHealthChoosesRouteTowardAvailablePickup()
        {
            Setup(1);
            var pickup = new GameObject("Available health").AddComponent<HealthPickup>();
            pickup.transform.SetParent(root.transform);
            pickup.transform.position = Origin + Vector3.right * 10;
            session.World.Pickups = new[] { pickup };
            bot.Health = 50;
            bot.Brain.Think(session, bot);
            session.Match.Advance();
            var command = bot.Brain.Think(session, bot);
            var worldMove = Quaternion.Euler(0, command.Yaw, 0) * new Vector3(command.Move.x, 0, command.Move.y);
            Assert.That(worldMove.x, Is.GreaterThan(.99f));
            Assert.That(Mathf.Abs(worldMove.z), Is.LessThan(.01f));
        }

        [Test]
        public void EditedReactionAffectsNewSessionAndLaterAssetEditsDoNotChangeItsSnapshot()
        {
            var source = Resources.Load<BotTuning>("BotTuning");
            var original = source.Veteran;
            try
            {
                var edited = original; edited.ReactionTicks = 60; source.Veteran = edited;
                Setup(1);
                source.Veteran = original;
                int acquired = session.Match.Tick;
                int firstFire = -1;
                for (int i = 0; i <= 60; i++)
                {
                    if (bot.Brain.Think(session, bot).Fire && firstFire < 0) firstFire = session.Match.Tick;
                    session.Match.Advance();
                }
                Assert.That(firstFire - acquired, Is.EqualTo(60));
                Assert.That(source.Veteran.ReactionTicks, Is.EqualTo(27));
            }
            finally { source.Veteran = original; }
        }

        [Test]
        public void InvalidBotTuningRejectsZeroIntervalsNonfiniteValuesAndUnknownDifficulty()
        {
            var tuning = ScriptableObject.CreateInstance<BotTuning>();
            try
            {
                tuning.AimRefreshTicks = 0;
                Assert.Throws<ArgumentException>(() => tuning.SettingsFor(1));
                tuning.AimRefreshTicks = 12;
                var profile = tuning.Ace; profile.AimErrorDegrees = float.NaN; tuning.Ace = profile;
                Assert.Throws<ArgumentException>(() => tuning.SettingsFor(1), "Even an unselected invalid profile must be rejected.");
                profile.AimErrorDegrees = 1; profile.TurnDegreesPerSecond = float.PositiveInfinity; tuning.Ace = profile;
                Assert.Throws<ArgumentException>(() => tuning.SettingsFor(2));
                profile.TurnDegreesPerSecond = 330; tuning.Ace = profile;
                Assert.Throws<ArgumentOutOfRangeException>(() => tuning.SettingsFor(3));
            }
            finally { Object.DestroyImmediate(tuning); }
        }
    }
}
