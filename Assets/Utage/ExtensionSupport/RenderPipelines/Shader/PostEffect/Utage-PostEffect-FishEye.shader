Shader "Utage/PostEffect/FishEye"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IntensityX ("IntensityX", Range(0,1)) = 0.5
        _IntensityY ("IntensityY", Range(0,1)) = 0.5
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
            uniform float _IntensityX;
            uniform float _IntensityY;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half2 coords = i.uv;
                coords = (coords - 0.5) * 2.0;

                half2 realCoordOffs;
                realCoordOffs.x = (1-coords.y * coords.y) * _IntensityY * (coords.x);
                realCoordOffs.y = (1-coords.x * coords.x) * _IntensityX * (coords.y);

                return tex2D (_MainTex, i.uv - realCoordOffs);
            }
            ENDCG
        }
    }

    FallBack Off
}
