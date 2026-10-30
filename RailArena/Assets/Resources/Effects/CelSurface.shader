Shader "RailArena/CelSurface"
{
    Properties
    {
        [MainTexture] _BaseMap("Painted surface", 2D) = "white" {}
        [MainColor] _BaseColor("Palette tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Surface normals", 2D) = "bump" {}
        _BumpScale("Normal strength", Range(0,2)) = 1
        _OcclusionMap("Cavity occlusion", 2D) = "white" {}
        _OcclusionStrength("Cavity strength", Range(0,1)) = 1
        _EmissionMap("Emissive mask", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,1)
        _BandLow("Lower shadow edge", Range(0,1)) = .25
        _BandHigh("Upper shadow edge", Range(0,1)) = .65
        _BandSoftness("Edge softness", Range(.001,.1)) = .03
        _ShadowTint("Shadow palette", Color) = (.38,.47,.59,1)
        _MidTint("Midtone palette", Color) = (.72,.79,.84,1)
        _HighlightStrength("Enamel highlight", Range(0,.5)) = .08
        _Cutoff("Cutout threshold", Range(0,1)) = .5
        [HideInInspector] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
        TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor, _EmissionColor, _ShadowTint, _MidTint;
        half _BumpScale, _OcclusionStrength, _BandLow, _BandHigh, _BandSoftness, _HighlightStrength, _Cutoff;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            half fog : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes i)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_TRANSFER_INSTANCE_ID(i, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
            VertexNormalInputs n = GetVertexNormalInputs(i.normalOS, i.tangentOS);
            o.positionCS = p.positionCS; o.positionWS = p.positionWS;
            o.normalWS = n.normalWS;
            o.tangentWS = half4(n.tangentWS, i.tangentOS.w * GetOddNegativeScale());
            o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
            o.fog = ComputeFogFactor(p.positionCS.z);
            return o;
        }
        void AlphaClip(float2 uv)
        {
            #if defined(_ALPHATEST_ON)
            clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a - _Cutoff);
            #endif
        }
        half3 SurfaceNormal(Varyings i)
        {
            half3 n = normalize(i.normalWS);
            #if defined(_NORMALMAP)
            half3 t = normalize(i.tangentWS.xyz);
            half3 b = cross(n, t) * i.tangentWS.w;
            half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
            n = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(t, b, n)));
            #endif
            return n;
        }
        ENDHLSL
        Pass
        {
            Name "CelForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                AlphaClip(i.uv);
                half3 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half3 n = SurfaceNormal(i);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half exposure = saturate(dot(n, sun.direction)) * sun.shadowAttenuation;
                half low = smoothstep(_BandLow - _BandSoftness, _BandLow + _BandSoftness, exposure);
                half high = smoothstep(_BandHigh - _BandSoftness, _BandHigh + _BandSoftness, exposure);
                half3 bands = lerp(lerp(_ShadowTint.rgb, _MidTint.rgb, low), half3(1,1,1), high);
                half occlusion = lerp(1, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, i.uv).g, _OcclusionStrength);
                half3 ambient = max(SampleSH(n), half3(.04,.04,.04));
                AmbientOcclusionFactor screenAO = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                half3 color = base * bands * (sun.color * .85 * screenAO.directAmbientOcclusion + ambient * .5 * screenAO.indirectAmbientOcclusion) * occlusion;
                half3 view = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half sheen = smoothstep(.93, .97, saturate(dot(n, SafeNormalize(sun.direction + view))));
                color += sheen * _HighlightStrength * sun.color * sun.shadowAttenuation;
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint count = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
                    color += base * light.color * light.distanceAttenuation * light.shadowAttenuation * saturate(dot(n, light.direction));
                }
                #endif
                LIGHT_LOOP_BEGIN(count)
                    Light light = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
                    color += base * light.color * light.distanceAttenuation * light.shadowAttenuation * saturate(dot(n, light.direction));
                LIGHT_LOOP_END
                #endif
                color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            float3 _LightDirection, _LightPosition;
            Varyings ShadowVert(Attributes i)
            {
                Varyings o = Vert(i);
                float3 direction = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                direction = normalize(_LightPosition - o.positionWS);
                #endif
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(o.positionWS, o.normalWS, direction));
                o.positionCS = ApplyShadowClamping(o.positionCS);
                return o;
            }
            half4 DepthFrag(Varyings i) : SV_Target { AlphaClip(i.uv); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            half4 DepthFrag(Varyings i) : SV_Target { AlphaClip(i.uv); return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalFrag(Varyings i) : SV_Target
            {
                AlphaClip(i.uv);
                float3 normal = SurfaceNormal(i);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = saturate(PackNormalOctQuadEncode(normal) * .5 + .5);
                return half4(PackFloat2To888(oct), 0);
                #else
                return half4(normal, 0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack Off
}
