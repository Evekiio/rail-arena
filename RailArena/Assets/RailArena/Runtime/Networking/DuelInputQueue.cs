using System.Collections.Generic;
using RailArena.Simulation;

namespace RailArena.Networking
{
    /// <summary>Redundant commands are consumed at most once and at most one per authority tick.</summary>
    public sealed class DuelInputQueue
    {
        public const int MaximumPending = 128;
        private readonly SortedDictionary<int, DuelInput> pending = new();
        private MovementCommand previous;
        private int lastCommandTick = int.MinValue;
        private int lastConsumeTick = -1;
        public int LastConsumed { get; private set; }
        public int PendingCount => pending.Count;
        public DuelInput? Shot { get; private set; }

        public void Add(DuelInput[] inputs)
        {
            foreach (var input in inputs)
            {
                int sequence = input.Command.Sequence;
                if (sequence <= LastConsumed || (long)sequence - LastConsumed > MaximumPending) continue;
                if (pending.Count < MaximumPending) pending.TryAdd(sequence, input);
            }
        }

        public bool Consume(int authorityTick, out DuelInput input)
        {
            input = default;
            if (authorityTick <= lastConsumeTick) return false;
            lastConsumeTick = authorityTick;
            Shot = null;
            if (pending.Count > 0)
            {
                bool jump = false;
                foreach (var entry in pending)
                {
                    input = entry.Value;
                    jump |= input.Command.Jump;
                    if (input.Command.Fire) Shot = input;
                }
                // Never turn a burst into extra simulation steps or a permanent input delay.
                pending.Clear();
                input.Command.Fire = Shot.HasValue;
                input.Command.Jump = jump;
                LastConsumed = input.Command.Sequence;
                previous = input.Command;
                lastCommandTick = authorityTick;
                return true;
            }
            // Brief loss preserves motion, never an unacknowledged fire or jump.
            input.Command = previous;
            input.Command.Fire = input.Command.Jump = false;
            if ((long)authorityTick - lastCommandTick > 9) input.Command.Move = default;
            return true;
        }
    }
}
