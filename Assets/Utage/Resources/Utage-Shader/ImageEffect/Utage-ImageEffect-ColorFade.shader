Shader "Utage/ImageEffect/ColorFade" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_Color("Tint", Color) = (1,1,1,1)
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
half4 _MainTex_ST;
fixed4 _Color;
fixed _Strength;

fixed4 frag (v2f_img i) : SV_Target
{
	fixed4 color = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
	fixed4 output = lerp(color, _Color, _Strength);
	output.a = color.a;
	return output;
}
ENDCG

	}
}

Fallback off

}
