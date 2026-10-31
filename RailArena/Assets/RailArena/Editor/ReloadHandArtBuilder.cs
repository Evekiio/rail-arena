using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class ReloadHandArtBuilder
    {
        [Serializable] sealed class Source { public int fps; public float durationSeconds; public string master,masterSha256; public string[] joints; public RifleReloadHandArt.Pose[] samples; }
        public static void Prepare()
        {
            const string folder="../ArtSource/Original/ArtPass-v1/FirstPerson/ReloadHands-v1/";
            var source=JsonUtility.FromJson<Source>(File.ReadAllText(folder+"ReloadHandAction.json"));
            if(source.fps!=Simulation.CombatRules.TickRate||source.durationSeconds!=2||source.samples.Length!=Simulation.RifleMagazine.ReloadTicks+1)throw new InvalidOperationException("Reload hand timing must preserve simulation");
            var expected=(from finger in new[]{"Thumb","Index","Middle","Ring","Pinky"} from segment in new[]{1,2,3} select "LeftHand"+finger+segment).ToArray();
            if(!source.joints.SequenceEqual(expected))throw new InvalidOperationException("Reload hand joint order mismatch");
            using(var sha=SHA256.Create())
                if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(folder+source.master))).Replace("-","").ToLowerInvariant()!=source.masterSha256)throw new InvalidOperationException("Reload hand master hash mismatch");
            foreach(var pose in source.samples)
            {
                if(pose.leftDeltas==null||pose.leftDeltas.Length!=15)throw new InvalidOperationException("Every hand frame must have fifteen joints");
                foreach(var q in pose.leftDeltas)
                    if(!float.IsFinite(q.x)||!float.IsFinite(q.y)||!float.IsFinite(q.z)||!float.IsFinite(q.w)||Mathf.Abs(Quaternion.Dot(q,q)-1)>.0001f)throw new InvalidOperationException("Invalid hand rotation");
            }
            foreach(int index in new[]{0,source.samples.Length-1})foreach(var q in source.samples[index].leftDeltas)
                if(Quaternion.Angle(q,Quaternion.identity)>.01f)throw new InvalidOperationException("Reload fingers must return to existing support pose");
            const string path="Assets/Resources/Art/RifleReloadHands.asset";var art=AssetDatabase.LoadAssetAtPath<RifleReloadHandArt>(path);
            if(art==null){art=ScriptableObject.CreateInstance<RifleReloadHandArt>();AssetDatabase.CreateAsset(art,path);}
            art.joints=source.joints;art.samples=source.samples;art.sourceSha256=source.masterSha256;EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
            Debug.Log("RELOAD_HAND_ART_READY 121 frames / fifteen left finger joints / original timing");
        }
    }
}
