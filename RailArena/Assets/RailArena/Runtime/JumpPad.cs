using UnityEngine;
namespace RailArena
{
    public sealed class JumpPad : MonoBehaviour
    {
        public TraversalPresentation Presentation;
        public Vector3 Destination;
        public float FlightSeconds = 1.25f;
        public Vector3 Launch(Vector3 origin, float gravity, float flightMultiplier = 1)
        {
            float flight = FlightSeconds * flightMultiplier;
            Vector3 velocity = (Destination - origin) / flight;
            velocity.y += .5f * gravity * flight;
            return velocity;
        }
    }

}
