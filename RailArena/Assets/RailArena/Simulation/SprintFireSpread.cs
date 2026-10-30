using UnityEngine;

namespace RailArena.Simulation
{
    public static class SprintFireSpread
    {
        // Default 36-unit reticle, enlarged 25%, at 1080p and 100-degree vertical FOV.
        public const float HalfAngleDegrees = 2.843f;
        public static bool Active(bool grounded, bool crouched, MovementCommand command)
            => grounded && !crouched && command.Sprint && !command.Walk && command.Move.y > 0;

        public const float MovingHalfAngleDegrees = 1.0f;
        public const float CrouchMovingHalfAngleDegrees = .5f;
        public static float ShotHalfAngle(bool grounded, bool crouched, MovementCommand command, Vector3 velocity)
        {
            if (Active(grounded, crouched, command)) return HalfAngleDegrees;
            if (!grounded || new Vector2(velocity.x, velocity.z).sqrMagnitude < .01f) return 0;
            return crouched ? CrouchMovingHalfAngleDegrees : MovingHalfAngleDegrees;
        }

        public static float ScreenRadius(float pixelHeight, float verticalFov, float canvasScale, float halfAngleDegrees)
            => pixelHeight * .5f * Mathf.Tan(halfAngleDegrees * Mathf.Deg2Rad) /
               (Mathf.Tan(verticalFov * .5f * Mathf.Deg2Rad) * canvasScale);
        public static float ScreenRadius(float pixelHeight, float verticalFov, float canvasScale)
            => pixelHeight * .5f * Mathf.Tan(HalfAngleDegrees * Mathf.Deg2Rad) /
               (Mathf.Tan(verticalFov * .5f * Mathf.Deg2Rad) * canvasScale);
        public static Vector3 Direction(Vector3 aim, Vector2 disk, float halfAngleDegrees = HalfAngleDegrees)
        {
            disk = Vector2.ClampMagnitude(disk, 1);
            var rotation = Quaternion.LookRotation(aim.normalized);
            float radius = Mathf.Tan(halfAngleDegrees * Mathf.Deg2Rad);
            return (rotation * new Vector3(disk.x * radius, disk.y * radius, 1)).normalized;
        }
    }
}


