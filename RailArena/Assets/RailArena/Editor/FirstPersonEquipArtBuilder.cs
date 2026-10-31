using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class FirstPersonEquipArtBuilder
    {
        [Serializable] sealed class Source
        {
            public string master, masterSha256, sourceMaster, sourceSha256;
            public int fps, frames;
            public float durationSeconds;
            public FirstPersonEquipArt.Pose[] samples;
        }

        static string Digest(string path)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        public static void Prepare()
        {
            const string folder = "../ArtSource/Original/ArtPass-v1/Animation/FirstPersonReady/";
            var source = JsonUtility.FromJson<Source>(File.ReadAllText(folder + "ReadyAction.json"));
            if (source.fps != 60 || source.frames != 18 || source.samples.Length != 19 || Mathf.Abs(source.durationSeconds - .3f) > .000001f)
                throw new InvalidOperationException("Ready action must be the complete authored 18-frame sequence");
            if (Digest(folder + source.master) != source.masterSha256 || Digest("../" + source.sourceMaster) != source.sourceSha256)
                throw new InvalidOperationException("Ready source/master identity mismatch");
            foreach (var pose in source.samples)
            {
                float norm = Quaternion.Dot(pose.rotation, pose.rotation);
                if (!float.IsFinite(pose.position.sqrMagnitude) || !float.IsFinite(norm) || pose.position.magnitude > .2f || Mathf.Abs(norm - 1) > .001f || Quaternion.Angle(Quaternion.identity, pose.rotation) > 15)
                    throw new InvalidOperationException("Ready pose exceeds the measured position/rotation envelope");
            }
            var final = source.samples[^1];
            if (final.position.magnitude > .000001f || Quaternion.Angle(final.rotation, Quaternion.identity) > .001f)
                throw new InvalidOperationException("Ready action must finish at the exact calibrated grip/ADS rest pose");
            const string path = "Assets/Resources/Art/FirstPersonEquip.asset";
            var art = AssetDatabase.LoadAssetAtPath<FirstPersonEquipArt>(path);
            if (art == null) { art = ScriptableObject.CreateInstance<FirstPersonEquipArt>(); AssetDatabase.CreateAsset(art, path); }
            art.fps = source.fps;
            art.samples = source.samples;
            art.masterSha256 = source.masterSha256;
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("FIRST_PERSON_EQUIP_READY 19 authored samples / 0.3 seconds / exact calibrated endpoint");
        }
    }
}
