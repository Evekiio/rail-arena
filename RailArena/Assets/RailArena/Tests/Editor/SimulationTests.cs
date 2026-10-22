using NUnit.Framework;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class SimulationTests
    {
        [Test]
        public void SoloPracticeAllowsOneParticipantWithoutChangingTimedMatchMinimum()
        {
            var practice = new MatchState(new[] { 0 }, true);
            for (int i = 0; i < CombatRules.CountdownTicks + CombatRules.MatchTicks + 1; i++) practice.Advance();
            Assert.That(practice.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(practice.RecordKill(0, 0), Is.False);
            Assert.That(practice.RecordVoidDeath(0), Is.True);
            Assert.That(practice.Scores[0], Is.EqualTo(-1));
            Assert.Throws<System.ArgumentException>(() => new MatchState(new[] { 0 }, false));
            Assert.Throws<System.ArgumentException>(() => new MatchState(new int[0], true));
        }

        [Test]
        public void CountdownBlocksScoringAndTiedLeadersPlayNextKillSuddenDeath()
        {
            var match = new MatchState(new[] { 0, 1, 2 }, false);
            Assert.That(match.RecordKill(0, 1), Is.False);
            for (int i = 0; i < CombatRules.CountdownTicks; i++) match.Advance();
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            match.RecordKill(0, 2);
            match.RecordKill(1, 2);
            for (int i = 0; i < CombatRules.MatchTicks; i++) match.Advance();
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
            match.RecordKill(2, 0);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.SuddenDeath));
            match.RecordKill(1, 2);
            Assert.That(match.Winner, Is.EqualTo(1));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(match.RecordKill(0, 1), Is.False);
        }

        [Test]
        public void PracticeIsUntimedAndVoidDeathsSubtractOne()
        {
            var match = new MatchState(new[] { 0, 1 }, true);
            for (int i = 0; i < CombatRules.CountdownTicks + CombatRules.MatchTicks + 1; i++) match.Advance();
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(match.RecordVoidDeath(0), Is.True);
            Assert.That(match.Scores[0], Is.EqualTo(-1));
        }

        [Test]
        public void LocationDamageCombinesAndNeverBecomesNegative()
        {
            Assert.That(CombatRules.ApplyDamage(100, HitRegion.Head), Is.Zero);
            Assert.That(CombatRules.ApplyDamage(CombatRules.ApplyDamage(100, HitRegion.Body), HitRegion.Body), Is.Zero);
            int health = CombatRules.ApplyDamage(100, HitRegion.Limb);
            health = CombatRules.ApplyDamage(health, HitRegion.Limb);
            Assert.That(health, Is.EqualTo(32));
            Assert.That(CombatRules.ApplyDamage(health, HitRegion.Limb), Is.Zero);
            Assert.That(CombatRules.ApplyDamage(CombatRules.ApplyDamage(100, HitRegion.Body), HitRegion.Limb), Is.EqualTo(16));
        }

        [Test]
        public void PickupsCapHealthAndCannotResurrect()
        {
            Assert.That(CombatRules.RestoreHealth(50), Is.EqualTo(75));
            Assert.That(CombatRules.RestoreHealth(90), Is.EqualTo(100));
            Assert.That(CombatRules.RestoreHealth(0), Is.Zero);
        }

        [Test]
        public void FiringIsAllowedOnlyAtCooldownBoundaryAndWhileAlive()
        {
            Assert.That(CombatRules.CanFire(89, 90, true), Is.False);
            Assert.That(CombatRules.CanFire(90, 90, true), Is.True);
            Assert.That(CombatRules.CanFire(91, 90, false), Is.False);
            Assert.That(CombatRules.ShotIntervalTicks / (float)CombatRules.TickRate, Is.EqualTo(1.5f));
        }

        [Test]
        public void JumpPreservesHorizontalMomentumWhileGroundFrictionSlows()
        {
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            try
            {
                var velocity = new Vector3(15, 0, 0);
                var jump = ArenaMovement.Integrate(velocity, new MovementCommand { Jump = true }, true, tuning, 1f / 60);
                var walk = ArenaMovement.Integrate(velocity, default, true, tuning, 1f / 60);
                Assert.That(jump.x, Is.EqualTo(15));
                Assert.That(jump.y, Is.GreaterThan(0));
                Assert.That(walk.x, Is.LessThan(15));
                var air = ArenaMovement.Integrate(velocity, default, false, tuning, 1f / 60);
                Assert.That(air.x, Is.EqualTo(15));
            }
            finally { Object.DestroyImmediate(tuning); }
        }

        [Test]
        public void StrafeAccelerationCanAddSpeedWithoutClampingMomentum()
        {
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            try
            {
                var result = ArenaMovement.Integrate(new Vector3(15, 0, 0),
                    new MovementCommand { Move = Vector2.up }, false, tuning, 1f / 60);
                Assert.That(result.x, Is.EqualTo(15));
                Assert.That(result.z, Is.GreaterThan(0));
                Assert.That(new Vector2(result.x, result.z).magnitude, Is.GreaterThan(15));
            }
            finally { Object.DestroyImmediate(tuning); }
        }
    }
}
