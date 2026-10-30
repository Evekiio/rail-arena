using System;
using UnityEngine;
using UnityEngine.VFX;

namespace RailArena.Validation
{
    /// <summary>Test-only bridge for evaluating traversal-triggered GPU bursts.</summary>
    public sealed class TraversalVfxPrototype : IDisposable
    {
        private TraversalPresentation presentation;
        private VisualEffect effect;
        public int BurstsRequested { get; private set; }

        public void Bind(TraversalPresentation source, VisualEffect target)
        {
            if (source == null || target == null || target.visualEffectAsset == null)
                throw new ArgumentException("A presentation and configured VFX graph are required.");
            Unbind();
            presentation = source;
            effect = target;
            effect.initialEventName = "DisabledAutoStart";
            effect.Reinit();
            presentation.Activated += OnActivated;
        }

        private void OnActivated(bool arriving)
        {
            if (presentation.Reduced || !effect.isActiveAndEnabled) return;
            effect.Play();
            BurstsRequested++;
        }

        public bool TrySetHorizontalSurface(Vector3 worldPoint, Vector3 worldNormal)
        {
            if (effect == null || !effect.HasVector3("CollisionPosition") ||
                Vector3.Dot(worldNormal.normalized, Vector3.up) < .999f ||
                Vector3.Dot(effect.transform.up, Vector3.up) < .999f) return false;
            effect.SetVector3("CollisionPosition", effect.transform.InverseTransformPoint(worldPoint));
            return true;
        }

        public bool TrySetSurface(Vector3 worldPoint, Vector3 worldNormal)
        {
            if (effect == null || !effect.HasVector3("CollisionPosition") || !effect.HasVector3("CollisionNormal") ||
                worldNormal.sqrMagnitude < .000001f) return false;
            var scale = effect.transform.lossyScale;
            if (scale.x <= 0 || Mathf.Abs(scale.x - scale.y) > .0001f || Mathf.Abs(scale.x - scale.z) > .0001f) return false;
            effect.SetVector3("CollisionPosition", effect.transform.InverseTransformPoint(worldPoint));
            effect.SetVector3("CollisionNormal", effect.transform.InverseTransformDirection(worldNormal.normalized).normalized);
            return true;
        }

        public bool TrySetOrientedBox(BoxCollider box)
        {
            if (effect == null || box == null || !box.enabled || box.isTrigger ||
                !effect.HasVector3("BoxCenter") || !effect.HasVector3("BoxSize") || !effect.HasVector3("BoxAngles")) return false;
            if (!TryDecomposeBox(box.transform.localToWorldMatrix, out var boxRotation, out var boxScale) ||
                !TryDecomposeBox(effect.transform.localToWorldMatrix, out var effectRotation, out var effectScale)) return false;
            if (Mathf.Abs(effectScale.x - effectScale.y) > .0001f || Mathf.Abs(effectScale.x - effectScale.z) > .0001f) return false;
            var size = Vector3.Scale(box.size, boxScale) / effectScale.x;
            if (size.x <= 0 || size.y <= 0 || size.z <= 0) return false;
            effect.SetVector3("BoxCenter", effect.transform.InverseTransformPoint(box.transform.TransformPoint(box.center)));
            effect.SetVector3("BoxSize", size);
            effect.SetVector3("BoxAngles", (Quaternion.Inverse(effectRotation) * boxRotation).eulerAngles);
            return true;
        }

        private static bool TryDecomposeBox(Matrix4x4 matrix, out Quaternion rotation, out Vector3 scale)
        {
            rotation = Quaternion.identity;
            var x = (Vector3)matrix.GetColumn(0); var y = (Vector3)matrix.GetColumn(1); var z = (Vector3)matrix.GetColumn(2);
            scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (scale.x < .0001f || scale.y < .0001f || scale.z < .0001f) return false;
            x /= scale.x; y /= scale.y; z /= scale.z;
            if (Mathf.Abs(Vector3.Dot(x, y)) > .0001f || Mathf.Abs(Vector3.Dot(x, z)) > .0001f ||
                Mathf.Abs(Vector3.Dot(y, z)) > .0001f || Vector3.Dot(Vector3.Cross(x, y), z) < .9999f) return false;
            rotation = Quaternion.LookRotation(z, y);
            return true;
        }
        public bool TrySetAxisAlignedBox(BoxCollider box)
        {
            if (effect == null || box == null || !box.enabled || box.isTrigger ||
                !effect.HasVector3("BoxCenter") || !effect.HasVector3("BoxSize")) return false;
            var colliderMatrix = box.transform.localToWorldMatrix;
            var effectMatrix = effect.transform.localToWorldMatrix;
            if (!IsAxisAligned(colliderMatrix) || !IsPositiveAxisAligned(effectMatrix)) return false;
            float scale = effectMatrix.m00;
            if (Mathf.Abs(scale - effectMatrix.m11) > .0001f || Mathf.Abs(scale - effectMatrix.m22) > .0001f) return false;
            var size = Vector3.Scale(box.size, new Vector3(Mathf.Abs(colliderMatrix.m00), Mathf.Abs(colliderMatrix.m11), Mathf.Abs(colliderMatrix.m22))) / scale;
            if (size.x <= 0 || size.y <= 0 || size.z <= 0) return false;
            effect.SetVector3("BoxCenter", effect.transform.InverseTransformPoint(box.transform.TransformPoint(box.center)));
            effect.SetVector3("BoxSize", size);
            return true;
        }

        public bool TrySetAxisAlignedBoxMesh(MeshCollider collider)
        {
            if (effect == null || collider == null || !collider.enabled || collider.isTrigger ||
                collider.sharedMesh == null || !collider.sharedMesh.isReadable ||
                !effect.HasVector3("BoxCenter") || !effect.HasVector3("BoxSize")) return false;
            var colliderMatrix = collider.transform.localToWorldMatrix;
            var effectMatrix = effect.transform.localToWorldMatrix;
            if (!IsAxisAligned(colliderMatrix) || !IsPositiveAxisAligned(effectMatrix)) return false;
            float scale = effectMatrix.m00;
            if (Mathf.Abs(scale - effectMatrix.m11) > .0001f || Mathf.Abs(scale - effectMatrix.m22) > .0001f) return false;
            var mesh = collider.sharedMesh; var vertices = mesh.vertices; var triangles = mesh.triangles; var bounds = mesh.bounds;
            if ((vertices.Length != 8 && vertices.Length != 36) || triangles.Length != 36 || bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0) return false;
            var cornerIds = new int[vertices.Length]; int seen = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                int corner = 0;
                for (int axis = 0; axis < 3; axis++)
                {
                    float tolerance = Mathf.Max(.00001f, bounds.size[axis] * .00001f);
                    if (Mathf.Abs(vertices[i][axis] - bounds.min[axis]) <= tolerance) continue;
                    if (Mathf.Abs(vertices[i][axis] - bounds.max[axis]) <= tolerance) { corner |= 1 << axis; continue; }
                    return false;
                }
                seen |= 1 << corner; cornerIds[i] = corner;
            }
            if (seen != 255) return false;
            var first = new int[6]; var second = new int[6];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                if (a < 0 || a >= vertices.Length || b < 0 || b >= vertices.Length || c < 0 || c >= vertices.Length || a == b || b == c || c == a) return false;
                int ca = cornerIds[a], cb = cornerIds[b], cc = cornerIds[c], face = -1;
                for (int axis = 0; axis < 3; axis++)
                {
                    int bit = 1 << axis;
                    if ((ca & bit) == (cb & bit) && (ca & bit) == (cc & bit))
                    { if (face != -1) return false; face = axis * 2 + ((ca & bit) == 0 ? 0 : 1); }
                }
                if (face < 0) return false;
                int mask = (1 << ca) | (1 << cb) | (1 << cc);
                if (first[face] == 0) first[face] = mask;
                else if (second[face] == 0) second[face] = mask;
                else return false;
            }
            for (int face = 0; face < 6; face++)
            {
                if (first[face] == 0 || second[face] == 0 || first[face] == second[face]) return false;
                int shared = first[face] & second[face]; int a = -1, b = -1;
                for (int corner = 0; corner < 8; corner++) if ((shared & (1 << corner)) != 0)
                { if (a < 0) a = corner; else if (b < 0) b = corner; else return false; }
                if (a < 0 || b < 0) return false;
                int difference = a ^ b;
                if (difference != 3 && difference != 5 && difference != 6) return false;
            }
            var size = Vector3.Scale(bounds.size, new Vector3(Mathf.Abs(colliderMatrix.m00), Mathf.Abs(colliderMatrix.m11), Mathf.Abs(colliderMatrix.m22))) / scale;
            effect.SetVector3("BoxCenter", effect.transform.InverseTransformPoint(collider.transform.TransformPoint(bounds.center)));
            effect.SetVector3("BoxSize", size);
            return true;
        }
        private static bool IsAxisAligned(Matrix4x4 m)
        {
            return Mathf.Abs(m.m00) > .0001f && Mathf.Abs(m.m11) > .0001f && Mathf.Abs(m.m22) > .0001f &&
                Mathf.Abs(m.m01) < .0001f && Mathf.Abs(m.m02) < .0001f &&
                Mathf.Abs(m.m10) < .0001f && Mathf.Abs(m.m12) < .0001f &&
                Mathf.Abs(m.m20) < .0001f && Mathf.Abs(m.m21) < .0001f;
        }
        private static bool IsPositiveAxisAligned(Matrix4x4 m)
        {
            return m.m00 > 0 && m.m11 > 0 && m.m22 > 0 &&
                Mathf.Abs(m.m01) < .0001f && Mathf.Abs(m.m02) < .0001f &&
                Mathf.Abs(m.m10) < .0001f && Mathf.Abs(m.m12) < .0001f &&
                Mathf.Abs(m.m20) < .0001f && Mathf.Abs(m.m21) < .0001f;
        }
        public void Unbind()
        {
            if (presentation != null) presentation.Activated -= OnActivated;
            presentation = null;
            effect = null;
        }

        public void Dispose() => Unbind();
    }
}





