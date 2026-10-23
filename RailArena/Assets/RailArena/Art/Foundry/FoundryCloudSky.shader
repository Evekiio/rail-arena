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

            half4 Frag(Varyings input) : SV_Target
            {
                float3 ray = normalize(input.direction);
                float height = ray.y;
                float azimuth = atan2(ray.z,ray.x) / 6.2831853;
                // Periodic angular facets keep the cloud sea seamless and stationary.
                float ridge = abs(frac(azimuth * 12 + .15) * 2 - 1);
                float farRidge = abs(frac(azimuth * 7 + .34) * 2 - 1);
                float3 sky = lerp(_HorizonColor.rgb,_ZenithColor.rgb,pow(saturate(height),.65));
                float farBank = 1 - smoothstep(-.13 + farRidge*.05,-.12 + farRidge*.05,height);
                float nearBank = 1 - smoothstep(-.27 + ridge*.07,-.26 + ridge*.07,height);
                sky = lerp(sky,_CloudColor.rgb,farBank);
                sky = lerp(sky,lerp(_CloudColor.rgb,_CloudShadow.rgb,.55 + ridge*.2),nearBank);
                sky = lerp(sky,_CloudShadow.rgb,saturate((-height-.4)*1.3));
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
