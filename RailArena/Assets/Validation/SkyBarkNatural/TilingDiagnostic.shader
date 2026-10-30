Shader "Hidden/RailArena/Validation/TilingDiagnostic"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader { Pass { ZTest Always Cull Off ZWrite Off
        HLSLPROGRAM
        #pragma vertex vert_img
        #pragma fragment frag
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4 frag(v2f_img i) : SV_Target { return tex2D(_MainTex, i.uv * 2.0); }
        ENDHLSL
    } }
}
