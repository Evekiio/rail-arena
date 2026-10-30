#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena
{
    public sealed class PadGraphPlayerAudit : MonoBehaviour
    {
        [Serializable] private sealed class TierResult { public string tier, shader; public bool supported; public double burstDifference; }
        [Serializable] private sealed class Report { public string graphicsApi, gpu; public int padCount, portalCount; public TierResult[] tiers; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--audit-pad-graph") >= 0)
                new GameObject("Pad graph player audit").AddComponent<PadGraphPlayerAudit>();
        }
        private IEnumerator Start()
        {
            for (int i = 0; i < 5; i++) yield return null;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--validation-output");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("--audit-pad-graph requires --validation-output");
            var session = FindFirstObjectByType<ArenaSession>();
            var effects = FindObjectsByType<TraversalPresentation>(FindObjectsSortMode.None);
            var pads = Array.FindAll(effects, e => e.Kind == TraversalEffectKind.SuperPad);
            var portals = Array.FindAll(effects, e => e.Kind == TraversalEffectKind.Portal);
            var graph = Resources.Load<Shader>("Effects/PadEnergy");
            if (session == null || pads.Length == 0 || graph == null) throw new InvalidOperationException("Required audit objects unavailable");
            foreach (var pad in pads) if (pad.Surface == null || pad.Surface.sharedMaterial.shader != graph) throw new InvalidOperationException("A pad did not select the graph");
            var camera = Camera.main;
            var padMaterial = pads[0].Surface.sharedMaterial;
            session.enabled = false;
            Vector3 center = pads[0].transform.position;
            camera.transform.SetPositionAndRotation(center + new Vector3(-4, 4, 4), Quaternion.LookRotation(center + Vector3.up * .2f - (center + new Vector3(-4, 4, 4))));
            var originalTier = session.Graphics.Tier;
            var target = new RenderTexture(320, 180, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            var oldActive = RenderTexture.active;
            var results = new List<TierResult>();
            try
            {
                target.Create();
                foreach (GraphicsTier tier in new[] { GraphicsTier.High, GraphicsTier.Balanced, GraphicsTier.Performance })
                {
                    session.Graphics.Tier = tier; session.Graphics.Apply(camera);
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    Color32[] Capture(float burst)
                    {
                        padMaterial.SetFloat("_Clock", 1.7f); padMaterial.SetFloat("_Calm", 0); padMaterial.SetFloat("_Burst", burst);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); image.Apply();
                        return image.GetPixels32();
                    }
                    var baseline = Capture(0); var active = Capture(1); double difference = 0;
                    for (int i = 0; i < active.Length; i++) difference += Math.Abs(active[i].r - baseline[i].r) + Math.Abs(active[i].g - baseline[i].g) + Math.Abs(active[i].b - baseline[i].b);
                    results.Add(new TierResult { tier = tier.ToString(), shader = padMaterial.shader.name, supported = padMaterial.shader.isSupported, burstDifference = difference / active.Length });
                }
                string output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(new Report { graphicsApi = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName, padCount = pads.Length, portalCount = portals.Length, tiers = results.ToArray() }, true));
                Debug.Log("PAD_GRAPH_PLAYER_AUDIT " + output);
            }
            finally
            {
                session.Graphics.Tier = originalTier; session.Graphics.Apply(camera);
                RenderTexture.active = oldActive; target.Release(); Destroy(target); Destroy(image);
            }
            Application.Quit();
        }
    }
}
#endif
