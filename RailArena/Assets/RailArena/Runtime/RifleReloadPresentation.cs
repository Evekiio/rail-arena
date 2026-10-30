using System.Collections.Generic;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    /// <summary>Samples authored magazine/hand action at simulation time. Owns no combat or movement state.</summary>
    [DefaultExecutionOrder(20)]
    public sealed class RifleReloadPresentation : MonoBehaviour
    {
        struct Attachment { public Transform part, parent; public Vector3 position, weaponPosition; public Quaternion rotation, weaponRotation; public Vector3 scale; }
        readonly List<Attachment> attachments = new();
        ArenaSession session;
        ArenaActor actor;
        RailgunPresentation weapon;
        RifleReloadArt art;
        Transform carrier;
        Vector3 restGrip;
        Quaternion restGripRotation;
        bool posed;
        public bool Initialized => carrier != null;
        public float Progress { get; private set; }

        public void Initialize(RailgunPresentation target, ArenaSession authority, ArenaActor owner)
        {
            if (Initialized) return;
            art = Resources.Load<RifleReloadArt>("Art/RifleReload");
            if (art == null || art.samples.Length != RifleMagazine.ReloadTicks + 1)
                throw new System.InvalidOperationException("Authored rifle reload samples are missing");
            weapon = target; session = authority; actor = owner;
            restGrip = weapon.LeftGrip.localPosition; restGripRotation = weapon.LeftGrip.localRotation;
            carrier = new GameObject("Authored magazine carrier").transform;
            carrier.SetParent(weapon.transform, false); carrier.localPosition = art.magazinePivot;
            foreach (var part in weapon.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!part.name.StartsWith("RF_11_", System.StringComparison.Ordinal)) continue;
                var t = part.transform;
                attachments.Add(new Attachment { part = t, parent = t.parent, position = t.localPosition, rotation = t.localRotation, scale = t.localScale,
                    weaponPosition = weapon.transform.InverseTransformPoint(t.position), weaponRotation = Quaternion.Inverse(weapon.transform.rotation) * t.rotation });
            }
            if (attachments.Count == 0) throw new System.InvalidOperationException("The rifle magazine assembly is missing");
        }

        void LateUpdate()
        {
            if (!Initialized || session == null) return;
            var owner = actor != null ? actor : session.Human;
            if (owner == null || !owner.Alive || !owner.Magazine.Reloading || !session.Comfort.WeaponMotion)
            { if (posed) ResetPose(); return; }
            int tick = owner == session.Human ? session.LocalSimulationTick : session.Match.Tick;
            PoseAt(1 - (owner.Magazine.ReloadEndTick - tick) / (float)RifleMagazine.ReloadTicks);
            // Third-person wrist IK normally runs at order 0; apply the authored left target before fingers at order 100.
            if (actor != null) ArmContactPose.Apply(actor.Presentation.Animator, true, weapon.LeftGrip);
        }

        public void PoseAt(float progress)
        {
            var pose = art.Evaluate(progress);
            Progress = Mathf.Clamp01(progress); posed = true;
            carrier.SetLocalPositionAndRotation(art.magazinePivot + pose.magazineOffset, pose.magazineRotation);
            // Keep authored LOD hierarchies intact for model/material integration and renderer membership.
            foreach (var item in attachments)
                item.part.SetPositionAndRotation(weapon.transform.TransformPoint(art.magazinePivot + pose.magazineOffset + pose.magazineRotation * (item.weaponPosition - art.magazinePivot)),
                    weapon.transform.rotation * pose.magazineRotation * item.weaponRotation);
            weapon.LeftGrip.SetLocalPositionAndRotation(pose.leftPosition, pose.leftRotation);
        }

        public void ResetPose()
        {
            if (!Initialized) return;
            carrier.SetLocalPositionAndRotation(art.magazinePivot, Quaternion.identity);
            weapon.LeftGrip.SetLocalPositionAndRotation(restGrip, restGripRotation);
            foreach (var item in attachments)
                if (item.part != null) item.part.SetLocalPositionAndRotation(item.position, item.rotation);
            posed = false; Progress = 0;
        }

        void OnDestroy()
        {
            if (weapon != null) ResetPose();
            foreach (var item in attachments)
            {
                if (item.part == null || item.parent == null) continue;
                item.part.SetLocalPositionAndRotation(item.position, item.rotation); item.part.localScale = item.scale;
            }
            if (carrier != null)
                if (Application.isPlaying) Destroy(carrier.gameObject); else DestroyImmediate(carrier.gameObject);
        }
    }
}
