using System.Collections.Generic;
using UnityEngine;

namespace RailArena
{
    public enum PortalResult { None, Blocked, Teleported }

    /// <summary>Swept controller crossing; independent of render-frame trigger callbacks.</summary>
    public sealed class PortalGate : MonoBehaviour
    {
        public TraversalPresentation Presentation;
        public PortalGate Exit;
        public string DestinationName;
        public float HalfWidth = 1.3f;
        public float Height = 3.4f;
        public float EntryOffset;
        public const int CooldownTicks = 30;
        private readonly Collider[] overlaps = new Collider[32];

        public PortalResult TryTraverse(ArenaActor actor, Vector3 previousPosition, int tick, IReadOnlyList<ArenaActor> actors = null)
        {
            if (Exit == null) return PortalResult.None;
            Vector3 before = transform.InverseTransformPoint(previousPosition);
            Vector3 after = transform.InverseTransformPoint(actor.transform.position);
            before.z -= EntryOffset; after.z -= EntryOffset;
            if (before.z <= 0 || after.z > 0) return PortalResult.None;
            Vector3 crossing = Vector3.Lerp(before, after, before.z / (before.z - after.z));
            if (Mathf.Abs(crossing.x) > HalfWidth - actor.Controller.radius || crossing.y < -.1f ||
                crossing.y + actor.Controller.height > Height) return PortalResult.None;
            // This layout pairs upright, opposing gates: preserve world look and momentum without rotating input frames.
            bool aligned = Vector3.Dot(transform.up, Vector3.up) > .999f && Vector3.Dot(Exit.transform.up, Vector3.up) > .999f &&
                Vector3.Dot(transform.forward, Exit.transform.forward) < -.999f;
            Vector3 destination = Exit.transform.TransformPoint(new Vector3(-crossing.x, crossing.y, actor.Controller.radius + .24f));
            if (tick < actor.PortalReadyTick || !aligned || !ExitClear(actor, destination, actors))
            {
                Vector3 velocity = actor.Velocity;
                velocity -= transform.forward * Mathf.Min(0, Vector3.Dot(velocity, transform.forward));
                actor.Warp(previousPosition, velocity, crossing.y < .2f && velocity.y <= 0);
                return PortalResult.Blocked;
            }
            actor.Warp(destination, actor.Velocity, crossing.y < .2f && actor.Velocity.y <= 0);
            actor.PortalReadyTick = tick + CooldownTicks;
            actor.LastTeleportTick = tick;
            actor.TeleportSerial++;
            return PortalResult.Teleported;
        }

        private bool ExitClear(ArenaActor actor, Vector3 feet, IReadOnlyList<ArenaActor> actors)
        {
            float radius = actor.Controller.radius;
            Vector3 low = feet + Vector3.up * radius;
            Vector3 high = feet + Vector3.up * (actor.Controller.height - radius);
            int count = Physics.OverlapCapsuleNonAlloc(low, high, radius - .01f, overlaps,
                (1 << 0) | (1 << 9), QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (overlaps[i] != actor.Controller && !overlaps[i].transform.IsChildOf(actor.transform)) return false;
            // Include interpolated opponents whose controllers are disabled on the predicting client.
            if (actors != null) foreach (var other in actors)
            {
                if (other == actor || !other.Alive) continue;
                Vector3 separation = other.transform.position - feet;
                float otherRadius = other.Controller.radius;
                float vertical = Mathf.Max(0, other.transform.position.y + otherRadius - high.y,
                    low.y - (other.transform.position.y + other.Controller.height - otherRadius));
                float combined = radius + otherRadius;
                if (separation.x * separation.x + separation.z * separation.z + vertical * vertical < combined * combined) return false;
            }
            Vector3 support = new Vector3(feet.x, Exit.transform.position.y + .25f, feet.z);
            return Physics.Raycast(support, Vector3.down, .6f, 1 << 0, QueryTriggerInteraction.Ignore);
        }
    }
}
