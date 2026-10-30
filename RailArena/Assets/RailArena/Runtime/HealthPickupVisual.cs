using UnityEngine;

namespace RailArena
{
    /// <summary>Local art state only. Pickup authority and timers remain on HealthPickup.</summary>
    public sealed class HealthPickupVisual : MonoBehaviour
    {
        public Renderer Marker, Status;
        public Transform FloatingRoot;
        public Light Glow;
        public float AnimationTime { get; private set; }
        private ArenaSession session;
        private Vector3 restPosition;
        private void Awake()
        {
            session = FindFirstObjectByType<ArenaSession>();
            if (FloatingRoot != null) restPosition = FloatingRoot.localPosition;
        }
        private void Update()
        {
            if (FloatingRoot == null || !Available || (session != null && session.Running && session.Paused && !session.IsOnline)) return;
            AnimationTime += Mathf.Min(Time.deltaTime, .1f);
            FloatingRoot.localPosition = restPosition + Vector3.up * (Mathf.Sin(AnimationTime * 1.8f) * .14f);
            FloatingRoot.localRotation = Quaternion.Euler(0, AnimationTime * 38, 0);
            if (Glow != null) Glow.intensity = session != null && session.Comfort.ReducedFlash ? .45f : .8f;
        }
        public Material AvailableMaterial, DepletedMaterial;
        public bool Available { get; private set; } = true;
        private bool initialized;

        public void SetAvailable(bool available)
        {
            if(initialized && Available==available)return;
            initialized=true; Available=available;
            Marker.enabled=available;
            if (FloatingRoot != null) FloatingRoot.gameObject.SetActive(available);
            if (Glow != null) Glow.enabled=available;
            Status.sharedMaterial=available?AvailableMaterial:DepletedMaterial;
        }
    }
}
