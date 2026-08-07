Shader "Utage/GrabPass/Mosaic"
{
	Properties {
		_Size("Size", Float) = 4
		[Enum(UnityEngine.Rendering.CompareFunction)]
		_ZTest("ZTest", Int) = 8
	}
    SubShader
    {
        // Draw ourselves after all opaque geometry
        Tags
		{
			"Queue"="Transparent" 
			"RenderType"="Transparent" 
		}

        // Grab the screen behind the object into _BackgroundTexture
        GrabPass
        {
            "_BackgroundTexture"
        }
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [_ZTest]

        // Render the object with the texture generated above, and invert the colors
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            sampler2D _BackgroundTexture;
			fixed _Size;

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.grabPos.xy / i.grabPos.w; 
				float2 delta = _Size / _ScreenParams.xy;
				uv = (floor(uv / delta) + 0.5) * delta;
                return tex2D(_BackgroundTexture, uv);
            }
            ENDCG
        }
    }
}