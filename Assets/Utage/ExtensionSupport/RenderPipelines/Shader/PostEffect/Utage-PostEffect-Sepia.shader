Shader "Utage/PostEffect/Sepia"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
	            const float y = dot (float3(0.299, 0.587, 0.114), original.rgb);
	            const float4 sepiaConvert = float4 (0.191, -0.054, -0.221, 0.0);
	            float4 output = lerp(original, sepiaConvert + y, _Strength);
                output.a = original.a;
                return output;
            }
            ENDCG
        }
    }

    FallBack Off
}
