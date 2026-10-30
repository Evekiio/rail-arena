using UnityEngine;

namespace RailArena.Simulation
{
    [CreateAssetMenu(menuName = "Rail Arena/Movement tuning")]
    public sealed class MovementTuning : ScriptableObject
    {
        [Min(1)] public float GroundSpeed = 10;
        [Min(0)] public float GroundAcceleration = 10;
        [Min(0)] public float AirAcceleration = 1;
        [Min(0)] public float Friction = 6;
        [Min(0)] public float StopSpeed = 3.125f;
        [Min(0)] public float JumpSpeed = 8.4375f;
        [Min(0)] public float Gravity = 25;
        [Range(1, 2)] public float SprintSpeedMultiplier = 1.35f;
        [Range(.1f, 1)] public float WalkSpeedMultiplier = .5f;
        [Range(.1f, 1)] public float CrouchSpeedMultiplier = .45f;
    }
}
