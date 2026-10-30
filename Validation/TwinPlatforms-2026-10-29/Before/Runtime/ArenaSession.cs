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
        public int SelectedOfflineCharacter { get; private set; }
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
        public CombatFeed RecentCombat { get; } = new();
        public AimPreferences Aim { get; } = new();
        public ReticlePreferences Reticle { get; } = new();
        public ComfortPreferences Comfort { get; } = new();
        public FramePacingPreferences FramePacing { get; } = new();
        public SoundPreferences Sound { get; } = new();
        public float Sensitivity { get => Aim.Sensitivity; set => Aim.Sensitivity = value; }
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
        private readonly WeaponRecoil weaponRecoil = new();
        private readonly FirstPersonWeaponMotion weaponMotion = new();
        private int weaponMotionLife;
        private Vector2 lookMotion;
        private readonly WeaponTrigger trigger = new();
        private bool reloadRequested;
        private float reloadBlend;
        private readonly AimDownSights sights = new();
        public bool IsAiming { get; private set; }
        public float AimBlend => sights.Weight;
        private Vector3 sightLocal;
        private Networking.HitboxHistory hitHistory;
        public int RewoundShots { get; private set; }
        public int RejectedShotTimes { get; private set; }

        private void Awake()
        {
            Time.fixedDeltaTime = 1f / CombatRules.TickRate;
            Application.runInBackground = true;
            FramePacing.Load();
            FramePacing.Apply();
            Aim.Load();
            Reticle.Load();
            Comfort.Load();
            SelectedOfflineCharacter = OfflineRoster.NormalizeCharacter(PlayerPrefs.GetInt("offlineCharacter",0));
            Sound.Load();
            Sound.ApplyMaster();
            View = Camera.main;
            View.fieldOfView = Aim.FieldOfView;
            View.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Feedback = gameObject.AddComponent<RailFeedback>();
            Feedback.Preferences = Comfort;
            Feedback.Sound = Sound;
            Feedback.ApplySound();
            hud = gameObject.AddComponent<ArenaHud>();
            hud.Session = this;
            CreateWeapon();
        }

        public void StartSession(bool practice, int bots, int difficulty, TargetScenario scenario)
        {
            DisconnectNetwork();
            StartSessionCore(practice, bots, difficulty, scenario);
        }

        public void SelectOfflineCharacter(int character)
        {
            SelectedOfflineCharacter = OfflineRoster.NormalizeCharacter(character);
            PlayerPrefs.SetInt("offlineCharacter",SelectedOfflineCharacter);
            PlayerPrefs.Save();
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
            BotCount = Mathf.Clamp(bots, practice ? 0 : 1, CombatRules.MaximumBots);
            Difficulty = Mathf.Clamp(difficulty, 0, 2);
            Scenario = practice ? scenario : TargetScenario.Combat;
            var ids = new List<int>();
            for (int i = 0; i <= BotCount; i++)
            {
                var actor = CreateActor(i);
                Actors.Add(actor);
                ids.Add(i);
                actor.Place(World.Spawns[i % World.Spawns.Length].position);
            }
            Match = new MatchState(ids, practice);
            if (viewArms != null) ContenderPresentation.ApplyPalette(viewArms.Surface, Human.Presentation.BodyRenderers[0].sharedMaterial);
            hitHistory = new Networking.HitboxHistory(Actors);
            hitHistory.Capture(0);
            RewoundShots = RejectedShotTimes = 0;
            foreach (var pickup in World.Pickups) { pickup.AvailableTick = 0; pickup.SetAvailable(true); }
            Feed = "";
            RecentCombat.Clear();
            Feedback?.ResetFeedback();
            shotCount = deathCount = pickupCount = padCount = 0;
            VoidDeaths = 0;
            VoidTraces.Clear();
            yaw = 90;
            pitch = 0;
            ResetRecoil();
            ResetTrigger();
            reloadBlend = 0;
            ResetSights();
            weaponMotion.Reset(true);
            weaponMotionLife = Human.Life;
            SetPaused(false);
        }

        public void SetPaused(bool pause)
        {
            Paused = pause;
            ResetTrigger();
            reloadBlend = 0;
            ResetSights();
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
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && !hud.TryHandleBack() && Running) SetPaused(!Paused);
            if (!Running || OnlineWaiting || Paused || AutomatedHuman || Match.Phase == MatchPhase.Results)
            { ResetTrigger(); ResetSights(); return; }
            var mouse = Mouse.current;
            IsAiming = Human.Alive && mouse?.rightButton.isPressed == true;
            bool zoom = IsAiming;
            if (mouse != null)
            {
                var delta = Aim.LookDelta(mouse.delta.ReadValue(), zoom);
                lookMotion = delta;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, -89, 89);
            }
            if (Human.Alive)
            {
                var keyboard = Keyboard.current;
                float x = (keyboard?.dKey.isPressed == true ? 1 : 0) - (keyboard?.aKey.isPressed == true ? 1 : 0);
                float y = (keyboard?.wKey.isPressed == true ? 1 : 0) - (keyboard?.sKey.isPressed == true ? 1 : 0);
                trigger.Sample(mouse?.leftButton.isPressed == true, mouse?.leftButton.wasPressedThisFrame == true, Match.IsActive);
                reloadRequested |= Match.IsActive && keyboard?.rKey.wasPressedThisFrame == true;
                Human.Command = new MovementCommand { Move = new Vector2(x, y), Yaw = yaw, Pitch = pitch,
                    Jump = keyboard?.spaceKey.isPressed == true, Fire = false,
                    Sprint = keyboard?.leftShiftKey.isPressed == true,
                    Walk = keyboard?.leftAltKey.isPressed == true,
                    Crouch = keyboard?.leftCtrlKey.isPressed == true };
            }
            else { ResetTrigger(); Human.Command = new MovementCommand { Yaw = yaw, Pitch = pitch }; }
        }

        private void LateUpdate()
        {
            if (!Running) return;
            InterpolateOpponent();
            if (!Human.Alive) ResetSights();
            sights.Step(IsAiming && Human.Alive && !Human.Magazine.Reloading && !Paused && !OnlineWaiting && Match.Phase != MatchPhase.Results, Time.unscaledDeltaTime);
            View.fieldOfView = Mathf.Lerp(Aim.FieldOfView, Aim.ViewFov(true), sights.Weight);
            Vector3 eye = IsOnline && LocalPlayerId == 1 ? reconciliationView.Position(Human.Eye) : Human.Eye;
            View.transform.SetPositionAndRotation(eye, Quaternion.Euler(pitch, yaw, 0));
            reconciliationView.Decay(Time.unscaledDeltaTime);
            if (!Human.Alive || Paused || !Comfort.WeaponMotion) ResetRecoil();
            else weaponRecoil.Step(Time.unscaledDeltaTime);
            recoil = weaponRecoil.Kick;
            weaponPresentation.SetCharge((Human.NextShotTick - LocalSimulationTick) / (float)Mathf.Max(1, Human.Magazine.LastShotInterval));
            if (!Human.Alive || Paused || !Comfort.WeaponMotion) weaponMotion.Reset(false);
            else
            {
                if (weaponMotionLife != Human.Life) weaponMotion.Reset(true);
                weaponMotionLife = Human.Life;
                weaponMotion.Step(Time.unscaledDeltaTime, Human.Velocity, Human.Grounded && Human.AirborneTicks == 0, lookMotion, 0);
            }
            float reloadTarget = Human.Alive && !Paused && Comfort.WeaponMotion && Human.Magazine.Reloading ? 1 : 0;
            reloadBlend = !Human.Alive || Paused || !Comfort.WeaponMotion ? 0 :
                Mathf.MoveTowards(reloadBlend, reloadTarget, Time.unscaledDeltaTime * 8);
            float visibleRecoil = Comfort.WeaponMotion ? recoil : 0;
            weapon.localPosition = sights.Position(sightLocal, weaponMotion.Offset, visibleRecoil)
                + (Comfort.WeaponMotion ? weaponRecoil.Offset * (1 - sights.Weight * .8f) : Vector3.zero)
                + new Vector3(0, -.08f, -.04f) * reloadBlend;
            weapon.localRotation = sights.Rotation(weaponMotion.Rotation) * (Comfort.WeaponMotion ?
                Quaternion.Slerp(weaponRecoil.Rotation, Quaternion.identity, sights.Weight * .8f) : Quaternion.identity) * Quaternion.Euler(12 * reloadBlend, 0, -10 * reloadBlend);
            viewArms.SetRecoil(Human.Alive && !Paused ? visibleRecoil : 0);
            weapon.gameObject.SetActive(Human.Alive && !Paused);
        }

        private void FixedUpdate()
        {
            if (Running && Human != null && !AutomatedHuman)
            {
                var command = Human.Command;
                command.Fire = trigger.Consume();
                command.Reload = reloadRequested; reloadRequested = false;
                Human.Command = command;

            }
            if (IsOnline) { OnlineFixedUpdate(); return; }
            AdvanceAuthority();
        }

        private void ResetSights()
        {
            IsAiming = false;
            sights.Reset();
            if (View != null) View.fieldOfView = Aim.FieldOfView;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (AutomatedHuman && actor == Human) actor.Command = ValidationMovement(actor.Command, tick);
#endif
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
                        int previousHealth=actor.Health;
                        actor.Health = CombatRules.RestoreHealth(actor.Health);
                        pickup.AvailableTick = tick + CombatRules.PickupRespawnTicks;
                        pickup.SetAvailable(false);
                        pickupCount++;
                        if(actor==Human)Feedback.PickedUp(actor.Health-previousHealth);
                    }
                }
            }
            hitHistory.Capture(tick);
            foreach (var actor in Actors)
                if (AdvanceWeapon(actor, tick)) Shoot(actor);
            foreach (var pickup in World.Pickups) if (tick >= pickup.AvailableTick) pickup.SetAvailable(true);
        }

        private void Shoot(ArenaActor actor)
        {
            actor.Presentation.Fired();
            shotCount++;
            Vector3 origin = actor.Eye;
            int viewedTick = IsOnline && actor.Id != LocalPlayerId ? actor.ViewedTick : Match.Tick;
            var hit = hitHistory.Trace(Match.Tick, viewedTick, actor,
                IsOnline && actor.Id != LocalPlayerId ? actor.ShotCommand : (MovementCommand?)null);
            if (!hit.ValidTime) RejectedShotTimes++;
            else if (viewedTick < Match.Tick) RewoundShots++;
            Vector3 end = hit.End;
            Match.RecordShot(actor.Id,hit.Actor == null ? (HitRegion?)null : hit.Region);
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

        private void ResetTrigger() { trigger.Reset(); reloadRequested = false; }

        private static bool AdvanceWeapon(ArenaActor actor, int tick)
        {
            actor.Magazine.Advance(tick, actor.Command.Reload);
            return actor.Command.Fire && actor.Magazine.TryFire(tick, ref actor.NextShotTick);
        }

        private void ResetRecoil() { recoil = 0; weaponRecoil.Reset(); }

        private void ShowShot(ArenaActor actor, Vector3 origin, Vector3 end)
        {
            var muzzle = actor == Human ? weaponPresentation.Muzzle.position : actor.Presentation.Muzzle.position;
            Feedback.Shot(muzzle, end, origin, actor == Human, (end - origin).sqrMagnitude < 179.9f * 179.9f);
            if (actor == Human) weaponRecoil.Fire();
        }

        private void Kill(ArenaActor victim, ArenaActor killer, string reason)
        {
            victim.Health = 0;
            victim.RespawnTick = Match.Tick + CombatRules.RespawnTicks;
            victim.SetAlive(false);
            if (victim == Human) ResetRecoil();
            PublishFeed(killer == victim ? $"{victim.DisplayName} fell into the void" : $"{killer.DisplayName}  >  {victim.DisplayName}   {reason}");
            deathCount++;
        }

        private void PublishFeed(string text)
        {
            Feed=text;
            RecentCombat.Add(text,Time.realtimeSinceStartupAsDouble);
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
            actor.Magazine = RifleMagazine.Full;
            if (actor == Human) ResetRecoil();
            actor.Command = default;
            actor.Brain = new BotBrain(7291 + actor.Id * 917 + Match.Tick);
        }

        private ArenaActor CreateActor(int id)
        {
            var root = new GameObject(id == 0 ? "Player" : $"Bot {id}");
            root.layer = 9;
            var actor = root.AddComponent<ArenaActor>();
            actor.Id = id;
            actor.CharacterId = IsOnline ? id : OfflineRoster.CharacterForActor(SelectedOfflineCharacter,id);
            actor.DisplayName = id == LocalPlayerId ? "YOU" : IsOnline ? (id == 0 ? "HOST" : "CHALLENGER") : OfflineRoster.BotName(actor.CharacterId);
            actor.Bot = !IsOnline && id != 0;
            actor.Brain = new BotBrain(7291 + id * 917);
            actor.Controller = root.AddComponent<CharacterController>();
            actor.Controller.height = 1.8f;
            actor.Controller.radius = .36f;
            actor.Controller.center = Vector3.up * .9f;
            actor.Controller.stepOffset = .3f;
            actor.Controller.slopeLimit = 50;
            string characterPath = OfflineRoster.PrefabPath(actor.CharacterId);
            var prefab = Resources.Load<GameObject>(characterPath);
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
            if (weaponPresentation.Aim == null) throw new System.InvalidOperationException("The rifle sight anchor is missing.");
            sightLocal = weapon.InverseTransformPoint(weaponPresentation.Aim.position);
            weapon.GetComponent<LODGroup>().ForceLOD(0);
            viewArms = FirstPersonArms.CreateOnWeapon(weaponPresentation);
            foreach (var part in weapon.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 11;
            weapon.gameObject.SetActive(false);
        }

        public string Telemetry() => $"ticks={Match?.Tick} shots={shotCount} deaths={deathCount} pickups={pickupCount} pads={padCount}";
    }
}
