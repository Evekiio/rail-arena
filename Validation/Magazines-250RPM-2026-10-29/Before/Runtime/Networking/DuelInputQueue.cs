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
        private int previousViewedTick;
        public const int FireGraceTicks = 3;
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
                // Hold only the newest physical state, not a tap aggregated from an older command.
                previous = input.Command;
                previousViewedTick = input.ViewedTick;
                input.Command.Fire = Shot.HasValue;
                input.Command.Jump = jump;
                LastConsumed = input.Command.Sequence;
                lastCommandTick = authorityTick;
                return true;
            }
            // Held automatic fire tolerates only a brief gap; never invent a newer aim timestamp.
            // A received release immediately wins over any older redundant held command.
            input.Command = previous;
            input.ViewedTick = previousViewedTick;
            input.Command.Fire &= (long)authorityTick - lastCommandTick <= FireGraceTicks;
            if (input.Command.Fire) Shot = input;
            if ((long)authorityTick - lastCommandTick > 9)
            { input.Command.Move = default; input.Command.Jump = false; input.Command.Sprint = input.Command.Walk = false; }
            return true;
        }
    }
}
