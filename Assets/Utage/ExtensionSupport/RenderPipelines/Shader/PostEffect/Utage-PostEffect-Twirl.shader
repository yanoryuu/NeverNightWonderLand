Shader "Utage/PostEffect/Twirl"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Center ("_CenterRadius", Vector) = (0.5, 0.5, 0.3, 0.3)
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
            uniform float4 _CenterRadius;
            uniform float4x4 _RotationMatrix;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv - _CenterRadius.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
	            float2 offset = i.uv;
	            const float2 distortedOffset = MultiplyUV (_RotationMatrix, offset.xy);
	            const float2 tmp = offset / _CenterRadius.zw;
	            const float t = min (1, length(tmp));
	            
	            offset = lerp (distortedOffset, offset, t);
	            offset += _CenterRadius.xy;
                return tex2D(_MainTex, offset);
            }
            ENDCG
        }
    }

    FallBack Off
}
