Shader "Utage/PostEffect/Blur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BloomTex("Bloom Texture", 2D) = "black" {}
        _BlurRadius("Blur Radius", Range(0.0, 1.0)) = 0.25
        _BlurIterations("Blur Iterations", Range(1, 8)) = 4
    }

    SubShader
    {
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
            uniform float _BlurSize;
            uniform int _BlurIterations;
            uniform half4 _MainTex_TexelSize;

            static const half curve[7] = { 0.0205, 0.0855, 0.232, 0.324, 0.232, 0.0855, 0.0205 };  // gauss'ish blur weights
            static const half4 curve4[7] = { half4(0.0205,0.0205,0.0205,0), half4(0.0855,0.0855,0.0855,0), half4(0.232,0.232,0.232,0),half4(0.324,0.324,0.324,1), half4(0.232,0.232,0.232,0), half4(0.0855,0.0855,0.0855,0), half4(0.0205,0.0205,0.0205,0) };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {

                // DownSample
                fixed4 col = tex2D(_MainTex, i.uv) * curve4[3];
                col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(-0.5h, -0.5h)) * curve4[0];
                col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(-0.5h, 0.5h)) * curve4[0];
                col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(0.5h, -0.5h)) * curve4[0];
                col += tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * half2(0.5h, 0.5h)) * curve4[0];
//                col /= 5.0f;
/*
                // Blur
                const float2 texelSize = _MainTex_TexelSize.xy;
                const float2 blurDir = float2(1.0, 0.0);
                for (int index = 0; index < _BlurIterations; index++)
                {
                    const float radius = _BlurSize * (index + 1);
                    const float offset = radius * 1.5f;

                    col += tex2D(_MainTex, i.uv + blurDir * offset * texelSize) * curve4[0];
                    col += tex2D(_MainTex, i.uv - blurDir * offset * texelSize) * curve4[0];
                }
                col /= (_BlurIterations * 2.0f + 1.0f);
*/
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
