using System;
using System.Collections.Generic;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Networking
{
    public readonly struct HistoricalHit
    {
        public readonly bool ValidTime;
        public readonly ArenaActor Actor;
        public readonly HitRegion Region;
        public readonly Vector3 End;
        public HistoricalHit(bool validTime, ArenaActor actor, HitRegion region, Vector3 end)
        { ValidTime = validTime; Actor = actor; Region = region; End = end; }
    }

    /// <summary>Thirteen poses cover the current tick and 200 ms at 60 Hz. Live actors are never rewound.</summary>
    public sealed class HitboxHistory : IDisposable
    {
        private struct Pose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }
        private sealed class Frame
        {
            public int Tick = -1;
            public Pose[] Poses;
            public int[] Lives;
            public bool[] Alive;
        }
        private sealed class Shape
        {
            public Hitbox Source;
            public Collider Proxy;
            public int ActorIndex;
        }
        private readonly ArenaActor[] actors;
        private readonly Shape[] shapes;
        private readonly Frame[] frames = new Frame[CombatRules.MaximumRewindTicks + 1];
        private readonly GameObject root;
        private int latestTick = -1;
        private bool disposed;

        public HitboxHistory(IReadOnlyList<ArenaActor> participants)
        {
            actors = new ArenaActor[participants.Count];
            root = new GameObject("Historical raycast proxies");
            var built = new List<Shape>();
            try
            {
                for (int i = 0; i < actors.Length; i++)
                {
                    actors[i] = participants[i];
                    foreach (var source in actors[i].GetComponentsInChildren<Hitbox>(true))
                    {
                        var proxy = new GameObject("Historical " + source.name) { layer = 2 };
                        proxy.transform.SetParent(root.transform, false);
                        Collider collider;
                        if (source.TryGetComponent<BoxCollider>(out var box))
                        {
                            var copy = proxy.AddComponent<BoxCollider>();
                            copy.center = box.center; copy.size = box.size;
                            collider = copy;
                        }
                        else if (source.TryGetComponent<CapsuleCollider>(out var capsule))
                        {
                            var copy = proxy.AddComponent<CapsuleCollider>();
                            copy.center = capsule.center; copy.radius = capsule.radius;
                            copy.height = capsule.height; copy.direction = capsule.direction;
                            collider = copy;
                        }
                        else throw new ArgumentException("Historical hurtboxes must be boxes or capsules.");
                        collider.isTrigger = true;
                        collider.enabled = false;
                        built.Add(new Shape { Source = source, Proxy = collider, ActorIndex = i });
                    }
                }
                shapes = built.ToArray();
                for (int i = 0; i < frames.Length; i++)
                    frames[i] = new Frame { Poses = new Pose[shapes.Length], Lives = new int[actors.Length], Alive = new bool[actors.Length] };
            }
            catch { Dispose(); throw; }
        }

        public void Capture(int tick)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HitboxHistory));
            if (tick <= latestTick) throw new ArgumentOutOfRangeException(nameof(tick), "History requires increasing authority ticks.");
            var frame = frames[tick % frames.Length];
            frame.Tick = latestTick = tick;
            for (int i = 0; i < actors.Length; i++)
            { frame.Alive[i] = actors[i].Alive; frame.Lives[i] = actors[i].Life; }
            for (int i = 0; i < shapes.Length; i++)
            {
                var transform = shapes[i].Source.transform;
                frame.Poses[i] = new Pose { Position = transform.position, Rotation = transform.rotation, Scale = transform.lossyScale };
            }
        }

        public HistoricalHit Trace(int authorityTick, int viewedTick, ArenaActor shooter, MovementCommand? shot = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HitboxHistory));
            Vector3 direction = shot.HasValue ? Quaternion.Euler(shot.Value.Pitch, shot.Value.Yaw, 0) * Vector3.forward : shooter.AimDirection;
            var ray = new Ray(shooter.Eye, direction);
            float nearest = 180;
            Vector3 end = ray.GetPoint(nearest);
            if (Physics.Raycast(ray, out var wall, nearest, 1 << 0, QueryTriggerInteraction.Ignore))
            { nearest = wall.distance; end = wall.point; }
            if (authorityTick != latestTick || viewedTick < 0 || viewedTick > authorityTick ||
                (long)authorityTick - viewedTick > CombatRules.MaximumRewindTicks)
                return new HistoricalHit(false, null, default, end);
            var frame = frames[viewedTick % frames.Length];
            int shooterIndex = Array.IndexOf(actors, shooter);
            // The firing origin is always current host state, including immediately after respawn.
            if (frame.Tick != viewedTick || shooterIndex < 0 || !shooter.Alive)
                return new HistoricalHit(false, null, default, end);
            ArenaActor victim = null;
            HitRegion region = default;
            try
            {
                for (int i = 0; i < shapes.Length; i++)
                {
                    var shape = shapes[i];
                    int index = shape.ActorIndex;
                    if (index == shooterIndex || !actors[index].Alive || !frame.Alive[index] || frame.Lives[index] != actors[index].Life) continue;
                    var pose = frame.Poses[i];
                    shape.Proxy.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
                    shape.Proxy.transform.localScale = pose.Scale;
                    shape.Proxy.enabled = true;
                }
                Physics.SyncTransforms();
                foreach (var shape in shapes)
                {
                    if (!shape.Proxy.enabled || !shape.Proxy.Raycast(ray, out var hit, nearest)) continue;
                    nearest = hit.distance;
                    end = hit.point;
                    victim = actors[shape.ActorIndex];
                    region = shape.Source.Region;
                }
            }
            finally { foreach (var shape in shapes) shape.Proxy.enabled = false; }
            return new HistoricalHit(true, victim, region, end);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (root == null) return;
            foreach (var collider in root.GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
