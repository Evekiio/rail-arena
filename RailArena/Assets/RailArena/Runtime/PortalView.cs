using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Profiling;
namespace RailArena
{
    /// <summary>Single-level destination view. Owns its camera and texture; never changes traversal physics.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class PortalView : MonoBehaviour
    {
        public PortalGate Gate;
        public RenderTexture Texture { get; private set; }
        public Camera DestinationCamera { get; private set; }
        public int RenderedFrames { get; private set; }
        public bool CropToAperture { get; set; } = true;
        public Rect Viewport { get; private set; } = PortalViewport.Full;
        private ArenaSession session;
        private bool[] hidden;
        private readonly Plane[] planes = new Plane[6];
        static readonly ProfilerMarker RenderMarker = new(ProfilerCategory.Render,"RailArena.PortalView");
        private void Awake()
        {
            session = FindFirstObjectByType<ArenaSession>();
            hidden = new bool[session.World.Portals.Length];
            var root = new GameObject("Owned portal camera"); root.transform.SetParent(transform, false);
            DestinationCamera = root.AddComponent<Camera>(); DestinationCamera.enabled = false;
            var data = DestinationCamera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off; data.requiresDepthOption = CameraOverrideOption.Off;
        }
        private void LateUpdate() { if (session != null && session.View != null) RenderView(session.View); }
        public bool RenderView(Camera viewer)
        {
            using(RenderMarker.Auto())return RenderDestination(viewer);
        }
        bool RenderDestination(Camera viewer)
        {
            if (Gate == null || Gate.Exit == null || Gate.Presentation == null || Gate.Presentation.Surface == null) return false;
            var surface = Gate.Presentation.Surface;
            if (Vector3.Dot(viewer.transform.position - transform.position, transform.forward) <= .04f) return false;
            GeometryUtility.CalculateFrustumPlanes(viewer, planes);
            if (!GeometryUtility.TestPlanesAABB(planes, surface.bounds)) return false;
            int width = Mathf.Clamp(Mathf.RoundToInt(viewer.pixelWidth * .65f), 320, 960);
            int height = Mathf.Max(180, Mathf.RoundToInt(width / viewer.aspect));
            Viewport=CropToAperture?PortalViewport.ForBounds(viewer,surface.bounds):PortalViewport.Full;
            if(CropToAperture)
            {
                width=PortalViewport.TextureDimension(width,Viewport.width,Texture!=null?Texture.width:0);
                height=PortalViewport.TextureDimension(height,Viewport.height,Texture!=null?Texture.height:0);
            }
            if (Texture == null || Texture.width != width || Texture.height != height)
            {
                ReleaseTexture(); Texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                { name = "Owned portal destination", antiAliasing = 1, wrapMode=TextureWrapMode.Clamp }; Texture.Create();
            }
            DestinationCamera.CopyFrom(viewer); DestinationCamera.enabled = false;
            DestinationCamera.targetTexture = Texture;
            DestinationCamera.cullingMask = (viewer.cullingMask | (1 << 10)) & ~((1 << 11) | (1 << 5));
            DestinationCamera.transform.SetPositionAndRotation(viewer.transform.position + Gate.Exit.transform.position - transform.position, viewer.transform.rotation);
            DestinationCamera.projectionMatrix = viewer.projectionMatrix;
            Vector3 normal = Gate.Exit.transform.forward;
            Vector3 point = Gate.Exit.transform.position + normal * .08f;
            Matrix4x4 worldToCamera = DestinationCamera.worldToCameraMatrix;
            Vector3 cameraNormal = worldToCamera.MultiplyVector(normal).normalized;
            Vector3 cameraPoint = worldToCamera.MultiplyPoint(point);
            DestinationCamera.projectionMatrix = DestinationCamera.CalculateObliqueMatrix(new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPoint, cameraNormal)));
            DestinationCamera.projectionMatrix=PortalViewport.CropProjection(DestinationCamera.projectionMatrix,Viewport);
            // Hide all portal faces during the extra render: bounded cost, no recursive feedback or physics changes.
            var gates = session.World.Portals;
            try
            {
                for (int i = 0; i < gates.Length; i++) if (gates[i].Presentation != null)
                { var renderer = gates[i].Presentation.Surface; hidden[i] = renderer.forceRenderingOff; renderer.forceRenderingOff = true; }
                RenderPipeline.SubmitRenderRequest(DestinationCamera, new RenderPipeline.StandardRequest { destination = Texture });
                surface.sharedMaterial.SetTexture("_PortalTex", Texture); surface.sharedMaterial.SetFloat("_HasView", 1);
                surface.sharedMaterial.SetVector("_PortalViewport",new Vector4(Viewport.x,Viewport.y,Viewport.width,Viewport.height));
                RenderedFrames++; return true;
            }
            finally
            {
                for (int i = 0; i < gates.Length; i++) if (gates[i].Presentation != null) gates[i].Presentation.Surface.forceRenderingOff = hidden[i];
            }
        }
        private void ReleaseTexture() { if (Texture == null) return; Texture.Release(); Release(Texture); Texture = null; }
        private void OnDestroy()
        {
            ReleaseTexture(); if (DestinationCamera != null) Release(DestinationCamera.gameObject);
        }
        private static void Release(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
