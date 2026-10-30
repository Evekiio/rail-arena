Shader "Rail Arena/Foundry Cloud Sky"
{
    Properties
    {
        _ZenithColor ("Upper sky", Color) = (.22,.46,.65,1)
        _HorizonColor ("Bright horizon", Color) = (.74,.84,.87,1)
        _CloudColor ("Cloud highlights", Color) = (.91,.91,.84,1)
        _CloudShadow ("Cloud undersides", Color) = (.57,.70,.75,1)
        _SunDirection ("Direction to sun", Vector) = (-.5,.7,.5,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            struct Attributes { float4 vertex : POSITION; };
            struct Varyings { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            float4 _ZenithColor, _HorizonColor, _CloudColor, _CloudShadow, _SunDirection;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = mul((float3x3)unity_ObjectToWorld,input.vertex.xyz);
                return output;
            }

            float Hash(float3 p)
            {
                p = frac(p * .1031);
                p += dot(p,p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float CloudNoise(float3 p)
            {
                float3 cell = floor(p);
                float3 blend = frac(p);
                blend = blend * blend * (3 - 2 * blend);
                float lower = lerp(lerp(Hash(cell),Hash(cell + float3(1,0,0)),blend.x),
                    lerp(Hash(cell + float3(0,1,0)),Hash(cell + float3(1,1,0)),blend.x),blend.y);
                float upper = lerp(lerp(Hash(cell + float3(0,0,1)),Hash(cell + float3(1,0,1)),blend.x),
                    lerp(Hash(cell + float3(0,1,1)),Hash(cell + float3(1,1,1)),blend.x),blend.y);
                return lerp(lower,upper,blend.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 ray = normalize(input.direction);
                float height = ray.y;
                // Direction-space noise is continuous across the wrap and poles, with no time input.
                float broad = CloudNoise(ray * 5 + float3(3,9,2));
                float detail = CloudNoise(ray * 17 + float3(18,1,5));
                float3 sky = lerp(_HorizonColor.rgb,_ZenithColor.rgb,pow(saturate(height),.65));
                float bankHeight = -.10 + (broad - .5) * .24 + (detail - .5) * .045;
                float bank = 1 - smoothstep(bankHeight - .035,bankHeight + .035,height);
                float topLight = saturate((height - bankHeight + .22) * 3);
                float billowLight = saturate(.38 + broad * .35 + detail * .18 + topLight * .23);
                float3 cloud = lerp(_CloudShadow.rgb,_CloudColor.rgb,billowLight);
                sky = lerp(sky,cloud,bank);
                // Sparse soft banks above the horizon leave the aim field mostly open.
                float highCloud = smoothstep(.58,.75,broad * .72 + detail * .28);
                highCloud *= smoothstep(.035,.15,height) * (1 - smoothstep(.45,.7,height));
                sky = lerp(sky,_CloudColor.rgb,highCloud * .6);
                sky = lerp(sky,_CloudShadow.rgb,saturate((-height - .6) * 1.4));
                float facingSun = saturate(dot(ray,normalize(_SunDirection.xyz)));
                sky += float3(.12,.10,.055) * pow(facingSun,24);
                sky = lerp(sky,float3(1,.97,.83),smoothstep(.9996,.99985,facingSun));
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
