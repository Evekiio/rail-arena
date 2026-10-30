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
            if (!ArtSliceBuilder.Building || shader.name != "Universal Render Pipeline/Lit") return;
            var transparent = new ShaderKeyword(shader, "_SURFACE_TYPE_TRANSPARENT");
            var parallax = new ShaderKeyword(shader, "_PARALLAXMAP");
            var detail = new ShaderKeyword(shader, "_DETAIL_MULX2");
            var scaledDetail = new ShaderKeyword(shader, "_DETAIL_SCALED");
            int removed = 0;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var keywords = data[i].shaderKeywordSet;
                if (!keywords.IsEnabled(transparent) && (keywords.IsEnabled(parallax) || keywords.IsEnabled(detail) || keywords.IsEnabled(scaledDetail)))
                { data.RemoveAt(i); removed++; }
            }
            if (removed > 0) Debug.Log("ART_SHADER_VARIANTS " + snippet.passName + " legacy opaque detail removed=" + removed + " retained=" + data.Count);
        }
    }
}
