// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Image Effects/StylisticFog"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	#define FOG_HANDLE_GAMMA_CORRECTION

	#define SKYBOX_THREASHOLD_VALUE 0.9999
	#define FOG_AMOUNT_CONTRIBUTION_THREASHOLD 0.0001

	bool _ApplyDistToSkybox;
	bool _ApplyHeightToSkybox;

	half4 _MainTex_TexelSize;

	sampler2D _MainTex;
	sampler2D _CameraDepthTexture;

	sampler2D _FogColorTexture0;
	sampler2D _FogColorTexture1;

	float4x4 _InverseViewMatrix;

	uniform float _FogEndDistance;

	uniform float _Height;
	uniform float _BaseDensity;
	uniform float _DensityFalloff;
	half4 _MainTex_ST;

	struct v2f_multitex
	{
		float4 pos : SV_POSITION;
		float2 uv0 : TEXCOORD0;
		float2 uv1 : TEXCOORD1;
	};

	v2f_multitex vert_img_fog(appdata_img v)
	{
		// Handles vertically-flipped case.
		float vflip = sign(_MainTex_TexelSize.y);

		v2f_multitex o;
		o.pos = UnityObjectToClipPos(v.vertex);
		float2 uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
		o.uv0 = uv.xy;
		o.uv1 = (uv.xy - 0.5) * float2(1, vflip) + 0.5;
		return o;
	}

	// from https://github.com/keijiro/DepthToWorldPos
	inline float4 DepthToWorld(float depth, float2 uv, float4x4 inverseViewMatrix)
	{
		float viewDepth = LinearEyeDepth(depth);
		float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
		float3 vpos = float3((uv * 2 - 1) / p11_22, -1) * viewDepth;
		float4 wpos = mul(inverseViewMatrix, float4(vpos, 1));
		return wpos;
	}

	// Compute how intense the distance fog is according to the distance
	// and the fog intensity curve.
	inline float ComputeDistanceFogAmount(float distance)
	{
		float f = distance / _FogEndDistance;
		return saturate(f);
	}

	// Computes the amount of fog treversed based on a desnity function d(h)
	// where d(h) = _BaseDensity * exp2(-DensityFalloff * h) <=> d(h) = a * exp2(b * h)
	inline float ComputeHeightFogAmount(float viewDirY, float effectiveDistance)
	{
		float relativeHeight = _WorldSpaceCameraPos.y - _Height;
		return _BaseDensity * (exp2(-relativeHeight * _DensityFalloff) - exp2((-effectiveDistance * viewDirY - relativeHeight) * _DensityFalloff)) / viewDirY;
		//return _BaseDensity * exp2(-relativeHeight * _DensityFalloff) * (1. - exp2(-effectiveDistance * viewDirY * _DensityFalloff)) * 1./viewDirY;
	}

	inline half4 GetColorFromTexture(sampler2D source, float fogAmount)
	{
		half4 textureColor = tex2D(source, float2(fogAmount, 0));
		#if defined(UNITY_COLORSPACE_GAMMA) && defined(FOG_HANDLE_GAMMA_CORRECTION)
			textureColor.rgb = GammaToLinearSpace(textureColor.rgb);
		#endif
		return textureColor;
	}

	// Not used yet, but might be useful for pass seperation.
	inline half4 BlendFogToScene(float2 uv, half4 fogColor, float fogAmount)
	{
		// clamp the scene color to at most 1. to avoid HDR rendering to change lumiance in final image.
		half4 sceneColor = min(1., tex2D(_MainTex, uv));
		#if defined(UNITY_COLORSPACE_GAMMA) && defined(FOG_HANDLE_GAMMA_CORRECTION)
			sceneColor.rgb = GammaToLinearSpace(sceneColor.rgb);
		#endif

		half4 blended = lerp(sceneColor, half4(fogColor.xyz, 1.), fogColor.a * step(FOG_AMOUNT_CONTRIBUTION_THREASHOLD, fogAmount));
		#if defined(UNITY_COLORSPACE_GAMMA) && defined(FOG_HANDLE_GAMMA_CORRECTION)
			blended.rgb = LinearToGammaSpace(blended.rgb);
		#endif

		blended.a = 1.;
		return blended;
	}

	half4 fragment_distance(v2f_multitex i) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv1);

		float4 wpos = DepthToWorld(depth, i.uv1, _InverseViewMatrix);

		float4 cameraToFragment = wpos - float4(_WorldSpaceCameraPos, 1.);
		float totalDistance = length(cameraToFragment);

		float linDepth = Linear01Depth(depth);

		float distanceFogAmount = 0.;
		if (_ApplyDistToSkybox)
			distanceFogAmount = ComputeDistanceFogAmount(totalDistance);
		else if (linDepth < SKYBOX_THREASHOLD_VALUE)
			distanceFogAmount = ComputeDistanceFogAmount(totalDistance);

		half4 fogColor = GetColorFromTexture(_FogColorTexture0, distanceFogAmount);

		return BlendFogToScene(i.uv0, fogColor, fogColor.a);
	}

	half4 fragment_height(v2f_multitex i) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv1);

		float4 wpos = DepthToWorld(depth, i.uv1, _InverseViewMatrix);

		float4 cameraToFragment = wpos - float4(_WorldSpaceCameraPos, 1.);
		float viewDirY = normalize(cameraToFragment).y;
		float totalDistance = length(cameraToFragment);

		float linDepth = Linear01Depth(depth);

		float heightFogAmount = 0.;
		if (_ApplyHeightToSkybox)
			heightFogAmount = ComputeHeightFogAmount(viewDirY, totalDistance);
		else if (linDepth < SKYBOX_THREASHOLD_VALUE)
			heightFogAmount = ComputeHeightFogAmount(viewDirY, totalDistance);

		half4 fogColor = GetColorFromTexture(_FogColorTexture0, heightFogAmount);

		return BlendFogToScene(i.uv0, fogColor, fogColor.a);
	}


	half4 fragment_distance_height_shared_color(v2f_multitex i) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv1);

		float4 wpos = DepthToWorld(depth, i.uv1, _InverseViewMatrix);

		float4 cameraToFragment = wpos - float4(_WorldSpaceCameraPos, 1.);
		float3 viewDir = normalize(cameraToFragment);
		float totalDistance = length(cameraToFragment);

		float linDepth = Linear01Depth(depth);

		float distanceFogAmount = 0.;
		float heightFogAmount = 0.;
		if (_ApplyDistToSkybox)
			distanceFogAmount = ComputeDistanceFogAmount(totalDistance);
		else if (linDepth < SKYBOX_THREASHOLD_VALUE)
			distanceFogAmount = ComputeDistanceFogAmount(totalDistance);

		if (_ApplyHeightToSkybox)
			heightFogAmount = ComputeHeightFogAmount(viewDir.y, totalDistance);
		else if (linDepth < SKYBOX_THREASHOLD_VALUE)
			heightFogAmount = ComputeHeightFogAmount(viewDir.y, totalDistance);

		float totalFogAmount = distanceFogAmount +heightFogAmount;

		half4 fogColor = GetColorFromTexture(_FogColorTexture0, totalFogAmount);

		return BlendFogToScene(i.uv0, fogColor, fogColor.a);
	}

	half4 fragment_distance_height_seperate_color(v2f_multitex i) : SV_Target
	{
		float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv1);

		float4 wpos = DepthToWorld(depth, i.uv1, _InverseViewMatrix);

		float4 cameraToFragment = wpos - float4(_WorldSpaceCameraPos, 1.);
		float3 viewDir = normalize(cameraToFragment);
		float totalDistance = length(cameraToFragment);

		float linDepth = Linear01Depth(depth);

		float distanceFogAmount = 0.;
		float heightFogAmount = 0.;
		if (_ApplyDistToSkybox)
			distanceFogAmount = ComputeDistanceFogAmount(totalDistance);
		else if (linDepth < SKYBOX_THREASHOLD_VALUE)
			distanceFogAmount = ComputeDistanceFogAmount(totalDistance);

		if (_ApplyHeightToSkybox)
			heightFogAmount = ComputeHeightFogAmount(viewDir.y, totalDistance);
		else if (linDepth < SKYBOX_THREASHOLD_VALUE)
			heightFogAmount = ComputeHeightFogAmount(viewDir.y, totalDistance);

		half4 distanceFogColor  = GetColorFromTexture(_FogColorTexture0, distanceFogAmount);
		half4 heightFogColor = GetColorFromTexture(_FogColorTexture1, heightFogAmount);
		distanceFogColor *= step(FOG_AMOUNT_CONTRIBUTION_THREASHOLD, distanceFogAmount);
		heightFogColor *= step(FOG_AMOUNT_CONTRIBUTION_THREASHOLD, heightFogAmount);

		half combinedAlpha = distanceFogColor.a + heightFogColor.a;
		half4 fogColor = lerp(distanceFogColor, heightFogColor, saturate(heightFogColor.a / distanceFogColor.a));

		return BlendFogToScene(i.uv0, fogColor, fogColor.a);
	}

	ENDCG
	SubShader
	{
		// 0: Distance fog only
		Pass
		{
			Cull Off ZWrite Off ZTest Always
			CGPROGRAM
			#pragma multi_compile __ UNITY_COLORSPACE_GAMMA
			#pragma vertex vert_img_fog
			#pragma fragment fragment_distance
			ENDCG
		}
		// 1: Height fog only
		Pass
		{
			Cull Off ZWrite Off ZTest Always
			CGPROGRAM
			#pragma multi_compile __ UNITY_COLORSPACE_GAMMA
			#pragma vertex vert_img_fog
			#pragma fragment fragment_height
			ENDCG
		}
		// 2: Distance and height fog using same color source
		Pass
		{
			Cull Off ZWrite Off ZTest Always
			CGPROGRAM
			#pragma multi_compile __ UNITY_COLORSPACE_GAMMA
			#pragma vertex vert_img_fog
			#pragma fragment fragment_distance_height_shared_color
			ENDCG
		}
		// 3: Distance and height fog each using their
		Pass
		{
			Cull Off ZWrite Off ZTest Always
			CGPROGRAM
			#pragma multi_compile __ UNITY_COLORSPACE_GAMMA
			#pragma vertex vert_img_fog
			#pragma fragment fragment_distance_height_seperate_color
			ENDCG
		}
	}
}
