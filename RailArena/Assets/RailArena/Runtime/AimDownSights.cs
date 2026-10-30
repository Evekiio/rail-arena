using UnityEngine;

namespace RailArena
{
    /// <summary>View-only sight alignment; never changes movement, hit rays or weapon cadence.</summary>
    public sealed class AimDownSights
    {
        public const float TransitionSeconds = .18f;
        public const float EyeRelief = .38f;
        public float Blend { get; private set; }
        public float Weight => Mathf.SmoothStep(0, 1, Blend);
        public void Reset() => Blend = 0;
        public void Step(bool aiming, float deltaTime) => Blend = Mathf.MoveTowards(Blend, aiming ? 1 : 0,
            Mathf.Max(0, deltaTime) / TransitionSeconds);

        public Vector3 Position(Vector3 sightLocal, Vector3 motion, float recoil)
            => Vector3.Lerp(FirstPersonWeaponMotion.RestPosition, Vector3.forward * EyeRelief - sightLocal, Weight)
                + motion * (1 - Weight) - Vector3.forward * (recoil * RailgunPresentation.ViewRecoilTravel);

        public Quaternion Rotation(Quaternion motion) => Quaternion.Slerp(motion, Quaternion.identity, Weight);
    }
}
