using UnityEngine;

namespace RailArena.Simulation
{
    [CreateAssetMenu(menuName = "Rail Arena/Movement tuning")]
    public sealed class MovementTuning : ScriptableObject
    {
        [Min(1)] public float GroundSpeed = 8;
        [Min(0)] public float GroundAcceleration = 8;
        [Min(0)] public float AirAcceleration = 1;
        [Min(0)] public float Friction = 6;
        [Min(0)] public float StopSpeed = 3.125f;
        [Min(0)] public float JumpSpeed = 8.4375f;
        [Min(0)] public float Gravity = 25;
        [Range(1, 2)] public float SprintSpeedMultiplier = 1.35f;
        [Range(.1f, 1)] public float WalkSpeedMultiplier = .5f;
        [Range(.1f, 1)] public float CrouchSpeedMultiplier = .45f;
        [Range(1, 120)] public int SlideDurationTicks = 36;
        [Range(0, 180)] public int SlideCooldownTicks = 48;
        [Range(1, 30)] public float SlideMinimumSpeed = 8.8f;
        [Range(0, 40)] public float SlideDeceleration = 15;
        [Range(0, 180)] public float SlideSteeringDegrees = 45;
        [Range(0, .5f)] public float StepOffset = .3f;
        [Range(20, 60)] public float SlopeLimit = 50;
        [Range(.02f, .12f)] public float SkinWidth = .08f;
        [Range(.75f, 1.4f)] public float PadFlightMultiplier = 1;
    }
}

