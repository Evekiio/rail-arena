using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Cosmetic daylight treatment; never creates or moves gameplay geometry.</summary>
    public static class FoundryLightingBuilder
    {
        private const string ScenePath = "Assets/RailArena/Generated/Foundry.unity";
        private const string SkyPath = "Assets/RailArena/Art/Foundry/Cloud world.mat";
        private const string Output = "../Validation/FoundryLighting";

        [Serializable]
        private sealed class Review
        {
            public string unityVersion, gameplayBefore, gameplayAfter;
            public bool unchanged;
            public string scope = "Static procedural cloud sky and daylight on the existing playable arena; not installation of the Foundry v2 map or production environment models.";
        }

        [MenuItem("Rail Arena/Apply Foundry daylight")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Lighting authoring requires edit mode.");
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            var world = Object.FindFirstObjectByType<ArenaWorld>();
            if (world == null || Camera.main == null) throw new InvalidOperationException("Foundry world and main camera are required.");
            string before = FoundryArtBuilder.GameplaySignature(world);
            var shader = Shader.Find("Rail Arena/Foundry Cloud Sky");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("The Foundry cloud shader is unavailable or unsupported.");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
            if (sky == null)
            {
                sky = new Material(shader) { name = "Cloud world" };
                AssetDatabase.CreateAsset(sky,SkyPath);
            }
            sky.shader = shader;
            sky.SetColor("_ZenithColor",new Color(.22f,.46f,.65f));
            sky.SetColor("_HorizonColor",new Color(.74f,.84f,.87f));
            sky.SetColor("_CloudColor",new Color(.91f,.91f,.84f));
            sky.SetColor("_CloudShadow",new Color(.57f,.70f,.75f));
            var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Single(light => light.name == "Orbital sunlight");
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(.5f,-.7f,-.5f).normalized);
            sun.color = new Color(.99f,.94f,.84f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sky.SetVector("_SunDirection",-sun.transform.forward);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.56f,.68f,.75f);
            RenderSettings.ambientEquatorColor = new Color(.40f,.50f,.55f);
            RenderSettings.ambientGroundColor = new Color(.22f,.29f,.33f);
            RenderSettings.reflectionIntensity = .45f;
            Camera.main.clearFlags = CameraClearFlags.Skybox;
            foreach (var stars in Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.scene == world.gameObject.scene && obj.name == "Orbital starfield"))
                stars.SetActive(false);
            string after = FoundryArtBuilder.GameplaySignature(world);
            if (before != after) throw new InvalidOperationException("Lighting changed a gameplay envelope or route.");
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            EditorSceneManager.SaveScene(world.gameObject.scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output,"geometry.json"),JsonUtility.ToJson(new Review
            { unityVersion = Application.unityVersion, gameplayBefore = before, gameplayAfter = after, unchanged = true },true));
        }

        public static void ApplyAndReview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(Output);
            Capture("Before-Overview",new Vector3(46,35,-52),new Vector3(0,2,0),65);
            Capture("Before-LowerRoute",new Vector3(-18,1.58f,-18),new Vector3(12,3,4),90);
            Apply();
            Capture("After-Overview",new Vector3(46,35,-52),new Vector3(0,2,0),65);
            Capture("After-LowerRoute",new Vector3(-18,1.58f,-18),new Vector3(12,3,4),90);
        }

        private static void Capture(string name, Vector3 position, Vector3 target, float fov)
        {
            var camera = Camera.main;
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            float oldFov = camera.fieldOfView;
            var texture = new RenderTexture(1920,1080,24);
            var pixels = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var oldActive = RenderTexture.active;
            try
            {
                camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
                camera.fieldOfView = fov;
                texture.Create();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(oldPosition,oldRotation); camera.fieldOfView = oldFov;
                RenderTexture.active = oldActive;
                Object.DestroyImmediate(pixels); texture.Release(); Object.DestroyImmediate(texture);
            }
        }
    }
}
