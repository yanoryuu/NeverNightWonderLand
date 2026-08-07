Shader "Utage/ImageEffect/Mosaic" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_Size("Size", Float) = 4
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
fixed _Size;

fixed4 frag (v2f_img i) : SV_Target
{
	float2 delta = _Size / _ScreenParams.xy;
	float2 uv = (floor(i.uv / delta) + 0.5) * delta;
	return tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST));
}
ENDCG

	}
}

Fallback off

}
