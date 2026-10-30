using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    public sealed class ArenaActor : MonoBehaviour
    {
        public const float StandingHeight = 1.8f, CrouchingHeight = 1.2f;
        public const float StandingEyeHeight = 1.58f, CrouchingEyeHeight = .98f;
        public bool IsCrouched { get; private set; }
        public SlideState Slide;
        public bool IsSliding => Slide.Active;
        private readonly Collider[] standingOverlaps = new Collider[16];
        public int Id;
        public int CharacterId;
        public string DisplayName;
        public bool Bot;
        public int Health = 100;
        public int NextShotTick;
        public int RespawnTick;
        public int PadReadyTick;
        public int AirborneTicks;
        public Vector3 Velocity;
        public Vector3 LastGroundedPosition;
        public MovementCommand Command;
        public CharacterController Controller;
        public Transform Model;
        public Transform Head;
        public BotBrain Brain;
        public ContenderPresentation Presentation;
        public bool? GroundedOverride;
        public bool Grounded => GroundedOverride ?? (Controller.enabled && Controller.isGrounded);
        public int ShotSerial;
        public int HitSerial;
        public int Life = 1;
        public int ViewedTick;
        public MovementCommand ShotCommand;
        public Vector3 LastShotEnd;
        public bool Alive => Health > 0;
        public Vector3 Eye => transform.position + Vector3.up * (IsCrouched ? CrouchingEyeHeight : StandingEyeHeight);
        public Vector3 AimDirection => Quaternion.Euler(Command.Pitch, Command.Yaw, 0) * Vector3.forward;

        public void Move(MovementTuning tuning)
        {
            bool grounded = Grounded && AirborneTicks == 0;
            bool sliding = SlideMovement.Step(ref Slide, Velocity, Command, grounded);
            bool crouch = sliding || Command.Crouch;
            if (crouch || !IsCrouched || CanStand()) SetCrouched(crouch);
            var movement = Command;
            movement.Crouch = IsCrouched;
            Velocity = sliding ? SlideMovement.Integrate(Velocity, movement, tuning, 1f / CombatRules.TickRate) :
                ArenaMovement.Integrate(Velocity, movement, grounded, tuning, 1f / CombatRules.TickRate);
            GroundedOverride = null;
            if (AirborneTicks > 0) AirborneTicks--;
            var flags = Controller.Move(Velocity / CombatRules.TickRate);
            if ((flags & CollisionFlags.Below) != 0) LastGroundedPosition = transform.position;
            if ((flags & CollisionFlags.Above) != 0 && Velocity.y > 0) Velocity.y = 0;
            if ((flags & CollisionFlags.Below) != 0 && Velocity.y < 0) Velocity.y = -1;
        }

        public void Place(Vector3 position)
        {
            Controller.enabled = false;
            SetCrouched(false);
            Slide = default;
            GroundedOverride = null;
            transform.position = position;
            Velocity = Vector3.zero;
            Controller.enabled = true;
        }

        public bool CanStand()
        {
            float radius = Controller.radius - .02f;
            Vector3 bottom = transform.position + Vector3.up * (Controller.radius + .02f);
            Vector3 top = transform.position + Vector3.up * (StandingHeight - Controller.radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, standingOverlaps,
                (1 << 0) | (1 << 9), QueryTriggerInteraction.Ignore);
            if (count == standingOverlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (standingOverlaps[i] != Controller && !standingOverlaps[i].transform.IsChildOf(transform)) return false;
            return true;
        }

        // Authority snapshots bypass local clearance checks; input-driven attempts use CanStand.
        public void SetCrouched(bool crouched)
        {
            bool changed = IsCrouched != crouched;
            IsCrouched = crouched;
            Controller.height = crouched ? CrouchingHeight : StandingHeight;
            Controller.center = Vector3.up * (Controller.height * .5f);
            if (changed && Presentation != null) Presentation.ApplyStance();
        }

        public void SetAlive(bool alive)
        {
            if (!alive) Slide = default;
            Controller.enabled = alive;
            Presentation.SetAlive(alive);
        }
    }

}
