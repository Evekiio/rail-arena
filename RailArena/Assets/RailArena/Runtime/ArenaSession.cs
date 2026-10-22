using System.Collections.Generic;
using RailArena.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace RailArena
{
    public sealed class ArenaSession : MonoBehaviour
    {
        public ArenaWorld World;
        public MovementTuning Tuning;
        public Material PlayerMaterial;
        public Material BotMaterial;
        public Material RailMaterial;
        public List<ArenaActor> Actors { get; } = new();
        public MatchState Match { get; private set; }
        public ArenaActor Human => Actors.Count > 0 ? Actors[0] : null;
        public int Difficulty { get; private set; } = 1;
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
        private AudioSource audioSource;
        private AudioClip shotSound;
        private int shotCount;
        private int deathCount;
        private int pickupCount;
        private int padCount;
        private Transform weapon;
        private float recoil;

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
            audioSource = gameObject.AddComponent<AudioSource>();
            shotSound = CreateShotSound();
            hud = gameObject.AddComponent<ArenaHud>();
            hud.Session = this;
            CreateWeapon();
        }

        public void StartSession(bool practice, int bots, int difficulty, TargetScenario scenario)
        {
            foreach (var actor in Actors) Destroy(actor.gameObject);
            Actors.Clear();
            Practice = practice;
            BotCount = Mathf.Clamp(bots, 1, 7);
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
            foreach (var pickup in World.Pickups) { pickup.AvailableTick = 0; pickup.SetAvailable(true); }
            Feed = "";
            shotCount = deathCount = pickupCount = padCount = 0;
            VoidDeaths = 0;
            VoidTraces.Clear();
            yaw = 90;
            pitch = 0;
            SetPaused(false);
        }

        public void SetPaused(bool pause)
        {
            Paused = pause;
            bool playing = Running && !pause && Match.Phase != MatchPhase.Results;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            hud?.RefreshMenu();
        }

        public void MainMenu()
        {
            Match = null;
            foreach (var actor in Actors) Destroy(actor.gameObject);
            Actors.Clear();
            weapon.gameObject.SetActive(false);
            SetPaused(false);
            View.transform.SetPositionAndRotation(new Vector3(39, 28, -43), Quaternion.Euler(25, -40, 0));
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && Running) SetPaused(!Paused);
            if (!Running || Paused || AutomatedHuman || Match.Phase == MatchPhase.Results) return;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var delta = mouse.delta.ReadValue();
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
        }

        private void LateUpdate()
        {
            if (!Running) return;
            View.transform.SetPositionAndRotation(Human.Eye, Quaternion.Euler(pitch, yaw, 0));
            recoil = Mathf.MoveTowards(recoil, 0, Time.unscaledDeltaTime * 5);
            weapon.localPosition = new Vector3(.33f, -.3f, .62f - recoil * .15f);
            weapon.gameObject.SetActive(Human.Alive && !Paused);
        }

        private void FixedUpdate()
        {
            if (!Running || Paused || Match.Phase == MatchPhase.Results) return;
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
                if (actor.Bot || AutomatedHuman) actor.Command = actor.Brain.Think(this, actor);
                actor.Move(Tuning);
                if (actor.transform.position.y < -20)
                {
                    Match.RecordVoidDeath(actor.Id);
                    VoidDeaths++;
                    VoidTraces.Add($"tick={tick} actor={actor.DisplayName} lastGround={actor.LastGroundedPosition} velocity={actor.Velocity}");
                    Kill(actor, actor, "VOID");
                    continue;
                }
                foreach (var pad in World.Pads)
                {
                    Vector3 offset = actor.transform.position - pad.transform.position;
                    if (tick >= actor.PadReadyTick && Mathf.Abs(offset.y) < 1 && new Vector2(offset.x, offset.z).magnitude < 1.5f)
                    {
                        actor.Velocity = pad.Launch(actor.transform.position, Tuning.Gravity);
                        actor.AirborneTicks = 2;
                        if (actor.Bot || AutomatedHuman) actor.Brain.Launched(World.NearestNode(pad.Destination));
                        actor.PadReadyTick = tick + 90;
                        padCount++;
                    }
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
                if (actor.Command.Fire && CombatRules.CanFire(tick, actor.NextShotTick, actor.Alive)) Shoot(actor);
            }
            foreach (var pickup in World.Pickups) if (tick >= pickup.AvailableTick) pickup.SetAvailable(true);
        }

        private void Shoot(ArenaActor actor)
        {
            if (shotSound == null) shotSound = CreateShotSound();
            actor.NextShotTick = Match.Tick + CombatRules.ShotIntervalTicks;
            actor.Presentation.Fired();
            shotCount++;
            Vector3 origin = actor.Eye;
            Vector3 direction = actor.AimDirection;
            Vector3 end = origin + direction * 180;
            // Self is on a separate layer for the local camera, but rays explicitly skip every own hitbox.
            var hits = Physics.RaycastAll(origin, direction, 180, (1 << 0) | (1 << 8) | (1 << 10), QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var box = hit.collider.GetComponent<Hitbox>();
                if (box != null && (box.Actor == actor || !box.Actor.Alive)) continue;
                end = hit.point;
                if (box != null)
                {
                    box.Actor.Health = CombatRules.ApplyDamage(box.Actor.Health, box.Region);
                    box.Actor.Presentation.Hit();
                    if (!box.Actor.Alive)
                    {
                        Match.RecordKill(actor.Id, box.Actor.Id);
                        Kill(box.Actor, actor, box.Region.ToString().ToUpperInvariant());
                    }
                }
                break;
            }
            var effect = new GameObject("Rail discharge").AddComponent<LineRenderer>();
            effect.sharedMaterial = RailMaterial;
            effect.positionCount = 2;
            effect.SetPosition(0, actor.Bot ? actor.Presentation.Muzzle.position : weapon.TransformPoint(new Vector3(0, .12f, .5f)));
            effect.SetPosition(1, end);
            effect.startWidth = .065f;
            effect.endWidth = .025f;
            effect.numCapVertices = 4;
            Destroy(effect.gameObject, .18f);
            if (!actor.Bot) { recoil = 1; audioSource.PlayOneShot(shotSound, .7f); }
            else AudioSource.PlayClipAtPoint(shotSound, origin, .2f);
            if (Match.Phase == MatchPhase.Results) { SetPaused(false); hud.RefreshMenu(); }
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
            actor.DisplayName = id == 0 ? "YOU" : new[] { "", "ION", "CINDER", "VECTOR", "HEX", "EMBER", "NOVA", "FLUX" }[id];
            actor.Bot = id != 0;
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
            foreach (var part in actor.Model.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = actor.Bot ? 8 : 10;
            actor.Presentation = actor.Model.GetComponent<ContenderPresentation>();
            actor.Presentation.Initialize(actor, this);
            actor.Head = actor.Presentation.Animator.GetBoneTransform(HumanBodyBones.Head);
            return actor;
        }

        private void CreateWeapon()
        {
            View.cullingMask &= ~(1 << 10);
            weapon = new GameObject("Rail accelerator").transform;
            weapon.SetParent(View.transform, false);
            for (int i = 0; i < 4; i++)
            {
                var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(part.GetComponent<Collider>());
                part.transform.SetParent(weapon, false);
                part.transform.localScale = i == 0 ? new Vector3(.2f, .17f, .65f) : new Vector3(.035f, .04f, .8f);
                part.transform.localPosition = i == 0 ? Vector3.zero : new Vector3((i - 2) * .08f, .12f, .1f);
                part.GetComponent<Renderer>().sharedMaterial = i == 0 ? PlayerMaterial : RailMaterial;
                part.layer = 11;
            }
            weapon.gameObject.SetActive(false);
        }

        private static AudioClip CreateShotSound()
        {
            const int rate = 44100;
            var data = new float[rate / 3];
            var random = new System.Random(197);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Exp(-t * 22);
                data[i] = envelope * (.5f * Mathf.Sin(2 * Mathf.PI * (950 * t - 1000 * t * t)) +
                    .35f * ((float)random.NextDouble() * 2 - 1));
            }
            var clip = AudioClip.Create("Original rail discharge", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public string Telemetry() => $"ticks={Match?.Tick} shots={shotCount} deaths={deathCount} pickups={pickupCount} pads={padCount}";
    }
}
