Shader "Utage/PostEffect/RuleFade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Strength("Strength", Range(0.0, 1.0)) = 0
		_RuleTex("Rule", 2D) = "white" {}
		_Vague("Vague", Range(0.001, 1.0)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

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
                float4 vertex : SV_POSITION;
            };

            uniform sampler2D _MainTex;
			uniform sampler2D _RuleTex;
            uniform float4 _Color;
            uniform float _Strength;
            uniform float _Vague;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float4 color = tex2D(_MainTex, i.uv);
                float fade = (tex2D(_RuleTex, i.uv).a);
				fade = clamp(-(1 / _Vague)*_Strength + (1 / _Vague - 1)*fade + 1, 0, 1);
				return lerp(_Color,color, fade);
            }
            ENDCG
        }
    }

    FallBack Off
}
