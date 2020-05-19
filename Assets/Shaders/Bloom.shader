Shader "Pixar/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    CGINCLUDE
    #pragma enable_d3d11_debug_symbols
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    sampler2D _BlurTex;
    float2 _MainTex_TexelSize;
    float _Threshold;
    float _Intensity;
    half4 frag_downsample(v2f_img i) : SV_Target
    {
        float4 d = _MainTex_TexelSize.xyxy * float4(1, 1, -1, -1);
        half4 s;
        s = tex2D(_MainTex, i.uv + d.xy);
        s += tex2D(_MainTex, i.uv + d.xw);
        s += tex2D(_MainTex, i.uv + d.zy);
        s += tex2D(_MainTex, i.uv + d.zw);
        //return s * 0.25;
        s = s * 0.25;
        return s;
    }
    half4 frag_threshold(v2f_img i) : SV_Target{
        half4 cs = tex2D(_MainTex, i.uv);
        half lm = Luminance(cs.rgb);
        return cs * smoothstep(_Threshold, _Threshold * 1.5, lm);
    }
    half4 gaussian_filter(float2 uv, float2 distance) {
        half4 s = tex2D(_MainTex, uv) * 0.227027027;

        float2 d1 = distance * 1.3846153846;
        s += tex2D(_MainTex, uv + d1) * 0.3162162162;
        s += tex2D(_MainTex, uv - d1) * 0.3162162162;

        float2 d2 = distance * 3.2307692308;
        s += tex2D(_MainTex, uv + d2) * 0.0702702703;
        s += tex2D(_MainTex, uv - d2) * 0.0702702703;

        return s;
    }

    half4 frag_gaussian_blur_h(v2f_img i) : SV_Target{
        return gaussian_filter(i.uv, float2(_MainTex_TexelSize.x, 0));
    }

    half4 frag_gaussian_blur_v(v2f_img i) : SV_Target{
        return gaussian_filter(i.uv, float2(0, _MainTex_TexelSize.y));
    }

    half4 gaussian_fliter1(float2 uv, float2 distance) : SV_Target{
    	half4 s = tex2D(_MainTex, uv) * 0.227027027;
    	float2 d1 = float2(distance.x, 0) * 1.3846153846;
    	s += tex2D(_MainTex, uv + d1) * 0.1581;
    	s += tex2D(_MainTex, uv - d1) * 0.1581;
    	float2 d2 = float2(0, distance.y) * 1.3846153846;
    	s += tex2D(_MainTex, uv + d2) * 0.1581;
    	s += tex2D(_MainTex, uv - d2) * 0.1581;

    	float2 d3 = float2(distance.x, 0) * 3.2307692308;
    	s += tex2D(_MainTex, uv + d3) * 0.03513;
    	s += tex2D(_MainTex, uv - d3) * 0.03513;
    	float2 d4 = float2(0, distance.y) * 3.2307692308;
    	s += tex2D(_MainTex, uv + d4) * 0.03513;
    	s += tex2D(_MainTex, uv - d4) * 0.03513;

    	return s;
    }

    half4 frag_gaussian(v2f_img i) : SV_Target{
    	return gaussian_fliter1(i.uv, float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y ));
    }

    half4 frag_composite(v2f_img i) : SV_Target{
        half3 c1 = LinearToGammaSpace(tex2D(_MainTex, i.uv).rgb);
        half3 c2 = LinearToGammaSpace(tex2D(_BlurTex, i.uv).rgb);
        half3 co = c1 + c2 * _Intensity;
        return half4(GammaToLinearSpace(co), 1.0f);
    }
    ENDCG
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_downsample
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_threshold
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_gaussian_blur_h
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_gaussian_blur_v
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_composite
            ENDCG
        }
    }
}
