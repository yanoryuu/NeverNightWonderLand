Shader "Utage/ImageEffect/RuleFade"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Strength("Strength", Range(0.0, 1.0)) = 1
		_RuleTex("Rule", 2D) = "white" {}
		_Vague("Vague", Range(0.001, 1.0)) = 0.2
	}

	SubShader
	{
		Pass 
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform sampler2D _RuleTex;
			half4 _MainTex_ST;
			fixed4 _Color;
			fixed _Strength;
			fixed _Vague;

			fixed4 frag(v2f_img i) : SV_Target
			{
				half2 uv = UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST);
				fixed4 color = tex2D(_MainTex, uv);
				half fade = (tex2D(_RuleTex, uv).a);
				fade = clamp(-(1 / _Vague)*_Strength + (1 / _Vague - 1)*fade + 1, 0, 1);
				fixed4 output = lerp(_Color,color, fade);
				return output;
			}
			ENDCG

		}
	}
	Fallback off
}
