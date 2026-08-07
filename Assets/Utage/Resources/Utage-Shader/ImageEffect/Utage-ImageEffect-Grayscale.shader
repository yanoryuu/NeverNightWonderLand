Shader "Utage/ImageEffect/Grayscale" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_RampTex ("Base (RGB)", 2D) = "grayscaleRamp" {}
	_Strength("Strength", Range(0.0, 1.0)) = 1
}

SubShader {
	Pass {
		ZTest Always Cull Off ZWrite Off
				
CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"

uniform sampler2D _MainTex;
uniform sampler2D _RampTex;
uniform half _RampOffset;
half4 _MainTex_ST;
fixed _Strength;

fixed4 frag (v2f_img i) : SV_Target
{
	fixed4 original = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
	fixed grayscale = Luminance(original.rgb);
	half2 remap = half2 (grayscale + _RampOffset, .5);
	fixed4 output = tex2D(_RampTex, remap);
	output.a = original.a;
	return lerp(original, output, _Strength);
}
ENDCG

	}
}

Fallback off

}
