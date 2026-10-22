using UnityEngine;
namespace RailArena
{
    public sealed class HealthPickup : MonoBehaviour
    {
        public int AvailableTick;
        public Renderer[] Visuals;
        public void SetAvailable(bool available)
        {
            foreach (var visual in Visuals) visual.enabled = available;
        }
    }
}
