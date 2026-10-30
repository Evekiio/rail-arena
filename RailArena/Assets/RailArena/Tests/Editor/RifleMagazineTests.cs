using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;

namespace RailArena.Tests
{
    public sealed class RifleMagazineTests
    {
        [Test]
        public void SustainedCadenceIsExactly250RpmWithoutFractionalDrift()
        {
            var magazine = RifleMagazine.Full; int next = 0, shots = 0;
            for (int tick = 0; tick < 360; tick++)
            {
                magazine.Advance(tick, false);
                if (magazine.TryFire(tick, ref next)) shots++;
            }
            Assert.That(shots, Is.EqualTo(25));
            Assert.That(next, Is.EqualTo(360));
            Assert.That(magazine.Rounds, Is.EqualTo(5));
            Assert.That(magazine.CadenceRemainder, Is.Zero);
        }

        [Test]
        public void EmptyMagazineCannotFireAndOnlyReloadCompletionRefillsIt()
        {
            var magazine = RifleMagazine.Full; int next = 0;
            for (int shot = 0; shot < 30; shot++) Assert.That(magazine.TryFire(next, ref next), Is.True);
            Assert.That(magazine.Rounds, Is.Zero);
            Assert.That(magazine.TryFire(next + 100, ref next), Is.False);
            magazine.Advance(600, true);
            Assert.That(magazine.ReloadEndTick, Is.EqualTo(720));
            magazine.Advance(719, false);
            Assert.That(magazine.Rounds, Is.Zero);
            Assert.That(magazine.TryFire(719, ref next), Is.False);
            magazine.Advance(720, false);
            Assert.That(magazine.Reloading, Is.False);
            Assert.That(magazine.Rounds, Is.EqualTo(30));
            Assert.That(magazine.TryFire(720, ref next), Is.True);
        }

        [Test]
        public void FullReloadIsIgnoredAndRepeatedRequestsCannotRestartAReload()
        {
            var magazine = RifleMagazine.Full; int next = 0;
            magazine.Advance(1, true); Assert.That(magazine.Reloading, Is.False);
            magazine.TryFire(2, ref next); magazine.Advance(3, true);
            magazine.Advance(60, true);
            Assert.That(magazine.Rounds, Is.EqualTo(29));
            Assert.That(magazine.ReloadEndTick, Is.EqualTo(123));
            magazine.Advance(123, true);
            Assert.That(magazine.Rounds, Is.EqualTo(30));
            Assert.That(magazine.Reloading, Is.False);
        }

        [Test]
        public void ReloadInputSurvivesOneBurstAndIsNeverRepeatedDuringPacketGaps()
        {
            var queue = new DuelInputQueue();
            queue.Add(new[] { new DuelInput { Command = new MovementCommand { Sequence = 1, Reload = true } },
                new DuelInput { Command = new MovementCommand { Sequence = 2 } } });
            queue.Consume(10, out var tap); Assert.That(tap.Command.Reload, Is.True);
            queue.Consume(11, out var gap); Assert.That(gap.Command.Reload, Is.False);
            queue.Add(new[] { new DuelInput { Command = new MovementCommand { Sequence = 1, Reload = true } } });
            queue.Consume(12, out var duplicate); Assert.That(duplicate.Command.Reload, Is.False);
        }

        [Test]
        public void WirePreservesAmmoReloadDeadlineAndCadencePhaseAndRejectsInvalidState()
        {
            var state = new DuelSnapshot();
            state.Actors[0] = new ActorSnapshot { Health = 100, Life = 1,
                Magazine = new RifleMagazine { Rounds = 7, ReloadEndTick = 120, CadenceRemainder = 4, LastShotInterval = 14 } };
            state.Actors[1] = new ActorSnapshot { Health = 100, Life = 1, Magazine = RifleMagazine.Full };
            var packet = new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = state };
            var bytes = DuelProtocol.Encode(packet);
            Assert.That(DuelProtocol.TryDecode(bytes, out var decoded), Is.True);
            Assert.That(decoded.Snapshot.Actors[0].Magazine, Is.EqualTo(state.Actors[0].Magazine));
            state.Actors[0].Magazine.Rounds = 31;
            Assert.Throws<System.ArgumentException>(() => DuelProtocol.Encode(packet));
            var input = new DuelPacket { Kind = DuelMessage.Input, Inputs = new[] { new DuelInput { Command = new MovementCommand { Sequence = 1, Reload = true } } } };
            Assert.That(DuelProtocol.TryDecode(DuelProtocol.Encode(input), out decoded), Is.True);
            Assert.That(decoded.Inputs[0].Command.Reload, Is.True);
            bytes[4] = 7; Assert.That(DuelProtocol.TryDecode(bytes, out _), Is.False);
        }
    }
}
