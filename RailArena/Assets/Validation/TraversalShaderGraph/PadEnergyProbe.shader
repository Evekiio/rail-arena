Shader "Hidden/RailArena/Validation/PadEnergyProbe"
{
    Properties { _Params ("Clock, Burst, Calm, Charge", Vector) = (0, 0, 0, 0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Resources/Effects/PadEnergyNode.hlsl"
            float4 _Params;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input) { Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS.xyz); output.uv = input.uv; return output; }
            half4 Frag(Varyings input) : SV_Target
            {
                return half4(RailArenaPadEnergy(input.uv - .5, _Params.x, _Params.y, _Params.z, _Params.w, float3(.06, .65, .61)), 1);
            }
            ENDHLSL
        }
    }
}
