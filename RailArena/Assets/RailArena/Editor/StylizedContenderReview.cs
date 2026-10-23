using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Batch-mode graphics capture; no Editor or game window is opened.</summary>
    public static class StylizedContenderReview
    {
        public static void BuildAndCapture()
        {
            StylizedContenderBuilder.Build();
            Capture();
        }

        public static void Capture()
        {
            string folder = Path.GetFullPath("../Validation/StylizedContenders");
            Directory.CreateDirectory(folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.32f, .34f, .37f);
            var lightObject = new GameObject("Studio key");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(35, 155, 0);
            var fill = new GameObject("Studio fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = .4f;
            fill.transform.rotation = Quaternion.Euler(20, -130, 0);
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.18f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.75f, .75f, .73f);
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 30;
            camera.transform.position = new Vector3(0, 1.02f, 8);
            camera.transform.rotation = Quaternion.Euler(0, 180, 0);
            string[] paths = { "Characters/Contender", "Characters/Stylized/Bot01",
                "Characters/Stylized/Bot02", "Characters/Stylized/Bot03" };
            var instances = new GameObject[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                var instance = Object.Instantiate(Resources.Load<GameObject>(paths[i]));
                instance.transform.position = new Vector3((1.5f - i) * 1.12f, 0, 0);
                instance.transform.rotation = Quaternion.Euler(0, -20, 0);
                instance.GetComponent<LODGroup>().ForceLOD(0);
                instance.GetComponent<ContenderPresentation>().ApplySkin(0);
                // Remove the weapon from the character modeling review only.
                instance.GetComponent<ContenderPresentation>().Weapon.gameObject.SetActive(false);
                instances[i] = instance;
            }
            Write(camera, 2200, 1100, Path.Combine(folder, "Unity-Lineup.png"));
            for (int i = 1; i < instances.Length; i++) instances[i].SetActive(false);
            instances[0].transform.position = Vector3.zero;
            foreach (var view in new[] { ("Front", 0f), ("Side", 90f), ("Back", 180f) })
            {
                instances[0].transform.rotation = Quaternion.Euler(0, view.Item2, 0);
                Write(camera, 900, 1100, Path.Combine(folder, "Unity-Player-" + view.Item1 + ".png"));
            }
            instances[0].transform.rotation = Quaternion.Euler(0, -20, 0);
            camera.transform.position = new Vector3(0, 1.70f, 8);
            camera.orthographicSize = .22f;
            Write(camera, 1100, 1100, Path.Combine(folder, "Unity-Player-Face.png"));
            Debug.Log("STYLIZED_REVIEW_SAVED " + folder);
        }

        private static void Write(Camera camera, int width, int height, string path)
        {
            var target = new RenderTexture(width, height, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
