Shader "RailArena/ArtAtmosphere"
{
    Properties
    {
        _AtmosphereColor ("Atmosphere Color", Color) = (0.55, 0.7, 0.78, 1)
        _AtmosphereParams ("Density, Height Falloff, Distance, Intensity", Vector) = (0.006, 0.02, 100, 0.8)
        _AtmosphereShadowStrength ("Shadow Strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "SkyGardensAtmosphere"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float4 _AtmosphereColor;
            float4 _AtmosphereParams; // density, height falloff, max distance, intensity
            float _AtmosphereShadowStrength;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool isSky = depth <= 0.000001;
                    float deviceDepth = max(depth, 0.000001);
                #else
                    bool isSky = depth >= 0.999999;
                    float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, min(depth, 0.999999));
                #endif
                float3 world = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 delta = world - GetCameraPositionWS();
                float lengthToSurface = length(delta);
                float rayLength = isSky ? _AtmosphereParams.z : min(lengthToSurface, _AtmosphereParams.z);
                float3 ray = delta / max(lengthToSurface, 0.001);
                float stepLength = rayLength / 12.0;
                float transmittance = 1.0;
                float scatter = 0.0;
                [unroll] for (int i = 0; i < 12; i++)
                {
                    float distanceAlongRay = (i + 0.5) * stepLength;
                    float3 p = GetCameraPositionWS() + ray * distanceAlongRay;
                    float density = _AtmosphereParams.x * exp(-max(p.y, 0.0) * _AtmosphereParams.y);
                    float extinction = 1.0 - exp(-density * stepLength);
                    Light sun = GetMainLight(TransformWorldToShadowCoord(p));
                    float directional = lerp(1.0, sun.shadowAttenuation, 0.65 * _AtmosphereShadowStrength);
                    scatter += transmittance * extinction * directional;
                    transmittance *= 1.0 - extinction;
                }
                float amount = saturate(scatter * _AtmosphereParams.w);
                source.rgb = source.rgb * transmittance + _AtmosphereColor.rgb * amount;
                return source;
            }
            ENDHLSL
        }
    }
}
