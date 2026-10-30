using UnityEngine;

namespace RailArena
{
    /// <summary>Visual charge state observes the shared weapon cooldown without changing it.</summary>
    public sealed class RailgunPresentation : MonoBehaviour
    {
        public const float ViewRecoilTravel = .055f;
        public Transform Muzzle;
        public Transform RightGrip;
        public Transform LeftGrip;
        public Transform Aim;
        public Transform Shoulder;
        public Transform[] Triggers = System.Array.Empty<Transform>();
        public Renderer[] RearSurfaces = System.Array.Empty<Renderer>();
        public Transform[] Actuators;
        public Renderer[] EnergySurfaces;
        private Vector3[] restPositions;
        private MaterialPropertyBlock properties;
        private float previous = float.NaN;
        private Quaternion[] triggerRest;
        private readonly System.Collections.Generic.List<Mesh> viewOpticMeshes = new();
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
                triggerRest = System.Array.ConvertAll(Triggers, t => t.localRotation);
            }
            previous = remaining;
            for (int i = 0; i < Triggers.Length; i++)
                Triggers[i].localRotation = triggerRest[i] * Quaternion.AngleAxis(-8 * remaining, Vector3.right);
            for (int i = 0; i < Actuators.Length; i++)
                Actuators[i].localPosition = restPositions[i] - Actuators[i].parent.InverseTransformVector(transform.forward * remaining * .025f);
            properties.SetColor(Emission, new Color(.015f, .65f, 1) * (.25f + 2.25f * (1 - remaining)));
            foreach (var surface in EnergySurfaces) surface.SetPropertyBlock(properties);
        }

        public void ConfigureFirstPerson()
        {
            foreach (var surface in RearSurfaces)
                surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            // The approved model uses a capped 0.14m optic tube and opaque glass for exterior views.
            // Open only those caps on an instance-owned mesh; retain its ceramic rims and outer walls.
            Vector3 axis = transform.InverseTransformPoint(Aim.position);
            foreach (var filter in GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.name.StartsWith("RF_07_", System.StringComparison.Ordinal)) continue;
                if (filter.name.EndsWith("Energy", System.StringComparison.Ordinal))
                {
                    filter.GetComponent<Renderer>().forceRenderingOff = true;
                    continue;
                }
                var source = filter.sharedMesh;
                var vertices = source.vertices;
                var mesh = Instantiate(source);
                mesh.name = "First-person open optic";
                for (int submesh = 0; submesh < source.subMeshCount; submesh++)
                {
                    var triangles = source.GetTriangles(submesh);
                    var kept = new System.Collections.Generic.List<int>(triangles.Length);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        bool cap = true;
                        for (int j = 0; j < 3; j++)
                        {
                            Vector3 point = transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i + j]])) - axis;
                            cap &= Mathf.Abs(Mathf.Abs(point.z) - .07f) < .0005f &&
                                new Vector2(point.x, point.y).sqrMagnitude < .0305f * .0305f;
                        }
                        if (!cap) { kept.Add(triangles[i]); kept.Add(triangles[i + 1]); kept.Add(triangles[i + 2]); }
                    }
                    mesh.SetTriangles(kept, submesh);
                }
                filter.sharedMesh = mesh;
                viewOpticMeshes.Add(mesh);
            }
        }

        private void OnDestroy()
        {
            foreach (var mesh in viewOpticMeshes)
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
