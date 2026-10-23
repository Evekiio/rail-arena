using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RailArena.Editor
{
    public static class CarbineProduction
    {
        [Serializable] sealed class Profile
        {
            public string source="Creator-approved precision carbine v3; fitted from shared bind-pose hand geometry";
            public string[] joints;public Quaternion[] rotations;
            public Vector3 rightPosition,leftPosition;public Quaternion rightRotation,leftRotation;
        }
        [Serializable] sealed class BuildRecord
        {
            public string unityVersion,compatibilityIdentity,executableSha256;
            public bool succeeded;
        }
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Calibrate();RailgunBuilder.Build();
            Debug.Log("CARBINE_PRODUCTION_BUILT");
        }
        public static void BuildWindows()
        {
            BuildIdentityGenerator.Generate();
            ArenaBuilder.BuildWindowsPlaytestTo("../Builds/Carbine-v1/RailArena.exe");
            using var sha=System.Security.Cryptography.SHA256.Create();
            File.WriteAllText("../ArtSource/Original/PrecisionCarbine-Production-v1/Windows-Build.json",
                JsonUtility.ToJson(new BuildRecord{unityVersion=Application.unityVersion,
                    compatibilityIdentity=File.ReadAllText("Assets/Resources/BuildIdentity.txt").Trim(),
                    executableSha256=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes("../Builds/Carbine-v1/RailArena.exe"))).Replace("-","").ToLowerInvariant(),succeeded=true},true));
        }
        public static void BuildValidationWindows()
        {
            BuildWindows();
            ArenaBuilder.BuildWindowsTo("../Builds/Carbine-v1-Validation/RailArena.exe");
        }
        public static void BuildPreAlpha()
        {
            PlayerSettings.bundleVersion = "0.1.0-pre-alpha.1";
            AssetDatabase.SaveAssets();
            BuildValidationWindows();
        }
        static void Calibrate()
        {
            var root=Object.Instantiate(Resources.Load<GameObject>("Characters/Contender"));
            try
            {
                var animator=root.GetComponent<Animator>();animator.enabled=false;
                var hands=root.GetComponent<HandArticulation>();hands.enabled=false;
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
                var poses=skin.sharedMesh.bindposes;
                // Restore original joint bases from the skin bind matrices, rather than old gun offsets.
                foreach(var joint in hands.Joints)
                {
                    int j=Array.IndexOf(skin.bones,joint),p=Array.IndexOf(skin.bones,joint.parent);
                    if(j<0||p<0)throw new InvalidOperationException("Missing finger bind matrices");
                    joint.localRotation=(poses[p]*poses[j].inverse).rotation;
                }
                for(int i=0;i<30;i++)
                {
                    int finger=i%15/3,segment=i%3;bool left=i<15;
                    float[] curl= finger==0?new[]{12f,18f,10f}:finger==1&&!left?new[]{24f,38f,22f}:new[]{48f,62f,32f};
                    hands.Joints[i].localRotation*=Quaternion.AngleAxis(-curl[segment],Vector3.right);
                }
                Quaternion Wrist(bool left)
                {
                    var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                    string prefix=left?"LeftHand":"RightHand";
                    Transform Joint(string name)=>hands.Joints.First(j=>j.name==prefix+name+"1");
                    var direction=hand.InverseTransformVector(Joint("Middle").position-hand.position).normalized;
                    var normal=hand.InverseTransformVector(Vector3.Cross(Joint("Middle").position-hand.position,Joint("Index").position-Joint("Pinky").position)).normalized;
                    var original=Quaternion.LookRotation(normal,direction);
                    var desired=Quaternion.LookRotation(left?Vector3.up:Vector3.left,left?Vector3.right:new Vector3(0,-1,.15f).normalized);
                    return desired*Quaternion.Inverse(original);
                }
                var profile=new Profile{joints=hands.Joints.Select(j=>j.name).ToArray(),rotations=hands.Joints.Select(j=>j.localRotation).ToArray(),
                    rightPosition=new Vector3(.035f,.015f,-.103f),leftPosition=new Vector3(-.075f,-.070f,.17f),rightRotation=Wrist(false),leftRotation=Wrist(true)};
                File.WriteAllText("Assets/RailArena/Characters/Hand grip.json",JsonUtility.ToJson(profile,true));
                AssetDatabase.ImportAsset("Assets/RailArena/Characters/Hand grip.json",ImportAssetOptions.ForceSynchronousImport);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
