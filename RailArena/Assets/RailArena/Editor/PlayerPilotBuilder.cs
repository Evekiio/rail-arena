using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    /// <summary>Isolated player art review. Does not write Resources, gameplay scenes or bot assets.</summary>
    public static class PlayerPilotBuilder
    {
        public const string Root = "Assets/RailArena/Art/PlayerPilot";
        public const string PrefabPath = Root + "/Player-Pilot.prefab";
        private const string Source = "../ArtSource/Original/PlayerPilot-v1/";
        private static SkinnedMeshRenderer[] captureSkins;
        private static int captureLevel;
        private static readonly Vector3 ShoulderContactOffset = new Vector3(.015f,.02f,.075f);

        [Serializable] private sealed class BoneFrame { public string name; public Matrix4x4 rest, posed; }
        [Serializable] private sealed class PoseRecord { public BoneFrame[] bones; public Matrix4x4 weapon, rightGrip, leftGrip, shoulder; }
        [Serializable] private sealed class Sample
        {
            public string phase;
            public int frame;
            public float rightWristError, leftWristError, shoulderError, height, rightHandSkinDistance, leftHandSkinDistance;
            public bool finite;
        }
        [Serializable] private sealed class Record
        {
            public string unityVersion;
            public bool passed;
            public int[] triangles;
            public Sample[] samples;
            public string scope = "Isolated prefab: humanoid rig, retained hurtboxes, three LODs, baked animation geometry and wrist/shoulder anchors. Fine finger contact, armor collision, visual quality and gameplay performance require review.";
        }

        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var surface = Surface();
            var root = PrefabUtility.LoadPrefabContents("Assets/Resources/Characters/Contender.prefab");
            try
            {
                root.name = "Player pilot - creator review";
                var presentation = root.GetComponent<ContenderPresentation>();
                presentation.enabled = false;
                presentation.Hands.enabled = false;
                var previous = presentation.BodyRenderers;
                var bones = root.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name)
                    .ToDictionary(g => g.Key, g => g.First());
                var lods = new LOD[3];
                var renderers = new List<Renderer>();
                for (int level = 0; level < 3; level++)
                {
                    string path = Root + "/PlayerPilot_LOD" + level + ".fbx";
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    Require(importer != null, "Missing pilot FBX: " + path);
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = false;
                    importer.importCameras = importer.importLights = false;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.isReadable = true;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.SaveAndReimport();
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var avatar = model.GetComponent<Animator>()?.avatar;
                    Require(avatar != null && avatar.isHuman && avatar.isValid, "Invalid pilot humanoid avatar");
                    var imported = model.GetComponentInChildren<SkinnedMeshRenderer>();
                    Require(imported != null && imported.sharedMesh.subMeshCount == 1, "Expected one atlas skin");
                    var body = new GameObject("Pilot body LOD" + level);
                    body.transform.SetParent(root.transform, false);
                    var local = model.transform.worldToLocalMatrix * imported.transform.localToWorldMatrix;
                    body.transform.SetLocalPositionAndRotation(local.GetColumn(3), local.rotation);
                    body.transform.localScale = local.lossyScale;
                    var skin = body.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMaterial = surface;
                    skin.bones = imported.bones.Select(b => bones[b.name]).ToArray();
                    skin.rootBone = bones[imported.rootBone.name];
                    var mesh = Object.Instantiate(imported.sharedMesh);
                    mesh.name = "Player pilot calibrated LOD" + level;
                    mesh.bindposes = skin.bones.Select(b => b.worldToLocalMatrix * skin.transform.localToWorldMatrix).ToArray();
                    string meshPath = Root + "/PlayerPilot_LOD" + level + "-Skin.asset";
                    var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (saved == null) { AssetDatabase.CreateAsset(mesh, meshPath); saved = mesh; }
                    else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); }
                    skin.sharedMesh = saved;
                    skin.localBounds = imported.localBounds;
                    skin.updateWhenOffscreen = true;
                    skin.quality = SkinQuality.Bone4;
                    renderers.Add(skin);
                    lods[level] = new LOD(new[] { .18f, .065f, .012f }[level], new Renderer[] { skin });
                }
                presentation.BodyRenderers = renderers.ToArray();
                presentation.Skins = new[] { surface };
                root.GetComponent<LODGroup>().SetLODs(lods);
                root.GetComponent<LODGroup>().RecalculateBounds();
                foreach (var renderer in previous) Object.DestroyImmediate(renderer.gameObject);
                // Retain the original collision definitions in this isolated review prefab.
                HandGripAuthoring.Apply(presentation.Hands);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("PLAYER_PILOT_IMPORTED_AND_VALIDATED");
        }

        private static Material Surface()
        {
            string path = Root + "/PlayerPilot.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            foreach (string name in new[] { "BaseColor", "MetallicSmoothness", "Emission" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/" + name + ".png");
                importer.sRGBTexture = name != "MetallicSmoothness";
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/BaseColor.png"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/MetallicSmoothness.png"));
            mat.SetFloat("_Smoothness", 1);
            mat.SetFloat("_SmoothnessTextureChannel", 0);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Emission.png"));
            mat.SetColor("_EmissionColor", Color.white * .35f);
            mat.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void Fit(Animator animator, ContenderPresentation presentation, float pitch)
        {
            // Seat the stock on the pilot shell and bring the support palm back
            // along the fore-end so the shared rig does not overreach.
            presentation.LeftGrip.localPosition = new Vector3(-.075f,-.070f,.055f);
            ArmContactPose.AlignBody(animator);
            var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            var left = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var right = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var axis = Vector3.ProjectOnPlane(right.position - left.position, Vector3.up);
            chest.rotation = Quaternion.AngleAxis(35, Vector3.up) * Quaternion.FromToRotation(axis, Vector3.right) * chest.rotation;
            var aim = Quaternion.Euler(pitch, 0, 0);
            var contact = right.position + aim * ShoulderContactOffset;
            presentation.Weapon.SetPositionAndRotation(contact - aim * presentation.Railgun.Shoulder.localPosition, aim);
            ArmContactPose.Apply(animator, true, presentation.LeftGrip);
            ArmContactPose.Apply(animator, false, presentation.RightGrip);
            presentation.Hands.Pose(0);
        }

        public static void Validate()
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var baked = new Mesh();
            try
            {
                var animator = instance.GetComponent<Animator>();
                var presentation = instance.GetComponent<ContenderPresentation>();
                Require(animator.avatar.isHuman && animator.avatar.isValid, "Humanoid avatar");
                Require(!animator.applyRootMotion, "Root motion must remain disabled");
                Require(instance.GetComponentsInChildren<Hitbox>(true).Length == 11, "Eleven retained hurtboxes");
                Require(presentation.Hands.Joints.Length == 30, "Thirty finger joints");
                var lods = instance.GetComponent<LODGroup>().GetLODs();
                var counts = lods.Select(l => ((SkinnedMeshRenderer)l.renderers[0]).sharedMesh.triangles.Length / 3).ToArray();
                Require(counts.Length == 3 && counts[0] > counts[1] && counts[1] > counts[2], "Descending LOD topology");
                var samples = new List<Sample>();
                foreach (string phase in new[] { "Idle", "Run", "Backward", "Jump", "Death" })
                {
                    SetPhase(animator, phase);
                    for (int frame = 0; frame < 30; frame++)
                    {
                        animator.Update(1f / 30);
                        if (phase != "Death") Fit(animator, presentation, 0);
                        foreach (var lod in lods)
                        {
                            var renderer = (SkinnedMeshRenderer)lod.renderers[0];
                            renderer.BakeMesh(baked, true);
                            var points = baked.vertices.Select(renderer.transform.TransformPoint).ToArray();
                            bool finite = points.All(p => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z));
                            Require(finite, "Nonfinite deformed geometry");
                            float height = points.Max(p => p.y) - points.Min(p => p.y);
                            Require(height > .15f && height < 2.6f, "Animated metre scale");
                            if (lod.renderers[0] != lods[0].renderers[0]) continue;
                            var sample = new Sample { phase = phase, frame = frame, finite = finite, height = height };
                            if (phase != "Death")
                            {
                                sample.rightWristError = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position, presentation.RightGrip.position);
                                sample.leftWristError = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftHand).position, presentation.LeftGrip.position);
                                var desired = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position + ShoulderContactOffset;
                                sample.shoulderError = Vector3.Distance(presentation.Railgun.Shoulder.position, desired);
                                Require(sample.rightWristError < .008f && sample.leftWristError < .008f, "Wrist target outside eight millimetres");
                                Require(sample.shoulderError < .001f, "Stock anchor error");
                                float HandDistance(bool isLeft)
                                {
                                    string prefix=isLeft?"LeftHand":"RightHand";
                                    var weights=renderer.sharedMesh.boneWeights;
                                    var handBones=renderer.bones;
                                    var positions=points.Where((point,index)=>
                                    {
                                        var weight=weights[index];
                                        return (weight.weight0>.3f && handBones[weight.boneIndex0].name.StartsWith(prefix)) ||
                                            (weight.weight1>.3f && handBones[weight.boneIndex1].name.StartsWith(prefix));
                                    }).ToArray();
                                    Require(positions.Length>10,"Weighted hand surface missing");
                                    var centre=positions.Aggregate(Vector3.zero,(sum,point)=>sum+point)/positions.Length;
                                    return Vector3.Distance(centre,animator.GetBoneTransform(isLeft?HumanBodyBones.LeftHand:HumanBodyBones.RightHand).position);
                                }
                                sample.rightHandSkinDistance=HandDistance(false);sample.leftHandSkinDistance=HandDistance(true);
                                Require(sample.rightHandSkinDistance<.22f && sample.leftHandSkinDistance<.22f,"Hand surface does not follow solved wrist");
                            }
                            samples.Add(sample);
                        }
                    }
                }
                File.WriteAllText(Source + "Unity-Validation.json", JsonUtility.ToJson(new Record { unityVersion = Application.unityVersion, passed = true, triangles = counts, samples = samples.ToArray() }, true));
            }
            finally { Object.DestroyImmediate(baked); Object.DestroyImmediate(instance); }
        }

        private static void SetPhase(Animator animator, string phase)
        {
            animator.Rebind();
            animator.speed = 1;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.SetBool("Grounded", phase != "Jump");
            animator.SetBool("Dead", phase == "Death");
            animator.SetFloat("Gait", 1);
            animator.SetFloat("Speed", phase == "Run" ? 9 : phase == "Backward" ? -9 : 0);
            animator.Play(phase == "Death" ? "Base Layer.Death" : phase == "Jump" ? "Base Layer.Air" : "Base Layer.Locomotion", 0, 0);
            animator.Update(0);
        }

        public static void Capture()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.34f,.36f,.40f);
            foreach (var spec in new[] { ("Key",1.5f,new Vector3(35,155,0)), ("Fill",.6f,new Vector3(20,-100,0)) })
            {
                var light = new GameObject(spec.Item1).AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = spec.Item2; light.transform.rotation = Quaternion.Euler(spec.Item3);
            }
            var camera = new GameObject("Player pilot review camera").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1.1f;
            camera.nearClipPlane = .01f; camera.farClipPlane = 30;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.15f,.17f,.20f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var animator = instance.GetComponent<Animator>();
            var p = instance.GetComponent<ContenderPresentation>();
            var lod = instance.GetComponent<LODGroup>(); lod.ForceLOD(0);
            captureSkins=p.BodyRenderers.Cast<SkinnedMeshRenderer>().ToArray();captureLevel=0;
            var bones = instance.GetComponentsInChildren<Transform>().Where(t => p.BodyRenderers.Cast<SkinnedMeshRenderer>().First().bones.Contains(t)).ToArray();
            // Source rest frames come from the retained rig's skin bind matrices, not a posed prefab.
            var skin = (SkinnedMeshRenderer)p.BodyRenderers[0];
            var rest = bones.Select(b => new BoneFrame { name=b.name,
                rest=skin.transform.localToWorldMatrix * skin.sharedMesh.bindposes[Array.IndexOf(skin.bones,b)] .inverse }).ToArray();
            p.Weapon.gameObject.SetActive(false);
            animator.enabled = false;
            foreach (var view in new[] { ("Front",new Vector3(0,1,5)),("Right",new Vector3(-5,1,0)),
                ("Back",new Vector3(0,1,-5)),("ThreeQuarter",new Vector3(-3,1.3f,5)) })
            {
                camera.transform.position = view.Item2; camera.transform.LookAt(new Vector3(0,.96f,0));
                Write(camera,900,1100,Source+"Unity-"+view.Item1+".png");
            }
            camera.transform.position = new Vector3(-.45f,1.75f,3); camera.transform.LookAt(new Vector3(0,1.71f,0));
            camera.orthographicSize = .22f; Write(camera,1000,1000,Source+"Unity-Face.png");
            animator.enabled = true; p.Weapon.gameObject.SetActive(true);
            SetPhase(animator,"Idle"); Fit(animator,p,0);
            for(int i=0;i<rest.Length;i++)rest[i].posed=bones[i].localToWorldMatrix;
            File.WriteAllText(Source+"Fitted-Pose.json",JsonUtility.ToJson(new PoseRecord{bones=rest,weapon=p.Weapon.localToWorldMatrix,
                rightGrip=p.RightGrip.localToWorldMatrix,leftGrip=p.LeftGrip.localToWorldMatrix,shoulder=p.Railgun.Shoulder.localToWorldMatrix},true));
            camera.orthographicSize=1.12f; camera.transform.position=new Vector3(-3,1.45f,5);camera.transform.LookAt(new Vector3(0,1.0f,.10f));
            Write(camera,1100,1100,Source+"Unity-Carbine-Fit.png");
            foreach(var contact in new[]{("RightHand",p.RightGrip.position,new Vector3(.5f,.15f,.25f)),
                ("LeftHand",p.LeftGrip.position,new Vector3(-.5f,.12f,.25f)),
                ("Shoulder",p.Railgun.Shoulder.position,new Vector3(.6f,.2f,-.4f))})
            {
                camera.orthographicSize=.22f;camera.transform.position=contact.Item2+contact.Item3;
                camera.transform.LookAt(contact.Item2);Write(camera,1100,1100,Source+"Unity-Contact-"+contact.Item1+".png");
            }
            camera.orthographicSize=1.12f;camera.transform.position=new Vector3(-3,1.45f,5);camera.transform.LookAt(new Vector3(0,1.0f,.10f));
            foreach (int level in new[]{0,1,2}) { lod.ForceLOD(level);captureLevel=level; Write(camera,900,1100,Source+"Unity-LOD"+level+".png"); }
            lod.ForceLOD(0);captureLevel=0;
            Directory.CreateDirectory(Source+"Motion");
            int index=0;
            foreach(string phase in new[]{"Idle","Run","Backward","Jump","Death"})
            {
                SetPhase(animator,phase);
                p.Weapon.gameObject.SetActive(phase!="Death");
                for(int frame=0;frame<24;frame++)
                {
                    animator.Update(1f/24);
                    if(phase!="Death")Fit(animator,p,0);
                    Write(camera,600,720,Source+"Motion/frame-"+(index++).ToString("0000")+".png");
                }
            }
            // Save an editable isolated review scene, staged at idle with the fitted weapon.
            SetPhase(animator,"Idle");p.Weapon.gameObject.SetActive(true);Fit(animator,p,0);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Root+"/Player-Pilot-Review.unity");
            captureSkins=null;
            Debug.Log("PLAYER_PILOT_CAPTURE_COMPLETE: 120 fixed-step animation frames; isolated art review, not gameplay acceptance.");
        }

        private static void Write(Camera camera,int width,int height,string path)
        {
            var target=new RenderTexture(width,height,24);
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            GameObject proxy=null;Mesh deformed=null;
            var enabled=captureSkins?.Select(s=>s.enabled).ToArray();
            try
            {
                if(captureSkins!=null)
                {
                    // Editor offscreen draws can reuse an earlier GPU skin pose. Bake the
                    // imported skin with its current bones and render that evaluated surface.
                    var skin=captureSkins[captureLevel];deformed=new Mesh();skin.BakeMesh(deformed,true);
                    var matrix=skin.transform.localToWorldMatrix;
                    deformed.vertices=deformed.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                    deformed.normals=deformed.normals.Select(n=>matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                    deformed.RecalculateBounds();
                    proxy=new GameObject("Temporary evaluated pilot skin");proxy.AddComponent<MeshFilter>().sharedMesh=deformed;
                    proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                    foreach(var s in captureSkins)s.enabled=false;
                }
                camera.targetTexture=target;
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally
            {
                if(captureSkins!=null)for(int i=0;i<captureSkins.Length;i++)captureSkins[i].enabled=enabled[i];
                if(proxy!=null)Object.DestroyImmediate(proxy);if(deformed!=null)Object.DestroyImmediate(deformed);
                camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);
            }
        }

        private static void Require(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException("Player pilot: "+message);
        }
    }
}
