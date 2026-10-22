using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class NetworkingTests
    {
        private static DuelInput Input(int sequence) => new() { ViewedTick = 100, Command = new MovementCommand
        { Sequence = sequence, Move = new Vector2(.5f, -.5f), Yaw = 120, Pitch = -30, Jump = true, Fire = true } };

        [Test]
        public void OutboundControlsNormalizeFloatingPointOvershootAndWrappedAimWithoutRelaxingDecode()
        {
            var command = new MovementCommand { Move = new Vector2(1.00000012f, 0), Yaw = 721, Pitch = 359, Sequence = 1 };
            var normalized = DuelProtocol.CommandForWire(command);
            Assert.That(normalized.Move.x, Is.EqualTo(1));
            Assert.That(normalized.Yaw, Is.EqualTo(1));
            Assert.That(normalized.Pitch, Is.EqualTo(-1));
            var packet = new DuelPacket { Kind = DuelMessage.Input, Inputs = new[] { new DuelInput { Command = command } } };
            Assert.Throws<ArgumentException>(() => DuelProtocol.Encode(packet));
            packet.Inputs[0].Command = normalized;
            Assert.DoesNotThrow(() => DuelProtocol.Encode(packet));
        }

        [Test]
        public void ReplicaMatchPreservesAuthorityTimeScoresAndWinner()
        {
            var playing = MatchState.FromAuthority(1000, MatchPhase.Playing, -1, 3, -2);
            Assert.That(playing.Tick, Is.EqualTo(1000));
            Assert.That(playing.Scores[1], Is.EqualTo(-2));
            var result = MatchState.FromAuthority(36120, MatchPhase.Results, 1, 3, 4);
            Assert.That(result.Winner, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => MatchState.FromAuthority(10, MatchPhase.Playing, 1, 0, 0));
            Assert.Throws<ArgumentException>(() => MatchState.FromAuthority(10, MatchPhase.Results, -1, 0, 0));
        }

        [Test]
        public void ProtocolRoundTripsCommandsAndRejectsEveryTruncationAndTrailingBytes()
        {
            var source = new DuelPacket { Kind = DuelMessage.Input, Session = 9851, Sequence = 4, Round = 2,
                Inputs = new[] { Input(1), Input(2), Input(3) } };
            byte[] data = DuelProtocol.Encode(source);
            Assert.That(DuelProtocol.TryDecode(data, out var decoded), Is.True);
            Assert.That(decoded.Inputs[2].Command.Sequence, Is.EqualTo(3));
            Assert.That(decoded.Inputs[0].Command.Fire, Is.True);
            Assert.That(decoded.Inputs[0].Command.Move, Is.EqualTo(source.Inputs[0].Command.Move));
            for (int i = 0; i < data.Length; i++)
                Assert.That(DuelProtocol.TryDecode(data.Take(i).ToArray(), out _), Is.False, "prefix " + i);
            Assert.That(DuelProtocol.TryDecode(data.Concat(new byte[1]).ToArray(), out _), Is.False);
            data[4]++;
            Assert.That(DuelProtocol.TryDecode(data, out _), Is.False, "protocol mismatch");
        }

        [Test]
        public void ProtocolRejectsNonfiniteUnboundedAndUnorderedInputs()
        {
            var packet = new DuelPacket { Kind = DuelMessage.Input, Inputs = new[] { Input(1) } };
            packet.Inputs[0].Command.Move.x = float.NaN;
            Assert.Throws<ArgumentException>(() => DuelProtocol.Encode(packet));
            packet.Inputs[0] = Input(1);
            packet.Inputs[0].Command.Pitch = 90;
            Assert.Throws<ArgumentException>(() => DuelProtocol.Encode(packet));
            packet.Inputs = new[] { Input(2), Input(1) };
            Assert.Throws<ArgumentException>(() => DuelProtocol.Encode(packet));
            packet.Inputs = new DuelInput[7];
            Assert.Throws<ArgumentException>(() => DuelProtocol.Encode(packet));
            Assert.That(DuelProtocol.TryDecode(new byte[1201], out _), Is.False);
        }

        [Test]
        public void ProtocolRoundTripsAuthoritativeSnapshotAndSurvivesGarbage()
        {
            var state = Snapshot(1, 120);
            state.Actors[1] = new ActorSnapshot { Position = new Vector3(2, 3, 4), Velocity = new Vector3(1, 2, 3),
                Health = 34, Score = -1, LastInput = 58, NextShotTick = 200 };
            byte[] data = DuelProtocol.Encode(new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = state });
            Assert.That(data.Length, Is.LessThan(1200));
            Assert.That(DuelProtocol.TryDecode(data, out var packet), Is.True);
            Assert.That(packet.Snapshot.Actors[1].Position, Is.EqualTo(state.Actors[1].Position));
            Assert.That(packet.Snapshot.Actors[1].Score, Is.EqualTo(-1));
            var random = new System.Random(791);
            for (int i = 0; i < 2000; i++)
            {
                var garbage = new byte[random.Next(0, 1250)];
                random.NextBytes(garbage);
                Assert.DoesNotThrow(() => DuelProtocol.TryDecode(garbage, out _));
            }
        }

        [Test]
        public void RealUdpSocketsRejectOversizedTrafficAndCanRebindAfterDisposal()
        {
            int port;
            using (var receiver = new DuelSocket(0))
            using (var sender = new UdpClient())
            {
                port = receiver.LocalEndPoint.Port;
                var endpoint = new IPEndPoint(IPAddress.Loopback, port);
                byte[] oversized = new byte[1400];
                sender.Send(oversized, oversized.Length, endpoint);
                var clock = Stopwatch.StartNew();
                bool consumed = false;
                while (!consumed && clock.Elapsed.TotalSeconds < 2)
                {
                    consumed = receiver.Receive(out var packet, out _);
                    Assert.That(packet, Is.Null);
                    if (!consumed) Thread.Sleep(1);
                }
                Assert.That(consumed, Is.True);
            }
            using var rebound = new DuelSocket(port);
            Assert.That(rebound.LocalEndPoint.Port, Is.EqualTo(port));
        }

        [Test]
        public void RealUdpPeersHandshakeReadyExchangeInputsSnapshotsRematchAndDisconnect()
        {
            using var host = DuelLink.Host(0, 123);
            using var client = DuelLink.Join(new IPEndPoint(IPAddress.Loopback, host.LocalEndPoint.Port), 123);
            double now = 0;
            Pump(host, client, ref now, () => host.State == DuelLinkState.Connected && client.State == DuelLinkState.Connected);
            host.SetReady(true); client.SetReady(true);
            Pump(host, client, ref now, () => host.RemoteReady && client.RemoteReady);
            DuelInput[] received = null;
            host.InputsReceived += commands => received = commands;
            client.SendInputs(new[] { Input(1), Input(2) });
            Pump(host, client, ref now, () => received != null);
            Assert.That(received.Length, Is.EqualTo(2));
            host.SetRound(1);
            host.SendSnapshot(Snapshot(1, 40));
            Pump(host, client, ref now, () => client.LatestSnapshot != null);
            Assert.That(client.Round, Is.EqualTo(1));
            Assert.That(client.LocalReady || host.RemoteReady, Is.False);
            host.SetReady(true); client.SetReady(true);
            Pump(host, client, ref now, () => host.RemoteReady && client.RemoteReady);
            client.Dispose();
            Pump(host, null, ref now, () => host.State == DuelLinkState.Closed);
            Assert.That(host.Error, Does.Contain("disconnected"));
        }

        [Test]
        public void IncompatibleBuildIsRejectedAndAbsentHostTimesOut()
        {
            using var host = DuelLink.Host(0, 123);
            using var client = DuelLink.Join(new IPEndPoint(IPAddress.Loopback, host.LocalEndPoint.Port), 456);
            double now = 0;
            Pump(host, client, ref now, () => client.State == DuelLinkState.Failed);
            Assert.That(client.Error, Does.Contain("Incompatible"));
            Assert.That(host.State, Is.EqualTo(DuelLinkState.Listening));
            using var idle = new DuelSocket(0);
            using var missing = DuelLink.Join(new IPEndPoint(IPAddress.Loopback, idle.LocalEndPoint.Port), 123);
            missing.Tick(0);
            missing.Tick(8.01);
            Assert.That(missing.State, Is.EqualTo(DuelLinkState.Failed));
            Assert.That(missing.Error, Does.Contain("timed out"));
        }

        [Test]
        public void ConnectedPeerTimeoutDoesNotRequireAWorkingLeavePacket()
        {
            using var host = DuelLink.Host(0, 123);
            using var client = DuelLink.Join(new IPEndPoint(IPAddress.Loopback, host.LocalEndPoint.Port), 123);
            double now = 0;
            Pump(host, client, ref now, () => host.State == DuelLinkState.Connected && client.State == DuelLinkState.Connected);
            host.Tick(now + 9);
            // Drain a possible final handshake heartbeat before measuring eight seconds of silence.
            host.Tick(now + 18);
            Assert.That(host.State, Is.EqualTo(DuelLinkState.Failed));
            Assert.That(host.Error, Does.Contain("stopped responding"));
        }

        [Test]
        public void ForgedHandshakeWrongSessionAndReorderedReadyCannotChangeHostState()
        {
            using var host = DuelLink.Host(0, 123);
            using var client = new DuelSocket(0);
            var endpoint = new IPEndPoint(IPAddress.Loopback, host.LocalEndPoint.Port);
            double now = 0;
            client.Send(new DuelPacket { Kind = DuelMessage.Connect, Build = 123, Nonce = 77, Session = 999 }, endpoint);
            for (int i = 0; i < 5; i++) { host.Tick(now); now += .001; Thread.Sleep(1); }
            Assert.That(host.State, Is.EqualTo(DuelLinkState.Listening));
            client.Send(new DuelPacket { Kind = DuelMessage.Hello, Build = 123, Nonce = 77 }, endpoint);
            DuelPacket challenge = null;
            var clock = Stopwatch.StartNew();
            while (challenge == null && clock.Elapsed.TotalSeconds < 2)
            {
                host.Tick(now); now += .001;
                client.Receive(out challenge, out _);
                Thread.Sleep(1);
            }
            Assert.That(challenge?.Kind, Is.EqualTo(DuelMessage.Challenge));
            client.Send(new DuelPacket { Kind = DuelMessage.Connect, Build = 123, Nonce = 77, Session = challenge.Session }, endpoint);
            Pump(host, null, ref now, () => host.State == DuelLinkState.Connected);
            client.Send(new DuelPacket { Kind = DuelMessage.Ready, Session = challenge.Session, Sequence = 9, Ready = true }, endpoint);
            Pump(host, null, ref now, () => host.RemoteReady);
            client.Send(new DuelPacket { Kind = DuelMessage.Ready, Session = challenge.Session, Sequence = 8, Ready = false }, endpoint);
            client.Send(new DuelPacket { Kind = DuelMessage.Ready, Session = challenge.Session ^ 2UL, Sequence = 10, Ready = false }, endpoint);
            for (int i = 0; i < 5; i++) { host.Tick(now); now += .001; Thread.Sleep(1); }
            Assert.That(host.RemoteReady, Is.True);
            using var extra = DuelLink.Join(endpoint, 123);
            Pump(host, extra, ref now, () => extra.State == DuelLinkState.Failed);
            Assert.That(extra.Error, Does.Contain("already has a player"));
        }

        [Test]
        public void InputQueueDeduplicatesOrdersBoundsAndConsumesOneCommandPerTick()
        {
            var queue = new DuelInputQueue();
            queue.Add(new[] { Input(3), Input(1), Input(2), Input(1), Input(int.MaxValue) });
            Assert.That(queue.PendingCount, Is.EqualTo(3));
            Assert.That(queue.Consume(10, out var first), Is.True);
            Assert.That(first.Command.Sequence, Is.EqualTo(1));
            Assert.That(queue.Consume(10, out _), Is.False);
            queue.Consume(11, out var second);
            Assert.That(second.Command.Sequence, Is.EqualTo(2));
            queue.Add(new[] { Input(1) });
            queue.Consume(12, out var third);
            Assert.That(third.Command.Sequence, Is.EqualTo(3));
            queue.Consume(13, out var held);
            Assert.That(held.Command.Move.sqrMagnitude, Is.GreaterThan(0));
            Assert.That(held.Command.Fire || held.Command.Jump, Is.False);
            queue.Consume(22, out var stopped);
            Assert.That(stopped.Command.Move, Is.EqualTo(Vector2.zero));
        }

        private static DuelSnapshot Snapshot(int round, int tick) => new() { Round = round, Tick = tick,
            Phase = MatchPhase.Playing, Actors = new[] { new ActorSnapshot { Health = 100 }, new ActorSnapshot { Health = 100 } } };

        private static void Pump(DuelLink host, DuelLink client, ref double now, Func<bool> done)
        {
            var clock = Stopwatch.StartNew();
            while (!done() && clock.Elapsed.TotalSeconds < 4)
            {
                host?.Tick(now); client?.Tick(now);
                now += .001;
                Thread.Sleep(1);
            }
            Assert.That(done(), Is.True, $"Host={host?.State} client={client?.State} {client?.Error}");
        }
    }
}
