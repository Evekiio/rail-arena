using UnityEngine;
namespace RailArena
{
    public sealed class HealthPickup : MonoBehaviour
    {
        public int AvailableTick;
        public Renderer[] Visuals = System.Array.Empty<Renderer>();
        public HealthPickupVisual Presentation;
        public void SetAvailable(bool available)
        {
            foreach (var visual in Visuals) visual.enabled = available;
            if(Presentation!=null)Presentation.SetAvailable(available);
        }
    }
}
