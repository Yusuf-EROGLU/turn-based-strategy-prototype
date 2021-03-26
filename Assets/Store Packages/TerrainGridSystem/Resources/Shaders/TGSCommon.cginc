#ifndef TGS_DEPTH_FADE_COMMON
#define TGS_DEPTH_FADE_COMMON

float _Offset;
fixed4 _Color;
float _NearClip;
float _FarClip;
float _FallOff;
float _Thickness;
float _FarFadeDistance;
float _FarFadeFallOff;

fixed4 ApplyNearClipFade(float4 pos, fixed4 color) {

	#if NEAR_CLIP_FADE
		if (UNITY_MATRIX_P[3][3]!=1.0) {	// Non Orthographic camera
			color = fixed4(color.rgb, color.a * saturate((UNITY_Z_0_FAR_FROM_CLIPSPACE(pos.z) - _NearClip)/_FallOff));
		}
	#endif
	return color;
}


fixed4 ApplyFarClipFade(float4 pos, fixed4 color) {
	#if FAR_FADE
		if (UNITY_MATRIX_P[3][3]!=1.0) {	// Non Orthographic camera
			color = fixed4(color.rgb, color.a * saturate((_FarFadeDistance - UNITY_Z_0_FAR_FROM_CLIPSPACE(pos.z))/_FarFadeFallOff));
		}
	#endif
	return color;
}

fixed4 ApplyFade(float4 pos, fixed4 color) {
	color = ApplyNearClipFade(pos, color);
	color = ApplyFarClipFade(pos, color);
	return color;
}

#define APPLY_FADE(pos, color) color = ApplyFade(pos, _Color);

#endif