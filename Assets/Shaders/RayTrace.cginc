
#include "UnityCG.cginc"
sampler2D _CameraDepthTexture;
float4 _CameraDepthTexture_TexelSize;

sampler2D _ShadowMask;

sampler2D _NoiseTex;

float3 _LightVector;

float _RejectionDepth;

uint _SampleCount;

// Vertex shader - Full-screen triangle with procedural draw
float2 Vertex(
	uint vertexID : SV_VertexID,
	out float4 position : SV_POSITION
) : TEXCOORD
{
	float x = (vertexID != 1) ? -1 : 3;
	float y = (vertexID == 2) ? -3 : 1;
	position = float4(x, y, 1, 1);

	float u = (x + 1) / 2;
#ifdef UNITY_UV_STARTS_AT_TOP
	float v = (1 - y) / 2;
#else
	float v = (y + 1) / 2;
#endif
	return float2(u, v);
}

float SampleDepth(float2 uv){
	float z = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv, 0, 0));
#if defined(UNITY_REVERSED_Z)
	z = 1 - z;
#endif
	return z;
}

float3 GetViewPosFromDepth(float2 uv, float z){
	//屏幕空间->裁剪空间 
	float4 clipPos = float4(float3(uv, z) * 2 - 1, 1);
	//裁剪空间->观察空间/摄像机空间
	float4 viewPos = mul(unity_CameraInvProjection, clipPos);
	return float3(viewPos.xy, -viewPos.z) / viewPos.w;
}

float2 ProjectVP(float3 viewPos){
	float4 clipPos = mul(unity_CameraProjection, float4(viewPos.xy, -viewPos.z, 1));
	return (clipPos.xy / clipPos.w + 1) * 0.5;
}

half4 FragmentShadow(float2 uv : TEXCOORD) : SV_Target{
	float shadowmask = tex2D(_ShadowMask, uv).r;
	float offs = tex2D(_NoiseTex, uv).a;
	float z0 = SampleDepth(uv);
	//uv对应观察空间坐标
	float3 viewPos0 = GetViewPosFromDepth(uv, z0);
	UNITY_LOOP for(uint i = 0; i < _SampleCount; i++){
		//由该点向光源发射线
		float3 viewPos_ray = viewPos0 + _LightVector * (i + offs * 2);
		float2 temp_uv = ProjectVP(viewPos_ray);
		float z = SampleDepth(temp_uv);
		float3 viewPos_depth = GetViewPosFromDepth(temp_uv, z);

		float diff = viewPos_ray.z - viewPos_depth.z;

		if(diff > 0.01 && diff < _RejectionDepth) return 0;
	}
	return shadowmask;
}