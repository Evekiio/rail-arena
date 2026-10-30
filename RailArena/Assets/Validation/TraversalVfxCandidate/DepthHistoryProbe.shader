Shader "Hidden/RailArena/Validation/DepthHistoryProbe"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 frag(v2f_img input) : SV_Target
            {
                float depth = tex2D(_MainTex, input.uv).r;
                return float4(depth, depth, depth, 1);
            }
            ENDHLSL
        }
    }
}


