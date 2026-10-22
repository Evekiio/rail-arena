using UnityEngine;

namespace RailArena.Networking
{
    /// <summary>Client-only camera continuity. Authority and predicted collision state correct immediately.</summary>
    public sealed class ReconciliationView
    {
        private Vector3 offset;
        public Vector3 Offset => offset;
        public void Reset() => offset = Vector3.zero;

        public void Correct(Vector3 previous, Vector3 corrected)
        {
            var change = previous - corrected;
            if (change.sqrMagnitude > 16) { Reset(); return; }
            offset = Vector3.ClampMagnitude(offset + change, 4);
        }

        public void Decay(float seconds) => offset *= Mathf.Exp(-20 * Mathf.Max(0, seconds));

        public Vector3 Position(Vector3 eye)
        {
            if (offset.sqrMagnitude < .000001f) return eye;
            float distance = offset.magnitude;
            Vector3 direction = offset / distance;
            if (Physics.Raycast(eye, direction, out var wall, distance, 1 << 0, QueryTriggerInteraction.Ignore))
                return eye + direction * Mathf.Max(0, wall.distance - .05f);
            return eye + offset;
        }
    }
}
