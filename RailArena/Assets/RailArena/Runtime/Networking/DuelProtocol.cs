using System;
using System.IO;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Networking
{
    public enum DuelMessage : byte { Hello, Challenge, Connect, Welcome, Input, Snapshot, Ready, Leave, Reject }
    public enum RejectReason : byte { Incompatible, Full }

    public struct DuelInput
    {
        public MovementCommand Command;
        public int ViewedTick;
    }

    public struct ActorSnapshot
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Yaw;
        public float Pitch;
        public int Health;
        public int Life;
        public int NextShotTick;
        public int RespawnTick;
        public int PadReadyTick;
        public int AirborneTicks;
        public int Score;
        public int LastInput;
        public bool Grounded;
        public int ShotSerial;
        public int HitSerial;
        public Vector3 ShotEnd;
        public CombatStatistics Statistics;
    }

    public sealed class DuelSnapshot
    {
        public int Round;
        public int Tick;
        public MatchPhase Phase;
        public int Winner = -1;
        public ActorSnapshot[] Actors = new ActorSnapshot[2];
        public int[] PickupTicks = new int[3];
    }

    public sealed class DuelPacket
    {
        public DuelMessage Kind;
        public ulong Session;
        public ulong Nonce;
        public ulong Build;
        public uint Sequence;
        public int Round;
        public bool Ready;
        public RejectReason Rejection;
        public DuelInput[] Inputs = Array.Empty<DuelInput>();
        public DuelSnapshot Snapshot;
    }

    /// <summary>Fixed-layout, bounded datagrams. No object deserialization or client-supplied damage.</summary>
    public static class DuelProtocol
    {
        public const int MaximumDatagram = 1200;
        public const int MaximumInputBatch = 6;
        public const ushort Version = 4;
        private const uint Magic = 0x52414C52;

        public static MovementCommand CommandForWire(MovementCommand command)
        {
            command.Move = Vector2.ClampMagnitude(command.Move, 1);
            command.Move.x = Mathf.Clamp(command.Move.x, -1, 1);
            command.Move.y = Mathf.Clamp(command.Move.y, -1, 1);
            command.Yaw = Mathf.Repeat(command.Yaw, 360);
            command.Pitch = Mathf.Clamp(Mathf.DeltaAngle(0, command.Pitch), -89, 89);
            return command;
        }

        public static byte[] Encode(DuelPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            using var stream = new MemoryStream(512);
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write((byte)packet.Kind);
            writer.Write(packet.Session);
            writer.Write(packet.Sequence);
            switch (packet.Kind)
            {
                case DuelMessage.Hello:
                case DuelMessage.Challenge:
                case DuelMessage.Connect:
                case DuelMessage.Welcome:
                    writer.Write(packet.Nonce);
                    writer.Write(packet.Build);
                    break;
                case DuelMessage.Input:
                    writer.Write(packet.Round);
                    if (packet.Inputs == null || packet.Inputs.Length < 1 || packet.Inputs.Length > MaximumInputBatch)
                        throw new ArgumentException("Input batch must contain one to six commands.");
                    writer.Write((byte)packet.Inputs.Length);
                    foreach (var input in packet.Inputs) WriteInput(writer, input);
                    break;
                case DuelMessage.Snapshot:
                    WriteSnapshot(writer, packet.Snapshot);
                    break;
                case DuelMessage.Ready:
                    writer.Write(packet.Round);
                    writer.Write(packet.Ready);
                    break;
                case DuelMessage.Reject:
                    writer.Write((byte)packet.Rejection);
                    break;
                case DuelMessage.Leave:
                    break;
                default: throw new ArgumentException("Unknown packet type.");
            }
            byte[] data = stream.ToArray();
            if (!TryDecode(data, out _)) throw new ArgumentException("Packet contains invalid values.");
            return data;
        }

        public static bool TryDecode(byte[] data, out DuelPacket packet)
        {
            packet = null;
            if (data == null || data.Length < 19 || data.Length > MaximumDatagram) return false;
            try
            {
                using var stream = new MemoryStream(data, false);
                using var reader = new BinaryReader(stream);
                if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != Version) return false;
                var result = new DuelPacket
                {
                    Kind = (DuelMessage)reader.ReadByte(), Session = reader.ReadUInt64(), Sequence = reader.ReadUInt32()
                };
                switch (result.Kind)
                {
                    case DuelMessage.Hello:
                    case DuelMessage.Challenge:
                    case DuelMessage.Connect:
                    case DuelMessage.Welcome:
                        result.Nonce = reader.ReadUInt64();
                        result.Build = reader.ReadUInt64();
                        if (result.Nonce == 0 || result.Build == 0) return false;
                        break;
                    case DuelMessage.Input:
                        result.Round = Nonnegative(reader.ReadInt32());
                        int count = reader.ReadByte();
                        if (count < 1 || count > MaximumInputBatch) return false;
                        result.Inputs = new DuelInput[count];
                        for (int i = 0; i < count; i++)
                        {
                            result.Inputs[i] = ReadInput(reader);
                            if (i > 0 && result.Inputs[i].Command.Sequence <= result.Inputs[i - 1].Command.Sequence) return false;
                        }
                        break;
                    case DuelMessage.Snapshot:
                        result.Snapshot = ReadSnapshot(reader);
                        break;
                    case DuelMessage.Ready:
                        result.Round = Nonnegative(reader.ReadInt32());
                        result.Ready = ReadBool(reader);
                        break;
                    case DuelMessage.Reject:
                        result.Rejection = (RejectReason)reader.ReadByte();
                        if (result.Rejection > RejectReason.Full) return false;
                        break;
                    case DuelMessage.Leave: break;
                    default: return false;
                }
                if (stream.Position != stream.Length) return false;
                packet = result;
                return true;
            }
            catch (IOException) { return false; }
            catch (ArgumentException) { return false; }
        }

        private static void WriteInput(BinaryWriter writer, DuelInput input)
        {
            var command = input.Command;
            writer.Write(command.Sequence);
            writer.Write(input.ViewedTick);
            writer.Write(command.Move.x);
            writer.Write(command.Move.y);
            writer.Write(command.Yaw);
            writer.Write(command.Pitch);
            writer.Write((byte)((command.Jump ? 1 : 0) | (command.Fire ? 2 : 0)));
        }

        private static DuelInput ReadInput(BinaryReader reader)
        {
            int sequence = Nonnegative(reader.ReadInt32());
            int tick = Nonnegative(reader.ReadInt32());
            var move = new Vector2(Finite(reader.ReadSingle(), -1, 1), Finite(reader.ReadSingle(), -1, 1));
            float yaw = Finite(reader.ReadSingle(), -360, 360);
            float pitch = Finite(reader.ReadSingle(), -89, 89);
            byte flags = reader.ReadByte();
            if (flags > 3) throw new ArgumentException("Invalid input flags.");
            return new DuelInput { ViewedTick = tick, Command = new MovementCommand
            { Sequence = sequence, Move = Vector2.ClampMagnitude(move, 1), Yaw = yaw, Pitch = pitch,
                Jump = (flags & 1) != 0, Fire = (flags & 2) != 0 } };
        }

        private static void WriteSnapshot(BinaryWriter writer, DuelSnapshot state)
        {
            if (state == null || state.Actors == null || state.Actors.Length != 2 || state.PickupTicks == null || state.PickupTicks.Length != 3)
                throw new ArgumentException("A duel snapshot requires two actors and three pickups.");
            writer.Write(state.Round);
            writer.Write(state.Tick);
            writer.Write((byte)state.Phase);
            writer.Write((sbyte)state.Winner);
            foreach (var actor in state.Actors)
            {
                WriteVector(writer, actor.Position);
                WriteVector(writer, actor.Velocity);
                writer.Write(actor.Yaw); writer.Write(actor.Pitch);
                writer.Write(actor.Health); writer.Write(actor.Life); writer.Write(actor.NextShotTick); writer.Write(actor.RespawnTick);
                writer.Write(actor.PadReadyTick); writer.Write(actor.AirborneTicks); writer.Write(actor.Score); writer.Write(actor.LastInput);
                writer.Write(actor.Grounded); writer.Write(actor.ShotSerial); writer.Write(actor.HitSerial); WriteVector(writer, actor.ShotEnd);
                writer.Write(actor.Statistics.Kills); writer.Write(actor.Statistics.Deaths);
                writer.Write(actor.Statistics.Shots); writer.Write(actor.Statistics.Hits);
                writer.Write(actor.Statistics.Headshots); writer.Write(actor.Statistics.VoidDeaths);
            }
            foreach (int tick in state.PickupTicks) writer.Write(tick);
        }

        private static DuelSnapshot ReadSnapshot(BinaryReader reader)
        {
            var state = new DuelSnapshot { Round = Nonnegative(reader.ReadInt32()), Tick = Nonnegative(reader.ReadInt32()),
                Phase = (MatchPhase)reader.ReadByte(), Winner = reader.ReadSByte() };
            if (state.Phase > MatchPhase.Results || state.Winner < -1 || state.Winner > 1 ||
                (state.Phase == MatchPhase.Results) != (state.Winner >= 0)) throw new ArgumentException("Invalid match state.");
            for (int i = 0; i < 2; i++)
            {
                state.Actors[i] = new ActorSnapshot
                {
                    Position = ReadVector(reader, 10000), Velocity = ReadVector(reader, 1000),
                    Yaw = Finite(reader.ReadSingle(), -360, 360), Pitch = Finite(reader.ReadSingle(), -89, 89),
                    Health = Range(reader.ReadInt32(), 0, 100), Life = Range(reader.ReadInt32(), 1, int.MaxValue),
                    NextShotTick = Nonnegative(reader.ReadInt32()),
                    RespawnTick = Nonnegative(reader.ReadInt32()), PadReadyTick = Nonnegative(reader.ReadInt32()),
                    AirborneTicks = Range(reader.ReadInt32(), 0, 2), Score = reader.ReadInt32(), LastInput = Nonnegative(reader.ReadInt32()),
                    Grounded = ReadBool(reader), ShotSerial = Nonnegative(reader.ReadInt32()), HitSerial = Nonnegative(reader.ReadInt32()),
                    ShotEnd = ReadVector(reader, 10000),
                    Statistics = new CombatStatistics(reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32(),
                        reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32())
                };
            }
            for (int i = 0; i < 3; i++) state.PickupTicks[i] = Nonnegative(reader.ReadInt32());
            return state;
        }

        private static void WriteVector(BinaryWriter writer, Vector3 value) { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
        private static Vector3 ReadVector(BinaryReader reader, float limit) => new(Finite(reader.ReadSingle(), -limit, limit),
            Finite(reader.ReadSingle(), -limit, limit), Finite(reader.ReadSingle(), -limit, limit));
        private static int Nonnegative(int value) => Range(value, 0, int.MaxValue);
        private static int Range(int value, int min, int max) => value >= min && value <= max ? value : throw new ArgumentException("Out of range.");
        private static float Finite(float value, float min, float max) => !float.IsNaN(value) && value >= min && value <= max ? value : throw new ArgumentException("Nonfinite or out of range.");
        private static bool ReadBool(BinaryReader reader) => reader.ReadByte() switch { 0 => false, 1 => true, _ => throw new ArgumentException("Invalid boolean.") };
    }
}
