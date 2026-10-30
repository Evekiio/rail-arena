Shader "Hidden/RailArena/Validation/LightResponsiveWater"
{
    Properties { [MainColor] _BaseColor("Water tint", Color) = (.13,.59,.68,.72) _Foam("Foam ribbon", Float) = 0 _Clock("Flow clock", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; float _Foam, _Clock;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings Vert(Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.world = TransformObjectToWorld(i.positionOS.xyz); return o; }
            half4 Frag(Varyings i) : SV_Target
            {
                float streak = pow(saturate(.5 + .5 * sin(i.world.z * 19 + i.world.x * 11 + sin(i.world.y * 2 + _Clock * 5))), 5);
                float flow = .5 + .5 * sin(i.world.y * 5 + _Clock * 12 + i.world.z * 4);
                float foam = saturate(streak * .55 + pow(flow, 8) * .35 + _Foam * .5);
                float3 view = SafeNormalize(GetCameraPositionWS() - i.world);
                float3 normal = SafeNormalize(cross(ddy(i.world), ddx(i.world)));
                normal *= dot(normal, view) < 0 ? -1 : 1;
                float3 tangent = cross(normal, float3(0, 1, 0));
                tangent = dot(tangent, tangent) < .001 ? cross(normal, float3(1, 0, 0)) : tangent;
                tangent = SafeNormalize(tangent);
                float3 bitangent = cross(normal, tangent);
                normal = SafeNormalize(normal + tangent * (.035 * sin(i.world.y * 5 + _Clock * 2)) + bitangent * (.035 * cos(i.world.z * 6 + _Clock * 1.7)));
                Light sun = GetMainLight();
                float fresnel = pow(1 - saturate(dot(normal, view)), 5);
                float specular = pow(saturate(dot(normal, SafeNormalize(view + sun.direction))), 48);
                float3 baseColor = lerp(_BaseColor.rgb, float3(.9, .98, 1), foam);
                float3 lighting = fresnel * .35 + specular * sun.color * .8;
                return half4(baseColor + lighting * (1 - _Foam * .55), _BaseColor.a * lerp(.65, 1, flow));
            }
            ENDHLSL
        }
    }
}
