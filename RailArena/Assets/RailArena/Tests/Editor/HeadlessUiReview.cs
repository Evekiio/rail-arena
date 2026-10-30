using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    internal static class HeadlessUiReview
    {
        internal static void Draw(Canvas canvas, Camera camera, RenderTexture target, bool clear = true)
        {
            var size = new Vector2Int(target.width,target.height);
            // Screen-space UI is omitted by URP's isolated camera requests in batch mode.
            // Draw the generated uGUI meshes/materials against a neutral review background.
            using (var commands = new CommandBuffer { name = "uGUI geometry review" })
            {
                var meshes = new System.Collections.Generic.List<Mesh>();
                try
                {
                    commands.SetRenderTarget(target);
                    commands.SetViewport(new Rect(0,0,size.x,size.y));
                    if (clear) commands.ClearRenderTarget(true,true,new Color(.02f,.035f,.05f));
                    commands.SetViewProjectionMatrices(Matrix4x4.identity,GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0,size.x,0,size.y,-1,1),false));
                    foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                    {
                        if (!graphic.isActiveAndEnabled) continue;
                        graphic.canvasRenderer.cull = false;
                        graphic.SetVerticesDirty();
                        graphic.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
                        var source = graphic.canvasRenderer.GetMesh();
                        if (source == null) continue;
                        var mesh = Object.Instantiate(source); meshes.Add(mesh);
                        if (mesh.vertexCount == 0) continue;
                        mesh.vertices = mesh.vertices.Select(vertex =>
                        {
                            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera,graphic.transform.TransformPoint(vertex));
                            return new Vector3(point.x,point.y,0);
                        }).ToArray();
                        mesh.RecalculateBounds();
                        // CanvasRenderer tint multiplies vertex color during native uGUI rendering.
                        // State transitions live here, rather than in Graphic.color.
                        var tint=graphic.canvasRenderer.GetColor();
                        mesh.colors=mesh.colors.Select(color=>color*tint).ToArray();
                        var properties = new MaterialPropertyBlock();
                        properties.SetTexture("_MainTex",graphic.mainTexture);
                        properties.SetColor("_Color",Color.white);
                        properties.SetVector("_TextureSampleAdd",graphic.mainTexture is Texture2D texture && texture.format == TextureFormat.Alpha8
                            ? new Vector4(1,1,1,0) : Vector4.zero);
                        commands.DrawMesh(mesh,Matrix4x4.identity,graphic.materialForRendering,0,0,properties);
                    }
                    Assert.That(meshes.Sum(mesh => mesh.vertexCount), Is.GreaterThan(100), "The batch UI geometry was not rebuilt.");
                    Graphics.ExecuteCommandBuffer(commands);
                }
                finally { foreach (var mesh in meshes) Object.DestroyImmediate(mesh); }
            }
        }
    }
}
