using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace RailArena.Editor
{
    public static class RepairFoundryVolume
    {
        public static void Apply()
        {
            const string path="Assets/RailArena/Generated/Foundry grading.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(profile==null)throw new System.InvalidOperationException("Missing Foundry grading profile");
            profile.components.RemoveAll(component=>component==null);
            if (!profile.TryGet<Tonemapping>(out _))
                profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            if (!profile.TryGet<Bloom>(out _))
            {
                var bloom = profile.Add<Bloom>();
                bloom.intensity.Override(.5f); bloom.threshold.Override(1); bloom.clamp.Override(10); bloom.dirtIntensity.Override(0);
            }
            if (!profile.TryGet<Vignette>(out _))
                profile.Add<Vignette>().intensity.Override(.18f);
            foreach(var component in profile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var saved=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(saved.components.Exists(component=>component==null)||!saved.TryGet<Tonemapping>(out _)||!saved.TryGet<Bloom>(out _)||!saved.TryGet<Vignette>(out _))
                throw new System.InvalidOperationException("Volume profile failed serialization check");
            UnityEngine.Debug.Log("FOUNDRY_VOLUME_SERIALIZATION_VERIFIED");
        }
    }
}
