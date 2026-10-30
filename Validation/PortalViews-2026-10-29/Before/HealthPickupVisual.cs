using UnityEngine;

namespace RailArena
{
    /// <summary>Local art state only. Pickup authority and timers remain on HealthPickup.</summary>
    public sealed class HealthPickupVisual : MonoBehaviour
    {
        public Renderer Marker, Status;
        public Material AvailableMaterial, DepletedMaterial;
        public bool Available { get; private set; } = true;
        private bool initialized;

        public void SetAvailable(bool available)
        {
            if(initialized && Available==available)return;
            initialized=true; Available=available;
            Marker.enabled=available;
            Status.sharedMaterial=available?AvailableMaterial:DepletedMaterial;
        }
    }
}
