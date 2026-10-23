using UnityEngine;

namespace RailArena
{
    /// <summary>Animation observes authority; root motion never drives the gameplay controller.</summary>
    public sealed class ContenderPresentation : MonoBehaviour
    {
        public HandArticulation Hands;
        public Animator Animator;
        public Transform Weapon;
        public Transform RightGrip;
        public Transform LeftGrip;
        public Transform Muzzle;
        public RailgunPresentation Railgun;
        public Renderer[] BodyRenderers;
        public Material[] Skins;
        private ArenaActor actor;
        private ArenaSession session;
        private float recoil;
        private bool grounded;
        private float bodyYaw;
        private float corpseVelocity;
        private float corpseGround;
        private Transform[] surfaceBones;
        private static readonly float[] SurfaceMargins = { .17f, .2f, .07f, .07f, .035f, .035f };
#if UNITY_EDITOR
        [System.NonSerialized] public bool ReviewPlayback;
#endif
        private static readonly int Speed = UnityEngine.Animator.StringToHash("Speed");
        private static readonly int Grounded = UnityEngine.Animator.StringToHash("Grounded");
        private static readonly int Vertical = UnityEngine.Animator.StringToHash("Vertical");
        private static readonly int Dead = UnityEngine.Animator.StringToHash("Dead");

        public void Initialize(ArenaActor owner, ArenaSession authority)
        {
            actor = owner;
            session = authority;
            // The controller rests one skin width above the deck; keep boot soles at deck height.
            transform.localPosition = Vector3.down * owner.Controller.skinWidth;
            bodyYaw = owner.Command.Yaw;
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var surfaceIds = new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightFoot, HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
            surfaceBones = System.Array.ConvertAll(surfaceIds, bone => Animator.GetBoneTransform(bone));
            foreach (var renderer in BodyRenderers)
                renderer.sharedMaterial = Skins[owner.Id % Skins.Length];
            foreach (var hitbox in GetComponentsInChildren<Hitbox>(true))
            {
                hitbox.Actor = owner;
                hitbox.gameObject.layer = owner.Id == authority.LocalPlayerId ? 10 : 8;
            }
        }

        private void Update()
        {
            if (actor == null) return;
#if UNITY_EDITOR
            if (ReviewPlayback) return;
#endif
            bool worldPaused = session.Paused && !session.IsOnline;
            Animator.speed = worldPaused ? 0 : 1;
            if (worldPaused) return;
            int chargeTick = actor == session.Human ? session.LocalSimulationTick : session.Match.Tick;
            Railgun.SetCharge((actor.NextShotTick - chargeTick) / (float)Simulation.CombatRules.ShotIntervalTicks);
            float speed = new Vector2(actor.Velocity.x, actor.Velocity.z).magnitude;
            Vector3 aimForward = Quaternion.Euler(0, actor.Command.Yaw, 0) * Vector3.forward;
            bool backwards = speed > .5f && Vector3.Dot(actor.Velocity, aimForward) < -speed * .2f;
            bool onGround = actor.Grounded && actor.AirborneTicks == 0;
            Animator.SetFloat(Speed, backwards ? -speed : speed, .09f, Time.deltaTime);
            Animator.SetFloat(Vertical, actor.Velocity.y);
            Animator.SetBool(Grounded, onGround);
            Animator.SetBool(Dead, !actor.Alive);
            if (onGround && !grounded && actor.Alive) Animator.SetTrigger("Land");
            grounded = onGround;
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 7);
            if (!actor.Alive) return;
            // Turn the legs toward travel while keeping the torso and weapon aimed at the opponent.
            float aimYaw = actor.Command.Yaw;
            float travelYaw = speed > .5f ? Mathf.Atan2(actor.Velocity.x, actor.Velocity.z) * Mathf.Rad2Deg : aimYaw;
            if (backwards) travelYaw += 180;
            float offset = Mathf.Clamp(Mathf.DeltaAngle(aimYaw, travelYaw), -65, 65);
            bodyYaw = Mathf.LerpAngle(bodyYaw, aimYaw + offset, 1 - Mathf.Exp(-Time.deltaTime * 16));
            transform.localRotation = Quaternion.Euler(0, bodyYaw, 0);
            Animator.SetFloat("Gait", Mathf.Clamp(speed / 5.5f, .85f, 1.8f));
        }

        private void OnAnimatorIK(int layer)
        {
            if (actor == null || !actor.Alive) return;
            Quaternion aim = Quaternion.Euler(Mathf.Clamp(actor.Command.Pitch, -65, 65), actor.Command.Yaw, 0);
            Vector3 forward = aim * Vector3.forward;
            Vector3 right = aim * Vector3.right;
            Weapon.SetPositionAndRotation(actor.transform.position + Vector3.up * 1.31f + forward * .42f + right * .13f - forward * recoil * .045f, aim);
            Animator.SetLookAtWeight(.85f, .45f, .9f, 0, .55f);
            Animator.SetLookAtPosition(actor.Eye + forward * 15);
            Hand(AvatarIKGoal.RightHand, RightGrip);
            Hand(AvatarIKGoal.LeftHand, LeftGrip);
        }

        private void LateUpdate()
        {
            if (actor != null) Hands.SetRecoil(actor.Alive ? recoil : 0);
            if (actor == null || actor.Alive) return;
            bool paused = session.Paused && !session.IsOnline;
#if UNITY_EDITOR
            if (ReviewPlayback) paused = false;
#endif
            if (paused) return;
            corpseVelocity -= session.Tuning.Gravity * Time.deltaTime;
            transform.position += Vector3.up * corpseVelocity * Time.deltaTime;
            float lowest = float.PositiveInfinity;
            for (int i = 0; i < surfaceBones.Length; i++)
                lowest = Mathf.Min(lowest, surfaceBones[i].position.y - SurfaceMargins[i]);
            if (lowest < corpseGround + .01f)
            {
                transform.position += Vector3.up * (corpseGround + .01f - lowest);
                corpseVelocity = 0;
            }
        }

        private void Hand(AvatarIKGoal hand, Transform grip)
        {
            Animator.SetIKPositionWeight(hand, 1);
            Animator.SetIKRotationWeight(hand, 1);
            Animator.SetIKPosition(hand, grip.position);
            Animator.SetIKRotation(hand, grip.rotation);
        }

        public void Fired() { recoil = 1; }
        public void Hit() { if (actor != null && actor.Alive) Animator.SetTrigger("Hit"); }

        public void SetAlive(bool alive)
        {
            Weapon.gameObject.SetActive(alive);
            foreach (var box in GetComponentsInChildren<Hitbox>(true)) box.GetComponent<Collider>().enabled = alive;
            if (alive)
            {
                transform.localPosition = Vector3.down * actor.Controller.skinWidth;
                Animator.Rebind();
                Animator.Update(0);
                Animator.SetBool(Dead, false);
                recoil = 0;
            }
            else
            {
                corpseVelocity = actor.Velocity.y;
                corpseGround = Physics.Raycast(actor.transform.position + Vector3.up * .2f, Vector3.down,
                    out var ground, 150, 1 << 0, QueryTriggerInteraction.Ignore) ? ground.point.y : float.NegativeInfinity;
                Animator.SetBool(Dead, true);
            }
        }
    }
}
