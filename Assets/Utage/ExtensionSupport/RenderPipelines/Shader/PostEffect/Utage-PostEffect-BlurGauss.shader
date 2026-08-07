Shader "Utage/PostEffect/BlurGauss"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform half4 _MainTex_ST;
            uniform half4 _MainTex_TexelSize;
            uniform float _Parameter;
            uniform half4 _UvOffset;

            // ガウスブラーの重み weights
            static const half4 curve4[7] = {
                half4(0.0205, 0.0205, 0.0205, 0), half4(0.0855, 0.0855, 0.0855, 0), half4(0.232, 0.232, 0.232, 0),
                half4(0.324, 0.324, 0.324, 1), half4(0.232, 0.232, 0.232, 0), half4(0.0855, 0.0855, 0.0855, 0),
                half4(0.0205, 0.0205, 0.0205, 0)
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half4 uv : TEXCOORD0;
                half2 offs : TEXCOORD1;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv = half4(v.texcoord.xy, 1, 1);
                o.offs = _MainTex_TexelSize.xy * _UvOffset.xy * _Parameter;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                const half2 uv = i.uv.xy;
                const half2 netFilterWidth = i.offs;
                half2 coords = uv - netFilterWidth * 3.0;

                half4 color = 0;
                for (int l = 0; l < 7; l++)
                {
                    const half4 tap = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(coords, _MainTex_ST));
                    color += tap * curve4[l];
                    coords += netFilterWidth;
                }
                return color;
            }
            ENDCG
        }
    }

    FallBack Off
}
