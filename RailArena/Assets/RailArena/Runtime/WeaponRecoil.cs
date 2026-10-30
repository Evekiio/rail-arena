using UnityEngine;

namespace RailArena
{
    /// <summary>Analytic damped springs: presentation only, independent of camera and hit direction.</summary>
    public sealed class WeaponRecoil
    {
        private float kick, kickVelocity, roll, rollVelocity;
        private int shot;
        public float Kick => Mathf.Clamp(kick, 0, 1.4f);
        private float Roll => Mathf.Clamp(roll, -1, 1);
        public Vector3 Offset => new(Roll * .004f, Kick * .008f, 0);
        public Quaternion Rotation => Quaternion.Euler(-4.8f * Kick, Roll * .45f, Roll * 1.5f);

        public void Fire()
        {
            kickVelocity = Mathf.Min(kickVelocity + 95, 140);
            rollVelocity = Mathf.Clamp(rollVelocity + (++shot % 2 == 0 ? -55 : 55), -90, 90);
        }

        public void Reset() { kick = kickVelocity = roll = rollVelocity = 0; shot = 0; }

        public void Step(float delta)
        {
            if (delta <= 0) return;
            Spring(ref kick, ref kickVelocity, delta);
            Spring(ref roll, ref rollVelocity, delta);
        }

        private static void Spring(ref float value, ref float velocity, float delta)
        {
            const float frequency = 42, damping = .65f;
            float decay = frequency * damping;
            float oscillation = frequency * Mathf.Sqrt(1 - damping * damping);
            float envelope = Mathf.Exp(-decay * delta);
            float cosine = Mathf.Cos(oscillation * delta), sine = Mathf.Sin(oscillation * delta);
            float next = envelope * (value * cosine + (velocity + decay * value) / oscillation * sine);
            velocity = envelope * (velocity * cosine - (decay * velocity + frequency * frequency * value) / oscillation * sine);
            value = next;
        }
    }
}
