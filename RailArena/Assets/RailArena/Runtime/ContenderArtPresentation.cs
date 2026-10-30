using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailArena
{
    /// <summary>Dedicated visual renderers retain the compatible skeleton and original mesh buffers.</summary>
    public sealed class ContenderArtPresentation : MonoBehaviour
    {
        sealed class Binding { public SkinnedMeshRenderer original, painted, contour; public bool enabled; }
        readonly Dictionary<SkinnedMeshRenderer, Binding> bindings = new();
        readonly List<Binding> originals = new();
        readonly Dictionary<LODGroup, LOD[]> lods = new();
        ArenaActor owner;
        Renderer[] originalBody;
        FirstPersonArms armsOwner;
        SkinnedMeshRenderer originalArms;
        RuntimeAnimatorController originalMotion;

        public void ApplyBody(ArenaActor actor, ContenderArtSet.Variant art, RuntimeAnimatorController motion = null)
        {
            if (actor.Presentation.BodyRenderers.Length != art.lods.Length) throw new InvalidOperationException("Contender LOD count differs from art set");
            if (owner == null) { owner = actor; originalBody = (Renderer[])actor.Presentation.BodyRenderers.Clone(); originalMotion = actor.Presentation.Animator.runtimeAnimatorController; }
            if (motion != null && actor.Presentation.Animator.runtimeAnimatorController != motion)
                actor.Presentation.SetMotionController(motion);
            for (int i = 0; i < art.lods.Length; i++)
                actor.Presentation.BodyRenderers[i] = Apply((SkinnedMeshRenderer)actor.Presentation.BodyRenderers[i], actor.Presentation.Animator, art.lods[i], art.surface, art.outline);
        }

        public void ApplyArms(FirstPersonArms arms, ContenderArtSet.Variant art)
        {
            if (armsOwner == null) { armsOwner = arms; originalArms = arms.Surface; }
            arms.Surface = Apply(arms.Surface, arms.Animator, art.arms, art.surface, null);
        }

        SkinnedMeshRenderer Apply(SkinnedMeshRenderer target, Animator animator, ContenderArtSet.SkinMesh art, Material surface, Material outline)
        {
            if (!bindings.TryGetValue(target, out var binding))
            {
                binding = new Binding { original = target, enabled = target.enabled };
                bindings.Add(target, binding); originals.Add(binding);
            }
            if (binding.painted == null || binding.painted.sharedMesh != art.mesh)
            {
                var previous = binding.painted != null ? binding.painted : binding.original;
                var visual = new GameObject(binding.original.name + " / Painted cel");
                visual.layer = binding.original.gameObject.layer;
                visual.transform.SetParent(binding.original.transform, false);
                var renderer = visual.AddComponent<SkinnedMeshRenderer>();
                renderer.rootBone = binding.original.rootBone;
                renderer.shadowCastingMode = binding.original.shadowCastingMode; renderer.receiveShadows = binding.original.receiveShadows;
                renderer.lightProbeUsage = binding.original.lightProbeUsage; renderer.reflectionProbeUsage = binding.original.reflectionProbeUsage;
                renderer.updateWhenOffscreen = binding.original.updateWhenOffscreen; renderer.quality = binding.original.quality;
                renderer.skinnedMotionVectors = binding.original.skinnedMotionVectors;
                renderer.motionVectorGenerationMode = binding.original.motionVectorGenerationMode;
                renderer.renderingLayerMask = binding.original.renderingLayerMask;
                var lookup = new Dictionary<string, Transform>();
                foreach (var bone in animator.GetComponentsInChildren<Transform>(true)) lookup[bone.name] = bone;
                foreach (var bone in binding.original.bones) if (bone != null) lookup[bone.name] = bone;
                var bones = new Transform[art.bones.Length];
                for (int i = 0; i < bones.Length; i++)
                    if (!lookup.TryGetValue(art.bones[i], out bones[i])) throw new InvalidOperationException("Art skeleton is missing " + art.bones[i]);
                renderer.bones = bones;
                renderer.sharedMesh = art.mesh;
                renderer.localBounds = binding.original.localBounds;
                SkinnedMeshRenderer contour = null;
                if (outline != null)
                {
                    var shell = new GameObject("Selective character contour"); shell.layer = visual.layer; shell.transform.SetParent(visual.transform, false);
                    contour = shell.AddComponent<SkinnedMeshRenderer>(); contour.bones = bones; contour.rootBone = renderer.rootBone;
                    contour.sharedMesh = art.mesh; contour.localBounds = renderer.localBounds; contour.sharedMaterial = outline;
                    contour.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    contour.updateWhenOffscreen = renderer.updateWhenOffscreen; contour.quality = renderer.quality;
                }
                var group = binding.original.GetComponentInParent<LODGroup>(true);
                if (group != null)
                {
                    if (!lods.ContainsKey(group)) lods.Add(group, group.GetLODs());
                    var levels = group.GetLODs();
                    for (int level = 0; level < levels.Length; level++)
                    {
                        var entries = new List<Renderer>(levels[level].renderers);
                        int index = entries.IndexOf(previous);
                        if (index >= 0)
                        {
                            entries[index] = renderer;
                            if (binding.contour != null) entries.Remove(binding.contour);
                            if (contour != null) entries.Add(contour);
                            levels[level].renderers = entries.ToArray();
                        }
                    }
                    group.SetLODs(levels);
                }
                previous.enabled = false;
                if (binding.painted != null) Destroy(binding.painted.gameObject);
                binding.painted = renderer; binding.contour = contour; bindings.Add(renderer, binding);
            }
            binding.original.enabled = false;
            binding.painted.sharedMaterial = surface;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_BaseMap", surface.GetTexture("_BaseMap")); block.SetColor("_BaseColor", surface.GetColor("_BaseColor"));
            binding.painted.SetPropertyBlock(block);
            return binding.painted;
        }

        void OnDestroy()
        {
            if (owner != null && owner.Presentation != null)
            {
                owner.Presentation.BodyRenderers = originalBody;
                if (owner.Presentation.Animator != null) owner.Presentation.SetMotionController(originalMotion);
            }
            if (armsOwner != null) armsOwner.Surface = originalArms;
            foreach (var entry in lods) if (entry.Key != null) entry.Key.SetLODs(entry.Value);
            foreach (var binding in originals)
            {
                if (binding.original != null) binding.original.enabled = binding.enabled;
                if (binding.painted != null) Destroy(binding.painted.gameObject);
            }
        }
    }
}
