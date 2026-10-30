using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RailArena.Rendering
{
    public sealed class ArtAtmosphereFeature : ScriptableRendererFeature
    {
        public Material material;
        private AtmospherePass pass;
        public override void Create()
        {
            pass = new AtmospherePass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
            pass.ConfigureInput(ScriptableRenderPassInput.Depth);
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (material == null || camera == null || camera.cameraType != CameraType.Game || !camera.CompareTag("MainCamera")) return;
            pass.Material = material; renderer.EnqueuePass(pass);
        }
        private sealed class AtmospherePass : ScriptableRenderPass
        {
            public Material Material;
            private sealed class PassData { public TextureHandle source; public Material material; }
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (Material == null) return;
                var resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;
                var source = resources.activeColorTexture;
                var desc = source.GetDescriptor(renderGraph); desc.name = "Sky Gardens atmosphere color"; desc.clearBuffer = false; desc.msaaSamples = MSAASamples.None; desc.depthBufferBits = DepthBits.None;
                var destination = renderGraph.CreateTexture(desc);
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Sky Gardens atmosphere", out var data))
                {
                    data.source = source; data.material = Material;
                    builder.UseTexture(source, AccessFlags.Read); builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                    if (resources.mainShadowsTexture.IsValid()) builder.UseTexture(resources.mainShadowsTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0));
                }
                resources.cameraColor = destination;
            }
        }
    }
}
