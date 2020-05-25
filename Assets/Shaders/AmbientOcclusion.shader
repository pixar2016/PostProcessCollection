Shader "Pixar/AmbientOcclusion"
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
            #pragma vertex Vertex
            #pragma fragment FragmentShadow
            #include "RayTrace.cginc"
            #pragma enable_d3d11_debug_symbols
            ENDCG
        }
    }
}
