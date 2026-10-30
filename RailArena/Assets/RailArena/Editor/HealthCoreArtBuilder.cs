using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class HealthCoreArtBuilder
    {
        public static void Prepare()
        {
            const string root = "Assets/RailArena/Art/ArtPass/Devices/";
            string[] names = { "MedicalIvory", "MedicalBronze", "MedicalEnergy", "MedicalGraphite" };
            Color[] colors = { new(.88f,.91f,.84f), new(.66f,.43f,.2f), new(.1f,.85f,.59f), new(.08f,.18f,.18f) };
            var profile = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            var source = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var materials = new Material[4];
            try
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    string path = root + names[i] + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) { material = new Material(profile.SurfaceShader); AssetDatabase.CreateAsset(material, path); }
                    profile.Configure(material, source); material.SetColor("_BaseColor", colors[i].linear);
                    material.SetFloat("_OcclusionStrength", 0);
                    material.SetColor("_EmissionColor", i == 2 ? colors[i].linear * .65f : Color.black);
                    material.enableInstancing = true; EditorUtility.SetDirty(material); materials[i] = material;
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Medical-Core-v1.fbx");
                var instance = UnityEngine.Object.Instantiate(model);
                try
                {
                    var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers)
                    {
                        bounds.Encapsulate(renderer.bounds);
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials[Array.IndexOf(names, m.name)]).ToArray();
                    }
                    if ((bounds.size - new Vector3(.864f,.96f,.145f)).sqrMagnitude > .0001f || bounds.center.sqrMagnitude > .0001f)
                        throw new InvalidOperationException("Medical core dimensions/pivot mismatch: " + bounds);
                    if (instance.GetComponentsInChildren<Collider>().Length != 0) throw new InvalidOperationException("Medical art must not change pickup collision");
                    PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Resources/Art/MedicalCore.prefab");
                    Debug.Log("MEDICAL_CORE_IMPORT " + bounds + " original pickup authority retained");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
            AssetDatabase.SaveAssets();
        }
    }
}
