using System.Collections.Generic;
using RailArena.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace RailArena
{
    public sealed partial class ArenaSession : MonoBehaviour
    {
        public ArenaWorld World;
        public MovementTuning Tuning;
        public Material PlayerMaterial;
        public Material BotMaterial;
        public List<ArenaActor> Actors { get; } = new();
        public MatchState Match { get; private set; }
        public ArenaActor Human => Actors.Count > LocalPlayerId ? Actors[LocalPlayerId] : null;
        public int Difficulty { get; private set; } = 1;
        public BotSettings BotSettings { get; private set; }
        public int BotCount { get; private set; } = 3;
        public TargetScenario Scenario { get; private set; }
        public bool Paused { get; private set; }
        public bool Running => Match != null;
        public bool Practice { get; private set; }
        public bool AutomatedHuman { get; set; }
        public int VoidDeaths { get; private set; }
        public int ShotsFired => shotCount;
        public int Deaths => deathCount;
        public int PadLaunches => padCount;
        public List<string> VoidTraces { get; } = new();
        public string Feed { get; private set; } = "";
        public float Sensitivity = .12f;
        public Camera View { get; private set; }
        private ArenaHud hud;
        private float yaw;
        private float pitch;
        public RailFeedback Feedback { get; private set; }
        private int shotCount;
        private int deathCount;
        private int pickupCount;
        private int padCount;
        private Transform weapon;
        private RailgunPresentation weaponPresentation;
        private FirstPersonArms viewArms;
        private float recoil;
        private readonly FirstPersonWeaponMotion weaponMotion = new();
        private int weaponMotionLife;
        private Vector2 lookMotion;
        private Networking.HitboxHistory hitHistory;
        public int RewoundShots { get; private set; }
        public int RejectedShotTimes { get; private set; }

        private void Awake()
        {
            Time.fixedDeltaTime = 1f / CombatRules.TickRate;
            Application.runInBackground = true;
            Application.targetFrameRate = 144;
            Sensitivity = PlayerPrefs.GetFloat("sensitivity", .12f);
            AudioListener.volume = PlayerPrefs.GetFloat("volume", .65f);
            View = Camera.main;
            View.fieldOfView = 100;
            View.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Feedback = gameObject.AddComponent<RailFeedback>();
            hud = gameObject.AddComponent<ArenaHud>();
            hud.Session = this;
            CreateWeapon();
        }

        public void StartSession(bool practice, int bots, int difficulty, TargetScenario scenario)
        {
            DisconnectNetwork();
            StartSessionCore(practice, bots, difficulty, scenario);
        }

        private void StartSessionCore(bool practice, int bots, int difficulty, TargetScenario scenario)
        {
            var botTuning = Resources.Load<BotTuning>("BotTuning");
            if (botTuning == null) throw new System.InvalidOperationException("The bot tuning asset is missing.");
            BotSettings = botTuning.SettingsFor(Mathf.Clamp(difficulty, 0, 2));
            hitHistory?.Dispose();
            foreach (var actor in Actors) Destroy(actor.gameObject);
            Actors.Clear();
            Practice = practice;
            BotCount = Mathf.Clamp(bots, practice ? 0 : 1, 7);
            Difficulty = Mathf.Clamp(difficulty, 0, 2);
            Scenario = scenario;
            var ids = new List<int>();
            for (int i = 0; i <= BotCount; i++)
            {
                var actor = CreateActor(i);
                Actors.Add(actor);
                ids.Add(i);
                actor.Place(World.Spawns[i % World.Spawns.Length].position);
            }
            Match = new MatchState(ids, practice);
            if (viewArms != null) viewArms.Surface.sharedMaterial = Human.Presentation.BodyRenderers[0].sharedMaterial;
            hitHistory = new Networking.HitboxHistory(Actors);
            hitHistory.Capture(0);
            RewoundShots = RejectedShotTimes = 0;
            foreach (var pickup in World.Pickups) { pickup.AvailableTick = 0; pickup.SetAvailable(true); }
            Feed = "";
            Feedback?.ResetFeedback();
            shotCount = deathCount = pickupCount = padCount = 0;
            VoidDeaths = 0;
            VoidTraces.Clear();
            yaw = 90;
            pitch = 0;
            recoil = 0;
            weaponMotion.Reset(true);
            weaponMotionLife = Human.Life;
            SetPaused(false);
        }

        public void SetPaused(bool pause)
        {
            Paused = pause;
            bool playing = Running && !pause && !OnlineWaiting && Match.Phase != MatchPhase.Results;
            if (pause && IsOnline && Human != null) Human.Command = new MovementCommand { Yaw = yaw, Pitch = pitch };
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            hud?.RefreshMenu();
        }

        public void MainMenu()
        {
            Feedback?.ResetFeedback();
            hitHistory?.Dispose();
            hitHistory = null;
            DisconnectNetwork();
            Match = null;
            foreach (var actor in Actors) Destroy(actor.gameObject);
            Actors.Clear();
            weapon.gameObject.SetActive(false);
            SetPaused(false);
            View.transform.SetPositionAndRotation(new Vector3(39, 28, -43), Quaternion.Euler(25, -40, 0));
        }

        private void Update()
        {
            lookMotion = Vector2.zero;
            UpdateNetwork();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && Running) SetPaused(!Paused);
            if (!Running || OnlineWaiting || Paused || AutomatedHuman || Match.Phase == MatchPhase.Results) return;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var delta = mouse.delta.ReadValue();
                lookMotion = delta * Sensitivity;
                yaw += delta.x * Sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * Sensitivity, -89, 89);
            }
            if (Human.Alive)
            {
                var keyboard = Keyboard.current;
                float x = (keyboard?.dKey.isPressed == true ? 1 : 0) - (keyboard?.aKey.isPressed == true ? 1 : 0);
                float y = (keyboard?.wKey.isPressed == true ? 1 : 0) - (keyboard?.sKey.isPressed == true ? 1 : 0);
                Human.Command = new MovementCommand { Move = new Vector2(x, y), Yaw = yaw, Pitch = pitch,
                    Jump = keyboard?.spaceKey.isPressed == true, Fire = mouse?.leftButton.isPressed == true };
            }
            else Human.Command = new MovementCommand { Yaw = yaw, Pitch = pitch };
        }

        private void LateUpdate()
        {
            if (!Running) return;
            InterpolateOpponent();
            Vector3 eye = IsOnline && LocalPlayerId == 1 ? reconciliationView.Position(Human.Eye) : Human.Eye;
            View.transform.SetPositionAndRotation(eye, Quaternion.Euler(pitch, yaw, 0));
            reconciliationView.Decay(Time.unscaledDeltaTime);
            recoil = Mathf.MoveTowards(recoil, 0, Time.unscaledDeltaTime * 5);
            weaponPresentation.SetCharge((Human.NextShotTick - LocalSimulationTick) / (float)CombatRules.ShotIntervalTicks);
            if (!Human.Alive || Paused) weaponMotion.Reset(false);
            else
            {
                if (weaponMotionLife != Human.Life) weaponMotion.Reset(true);
                weaponMotionLife = Human.Life;
                weaponMotion.Step(Time.unscaledDeltaTime, Human.Velocity, Human.Grounded && Human.AirborneTicks == 0, lookMotion, recoil);
            }
            weapon.localPosition = FirstPersonWeaponMotion.RestPosition + weaponMotion.Offset - Vector3.forward * (recoil * RailgunPresentation.ViewRecoilTravel);
            weapon.localRotation = weaponMotion.Rotation;
            viewArms.SetRecoil(Human.Alive && !Paused ? recoil : 0);
            weapon.gameObject.SetActive(Human.Alive && !Paused);
        }

        private void FixedUpdate()
        {
            if (IsOnline) { OnlineFixedUpdate(); return; }
            AdvanceAuthority();
        }

        private void AdvanceAuthority()
        {
            if (!Running || (Paused && !IsOnline) || Match.Phase == MatchPhase.Results) return;
            Match.Advance();
            if (Match.Phase == MatchPhase.Results)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                hud.RefreshMenu();
                return;
            }
            int tick = Match.Tick;
            foreach (var actor in Actors)
            {
                if (!actor.Alive)
                {
                    if (tick >= actor.RespawnTick) Respawn(actor);
                    continue;
                }
                if (!Match.IsActive) continue;
                if (actor.Bot || (AutomatedHuman && (!IsOnline || actor == Human))) actor.Command = actor.Brain.Think(this, actor);
                MoveActor(actor, tick, true);
                if (actor.transform.position.y < -20)
                {
                    Match.RecordVoidDeath(actor.Id);
                    VoidDeaths++;
                    VoidTraces.Add($"tick={tick} actor={actor.DisplayName} lastGround={actor.LastGroundedPosition} velocity={actor.Velocity}");
                    Kill(actor, actor, "VOID");
                    continue;
                }
                foreach (var pickup in World.Pickups)
                {
                    if (tick >= pickup.AvailableTick && actor.Health < 100 &&
                        (actor.transform.position + Vector3.up - pickup.transform.position).sqrMagnitude < 3)
                    {
                        actor.Health = CombatRules.RestoreHealth(actor.Health);
                        pickup.AvailableTick = tick + CombatRules.PickupRespawnTicks;
                        pickup.SetAvailable(false);
                        pickupCount++;
                    }
                }
            }
            hitHistory.Capture(tick);
            foreach (var actor in Actors)
                if (Match.IsActive && actor.Command.Fire && CombatRules.CanFire(tick, actor.NextShotTick, actor.Alive)) Shoot(actor);
            foreach (var pickup in World.Pickups) if (tick >= pickup.AvailableTick) pickup.SetAvailable(true);
        }

        private void Shoot(ArenaActor actor)
        {
            actor.NextShotTick = Match.Tick + CombatRules.ShotIntervalTicks;
            actor.Presentation.Fired();
            shotCount++;
            Vector3 origin = actor.Eye;
            int viewedTick = IsOnline && actor.Id != LocalPlayerId ? actor.ViewedTick : Match.Tick;
            var hit = hitHistory.Trace(Match.Tick, viewedTick, actor,
                IsOnline && actor.Id != LocalPlayerId ? actor.ShotCommand : (MovementCommand?)null);
            if (!hit.ValidTime) RejectedShotTimes++;
            else if (viewedTick < Match.Tick) RewoundShots++;
            Vector3 end = hit.End;
            if (hit.Actor != null)
            {
                var victim = hit.Actor;
                victim.Health = CombatRules.ApplyDamage(victim.Health, hit.Region);
                victim.HitSerial++;
                victim.Presentation.Hit();
                if (victim == Human) Feedback.Damaged();
                if (actor == Human) Feedback.ConfirmHit(!victim.Alive);
                if (!victim.Alive)
                {
                    Match.RecordKill(actor.Id, victim.Id);
                    Kill(victim, actor, hit.Region.ToString().ToUpperInvariant());
                }
            }
            actor.ShotSerial++;
            actor.LastShotEnd = end;
            ShowShot(actor, origin, end);
            if (Match.Phase == MatchPhase.Results) { SetPaused(false); hud.RefreshMenu(); }
        }

        private void ShowShot(ArenaActor actor, Vector3 origin, Vector3 end)
        {
            var muzzle = actor == Human ? weaponPresentation.Muzzle.position : actor.Presentation.Muzzle.position;
            Feedback.Shot(muzzle, end, origin, actor == Human, (end - origin).sqrMagnitude < 179.9f * 179.9f);
            if (actor == Human) recoil = 1;
        }

        private void Kill(ArenaActor victim, ArenaActor killer, string reason)
        {
            victim.Health = 0;
            victim.RespawnTick = Match.Tick + CombatRules.RespawnTicks;
            victim.SetAlive(false);
            Feed = killer == victim ? $"{victim.DisplayName} fell into the void" : $"{killer.DisplayName}  >  {victim.DisplayName}   {reason}";
            deathCount++;
        }

        private void Respawn(ArenaActor actor)
        {
            Transform best = World.Spawns[0];
            float bestDistance = -1;
            foreach (var spawn in World.Spawns)
            {
                float nearest = float.MaxValue;
                foreach (var other in Actors)
                    if (other != actor && other.Alive) nearest = Mathf.Min(nearest, (other.transform.position - spawn.position).sqrMagnitude);
                if (nearest > bestDistance) { bestDistance = nearest; best = spawn; }
            }
            actor.Health = 100;
            actor.Life++;
            actor.SetAlive(true);
            actor.Place(best.position);
            actor.NextShotTick = Match.Tick;
            actor.Command = default;
            actor.Brain = new BotBrain(7291 + actor.Id * 917 + Match.Tick);
        }

        private ArenaActor CreateActor(int id)
        {
            var root = new GameObject(id == 0 ? "Player" : $"Bot {id}");
            root.layer = 9;
            var actor = root.AddComponent<ArenaActor>();
            actor.Id = id;
            actor.DisplayName = id == LocalPlayerId ? "YOU" : IsOnline ? (id == 0 ? "HOST" : "CHALLENGER") : new[] { "", "ION", "CINDER", "VECTOR", "HEX", "EMBER", "NOVA", "FLUX" }[id];
            actor.Bot = !IsOnline && id != 0;
            actor.Brain = new BotBrain(7291 + id * 917);
            actor.Controller = root.AddComponent<CharacterController>();
            actor.Controller.height = 1.8f;
            actor.Controller.radius = .36f;
            actor.Controller.center = Vector3.up * .9f;
            actor.Controller.stepOffset = .3f;
            actor.Controller.slopeLimit = 50;
            var prefab = Resources.Load<GameObject>("Characters/Contender");
            if (prefab == null) throw new System.InvalidOperationException("The animated contender prefab is missing.");
            actor.Model = Instantiate(prefab, root.transform, false).transform;
            foreach (var part in actor.Model.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = id == LocalPlayerId ? 10 : 8;
            actor.Presentation = actor.Model.GetComponent<ContenderPresentation>();
            actor.Presentation.Initialize(actor, this);
            actor.Head = actor.Presentation.Animator.GetBoneTransform(HumanBodyBones.Head);
            return actor;
        }

        private void CreateWeapon()
        {
            View.cullingMask &= ~(1 << 10);
            var prefab = Resources.Load<GameObject>("Weapons/Railgun");
            if (prefab == null) throw new System.InvalidOperationException("The railgun presentation prefab is missing.");
            weapon = Instantiate(prefab, View.transform, false).transform;
            weaponPresentation = weapon.GetComponent<RailgunPresentation>();
            weapon.GetComponent<LODGroup>().ForceLOD(0);
            viewArms = FirstPersonArms.CreateOnWeapon(weaponPresentation);
            foreach (var part in weapon.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 11;
            weapon.gameObject.SetActive(false);
        }

        public string Telemetry() => $"ticks={Match?.Tick} shots={shotCount} deaths={deathCount} pickups={pickupCount} pads={padCount}";
    }
}
