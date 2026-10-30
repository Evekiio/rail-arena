using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RailArena.Editor
{
    public static class SkyGradeCandidate
    {
        private const string Folder = "Assets/Validation/SkyGradeCandidate";
        private const string Scene = Folder + "/SkyGrade.unity";
        private const string ProductionProfile = "Assets/RailArena/Generated/Sky Gardens grading.asset";
        private const string MotionFolder = "Assets/Validation/SkyMotionBlurCandidate";
        private const string MotionScene = MotionFolder + "/SkyMotionBlur.unity";
        public static void IntegrateMotionBlur()
        {
            VerifyMotionBlur();
            var candidate = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MotionFolder + "/Motion grade.asset");
            var production = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProductionProfile);
            if (production == null || !candidate.TryGet<MotionBlur>(out var source) || production.TryGet<MotionBlur>(out _)) throw new Exception("Motion candidate missing or already integrated");
            var backup = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/sky-grade-before-motion.asset"));
            if (File.Exists(backup)) throw new IOException("Grade backup already exists");
            File.Copy(ProductionProfile, backup);
            var copy = UnityEngine.Object.Instantiate(source); copy.name = source.name;
            AssetDatabase.AddObjectToAsset(copy, production); production.components.Add(copy);
            EditorUtility.SetDirty(production); AssetDatabase.SaveAssets();
            Debug.Log("SKY_MOTION_INTEGRATED backup=" + backup);
        }
        public static void PrepareMotionBlur()
        {
            if (AssetDatabase.IsValidFolder(MotionFolder)) throw new IOException("Motion blur candidate already exists");
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            Directory.CreateDirectory(MotionFolder); AssetDatabase.Refresh();
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MotionScene, true)) throw new Exception("Could not copy scene");
            EditorSceneManager.OpenScene(MotionScene);
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.isGlobal && v.sharedProfile != null);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "Sky Gardens motion blur candidate";
            AssetDatabase.CreateAsset(profile, MotionFolder + "/Motion grade.asset");
            foreach (var existing in volume.sharedProfile.components)
            {
                if (existing == null) throw new Exception("Existing profile has a missing component");
                var copy = UnityEngine.Object.Instantiate(existing); copy.name = existing.name;
                AssetDatabase.AddObjectToAsset(copy, profile); profile.components.Add(copy);
            }
            if (profile.TryGet<MotionBlur>(out _)) throw new Exception("Profile already has motion blur");
            var blur = profile.Add<MotionBlur>(); blur.mode.Override(MotionBlurMode.CameraOnly); blur.quality.Override(MotionBlurQuality.High);
            blur.intensity.Override(.15f); blur.clamp.Override(.025f); AssetDatabase.AddObjectToAsset(blur, profile);
            EditorUtility.SetDirty(profile); volume.sharedProfile = profile;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Debug.Log("SKY_MOTION_CANDIDATE_PREPARED");
        }
        public static void VerifyMotionBlur()
        {
            EditorSceneManager.OpenScene(MotionScene);
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.isGlobal && v.sharedProfile != null);
            if (!volume.sharedProfile.TryGet<MotionBlur>(out var blur) || !blur.active || !blur.mode.overrideState || blur.mode.value != MotionBlurMode.CameraOnly || !blur.quality.overrideState || blur.quality.value != MotionBlurQuality.High || !blur.intensity.overrideState || Mathf.Abs(blur.intensity.value - .15f) > .0001f || !blur.clamp.overrideState || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count != 298)
                throw new Exception("Motion blur settings or baked probes did not persist");
            Debug.Log("SKY_MOTION_CANDIDATE_VERIFIED intensity=" + blur.intensity.value + " clamp=" + blur.clamp.value);
        }
        public static void Integrate()
        {
            var backup = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/main-before-grade.unity"));
            if (File.Exists(backup) || File.Exists(ProductionProfile)) throw new IOException("Grade backup or production profile already exists");
            Verify();
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.isGlobal && v.sharedProfile != null);
            if (AssetDatabase.GetAssetPath(volume.sharedProfile) != "Assets/RailArena/Generated/Foundry grading.asset") throw new Exception("Unexpected current grade; refusing replacement");
            File.Copy(SkyGardensBuilder.ScenePath, backup);
            if (!AssetDatabase.CopyAsset(Folder + "/Sky grading.asset", ProductionProfile)) throw new Exception("Could not copy grade profile");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProductionProfile);
            if (profile == null || !profile.TryGet<LiftGammaGain>(out _) || !profile.TryGet<ColorCurves>(out _)) throw new Exception("Copied profile incomplete");
            volume.sharedProfile = profile;
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new Exception("Could not save playable scene");
            AssetDatabase.SaveAssets(); Debug.Log("SKY_GRADE_INTEGRATED backup=" + backup);
        }
        public static void VerifyIntegrated()
        {
            PrepareSkyGiCandidate.VerifyIntegrated();
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.isGlobal && v.sharedProfile != null);
            var profile = volume.sharedProfile;
            if (AssetDatabase.GetAssetPath(profile) != ProductionProfile || !profile.TryGet<LiftGammaGain>(out var wheels) || !profile.TryGet<ColorCurves>(out var curves) || !wheels.lift.overrideState || !wheels.gamma.overrideState || !wheels.gain.overrideState || !curves.master.overrideState)
                throw new Exception("Integrated grade missing or incomplete");
            Debug.Log("SKY_GRADE_INTEGRATION_VERIFIED components=" + profile.components.Count);
        }
        public static void Prepare()
        {
            if (AssetDatabase.IsValidFolder(Folder)) throw new IOException("Grade candidate already exists");
            EditorSceneManager.OpenScene(SkyGardensBuilder.ScenePath);
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scene, true)) throw new Exception("Could not copy scene");
            EditorSceneManager.OpenScene(Scene);
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.isGlobal && v.sharedProfile != null);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Sky Gardens grading candidate";
            AssetDatabase.CreateAsset(profile, Folder + "/Sky grading.asset");
            foreach (var existing in volume.sharedProfile.components)
            {
                if (existing == null) throw new Exception("Existing profile has a missing component");
                var copy = UnityEngine.Object.Instantiate(existing); copy.name = existing.name;
                AssetDatabase.AddObjectToAsset(copy, profile); profile.components.Add(copy);
            }
            if (profile.TryGet<LiftGammaGain>(out _) || profile.TryGet<ColorCurves>(out _)) throw new Exception("Existing grade already contains grading components");
            var wheels = profile.Add<LiftGammaGain>();
            wheels.lift.Override(new Vector4(.99f, 1f, 1.02f, .015f));
            wheels.gamma.Override(new Vector4(1.01f, 1f, .99f, .015f));
            wheels.gain.Override(new Vector4(1.02f, 1.01f, .99f, 0));
            var curves = profile.Add<ColorCurves>();
            curves.master.Override(new TextureCurve(new[] { new Keyframe(0, 0, 1, 1), new Keyframe(.5f, .52f, 1, 1), new Keyframe(1, 1, 1, 1) }, 0, false, new Vector2(0, 1)));
            AssetDatabase.AddObjectToAsset(wheels, profile); AssetDatabase.AddObjectToAsset(curves, profile);
            EditorUtility.SetDirty(profile); volume.sharedProfile = profile;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Debug.Log("SKY_GRADE_CANDIDATE_PREPARED");
        }
        public static void Verify()
        {
            EditorSceneManager.OpenScene(Scene);
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v => v.isGlobal && v.sharedProfile != null);
            var profile = volume.sharedProfile;
            if (!profile.TryGet<LiftGammaGain>(out var wheels) || !profile.TryGet<ColorCurves>(out var curves) || !wheels.lift.overrideState || !wheels.gamma.overrideState || !wheels.gain.overrideState || !curves.master.overrideState || LightmapSettings.lightmaps.Length == 0 || LightmapSettings.lightProbes == null || LightmapSettings.lightProbes.count != 298)
                throw new Exception("Grade overrides or baked lighting did not persist");
            Debug.Log("SKY_GRADE_CANDIDATE_VERIFIED components=" + profile.components.Count + " probes=" + LightmapSettings.lightProbes.count);
        }
        public static void Capture()
        {
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Validation/SkyGardens-Playable-2026-10-29/GradeComparison"));
            Directory.CreateDirectory(output);
            foreach (var item in new[] { ("Current", SkyGardensBuilder.ScenePath), ("Candidate", Scene) })
            {
                EditorSceneManager.OpenScene(item.Item2); var camera = Camera.main;
                var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                var oldActive = RenderTexture.active; var oldTarget = camera.targetTexture;
                try
                {
                    target.Create(); camera.targetTexture = target;
                    foreach (var view in new[] { ("Underpass", new Vector3(0, 1.6f, -11), new Vector3(0, 1.6f, 4)), ("Garden", new Vector3(25, 2, -16), new Vector3(29, 2, -16)), ("Summit", new Vector3(6, 24, -5), new Vector3(0, 23, 0)) })
                    {
                        camera.transform.position = view.Item2; camera.transform.LookAt(view.Item3);
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                        RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                        File.WriteAllBytes(Path.Combine(output, item.Item1 + "-" + view.Item1 + ".png"), image.EncodeToPNG());
                    }
                }
                finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
            }
            Debug.Log("SKY_GRADE_COMPARISON " + output);
        }
    }
}
