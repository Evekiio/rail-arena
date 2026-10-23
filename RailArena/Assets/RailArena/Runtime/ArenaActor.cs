using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    public sealed class ArenaActor : MonoBehaviour
    {
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
        public Vector3 Eye => transform.position + Vector3.up * 1.58f;
        public Vector3 AimDirection => Quaternion.Euler(Command.Pitch, Command.Yaw, 0) * Vector3.forward;

        public void Move(MovementTuning tuning)
        {
            Velocity = ArenaMovement.Integrate(Velocity, Command, Grounded && AirborneTicks == 0, tuning, 1f / CombatRules.TickRate);
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
            GroundedOverride = null;
            transform.position = position;
            Velocity = Vector3.zero;
            Controller.enabled = true;
        }

        public void SetAlive(bool alive)
        {
            Controller.enabled = alive;
            Presentation.SetAlive(alive);
        }
    }

}
