using UnityEngine;
using UnityEngine.Rendering;
namespace RailArena
{
    public enum TraversalEffectKind { Portal, SuperPad }
    /// <summary>Owned cosmetic resources only. No colliders, physics impulses, camera changes or gameplay timers.</summary>
    public sealed class TraversalPresentation : MonoBehaviour
    {
        public TraversalEffectKind Kind;
        public Renderer Surface;
        public Color Tint = Color.cyan;
        public int Activations { get; private set; }
        public float AmbientVolume => ambient != null ? ambient.volume : 0;
        public float EventVolume => events != null ? events.volume : 0;
        public float BurstStrength { get; private set; }
        public bool Reduced { get; private set; }
        public event System.Action<bool> Activated;
        private ArenaSession session;
        private Material energy, lineMaterial, previousMaterial;
        private AudioSource ambient, events;
        private AudioClip idleClip, firstClip, secondClip;
        private Transform visualRoot;
        private readonly LineRenderer[] rings = new LineRenderer[3];
        private readonly Vector3[][] ringPoints = { new Vector3[49], new Vector3[49], new Vector3[49] };
        private LineRenderer ripple;
        private readonly Vector3[] ripplePoints = new Vector3[65];
        private readonly LineRenderer[] sparks = new LineRenderer[12];
        private Vector3 restScale, restPosition;
        private bool animateCoil, audioPaused;
        private float clock, activationTime = -10, charge;
        private static readonly int TintId = Shader.PropertyToID("_Tint"), ClockId = Shader.PropertyToID("_Clock"),
            BurstId = Shader.PropertyToID("_Burst"), CalmId = Shader.PropertyToID("_Calm"), ModeId = Shader.PropertyToID("_Mode"), ChargeId = Shader.PropertyToID("_Charge");

        private void Awake()
        {
            session = FindFirstObjectByType<ArenaSession>();
            var shader = Kind == TraversalEffectKind.SuperPad ? Resources.Load<Shader>("Effects/PadEnergy") : null;
            if (shader == null || !shader.isSupported) shader = Resources.Load<Shader>("Effects/TraversalEnergy");
            energy = new Material(shader) { name = "Owned traversal energy" };
            energy.SetColor(TintId, Tint);
            if (energy.HasProperty(ModeId)) energy.SetFloat(ModeId, Kind == TraversalEffectKind.Portal ? 0 : 1);
            lineMaterial = new Material(Resources.Load<Shader>("Effects/RailPulse")) { name = "Owned traversal trails" };
            if (Surface != null)
            {
                previousMaterial = Surface.sharedMaterial; Surface.sharedMaterial = energy;
                restScale = Surface.transform.localScale; restPosition = Surface.transform.localPosition;
                animateCoil = Kind == TraversalEffectKind.SuperPad && Surface.GetComponent<Collider>() == null;
            }
            visualRoot = new GameObject("Traversal visual effects") { layer = 2 }.transform; visualRoot.SetParent(transform, false);
            for (int i = 0; i < 3; i++) rings[i] = Line("Energy arc " + i, 49);
            ripple = Line("Activation ripple", 65);
            for (int i = 0; i < sparks.Length; i++) sparks[i] = Line("Energy streak " + i, 2);
            idleClip = TraversalAudio.Create(Kind == TraversalEffectKind.Portal ? TraversalSound.PortalIdle : TraversalSound.PadIdle);
            firstClip = TraversalAudio.Create(Kind == TraversalEffectKind.Portal ? TraversalSound.PortalEnter : TraversalSound.PadLaunch);
            if (Kind == TraversalEffectKind.Portal) secondClip = TraversalAudio.Create(TraversalSound.PortalExit);
            ambient = Source(true); events = Source(false); ambient.clip = idleClip;
            UpdateSound(); ambient.Play(); Render();
        }
        private LineRenderer Line(string name, int count)
        {
            var root = new GameObject(name) { layer = 2 }; root.transform.SetParent(visualRoot, false);
            var line = root.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial; line.positionCount = count;
            line.useWorldSpace = false; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.numCapVertices = 2; line.widthMultiplier = .018f; return line;
        }
        private AudioSource Source(bool loop)
        {
            var source = visualRoot.gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = loop;
            source.spatialBlend = 1; source.dopplerLevel = 0; source.minDistance = 2;
            source.maxDistance = loop ? 18 : 42; source.rolloffMode = AudioRolloffMode.Linear; return source;
        }
        public bool Activate(bool arriving = false)
        {
            if (clock - activationTime < .18f) return false;
            activationTime = clock; Activations++; UpdateSound();
            events.PlayOneShot(arriving && secondClip != null ? secondClip : firstClip); Render(); Activated?.Invoke(arriving); return true;
        }
        public void ResetPulse()
        {
            activationTime = -10; Activations = 0; if (events != null) events.Stop();
            if (energy != null) Render();
        }
        private void Update()
        {
            bool paused = session != null && session.Running && session.Paused && !session.IsOnline;
            if (paused != audioPaused)
            {
                if (paused) { ambient.Pause(); events.Pause(); } else { ambient.UnPause(); events.UnPause(); }
                audioPaused = paused;
            }
            if (!paused) clock += Mathf.Min(Time.deltaTime, .1f);
            float target = 0;
            if (Kind == TraversalEffectKind.SuperPad && session != null && session.Running)
                foreach (var actor in session.Actors)
                    if (actor.Alive) target = Mathf.Max(target, 1 - Mathf.Clamp01(Vector3.Distance(actor.transform.position, transform.position) / 6));
            charge = Mathf.Lerp(charge, target, 1 - Mathf.Exp(-8 * Time.deltaTime));
            UpdateSound(); Render();
        }
        private void UpdateSound()
        {
            float effects = session != null ? session.Sound.Effects : 1;
            ambient.volume = effects * (Kind == TraversalEffectKind.Portal ? .055f : .04f) * (1 + charge);
            events.volume = effects * .55f;
        }
        private Vector3 Plane(float x, float y, float depth)
            => Kind == TraversalEffectKind.Portal ? new Vector3(x, (Surface != null ? Surface.transform.localPosition.y : 1.7f) + y, .055f + depth) : new Vector3(x, .14f + depth, y);
        private void Render()
        {
            Reduced = session != null && session.Comfort.ReducedFlash;
            float age = clock - activationTime;
            float burst = Mathf.Clamp01(1 - age / .7f);
            BurstStrength = Reduced ? burst * .22f : burst;
            energy.SetFloat(ClockId, clock * (Reduced ? .45f : 1)); energy.SetFloat(BurstId, BurstStrength);
            energy.SetFloat(CalmId, Reduced ? 1 : 0); energy.SetFloat(ChargeId, charge);
            float baseRadius = Kind == TraversalEffectKind.Portal ? (Surface != null ? Surface.transform.localScale.x * .43f : 1.2f) : 1.42f;
            for (int ring = 0; ring < rings.Length; ring++)
            {
                float radius = baseRadius * (1 - ring * .17f);
                float spin = clock * (ring % 2 == 0 ? .7f : -.55f) * (Reduced ? .4f : 1) + ring * 2.1f;
                for (int i = 0; i < 49; i++)
                {
                    float angle = spin + i / 48f * Mathf.PI * 1.55f;
                    ringPoints[ring][i] = Plane(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * (Kind == TraversalEffectKind.Portal ? (Surface != null ? Surface.transform.localScale.y / Surface.transform.localScale.x : 1.23f) : 1), .015f * ring);
                }
                rings[ring].SetPositions(ringPoints[ring]);
                Color color = Tint * (Reduced ? .8f : 1.7f); color.a = Reduced ? .28f : .42f + charge * .15f;
                rings[ring].startColor = rings[ring].endColor = color;
            }
            ripple.enabled = burst > 0;
            if (ripple.enabled)
            {
                float radius = .4f + (1 - burst) * 2.4f;
                for (int i = 0; i < 65; i++)
                { float angle = i / 64f * Mathf.PI * 2; ripplePoints[i] = Plane(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, .03f); }
                ripple.SetPositions(ripplePoints); Color color = Tint * 2; color.a = BurstStrength * (Reduced ? .3f : .7f);
                ripple.startColor = ripple.endColor = color;
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                bool visible = !Reduced && age >= 0 && age < .5f; sparks[i].enabled = visible; if (!visible) continue;
                float angle = i * (Mathf.PI * 2 / sparks.Length);
                Vector3 direction = Kind == TraversalEffectKind.Portal ? new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), .35f) :
                    new Vector3(Mathf.Cos(angle) * .5f, 1.4f, Mathf.Sin(angle) * .5f);
                Vector3 center = Kind == TraversalEffectKind.Portal ? new Vector3(0, Surface != null ? Surface.transform.localPosition.y : 1.7f, .08f) : Vector3.up * .18f;
                Vector3 head = center + direction * (age * 5 + .2f); sparks[i].SetPosition(0, head); sparks[i].SetPosition(1, head - direction * .22f);
                Color color = Tint * 2; color.a = 1 - age / .5f; sparks[i].startColor = sparks[i].endColor = color;
            }
            if (animateCoil)
            {
                float kick = age >= 0 && age < .4f ? Mathf.Sin(age / .4f * Mathf.PI * 2) * Mathf.Exp(-age * 7) : 0;
                Surface.transform.localPosition = restPosition + Vector3.up * (.045f * kick);
                Surface.transform.localScale = new Vector3(restScale.x, restScale.y * (1 - .5f * kick), restScale.z);
            }
        }
        private void OnDestroy()
        {
            if (Surface != null) { Surface.sharedMaterial = previousMaterial; if (animateCoil) { Surface.transform.localPosition = restPosition; Surface.transform.localScale = restScale; } }
            Release(energy); Release(lineMaterial); Release(idleClip); Release(firstClip); Release(secondClip);
            if (visualRoot != null) Release(visualRoot.gameObject);
        }
        private static void Release(Object value) { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
