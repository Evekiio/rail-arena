using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    /// <summary>Stores a reviewed one-weapon hand pose; no runtime finger solver is required.</summary>
    public static class HandGripAuthoring
    {
        private const string Path = "Assets/RailArena/Characters/Hand grip.json";
        [Serializable] private sealed class Profile
        {
            public string source = "Original grip fitting on the CC0 contender and original RA-01";
            public string[] joints;
            public Quaternion[] rotations;
            public Vector3 rightPosition, leftPosition;
            public Quaternion rightRotation, leftRotation;
        }

        [MenuItem("Rail Arena/Save calibrated hand grip")]
        public static void SaveCalibration()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Save the calibrated live rig in Play mode.");
            var root = GameObject.Find("Hand grip calibration");
            if (root == null) throw new InvalidOperationException("The calibrated review rig is missing.");
            var weapon = root.GetComponent<RailgunPresentation>();
            var hands = root.GetComponentInChildren<HandArticulation>();
            var profile = new Profile { joints = Array.ConvertAll(hands.Joints, joint => joint.name),
                rotations = (Quaternion[])hands.GripRotations.Clone(), rightPosition = weapon.RightGrip.localPosition,
                rightRotation = weapon.RightGrip.localRotation, leftPosition = weapon.LeftGrip.localPosition,
                leftRotation = weapon.LeftGrip.localRotation };
            File.WriteAllText(Path, JsonUtility.ToJson(profile, true));
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static Profile Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Path);
            if (asset == null) throw new InvalidOperationException("The authored hand grip profile is missing.");
            var profile = JsonUtility.FromJson<Profile>(asset.text);
            if (profile == null || profile.joints == null || profile.rotations == null || profile.joints.Length != 30 || profile.rotations.Length != 30)
                throw new InvalidOperationException("The hand grip profile must contain all 30 joints.");
            return profile;
        }

        public static void Apply(HandArticulation hands)
        {
            var profile = Load();
            for (int i = 0; i < hands.Joints.Length; i++)
            {
                if (hands.Joints[i].name != profile.joints[i]) throw new InvalidOperationException("Hand grip joint order mismatch.");
                hands.GripRotations[i] = profile.rotations[i];
            }
            hands.Pose(0);
        }

        public static void Apply(RailgunPresentation weapon)
        {
            var profile = Load();
            weapon.RightGrip.SetLocalPositionAndRotation(profile.rightPosition, profile.rightRotation);
            weapon.LeftGrip.SetLocalPositionAndRotation(profile.leftPosition, profile.leftRotation);
        }

        [MenuItem("Rail Arena/Apply authored grip to railgun")]
        public static void ApplyToWeaponPrefab()
        {
            const string prefabPath = "Assets/Resources/Weapons/Railgun.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try { Apply(root.GetComponent<RailgunPresentation>()); PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
