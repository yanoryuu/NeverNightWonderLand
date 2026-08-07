Shader "Utage/PostEffect/Vortex"
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
            uniform float _Angle;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv - _CenterRadius.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
	            const float2 offset = i.uv;
	            float angle = 1.0 - length(offset / _CenterRadius.zw);
	            angle = max (0, angle);
	            angle = angle * angle * _Angle;
	            float cosLength, sinLength;
	            sincos (angle, sinLength, cosLength);
	            
	            float2 uv;
	            uv.x = cosLength * offset[0] - sinLength * offset[1];
	            uv.y = sinLength * offset[0] + cosLength * offset[1];
	            uv += _CenterRadius.xy;
	            
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }

    FallBack Off
}
