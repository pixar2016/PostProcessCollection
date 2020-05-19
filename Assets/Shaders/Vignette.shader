Shader "Pixar/Vignette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _Aspect;
            float _FallOff;

            half4 frag (v2f_img i) : SV_Target
            {
                float2 temp1 = (i.uv - 0.5) * _Aspect * 2;
                float temp2 = dot(temp1, temp1) * _FallOff + 1;
                float e = 1 / (temp2 * temp2);
                half4 col = tex2D(_MainTex, i.uv);
                return half4(col.rgb * e, col.a);
            }
            ENDCG
        }
    }
}
