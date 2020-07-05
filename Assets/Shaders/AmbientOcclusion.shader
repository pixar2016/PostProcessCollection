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
        CGINCLUDE
        #pragma enable_d3d11_debug_symbols
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentShadow
            #include "RayTrace.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentTempFilter
            #pragma target 4.5
            #include "TempFilter.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentTempFilter
            #pragma target 4.5
            #define TEMP_FILTER_ALT
            #include "TempFilter.cginc"
            ENDCG
        }
        Pass
        {
            Blend Zero SrcAlpha
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment FragmentComposite
            #include "Composite.cginc"
            ENDCG
        }
    }
}
