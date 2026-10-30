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
        public bool CanReady => link != null && link.State == DuelLinkState.Connected;
        public string ConnectionError { get; private set; } = "";
        public void ClearConnectionError() => ConnectionError = "";
        public string ConnectionStatus => link == null ? ConnectionError :
            link.State == DuelLinkState.Listening ? $"HOSTING // UDP {link.LocalEndPoint.Port}\nWaiting for a challenger" :
            link.State == DuelLinkState.Connecting ? "CONNECTING..." :
            $"CONNECTED // {(link.IsHost ? "HOST" : "CHALLENGER")}\nYOU {(link.LocalReady ? "READY" : "NOT READY")}  /  OPPONENT {(link.RemoteReady ? "READY" : "NOT READY")}";
        public int NetworkRound => link?.Round ?? 0;
        public int LastAcknowledgedInput { get; private set; }
        public int PendingInputs => prediction.Count;
        public int LocalSimulationTick => IsOnline && LocalPlayerId == 1 ? clientTick : Match?.Tick ?? 0;
        public int SnapshotsReceived { get; private set; }
        public float MaximumCorrection { get; private set; }
        public float MaximumActiveCorrection { get; private set; }
        public float MaximumPhaseChangeCorrection { get; private set; }
        public int OutboundClamps { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Serializable]
        public sealed class PredictionCorrection
        {
            public int tick, acknowledged, pending, previousClientTick;
            public float distance;
            public Vector3 predicted, authority, replayed, previousVelocity, correctedVelocity;
            public bool previousGrounded, authorityGrounded, replayGrounded;
            public int previousPadReady, authorityPadReady, replayPadReady;
            public int previousAirborne, authorityAirborne;
            public MatchPhase previousPhase, authorityPhase;
            public int replayMovementTicks, movementClockShift;
        }
        public List<PredictionCorrection> CorrectionSamples { get; } = new();
        public PredictionCorrection LargestActiveCorrection { get; private set; }
        public PredictionCorrection LargestPhaseChangeCorrection { get; private set; }
#endif
        private DuelLink link;
        private DuelInputQueue remoteInputs;
        private readonly List<DuelInput> prediction = new();
        private readonly List<DuelSnapshot> interpolation = new();
        private readonly ReconciliationView reconciliationView = new();
        private DuelSnapshot receivedSnapshot;
        private int inputSequence;
        private int clientTick;
        private int clientRound;
        private int snapshotCadence;
        private int displayedTick;
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
            {
                MainMenu();
                Debug.LogWarning("Duel socket could not open: "+e.SocketErrorCode);
                ConnectionError=e.SocketErrorCode switch
                {
                    SocketError.AddressAlreadyInUse => "That UDP port is already in use. Choose a different port, then try again.",
                    SocketError.AccessDenied => "The system denied access to this UDP port. Try a different port or check network permissions.",
                    _ => "Could not open the UDP connection. Check the port and network settings, then try again."
                };
                hud.RefreshMenu();
            }
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
            reconciliationView.Reset();
            receivedSnapshot = null;
            inputSequence = clientRound = clientTick = LastAcknowledgedInput = snapshotCadence = SnapshotsReceived = displayedTick = 0;
            MaximumCorrection = 0;
            MaximumActiveCorrection = MaximumPhaseChangeCorrection = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            CorrectionSamples.Clear();
            LargestActiveCorrection = LargestPhaseChangeCorrection = null;
#endif
            OutboundClamps = 0;
        }

        public void QuitGame()
        {
            ReleaseSessionResources();
            PlayerPrefs.Save();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Application.Quit();
        }

        private void ReleaseSessionResources()
        {
            enabled = false;
            DisconnectNetwork();
            hitHistory?.Dispose();
            hitHistory = null;
        }

        private void OnApplicationQuit() => ReleaseSessionResources();
        private void OnDestroy() => ReleaseSessionResources();

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
            // Unity may run several catch-up physics steps before the next Update.
            // Receive authority and controls before simulating any of those steps.
            UpdateNetwork();
            if (link == null || link.State != DuelLinkState.Connected) return;
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
                    Actors[1].ViewedTick = remoteInputs.Shot?.ViewedTick ?? input.ViewedTick;
                    Actors[1].ShotCommand = remoteInputs.Shot?.Command ?? input.Command;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AutomatedHuman) Human.Command = ValidationMovement(Human.Command, clientTick);
#endif
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
                var input = new DuelInput { Command = command, ViewedTick = displayedTick };
                prediction.Add(input);
                Human.Command = command;
                if (Human.Alive && Match.IsActive)
                {
                    MoveActor(Human, clientTick, false);
                    if (AdvanceWeapon(Human, clientTick))
                    {
                        Human.Presentation.Fired();
                        var ray = new Ray(Human.Eye, Human.AimDirection);
                        var end = Physics.Raycast(ray, out var wall, 180, 1 << 0, QueryTriggerInteraction.Ignore) ? wall.point : ray.GetPoint(180);
                        ShowShot(Human, Human.Eye, end);
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
                    Yaw = Mathf.Repeat(actor.Command.Yaw, 360), Pitch = actor.Command.Pitch, Health = actor.Health, Life = actor.Life,
                    NextShotTick = actor.NextShotTick, Magazine = actor.Magazine, RespawnTick = actor.RespawnTick, PadReadyTick = actor.PadReadyTick,
                    AirborneTicks = actor.AirborneTicks, Score = Match.Scores[actor.Id],
                    LastInput = actor.Id == 1 ? remoteInputs.LastConsumed : 0, Grounded = actor.Grounded, Crouched = actor.IsCrouched, Slide = actor.Slide,
                    ShotSerial = actor.ShotSerial, HitSerial = actor.HitSerial, ShotEnd = actor.LastShotEnd,
                    Statistics = Match.Statistics[actor.Id] };
            for (int i = 0; i < 3; i++) state.PickupTicks[i] = World.Pickups[i].AvailableTick;
            return state;
        }

        private void ApplySnapshot(DuelSnapshot state)
        {
            if (receivedSnapshot != null && state.Round == receivedSnapshot.Round && state.Tick < receivedSnapshot.Tick) return;
            bool newRound = state.Round != clientRound;
            bool localLifeChanged = Human.Life != state.Actors[LocalPlayerId].Life;
            if (newRound || localLifeChanged) Feedback.ResetFeedback();
            var previousPhase = Match.Phase;
            bool wasResults = previousPhase == MatchPhase.Results;
            if (newRound)
            {
                clientRound = state.Round;
                prediction.Clear(); interpolation.Clear();
                inputSequence = LastAcknowledgedInput = 0;
            }
            // Never replay a previous life's controls or blend an opponent through its respawn teleport.
            if (localLifeChanged) prediction.Clear();
            if (Actors[1 - LocalPlayerId].Life != state.Actors[1 - LocalPlayerId].Life) interpolation.Clear();
            receivedSnapshot = state;
            SnapshotsReceived++;
            snapshotTime = Time.realtimeSinceStartupAsDouble;
            var previousMatch=Match;
            if(newRound) { Feed=""; RecentCombat.Clear(); }
            Match = MatchState.FromAuthority(state.Tick,state.Phase,state.Winner,state.Actors[0].Score,state.Actors[1].Score,
                state.Actors[0].Statistics,state.Actors[1].Statistics);
            Vector3 oldPrediction = Human.transform.position;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var correction = new PredictionCorrection { tick = state.Tick, previousClientTick = clientTick,
                previousPhase = previousPhase, authorityPhase = state.Phase,
                predicted = oldPrediction, authority = state.Actors[LocalPlayerId].Position,
                previousVelocity = Human.Velocity, previousGrounded = Human.Grounded,
                authorityGrounded = state.Actors[LocalPlayerId].Grounded,
                previousPadReady = Human.PadReadyTick, authorityPadReady = state.Actors[LocalPlayerId].PadReadyTick,
                previousAirborne = Human.AirborneTicks, authorityAirborne = state.Actors[LocalPlayerId].AirborneTicks };
#endif
            bool continuousLife = !localLifeChanged && Human.Alive && state.Actors[LocalPlayerId].Health > 0;
            foreach (var actor in Actors)
            {
                var authoritative = state.Actors[actor.Id];
                bool wasAlive = actor.Alive;
                int previousHealth = actor.Health;
                bool lifeChanged = actor.Life != authoritative.Life;
                if (actor == Human || newRound || interpolation.Count == 0)
                    RestoreActor(actor, authoritative, actor == Human, newRound);
                else
                {
                    actor.Health = authoritative.Health;
                    actor.NextShotTick = authoritative.NextShotTick;
                    actor.RespawnTick = authoritative.RespawnTick;
                    actor.PadReadyTick = authoritative.PadReadyTick;
                    if (wasAlive != actor.Alive) actor.SetAlive(actor.Alive);
                    actor.Controller.enabled = false;
                }
                if(actor==Human && !newRound && !lifeChanged && wasAlive && actor.Alive && authoritative.Health>previousHealth)
                    Feedback.PickedUp(authoritative.Health-previousHealth);
                if (newRound || lifeChanged || (!wasAlive && actor.Alive)) actor.Brain = new BotBrain(7291 + actor.Id * 917 + state.Tick);
                if (authoritative.ShotSerial > actor.ShotSerial && !newRound && !lifeChanged && actor != Human)
                { actor.Presentation.Fired(); ShowShot(actor, actor.Eye, authoritative.ShotEnd); }
                if (authoritative.HitSerial > actor.HitSerial && !newRound && !lifeChanged)
                {
                    actor.Presentation.Hit();
                    if (actor == Human) Feedback.Damaged();
                    // Online matches contain exactly two humans: only the local player can damage this opponent.
                    else if (!localLifeChanged) Feedback.ConfirmHit(!actor.Alive);
                }
                actor.ShotSerial = authoritative.ShotSerial;
                actor.HitSerial = authoritative.HitSerial;
                if (wasAlive && !actor.Alive && !newRound && !lifeChanged)
                {
                    bool fell=authoritative.Statistics.VoidDeaths>previousMatch.Statistics[actor.Id].VoidDeaths;
                    PublishFeed(fell?$"{actor.DisplayName} fell into the void":$"{Actors[1-actor.Id].DisplayName}  >  {actor.DisplayName}");
                }
            }
            if (newRound || localLifeChanged)
            {
                ResetSights();
                ResetRecoil();
                weaponMotion.Reset(true);
                weaponMotionLife = Human.Life;
            }
            LastAcknowledgedInput = state.Actors[LocalPlayerId].LastInput;
            prediction.RemoveAll(input => input.Command.Sequence <= LastAcknowledgedInput);
            clientTick = state.Tick;
            int replayMovementTicks = 0;
            var liveCommand = Human.Command;
            foreach (var input in prediction)
            {
                clientTick++;
                Human.Command = input.Command;
                if (Human.Alive && Match.IsActive)
                { MoveActor(Human, clientTick, false, false); replayMovementTicks++; }
                if (Human.Alive && Match.IsActive) AdvanceWeapon(Human, clientTick);
            }
            Human.Command = liveCommand;
            if (!newRound && continuousLife)
            {
                float distance = Vector3.Distance(oldPrediction, Human.transform.position);
                bool activePlay = (previousPhase == MatchPhase.Playing || previousPhase == MatchPhase.SuddenDeath) && Match.IsActive;
                bool phaseChanged = previousPhase != Match.Phase;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                correction.distance = distance;
                correction.acknowledged = LastAcknowledgedInput;
                correction.pending = prediction.Count;
                correction.replayed = Human.transform.position;
                correction.correctedVelocity = Human.Velocity;
                correction.replayGrounded = Human.Grounded;
                correction.replayPadReady = Human.PadReadyTick;
                correction.replayMovementTicks = replayMovementTicks;
                correction.movementClockShift = state.Tick + replayMovementTicks - correction.previousClientTick;
                if (distance > .25f && (CorrectionSamples.Count < 24 || distance > MaximumCorrection))
                {
                    if (CorrectionSamples.Count < 24) CorrectionSamples.Add(correction);
                    else CorrectionSamples[CorrectionSamples.Count - 1] = correction;
                }
                if (activePlay && distance > MaximumActiveCorrection) LargestActiveCorrection = correction;
                if (phaseChanged && distance > MaximumPhaseChangeCorrection) LargestPhaseChangeCorrection = correction;
#endif
                MaximumCorrection = Mathf.Max(MaximumCorrection, distance);
                if (activePlay) MaximumActiveCorrection = Mathf.Max(MaximumActiveCorrection, distance);
                if (phaseChanged) MaximumPhaseChangeCorrection = Mathf.Max(MaximumPhaseChangeCorrection, distance);
                reconciliationView.Correct(oldPrediction, Human.transform.position);
            }
            else reconciliationView.Reset();
            if (interpolation.Count == 0) displayedTick = state.Tick;
            if (interpolation.Count == 0 || state.Tick > interpolation[interpolation.Count - 1].Tick) interpolation.Add(state);
            while (interpolation.Count > 32) interpolation.RemoveAt(0);
            for (int i = 0; i < 3; i++)
            { World.Pickups[i].AvailableTick = state.PickupTicks[i]; World.Pickups[i].SetAvailable(state.Tick >= state.PickupTicks[i]); }
            if (newRound || wasResults != (Match.Phase == MatchPhase.Results)) SetPaused(false);
        }

        private void RestoreActor(ArenaActor actor, ActorSnapshot state, bool local, bool resetPresentation)
        {
            bool wasAlive = actor.Alive;
            bool lifeChanged = actor.Life != state.Life;
            actor.Health = state.Health;
            actor.Life = state.Life;
            if (wasAlive != actor.Alive || lifeChanged || resetPresentation) actor.SetAlive(actor.Alive);
            actor.Controller.enabled = false;
            actor.transform.position = state.Position;
            actor.SetCrouched(state.Crouched);
            actor.Slide = state.Slide;
            actor.Controller.enabled = local && actor.Alive;
            actor.Velocity = state.Velocity;
            actor.GroundedOverride = state.Grounded;
            actor.AirborneTicks = state.AirborneTicks;
            actor.NextShotTick = state.NextShotTick;
            actor.Magazine = state.Magazine;
            actor.RespawnTick = state.RespawnTick;
            actor.PadReadyTick = state.PadReadyTick;
        }

        private void InterpolateOpponent()
        {
            if (link == null || link.IsHost || interpolation.Count == 0) return;
            double tick = RemoteViewTick;
            var first = interpolation[0];
            var second = first;
            foreach (var frame in interpolation)
            {
                if (frame.Tick <= tick) first = frame;
                second = frame;
                if (frame.Tick >= tick) break;
            }
            float blend = first.Tick == second.Tick ? 1 : Mathf.Clamp01((float)(tick - first.Tick) / (second.Tick - first.Tick));
            // Shots identify the pose actually displayed, including a held pose during snapshot loss.
            displayedTick = (int)Math.Floor(first.Tick + (second.Tick - first.Tick) * (double)blend);
            var a = first.Actors[0]; var b = second.Actors[0];
            var opponent = Actors[0];
            opponent.transform.position = Vector3.Lerp(a.Position, b.Position, blend);
            opponent.Velocity = Vector3.Lerp(a.Velocity, b.Velocity, blend);
            opponent.GroundedOverride = b.Grounded;
            opponent.AirborneTicks = b.AirborneTicks;
            opponent.SetCrouched(blend < 1 ? a.Crouched : b.Crouched);
            opponent.Slide = blend < 1 ? a.Slide : b.Slide;
            opponent.Magazine = blend < 1 ? a.Magazine : b.Magazine;
            opponent.Command = new MovementCommand { Yaw = Mathf.LerpAngle(a.Yaw, b.Yaw, blend), Pitch = Mathf.Lerp(a.Pitch, b.Pitch, blend) };
        }

        private void MoveActor(ArenaActor actor, int tick, bool authoritative, bool present = true)
        {
            actor.Move(Tuning);
            foreach (var pad in World.Pads)
            {
                Vector3 offset = actor.transform.position - pad.transform.position;
                if (tick < actor.PadReadyTick || Mathf.Abs(offset.y) >= 1 || new Vector2(offset.x, offset.z).magnitude >= 1.5f) continue;
                actor.Velocity = pad.Launch(actor.transform.position, Tuning.Gravity);
                actor.AirborneTicks = 2;
                actor.Slide.RemainingTicks = 0;
                if (actor.Bot || AutomatedHuman) actor.Brain.Launched(World.NearestNode(pad.Destination));
                actor.PadReadyTick = tick + 90;
                if (authoritative) padCount++;
                if(present && actor==Human)Feedback.Launched();
            }
        }

        private double RemoteViewTick => receivedSnapshot == null ? 0 : receivedSnapshot.Tick +
            Math.Min(.25, Time.realtimeSinceStartupAsDouble - snapshotTime) * CombatRules.TickRate - InterpolationTicks;
    }
}
