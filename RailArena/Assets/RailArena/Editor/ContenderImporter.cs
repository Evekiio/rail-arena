using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public sealed class ContenderImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/ThirdParty/Grab3D/") &&
                !assetPath.StartsWith("Assets/ThirdParty/Quaternius/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = assetPath.Contains("Quaternius");
            if (!importer.importAnimation) return;
            // Default takes are not populated until after the FBX has first been read.
            // Preserve configured clips here; the builder configures resolved default takes.
            var clips = importer.clipAnimations;
            ApplyClipSettings(clips);
            if (clips.Length > 0) importer.clipAnimations = clips;
        }

        public static void ConfigureAnimationClips(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips.Length == 0) throw new System.InvalidOperationException("Import the animation FBX before configuring its takes.");
            ApplyClipSettings(clips);
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ApplyClipSettings(ModelImporterClipAnimation[] clips)
        {
            foreach (var clip in clips)
            {
                string name = clip.name.ToLowerInvariant();
                clip.loopTime = name.Contains("_loop") || name.Contains("idle") || name.Contains("walk") || name.Contains("run") || name.Contains("jog") || name.Contains("strafe") || name.Contains("fall");
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                clip.keepOriginalPositionXZ = true;
            }
        }
    }
}
