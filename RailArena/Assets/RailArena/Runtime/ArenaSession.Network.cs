using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaSession
    {
        public int LocalPlayerId { get; private set; }
        public bool IsOnline => link != null;
        public bool OnlineWaiting => IsOnline && (link.State != DuelLinkState.Connected || link.Round == 0);
        public bool LocalReady => link != null && link.LocalReady;
        public string ConnectionError { get; private set; } = "";
        public string ConnectionStatus => link == null ? ConnectionError :
            link.State == DuelLinkState.Listening ? $"HOSTING // UDP {link.LocalEndPoint.Port}\nWaiting for a challenger" :
            link.State == DuelLinkState.Connecting ? "CONNECTING..." :
            $"CONNECTED // {(link.IsHost ? "HOST" : "CHALLENGER")}\nYOU {(link.LocalReady ? "READY" : "NOT READY")}  /  OPPONENT {(link.RemoteReady ? "READY" : "NOT READY")}";
        public int NetworkRound => link?.Round ?? 0;
        public int LastAcknowledgedInput { get; private set; }
        public int PendingInputs => prediction.Count;
        public int SnapshotsReceived { get; private set; }
        public float MaximumCorrection { get; private set; }
        public int OutboundClamps { get; private set; }
        private DuelLink link;
        private DuelInputQueue remoteInputs;
        private readonly List<DuelInput> prediction = new();
        private readonly List<DuelSnapshot> interpolation = new();
        private DuelSnapshot receivedSnapshot;
        private int inputSequence;
        private int clientTick;
        private int clientRound;
        private int snapshotCadence;
        private double snapshotTime;
        private const int InterpolationTicks = 6;

        public void HostDuel(int port) => OpenDuel(true, null, port);
        public void JoinDuel(string address, int port) => OpenDuel(false, address, port);
        public void ToggleReady() { if (link?.State == DuelLinkState.Connected) link.SetReady(!link.LocalReady); }

        private void OpenDuel(bool host, string address, int port)
        {
            MainMenu();
            ConnectionError = "";
            if (port < 1 || port > 65535) { ConnectionError = "Use a UDP port from 1 to 65535."; hud.RefreshMenu(); return; }
            if (!host && (!IPAddress.TryParse(address, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetwork))
            { ConnectionError = "Enter the host's IPv4 address."; hud.RefreshMenu(); return; }
            try
            {
                ulong identity = BuildIdentity.Token;
                link = host ? DuelLink.Host(port, identity) : DuelLink.Join(new IPEndPoint(IPAddress.Parse(address), port), identity);
                LocalPlayerId = host ? 0 : 1;
                remoteInputs = new DuelInputQueue();
                link.InputsReceived += inputs => remoteInputs.Add(inputs);
                StartSessionCore(false, 1, 1, TargetScenario.Combat);
                if (!host) Actors[0].Controller.enabled = false;
                SetPaused(false);
            }
            catch (SocketException e)
            { MainMenu(); ConnectionError = "Could not open UDP socket: " + e.SocketErrorCode; hud.RefreshMenu(); }
            catch (InvalidOperationException e)
            { MainMenu(); ConnectionError = e.Message; hud.RefreshMenu(); }
        }

        private void DisconnectNetwork()
        {
            link?.Dispose();
            link = null;
            LocalPlayerId = 0;
            remoteInputs = null;
            prediction.Clear(); interpolation.Clear();
            receivedSnapshot = null;
            inputSequence = clientRound = clientTick = LastAcknowledgedInput = snapshotCadence = SnapshotsReceived = 0;
            MaximumCorrection = 0;
            OutboundClamps = 0;
        }

        private void OnDestroy() => DisconnectNetwork();

        private void UpdateNetwork()
        {
            if (link == null) return;
            bool waiting = OnlineWaiting;
            link.Tick(Time.realtimeSinceStartupAsDouble);
            if (link.State == DuelLinkState.Closed || link.State == DuelLinkState.Failed)
            {
                string error = link.Error;
                MainMenu();
                ConnectionError = error;
                hud.RefreshMenu();
                return;
            }
            if (waiting != OnlineWaiting) SetPaused(false);
            if (!link.IsHost && link.LatestSnapshot != null && link.LatestSnapshot != receivedSnapshot)
                ApplySnapshot(link.LatestSnapshot);
        }

        private void OnlineFixedUpdate()
        {
            if (link.State != DuelLinkState.Connected) return;
            if (link.IsHost)
            {
                if ((link.Round == 0 || Match.Phase == MatchPhase.Results) && link.LocalReady && link.RemoteReady)
                {
                    link.SetRound(link.Round + 1);
                    remoteInputs = new DuelInputQueue();
                    StartSessionCore(false, 1, 1, TargetScenario.Combat);
                }
                if (!OnlineWaiting && Match.Phase != MatchPhase.Results)
                {
                    remoteInputs.Consume(Match.Tick + 1, out var input);
                    Actors[1].Command = input.Command;
                    AdvanceAuthority();
                }
                // Keep lobby/results snapshots flowing even while the match clock is stopped.
                if (++snapshotCadence % 3 == 0)
                    link.SendSnapshot(CaptureSnapshot());
            }
            else if (!OnlineWaiting && receivedSnapshot != null && Match.Phase != MatchPhase.Results)
                PredictClient();
        }

        private void PredictClient()
        {
            if (AutomatedHuman) Human.Command = Human.Brain.Think(this, Human);
            if (Paused) Human.Command = new MovementCommand { Yaw = yaw, Pitch = pitch };
            clientTick++;
            if (prediction.Count < DuelInputQueue.MaximumPending)
            {
                var command = Human.Command;
                if (command.Move.x < -1 || command.Move.x > 1 || command.Move.y < -1 || command.Move.y > 1 || command.Pitch < -89 || command.Pitch > 89)
                {
                    OutboundClamps++;
                    if (OutboundClamps == 1) Debug.Log($"Normalizing outbound controls: x={command.Move.x:R} y={command.Move.y:R} pitch={command.Pitch:R}");
                }
                command = DuelProtocol.CommandForWire(command);
                command.Sequence = ++inputSequence;
                var input = new DuelInput { Command = command, ViewedTick = Math.Max(0, receivedSnapshot.Tick - InterpolationTicks) };
                prediction.Add(input);
                Human.Command = command;
                if (Human.Alive && Match.IsActive)
                {
                    MoveActor(Human, clientTick, false);
                    if (command.Fire && CombatRules.CanFire(clientTick, Human.NextShotTick, true))
                    {
                        Human.NextShotTick = clientTick + CombatRules.ShotIntervalTicks;
                        Human.Presentation.Fired();
                        ShowShot(Human, Human.Eye, Human.Eye + Human.AimDirection * 180);
                    }
                }
            }
            int count = Math.Min(prediction.Count, DuelProtocol.MaximumInputBatch);
            if (count > 0)
            {
                int start = prediction.Count >= DuelInputQueue.MaximumPending - 6 ? 0 : prediction.Count - count;
                link.SendInputs(prediction.GetRange(start, count).ToArray());
            }
        }

        private DuelSnapshot CaptureSnapshot()
        {
            var state = new DuelSnapshot { Round = link.Round, Tick = Match.Tick, Phase = Match.Phase, Winner = Match.Winner };
            foreach (var actor in Actors)
                state.Actors[actor.Id] = new ActorSnapshot { Position = actor.transform.position, Velocity = actor.Velocity,
                    Yaw = Mathf.Repeat(actor.Command.Yaw, 360), Pitch = actor.Command.Pitch, Health = actor.Health,
                    NextShotTick = actor.NextShotTick, RespawnTick = actor.RespawnTick, PadReadyTick = actor.PadReadyTick,
                    AirborneTicks = actor.AirborneTicks, Score = Match.Scores[actor.Id],
                    LastInput = actor.Id == 1 ? remoteInputs.LastConsumed : 0, Grounded = actor.Grounded,
                    ShotSerial = actor.ShotSerial, HitSerial = actor.HitSerial, ShotEnd = actor.LastShotEnd };
            for (int i = 0; i < 3; i++) state.PickupTicks[i] = World.Pickups[i].AvailableTick;
            return state;
        }

        private void ApplySnapshot(DuelSnapshot state)
        {
            if (receivedSnapshot != null && state.Round == receivedSnapshot.Round && state.Tick < receivedSnapshot.Tick) return;
            bool newRound = state.Round != clientRound;
            bool wasResults = Match.Phase == MatchPhase.Results;
            if (newRound)
            {
                clientRound = state.Round;
                prediction.Clear(); interpolation.Clear();
                inputSequence = LastAcknowledgedInput = 0;
            }
            receivedSnapshot = state;
            SnapshotsReceived++;
            snapshotTime = Time.realtimeSinceStartupAsDouble;
            Match = MatchState.FromAuthority(state.Tick, state.Phase, state.Winner, state.Actors[0].Score, state.Actors[1].Score);
            Vector3 oldPrediction = Human.transform.position;
            bool continuousLife = Human.Alive && state.Actors[LocalPlayerId].Health > 0 && Human.RespawnTick == state.Actors[LocalPlayerId].RespawnTick;
            foreach (var actor in Actors)
            {
                var authoritative = state.Actors[actor.Id];
                bool wasAlive = actor.Alive;
                if (actor == Human || newRound || interpolation.Count == 0)
                    RestoreActor(actor, authoritative, actor == Human);
                else
                {
                    actor.Health = authoritative.Health;
                    if (wasAlive != actor.Alive) actor.SetAlive(actor.Alive);
                    actor.Controller.enabled = false;
                }
                if (!wasAlive && actor.Alive) actor.Brain = new BotBrain(7291 + actor.Id * 917 + state.Tick);
                if (authoritative.ShotSerial > actor.ShotSerial && !newRound && actor != Human)
                { actor.Presentation.Fired(); ShowShot(actor, actor.Eye, authoritative.ShotEnd); }
                if (authoritative.HitSerial > actor.HitSerial && !newRound) actor.Presentation.Hit();
                actor.ShotSerial = authoritative.ShotSerial;
                actor.HitSerial = authoritative.HitSerial;
                if (wasAlive && !actor.Alive) Feed = $"{Actors[1 - actor.Id].DisplayName}  >  {actor.DisplayName}";
            }
            LastAcknowledgedInput = state.Actors[LocalPlayerId].LastInput;
            prediction.RemoveAll(input => input.Command.Sequence <= LastAcknowledgedInput);
            clientTick = state.Tick;
            var liveCommand = Human.Command;
            foreach (var input in prediction)
            {
                clientTick++;
                Human.Command = input.Command;
                if (Human.Alive && Match.IsActive) MoveActor(Human, clientTick, false);
                if (Human.Alive && Match.IsActive && input.Command.Fire && CombatRules.CanFire(clientTick, Human.NextShotTick, true))
                    Human.NextShotTick = clientTick + CombatRules.ShotIntervalTicks;
            }
            Human.Command = liveCommand;
            if (!newRound && continuousLife) MaximumCorrection = Mathf.Max(MaximumCorrection, Vector3.Distance(oldPrediction, Human.transform.position));
            if (interpolation.Count == 0 || state.Tick > interpolation[interpolation.Count - 1].Tick) interpolation.Add(state);
            while (interpolation.Count > 32) interpolation.RemoveAt(0);
            for (int i = 0; i < 3; i++)
            { World.Pickups[i].AvailableTick = state.PickupTicks[i]; World.Pickups[i].SetAvailable(state.Tick >= state.PickupTicks[i]); }
            if (newRound || wasResults != (Match.Phase == MatchPhase.Results)) SetPaused(false);
        }

        private void RestoreActor(ArenaActor actor, ActorSnapshot state, bool local)
        {
            bool wasAlive = actor.Alive;
            actor.Health = state.Health;
            if (wasAlive != actor.Alive) actor.SetAlive(actor.Alive);
            actor.Controller.enabled = false;
            actor.transform.position = state.Position;
            actor.Controller.enabled = local && actor.Alive;
            actor.Velocity = state.Velocity;
            actor.GroundedOverride = state.Grounded;
            actor.AirborneTicks = state.AirborneTicks;
            actor.NextShotTick = state.NextShotTick;
            actor.RespawnTick = state.RespawnTick;
            actor.PadReadyTick = state.PadReadyTick;
        }

        private void InterpolateOpponent()
        {
            if (link == null || link.IsHost || interpolation.Count == 0) return;
            double tick = receivedSnapshot.Tick + Math.Min(.25, Time.realtimeSinceStartupAsDouble - snapshotTime) * 60 - InterpolationTicks;
            var first = interpolation[0];
            var second = first;
            foreach (var frame in interpolation)
            {
                if (frame.Tick <= tick) first = frame;
                second = frame;
                if (frame.Tick >= tick) break;
            }
            float blend = first.Tick == second.Tick ? 1 : Mathf.Clamp01((float)(tick - first.Tick) / (second.Tick - first.Tick));
            var a = first.Actors[0]; var b = second.Actors[0];
            var opponent = Actors[0];
            opponent.transform.position = Vector3.Lerp(a.Position, b.Position, blend);
            opponent.Velocity = Vector3.Lerp(a.Velocity, b.Velocity, blend);
            opponent.GroundedOverride = b.Grounded;
            opponent.Command = new MovementCommand { Yaw = Mathf.LerpAngle(a.Yaw, b.Yaw, blend), Pitch = Mathf.Lerp(a.Pitch, b.Pitch, blend) };
        }

        private void MoveActor(ArenaActor actor, int tick, bool authoritative)
        {
            actor.Move(Tuning);
            foreach (var pad in World.Pads)
            {
                Vector3 offset = actor.transform.position - pad.transform.position;
                if (tick < actor.PadReadyTick || Mathf.Abs(offset.y) >= 1 || new Vector2(offset.x, offset.z).magnitude >= 1.5f) continue;
                actor.Velocity = pad.Launch(actor.transform.position, Tuning.Gravity);
                actor.AirborneTicks = 2;
                if (actor.Bot || AutomatedHuman) actor.Brain.Launched(World.NearestNode(pad.Destination));
                actor.PadReadyTick = tick + 90;
                if (authoritative) padCount++;
            }
        }
    }
}
