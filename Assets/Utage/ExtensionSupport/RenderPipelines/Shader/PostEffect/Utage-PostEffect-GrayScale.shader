Shader "Utage/PostEffect/GrayScale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Strength("Strength", Range(0.0, 1.0)) = 0
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
            uniform float4 _Color;
            uniform float _Strength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 original = tex2D(_MainTex, i.uv);
                float4 grayscale = Luminance(original.rgb);
	            grayscale.a = original.a;
                return lerp(original, grayscale, _Strength);
            }
            ENDCG
        }
    }

    FallBack Off
}
