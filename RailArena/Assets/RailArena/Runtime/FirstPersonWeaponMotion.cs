using UnityEngine;

namespace RailArena
{
    /// <summary>Bounded view-weapon motion; never writes camera, actor or simulation state.</summary>
    public sealed class FirstPersonWeaponMotion
    {
        public static readonly Vector3 RestPosition = new(.33f, -.3f, .62f);
        public Vector3 Offset { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        private float phase, movement, landingStrength, landingAge = 10, raiseAge = 10;
        private float downwardSpeed;
        private Vector2 sway;
        private bool initialized, wasGrounded;

        public void Reset(bool raise)
        {
            phase = movement = landingStrength = downwardSpeed = 0;
            landingAge = 10;
            raiseAge = raise ? 0 : 10;
            sway = Vector2.zero;
            initialized = false;
            Offset = Vector3.zero;
            Rotation = Quaternion.identity;
        }

        public void Step(float deltaTime, Vector3 velocity, bool grounded, Vector2 lookDegrees, float recoil)
        {
            if (deltaTime <= 0) return;
            // A rendering hitch must not create a huge phase step or inferred angular speed.
            float dt = Mathf.Min(deltaTime, .1f);
            if (!initialized) { wasGrounded = grounded; initialized = true; }
            if (!grounded) downwardSpeed = Mathf.Max(downwardSpeed, -velocity.y);
            if (grounded && !wasGrounded)
            {
                landingStrength = Mathf.Clamp01((downwardSpeed - 2) / 18);
                landingAge = 0;
                downwardSpeed = 0;
            }
            wasGrounded = grounded;
            landingAge += dt;
            raiseAge += dt;

            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            movement = Mathf.Lerp(movement, grounded ? Mathf.Clamp01(speed / 10) : 0, 1 - Mathf.Exp(-14 * dt));
            phase = Mathf.Repeat(phase + Mathf.Min(speed, 18) * dt * 1.4f, Mathf.PI * 2);
            var lookRate = lookDegrees / deltaTime;
            var target = new Vector2(Mathf.Clamp(lookRate.y * .004f, -1.5f, 1.5f),
                Mathf.Clamp(-lookRate.x * .004f, -1.5f, 1.5f));
            sway = Vector2.Lerp(sway, target, 1 - Mathf.Exp(-12 * dt));

            float t = landingAge / .065f;
            float landing = t * Mathf.Exp(1 - t) * landingStrength;
            float raise = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(raiseAge / .28f));
            float stride = Mathf.Sin(phase), doubleStride = Mathf.Cos(phase * 2);
            Offset = new Vector3(stride * .012f * movement,
                doubleStride * .009f * movement - .04f * landing - .16f * raise,
                -.012f * landing - .035f * raise);
            Rotation = Quaternion.Euler(sway.x - 2.25f * Mathf.Clamp01(recoil) + 8 * raise + 1.5f * landing,
                sway.y * .3f, sway.y + stride * .45f * movement);
        }
    }
}
