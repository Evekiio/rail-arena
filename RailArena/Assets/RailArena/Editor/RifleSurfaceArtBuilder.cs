using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class RifleSurfaceArtBuilder
    {
        public static void Prepare()
        {
            const string production = "Assets/RailArena/Art/ArtPass/Rifle/";
            Directory.CreateDirectory(production); AssetDatabase.Refresh();
            foreach (string file in new[] { "AO-white-background.png", "Carbine_LOD0.fbx", "Carbine_LOD1.fbx", "Carbine_LOD2.fbx" })
                if (AssetDatabase.LoadMainAssetAtPath(production + file) == null && !AssetDatabase.CopyAsset("Assets/Validation/CarbineCandidate/" + file, production + file))
                    throw new InvalidOperationException("Cannot promote rifle input " + file);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            const string path = "Assets/Resources/Art/RifleSurface.asset";
            var art = AssetDatabase.LoadAssetAtPath<RifleSurfaceArt>(path);
            if (art == null) { art = ScriptableObject.CreateInstance<RifleSurfaceArt>(); AssetDatabase.CreateAsset(art, path); }
            art.sourceSurface = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Art/PrecisionCarbine/Surface.mat");
            art.cavityAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(production + "AO-white-background.png");
            art.models = Enumerable.Range(0,3).Select(i => AssetDatabase.LoadAssetAtPath<GameObject>(production + "Carbine_LOD" + i + ".fbx")).ToArray();
            if (art.sourceSurface == null || art.cavityAtlas == null || art.models.Any(m => m == null)) throw new InvalidOperationException("Previously reviewed rifle graphics are incomplete");
            var cel = Resources.Load<CelArtProfile>("Art/SkyGardensCel");
            cel.WaterShader = Resources.Load<Shader>("Effects/CelWater");
            if (cel.WaterShader == null) throw new InvalidOperationException("Water shader was not imported");
            EditorUtility.SetDirty(art); EditorUtility.SetDirty(cel); AssetDatabase.SaveAssets();
            Debug.Log("RIFLE_SURFACE_GRAPHICS_PROMOTED repacked LODs and cavity atlas; physical optic preserved");
        }
    }
}
