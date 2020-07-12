Shader "Pixar/ScanEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanTex("ScanTexture", 2D) = "white" {}
        _ScanDepth("ScanDepth", float) = 0
        _ScanWidth("ScanWidth", float) = 3
        _MeshWidth("MeshWidth",float) = 1
        _CamFar("CamFar", float) = 500
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 interpolateRay : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float4x4 _FrustumCorner;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                int rayIndex;
                if(v.uv.x<0.5&&v.uv.y<0.5){
					rayIndex = 0;
				}else if(v.uv.x>0.5&&v.uv.y<0.5){
					rayIndex = 1;
				}else if(v.uv.x>0.5&&v.uv.y>0.5){
					rayIndex = 2;
				}else{
					rayIndex = 3;
				}
				o.interpolateRay = _FrustumCorner[rayIndex];
                return o;
            }

            sampler2D _MainTex;
            sampler2D _ScanTex;
            float _ScanDepth;
            float _ScanWidth;
            float _MeshWidth;
            float3 _ScanCenter;
            float _CamFar;
            float4x4 _CamToWorld;

            sampler2D_float _CameraDepthTexture;
            sampler2D _CameraDepthNormalsTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = Linear01Depth(depth);

                float3 pixelWorldPos = _WorldSpaceCameraPos + linearDepth * i.interpolateRay;
                float pixelLinear = distance(pixelWorldPos, _ScanCenter)/_CamFar;
               	

               	float tempDepth;
				half3 normal;  
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv), tempDepth, normal);  
				normal = mul( (float3x3)_CamToWorld, normal);  
				normal = normalize(max(0, (abs(normal) - 0.25)));

                float3 modulo = pixelWorldPos - _MeshWidth*floor(pixelWorldPos/_ScanWidth);
				modulo = modulo/_MeshWidth;

				fixed4 c_right = tex2D(_ScanTex,modulo.yz)*normal.x;
				fixed4 c_front = tex2D(_ScanTex,modulo.xy)*normal.z;
				fixed4 c_up = tex2D(_ScanTex,modulo.xz)*normal.y;
				//混合
				fixed4 scanMeshCol =saturate(c_up +c_right+c_front);

				// float2 calculatedUV = modulo.xy;
				// if(normal.x > normal.y && normal.x > normal.z){
				// 	calculatedUV = modulo.yz;
				// }
				// else if(normal.y > normal.x && normal.y > normal.z){
				// 	calculatedUV = modulo.xz;
				// }
				// fixed4 scanMeshCol = tex2D(_ScanTex, calculatedUV);

                if(pixelLinear < _ScanDepth && pixelLinear > _ScanDepth - _ScanWidth/_CamFar){
                	fixed scanPer = 1 - (_ScanDepth - pixelLinear) / (_ScanWidth/_CamFar);
                	return lerp(col, scanMeshCol, scanPer);
                }

                // if(linearDepth < _ScanDepth && linearDepth > _ScanDepth - _ScanWidth/_CamFar){
                // 	fixed sacnPercent = 1 - (_ScanDepth - linearDepth)/(_ScanWidth/_CamFar);
                // 	return lerp(col, fixed4(1,1,0,1), sacnPercent);
                // }
                return col;
            }
            ENDCG
        }
    }
}
