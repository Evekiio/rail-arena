using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailArena.Editor
{
    /// <summary>Legacy opaque detail shaders are replaced by cel surfaces in the Sky Gardens art build.</summary>
    public sealed class ArtBuildShaderVariants : IPreprocessShaders
    {
        public int callbackOrder => 1000;
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            bool legacy = shader.name == "Universal Render Pipeline/Lit";
            if (!ArtSliceBuilder.Building || (!legacy && shader.name != "RailArena/CelSurface")) return;
            bool forward = snippet.passName == "ForwardLit" || snippet.passName == "CelForward";
            var transparent = new ShaderKeyword(shader, "_SURFACE_TYPE_TRANSPARENT");
            var parallax = new ShaderKeyword(shader, "_PARALLAXMAP");
            var detail = new ShaderKeyword(shader, "_DETAIL_MULX2");
            var scaledDetail = new ShaderKeyword(shader, "_DETAIL_SCALED");
            int removed = 0;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var keywords = data[i].shaderKeywordSet;
                bool unusedPipeline = forward && (!Enabled(keywords, "_CLUSTER_LIGHT_LOOP") || Enabled(keywords, "_MAIN_LIGHT_SHADOWS_SCREEN") ||
                    Enabled(keywords, "_SHADOWS_SOFT_LOW") || Enabled(keywords, "_SHADOWS_SOFT_MEDIUM") || Enabled(keywords, "FOG_EXP") || Enabled(keywords, "FOG_EXP2"));
                if (unusedPipeline || (legacy && !keywords.IsEnabled(transparent) && (keywords.IsEnabled(parallax) || keywords.IsEnabled(detail) || keywords.IsEnabled(scaledDetail))))
                { data.RemoveAt(i); removed++; }
            }
            if (removed > 0) Debug.Log("ART_SHADER_VARIANTS " + shader.name + " / " + snippet.passName + " unused target variants removed=" + removed + " retained=" + data.Count);
        }
        static bool Enabled(ShaderKeywordSet keywords, string name) => keywords.IsEnabled(new ShaderKeyword(name));
    }
}
