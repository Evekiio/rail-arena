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
        private readonly WeaponRecoil recoilSpring = new();
        private bool grounded;
        private float bodyYaw;
        private float corpseVelocity;
        private float corpseGround;
        private float reactionWeight;
        private bool reactionSuppressed;
        private CorpseContactSurface corpseSurface;
        public int CorpseContactSamples => corpseSurface?.SampleCount ?? 0;
        private Transform[] surfaceBones;
        private CrouchPose crouchPose;
        private bool directionalMotion;
        private bool authoredCrouch;
        private Quaternion headAimOffset;
        private static readonly float[] SurfaceMargins = { .17f, .2f, .07f, .07f, .035f, .035f };
#if UNITY_EDITOR
        [System.NonSerialized] public bool ReviewPlayback;
#endif
        private static readonly int Speed = UnityEngine.Animator.StringToHash("Speed");
        private static readonly int Grounded = UnityEngine.Animator.StringToHash("Grounded");
        private static readonly int Vertical = UnityEngine.Animator.StringToHash("Vertical");
        private static readonly int Dead = UnityEngine.Animator.StringToHash("Dead");
        private static readonly int MotionX = UnityEngine.Animator.StringToHash("MotionX");
        private static readonly int MotionZ = UnityEngine.Animator.StringToHash("MotionZ");
        private static readonly int AuthoredCrouch = UnityEngine.Animator.StringToHash("AuthoredCrouch");

        public void SetMotionController(RuntimeAnimatorController controller)
        {
            crouchPose?.Restore();
            Animator.runtimeAnimatorController = controller;
            corpseSurface = null;
            directionalMotion = System.Array.Exists(Animator.parameters, item => item.nameHash == MotionX) &&
                System.Array.Exists(Animator.parameters, item => item.nameHash == MotionZ);
            authoredCrouch = System.Array.Exists(Animator.parameters, item => item.nameHash == AuthoredCrouch);
            if (reactionSuppressed && Animator.layerCount > 1) Animator.SetLayerWeight(1, 0);
        }

        public void RefreshCorpseSurface()
        {
            var skin = (SkinnedMeshRenderer)BodyRenderers[0];
            if (corpseSurface == null || corpseSurface.Skin != skin) corpseSurface = new CorpseContactSurface(skin);
        }

        public void Initialize(ArenaActor owner, ArenaSession authority)
        {
            actor = owner;
            session = authority;
            // The controller rests one skin width above the deck; keep boot soles at deck height.
            transform.localPosition = Vector3.down * owner.Controller.skinWidth;
            bodyYaw = owner.Command.Yaw;
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            crouchPose = new CrouchPose(Animator);
            var surfaceIds = new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightFoot, HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
            surfaceBones = System.Array.ConvertAll(surfaceIds, bone => Animator.GetBoneTransform(bone));
            var skin = (SkinnedMeshRenderer)BodyRenderers[0];
            int headIndex = System.Array.IndexOf(skin.bones, Animator.GetBoneTransform(HumanBodyBones.Head));
            var restHead = skin.transform.localToWorldMatrix * skin.sharedMesh.bindposes[headIndex].inverse;
            var localForward = restHead.inverse.MultiplyVector(transform.forward).normalized;
            var localUp = restHead.inverse.MultiplyVector(transform.up).normalized;
            headAimOffset = Quaternion.Inverse(Quaternion.LookRotation(localForward, localUp));
            ApplySkin(owner.CharacterId);
            foreach (var hitbox in GetComponentsInChildren<Hitbox>(true))
            {
                hitbox.Actor = owner;
                hitbox.gameObject.layer = owner.Id == authority.LocalPlayerId ? 10 : 8;
            }
        }

        public void ApplySkin(int index)
        {
            foreach (var renderer in BodyRenderers) ApplyPalette(renderer, Skins[index % Skins.Length]);
        }

        public static void ApplyPalette(Renderer renderer, Material palette)
        {
            renderer.sharedMaterial = palette;
            // Bind each atlas per renderer rather than relying on shared SRP material texture state.
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetTexture("_BaseMap", palette.GetTexture("_BaseMap"));
            properties.SetColor("_BaseColor", palette.GetColor("_BaseColor"));
            renderer.SetPropertyBlock(properties);
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
            Railgun.SetCharge((actor.NextShotTick - chargeTick) / (float)Mathf.Max(1, actor.Magazine.LastShotInterval));
            ApplyMotionState(Time.deltaTime);
            recoilSpring.Step(Time.deltaTime);
            recoil = recoilSpring.Kick;
        }

        /// <summary>Observe actual velocity and stance. Used by live animation and measured pose review.</summary>
        public void ApplyMotionState(float delta)
        {
            if (actor == null) return;
            crouchPose?.Restore();
            float speed = new Vector2(actor.Velocity.x, actor.Velocity.z).magnitude;
            Vector3 aimForward = Quaternion.Euler(0, actor.Command.Yaw, 0) * Vector3.forward;
            bool backwards = speed > .5f && Vector3.Dot(actor.Velocity, aimForward) < -speed * .2f;
            bool onGround = actor.Grounded && actor.AirborneTicks == 0;
            if (delta > 0) Animator.SetFloat(Speed, backwards ? -speed : speed, .09f, delta);
            else Animator.SetFloat(Speed, backwards ? -speed : speed);
            if (directionalMotion)
            {
                var local = Quaternion.Inverse(Quaternion.Euler(0, actor.Command.Yaw, 0)) * actor.Velocity;
                if (delta > 0) { Animator.SetFloat(MotionX, local.x, .09f, delta); Animator.SetFloat(MotionZ, local.z, .09f, delta); }
                else { Animator.SetFloat(MotionX, local.x); Animator.SetFloat(MotionZ, local.z); }
            }
            Animator.SetFloat(Vertical, actor.Velocity.y);
            Animator.SetBool(Grounded, onGround);
            Animator.SetBool(Dead, !actor.Alive);
            if(authoredCrouch)Animator.SetBool(AuthoredCrouch,GroundCrouch());
            if (onGround && !grounded && actor.Alive) Animator.SetTrigger("Land");
            grounded = onGround;
            if (!actor.Alive) return;
            // Directional art faces aim; the legacy controller turns its legs toward travel.
            float aimYaw = actor.Command.Yaw;
            float travelYaw = speed > .5f ? Mathf.Atan2(actor.Velocity.x, actor.Velocity.z) * Mathf.Rad2Deg : aimYaw;
            if (backwards) travelYaw += 180;
            float offset = Mathf.Clamp(Mathf.DeltaAngle(aimYaw, travelYaw), -65, 65);
            bodyYaw = Mathf.LerpAngle(bodyYaw, directionalMotion ? aimYaw : aimYaw + offset, delta > 0 ? 1 - Mathf.Exp(-delta * 16) : 1);
            transform.localRotation = Quaternion.Euler(0, bodyYaw, 0);
            Animator.SetFloat("Gait", Mathf.Clamp(speed / 5.5f, .85f, 1.8f));
        }

        private void OnAnimatorIK(int layer)
        {
            if (actor == null) return;
            if (!actor.Alive) { Animator.SetLookAtWeight(0); return; }
            Quaternion aim = Quaternion.Euler(Mathf.Clamp(actor.Command.Pitch, -65, 65), actor.Command.Yaw, 0);
            Vector3 forward = aim * Vector3.forward;
            Animator.SetLookAtWeight(.85f, .45f, .9f, 0, .55f);
            Animator.SetLookAtPosition(actor.Eye + forward * 15);
        }

        private void LateUpdate()
        {
            ApplyCombatPose();
            if (actor != null) Hands.SetRecoil(actor.Alive ? recoil : 0);
            if (actor == null || actor.Alive) return;
            bool paused = session.Paused && !session.IsOnline;
#if UNITY_EDITOR
            if (ReviewPlayback) paused = false;
#endif
            if (paused) return;
            ApplyCorpsePose(Time.deltaTime);
        }

        /// <summary>Cosmetic corpse floor support; never moves the authoritative actor.</summary>
        public void ApplyCorpsePose(float delta)
        {
            if (actor == null || actor.Alive) return;
            corpseVelocity -= session.Tuning.Gravity * delta;
            transform.position += Vector3.up * corpseVelocity * delta;
            float lowest = float.PositiveInfinity;
            if (corpseSurface != null) lowest = corpseSurface.MinimumWorldHeight();
            else for (int i = 0; i < surfaceBones.Length; i++)
                lowest = Mathf.Min(lowest, surfaceBones[i].position.y - SurfaceMargins[i]);
            if (lowest < corpseGround + .01f)
            {
                transform.position += Vector3.up * (corpseGround + .01f - lowest);
                corpseVelocity = 0;
            }
        }

        /// <summary>Shared live/review presentation pass; never writes the actor's authoritative movement.</summary>
        public void ApplyCombatPose()
        {
            if (actor == null || !actor.Alive) return;
            Quaternion aim = Quaternion.Euler(Mathf.Clamp(actor.Command.Pitch, -65, 65), actor.Command.Yaw, 0);
            ArmContactPose.AlignBody(Animator);
            ApplyStance();
            var chest = Animator.GetBoneTransform(HumanBodyBones.Chest);
            var head = Animator.GetBoneTransform(HumanBodyBones.Head);
            var leftArm = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var rightArm = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var chestAxis = Vector3.ProjectOnPlane(rightArm.position - leftArm.position, Vector3.up);
            var facing = Quaternion.FromToRotation(chestAxis, Quaternion.Euler(0, actor.Command.Yaw, 0) * Vector3.right);
            chest.rotation = Quaternion.AngleAxis(35, Vector3.up) * facing * chest.rotation;
            head.rotation = aim * headAimOffset;
            var shoulder = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var contact = shoulder.position + aim * new Vector3(.015f, .02f, -.04f);
            aim *= Quaternion.Euler(-2.5f * recoil, 0, 0);
            Weapon.SetPositionAndRotation(contact - aim * Railgun.Shoulder.localPosition - aim * Vector3.forward * recoil * .025f, aim);
            ArmContactPose.Apply(Animator, true, LeftGrip);
            ArmContactPose.Apply(Animator, false, RightGrip);
        }

        public void Fired() { recoilSpring.Fire(); }
        public void ApplyStance()
        {
            if (crouchPose == null) return;
            crouchPose.Restore();
            if(authoredCrouch&&actor.Alive)
            {
                bool desired=GroundCrouch();var state=Animator.GetCurrentAnimatorStateInfo(0);
                if(desired!=state.IsTag("AuthoredCrouch"))
                {
                    Animator.SetBool(AuthoredCrouch,desired);
                    Animator.SetBool(Grounded,actor.Grounded&&actor.AirborneTicks==0);
                    Animator.SetFloat(Vertical,actor.Velocity.y);Animator.SetBool(Dead,false);
                    string next=desired?"Crouch":actor.Grounded&&actor.AirborneTicks==0?"Locomotion":actor.Velocity.y>0?"Jump":"Air";
                    // Stance changes must affect the pose during the authoritative tick, before hit queries.
                    Animator.Play(next,0,Mathf.Repeat(state.normalizedTime,1));Animator.Update(0);
                }
                if(desired)return;
            }
            if (actor.Alive && actor.IsCrouched) crouchPose.Apply();
        }
        bool GroundCrouch()=>actor.Alive&&actor.IsCrouched&&actor.Grounded&&actor.AirborneTicks==0&&!actor.IsSliding;
        public void StanceChanged()
        {
            ApplyStance();
            if(authoredCrouch)ApplyCombatPose();
        }
        public void Hit() { if (actor != null && actor.Alive) Animator.SetTrigger("Hit"); }

        public void SetAlive(bool alive)
        {
            crouchPose?.Restore();
            Weapon.gameObject.SetActive(alive);
            foreach (var box in GetComponentsInChildren<Hitbox>(true)) box.GetComponent<Collider>().enabled = alive;
            if (alive)
            {
                transform.localPosition = Vector3.down * actor.Controller.skinWidth;
                Animator.Rebind();
                Animator.Update(0);
                Animator.SetBool(Dead, false);
                if (reactionSuppressed && Animator.layerCount > 1) Animator.SetLayerWeight(1, reactionWeight);
                reactionSuppressed = false;
                recoil = 0;
                recoilSpring.Reset();
                if (Hands != null) { Hands.enabled = true; Hands.Pose(0); }
            }
            else
            {
                if (Animator.layerCount > 1)
                {
                    if (!reactionSuppressed) reactionWeight = Animator.GetLayerWeight(1);
                    Animator.SetLayerWeight(1, 0); reactionSuppressed = true;
                }
                Animator.ResetTrigger("Hit");
                if (Hands != null) Hands.enabled = false;
                corpseVelocity = actor.Velocity.y;
                corpseGround = Physics.Raycast(actor.transform.position + Vector3.up * .2f, Vector3.down,
                    out var ground, 150, 1 << 0, QueryTriggerInteraction.Ignore) ? ground.point.y : float.NegativeInfinity;
                Animator.SetBool(Dead, true);
            }
        }
    }
}
