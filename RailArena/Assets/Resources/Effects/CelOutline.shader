Shader "RailArena/CelOutline"
{
    Properties { _OutlineColor("Ink",Color)=(.012,.019,.025,1) _OutlinePixels("Width in pixels",Range(0,2))=1.25 _FadeStart("Fade starts (m)",Float)=12 _FadeEnd("Fade ends (m)",Float)=38 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry-10" }
        Pass
        {
            Name "SelectiveContour"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor; float _OutlinePixels, _FadeStart, _FadeEnd;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION;half fog:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(world);
                float3 normal=TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
                float2 direction=normal.xy*rsqrt(max(dot(normal.xy,normal.xy),.0001));
                float fade=1-smoothstep(_FadeStart,_FadeEnd,distance(world,GetCameraPositionWS()));
                output.positionCS.xy+=direction*(2*_OutlinePixels/_ScaledScreenParams.xy)*output.positionCS.w*fade;
                output.fog=ComputeFogFactor(output.positionCS.z);return output;
            }
            half4 Frag(Varyings input):SV_Target {return half4(MixFog(_OutlineColor.rgb,input.fog),1);}
            ENDHLSL
        }
    }
}
