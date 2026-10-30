using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena
{
    /// <summary>Cosmetic shot pool and local confirmation. Never traces damage or changes simulation.</summary>
    public sealed class RailFeedback : MonoBehaviour
    {
        public const int Capacity = 12;
        public const float BeamSeconds = .24f;
        public const float PickupSeconds = 1.25f;
        public ComfortPreferences Preferences { get; internal set; } = new();
        public SoundPreferences Sound { get; internal set; } = new();
        private sealed class Pulse
        {
            public GameObject Root;
            public LineRenderer Core, Halo, Muzzle, Impact;
            public AudioSource Audio;
            public double Started;
            public bool HasImpact;
        }
        private readonly Pulse[] pulses = new Pulse[Capacity];
        private readonly AudioClip[] clips = new AudioClip[System.Enum.GetValues(typeof(RailCue)).Length];
        private Material material;
        private AudioSource localAudio;
        private int next;
        private double hitUntil, damageUntil, pickupUntil;
        public bool LastHitWasKill { get; private set; }
        public int ConfirmedHits { get; private set; }
        public int DamageCues { get; private set; }
        public int PickupCues { get; private set; }
        public int LastPickupAmount { get; private set; }
        public float PickupStrength => Mathf.Clamp01((float)((pickupUntil-Time.realtimeSinceStartupAsDouble)/.25));
        public int LaunchCues { get; private set; }
        public float HitStrength => Mathf.Clamp01((float)((hitUntil - Time.realtimeSinceStartupAsDouble) / .22));
        public float DamageStrength => Mathf.Clamp01((float)((damageUntil - Time.realtimeSinceStartupAsDouble) / .3));

        private void Awake()
        {
            material = new Material(Resources.Load<Shader>("Effects/RailPulse")) { name = "Runtime rail pulse" };
            for (int i = 0; i < clips.Length; i++) clips[i] = RailAudio.Create((RailCue)i);
            localAudio = gameObject.AddComponent<AudioSource>();
            localAudio.playOnAwake = false; localAudio.spatialBlend = 0;
            for (int i = 0; i < pulses.Length; i++)
            {
                var root = new GameObject("Rail pulse " + i); root.transform.SetParent(transform, false);
                var pulse = new Pulse { Root = root };
                pulse.Core = Line(root.transform, "Core", 2);
                pulse.Halo = Line(root.transform, "Halo", 2);
                pulse.Muzzle = Line(root.transform, "Muzzle", 9);
                pulse.Impact = Line(root.transform, "Impact", 13);
                pulse.Audio = root.AddComponent<AudioSource>();
                pulse.Audio.playOnAwake = false; pulse.Audio.spatialBlend = 1;
                pulse.Audio.minDistance = 6; pulse.Audio.maxDistance = 75;
                pulse.Audio.rolloffMode = AudioRolloffMode.Linear; pulse.Audio.dopplerLevel = 0;
                root.SetActive(false); pulses[i] = pulse;
            }
            ApplySound();
        }

        public void ApplySound()
        {
            localAudio.volume = Sound.Effects;
            foreach (var pulse in pulses) pulse.Audio.volume = .5f * Sound.Effects;
        }

        public void PreviewShot() => localAudio.PlayOneShot(clips[(int)RailCue.Shot], .6f);

        private LineRenderer Line(Transform parent, string name, int count)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.positionCount = count;
            line.useWorldSpace = true; line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off; line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return line;
        }

        public void Shot(Vector3 muzzle, Vector3 end, Vector3 soundOrigin, bool local, bool impact)
        {
            var pulse = pulses[next]; next = (next + 1) % pulses.Length;
            pulse.Audio.Stop(); pulse.Root.SetActive(true);
            pulse.Root.transform.position = soundOrigin;
            pulse.Started = Time.realtimeSinceStartupAsDouble; pulse.HasImpact = impact;
            pulse.Core.SetPosition(0, muzzle); pulse.Core.SetPosition(1, end);
            pulse.Halo.SetPosition(0, muzzle); pulse.Halo.SetPosition(1, end);
            Vector3 direction = (end - muzzle).normalized;
            Vector3 right = Vector3.Cross(direction, Mathf.Abs(direction.y) > .9f ? Vector3.right : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, direction);
            Star(pulse.Muzzle, muzzle, right, up, .13f, 4);
            Star(pulse.Impact, end - direction * .025f, right, up, .18f, 6);
            if (local) localAudio.PlayOneShot(clips[(int)RailCue.Shot], .6f);
            else { pulse.Audio.clip = clips[(int)RailCue.Shot]; pulse.Audio.Play(); }
            Animate(pulse, 0);
        }

        private static void Star(LineRenderer line, Vector3 center, Vector3 right, Vector3 up, float radius, int spokes)
        {
            line.SetPosition(0, center);
            for (int i = 0; i < spokes; i++)
            {
                float angle = i * (2 * Mathf.PI / spokes);
                line.SetPosition(i * 2 + 1, center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
                line.SetPosition(i * 2 + 2, center);
            }
        }

        private static void Appearance(LineRenderer line, float width, Color color, bool visible)
        {
            line.enabled = visible;
            line.startWidth = width; line.endWidth = width * .65f;
            line.startColor = line.endColor = color;
        }

        private void Animate(Pulse pulse, float age)
        {
            if (Preferences.ReducedFlash)
            {
                float reducedFade = Mathf.Clamp01(1 - age / .14f);
                Appearance(pulse.Core, .022f, new Color(.35f,.65f,.75f,reducedFade*.65f), reducedFade > 0);
                Appearance(pulse.Halo, .13f, Color.clear, false);
                Appearance(pulse.Muzzle, .025f, Color.clear, false);
                Appearance(pulse.Impact, .014f, new Color(.65f,.45f,.2f,reducedFade*.7f), pulse.HasImpact && age < .12f);
                return;
            }
            float fade = Mathf.Clamp01(1 - age / BeamSeconds);
            Appearance(pulse.Core, .032f * (.4f + fade * .6f), new Color(1.6f, 2.5f, 3, fade), fade > 0);
            Appearance(pulse.Halo, .13f, new Color(.03f, .55f, 1.1f, fade * fade * .3f), fade > 0);
            Appearance(pulse.Muzzle, .025f, new Color(.4f, 1.4f, 2, fade), age < .065f);
            Appearance(pulse.Impact, .018f, new Color(1.6f, .85f, .25f, fade), pulse.HasImpact && age < .16f);
        }

        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            foreach (var pulse in pulses)
            {
                if (!pulse.Root.activeSelf) continue;
                float age = (float)(now - pulse.Started);
                Animate(pulse, age);
                if (age >= .6f) pulse.Root.SetActive(false);
            }
        }

        public void ConfirmHit(bool killed)
        {
            ConfirmedHits++; LastHitWasKill = killed;
            hitUntil = Time.realtimeSinceStartupAsDouble + .22;
            localAudio.PlayOneShot(clips[(int)(killed ? RailCue.Elimination : RailCue.Hit)], .35f);
        }

        public void Damaged()
        {
            pickupUntil = 0;
            DamageCues++; damageUntil = Time.realtimeSinceStartupAsDouble + .3;
            localAudio.PlayOneShot(clips[(int)RailCue.Hurt], .45f);
        }

        public void PickedUp(int restored)
        {
            if(restored<=0)return;
            LastPickupAmount=restored;
            pickupUntil=Time.realtimeSinceStartupAsDouble+PickupSeconds;
            PickupCues++;
            localAudio.PlayOneShot(clips[(int)RailCue.Pickup], .35f);
        }

        public void Launched(bool playSound = true)
        {
            LaunchCues++;
            if (playSound) localAudio.PlayOneShot(clips[(int)RailCue.Launch], .4f);
        }

        public void ResetFeedback()
        {
            hitUntil = damageUntil = pickupUntil = 0; LastPickupAmount = 0; ConfirmedHits = DamageCues = PickupCues = LaunchCues = 0; LastHitWasKill = false;
            localAudio.Stop();
            foreach (var pulse in pulses) { pulse.Audio.Stop(); pulse.Root.SetActive(false); }
        }

        private void OnDestroy()
        {
            if (material != null) Release(material);
            foreach (var clip in clips) if (clip != null) Release(clip);
        }

        private static void Release(Object resource)
        { if (Application.isPlaying) Destroy(resource); else DestroyImmediate(resource); }
    }
}
