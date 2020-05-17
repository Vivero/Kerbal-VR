// Upgrade NOTE: upgraded instancing buffer 'InstanceProperties' to new syntax.

#if !defined(MY_SHADOWS_INCLUDED)
#define MY_SHADOWS_INCLUDED

#include "UnityCG.cginc"

#if defined(_RENDERING_FADE) || defined(_RENDERING_TRANSPARENT)
	#if defined(_SEMITRANSPARENT_SHADOWS)
		#define SHADOWS_SEMITRANSPARENT 1
	#else
		#define _RENDERING_CUTOUT
	#endif
#endif

#if SHADOWS_SEMITRANSPARENT || defined(_RENDERING_CUTOUT)
	#if !defined(_SMOOTHNESS_ALBEDO)
		#define SHADOWS_NEED_UV 1
	#endif
#endif

UNITY_INSTANCING_BUFFER_START(InstanceProperties)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
#define _Color_arr InstanceProperties
UNITY_INSTANCING_BUFFER_END(InstanceProperties)

sampler2D _MainTex;
float4 _MainTex_ST;
float _Cutoff;

sampler3D _DitherMaskLOD;

struct VertexData {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 position : POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};

struct InterpolatorsVertex {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 position : SV_POSITION;
	#if SHADOWS_NEED_UV
		float2 uv : TEXCOORD0;
	#endif
	#if defined(SHADOWS_CUBE)
		float3 lightVec : TEXCOORD1;
	#endif
};

struct Interpolators {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	#if SHADOWS_SEMITRANSPARENT || defined(LOD_FADE_CROSSFADE)
		UNITY_VPOS_TYPE vpos : VPOS;
	#else
		float4 positions : SV_POSITION;
	#endif

	#if SHADOWS_NEED_UV
		float2 uv : TEXCOORD0;
	#endif
	#if defined(SHADOWS_CUBE)
		float3 lightVec : TEXCOORD1;
	#endif
};

float GetAlpha (Interpolators i) {
	float alpha = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color).a;
	#if SHADOWS_NEED_UV
		alpha *= tex2D(_MainTex, i.uv.xy).a;
	#endif
	return alpha;
}

InterpolatorsVertex MyShadowVertexProgram (VertexData v) {
	InterpolatorsVertex i;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, i);
	#if defined(SHADOWS_CUBE)
		i.position = UnityObjectToClipPos(v.position);
		i.lightVec =
			mul(unity_ObjectToWorld, v.position).xyz - _LightPositionRange.xyz;
	#else
		i.position = UnityClipSpaceShadowCasterPos(v.position.xyz, v.normal);
		i.position = UnityApplyLinearShadowBias(i.position);
	#endif

	#if SHADOWS_NEED_UV
		i.uv = TRANSFORM_TEX(v.uv, _MainTex);
	#endif
	return i;
}

float4 MyShadowFragmentProgram (Interpolators i) : SV_TARGET {
	UNITY_SETUP_INSTANCE_ID(i);
	#if defined(LOD_FADE_CROSSFADE)
		UnityApplyDitherCrossFade(i.vpos);
	#endif

	float alpha = GetAlpha(i);
	#if defined(_RENDERING_CUTOUT)
		clip(alpha - _Cutoff);
	#endif

	#if SHADOWS_SEMITRANSPARENT
		float dither =
			tex3D(_DitherMaskLOD, float3(i.vpos.xy * 0.25, alpha * 0.9375)).a;
		clip(dither - 0.01);
	#endif
	
	#if defined(SHADOWS_CUBE)
		float depth = length(i.lightVec) + unity_LightShadowBias.x;
		depth *= _LightPositionRange.w;
		return UnityEncodeCubeShadowDepth(depth);
	#else
		return 0;
	#endif
}

#endif