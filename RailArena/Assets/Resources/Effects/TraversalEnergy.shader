Shader "RailArena/TraversalEnergy"
{
    Properties { _PortalTex("Destination", 2D) = "black" {} _PortalViewport("Destination viewport", Vector) = (0,0,1,1) _HasView("Live destination", Float) = 0  _Tint("Tint", Color) = (0, .5, 1, 1) _Mode("Pad mode", Float) = 0 _Clock("Clock", Float) = 0 _Burst("Burst", Float) = 0 _Calm("Reduced flash", Float) = 0 _Charge("Charge", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_PortalTex); SAMPLER(sampler_PortalTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _PortalTex_TexelSize;
            float4 _PortalViewport;
            float4 _Tint; float _Mode, _Clock, _Burst, _Calm, _Charge, _HasView;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 local:TEXCOORD1; float4 screen:TEXCOORD2; };
            Varyings Vert(Attributes input) { Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.uv=input.uv; o.local=input.positionOS.xyz; o.screen=ComputeScreenPos(o.positionCS); return o; }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p = _Mode > .5 ? i.local.xz * 2 : i.uv * 2 - 1;
                float r=length(p), angle=atan2(p.y,p.x), t=_Clock;
                float spiral=sin(r*22-angle*4-t*3+sin(angle*3+t)*.8);
                float strands=pow(saturate(.5+.5*spiral),4);
                float rim=pow(saturate(1-abs(r-.84)/.12),2);
                float aperture=saturate((1.03-r)*8);
                float field=.07+aperture*(.16+strands*.7+rim*.8)*(saturate(r*2)+.2);
                if (_Mode>.5) field=.2+pow(saturate(.5+.5*sin(r*25-t*5)),5)*.8+rim*.9+_Charge*.25;
                field+=_Burst*(.3+.65*pow(saturate(.5+.5*sin(r*18-t*8)),3));
                field*=lerp(1,.55,_Calm);
                if (_Mode < .5 && _HasView > .5)
                {
                    float2 uv = i.screen.xy / i.screen.w;
                    uv=(uv-_PortalViewport.xy)/max(_PortalViewport.zw,float2(.00001,.00001));
                    half3 destination = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, uv).rgb;
                    float border = smoothstep(.74,1,r);
                    return half4(destination * (1-border*.25) + _Tint.rgb * (field * (.035 + border*.5) + _Burst*.05),1);
                }
                return half4(_Tint.rgb*min(field,2.5),1);
            }
            ENDHLSL
        }
    }
}
