using UnityEngine;

namespace RailArena
{
    /// <summary>Visual charge state observes the shared weapon cooldown without changing it.</summary>
    public sealed class RailgunPresentation : MonoBehaviour
    {
        public Transform Muzzle;
        public Transform[] Actuators;
        public Renderer[] EnergySurfaces;
        private Vector3[] restPositions;
        private MaterialPropertyBlock properties;
        private float previous = float.NaN;
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        private void Awake() => SetCharge(0);

        public void SetCharge(float remaining)
        {
            remaining = Mathf.Clamp01(remaining);
            if (remaining == previous) return;
            if (restPositions == null)
            {
                restPositions = new Vector3[Actuators.Length];
                for (int i = 0; i < Actuators.Length; i++) restPositions[i] = Actuators[i].localPosition;
                properties = new MaterialPropertyBlock();
            }
            previous = remaining;
            for (int i = 0; i < Actuators.Length; i++)
                Actuators[i].localPosition = restPositions[i] - Actuators[i].parent.InverseTransformVector(transform.forward * remaining * .025f);
            properties.SetColor(Emission, new Color(.015f, .65f, 1) * (.25f + 2.25f * (1 - remaining)));
            foreach (var surface in EnergySurfaces) surface.SetPropertyBlock(properties);
        }
    }
}
