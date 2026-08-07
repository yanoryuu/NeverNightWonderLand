Shader "Utage/PostEffect/Mosaic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size("Size", Float) = 4
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
            uniform float _Size;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                const float2 delta = _Size / _ScreenParams.xy;
                return tex2D(_MainTex, (floor(i.uv / delta) + 0.5) * delta);
            }
            ENDCG
        }
    }

    FallBack Off
}
