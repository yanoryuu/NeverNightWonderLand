Shader "Utage/ImageEffect/NegaPosi" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_Color("Tint", Color) = (1,1,1,1)
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
fixed _Strength;

fixed4 frag (v2f_img i) : SV_Target
{
	fixed4 output = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
	output.rgb =  float3(1, 1, 1) - output.rgb;
	return output;
}
ENDCG

	}
}

Fallback off

}
