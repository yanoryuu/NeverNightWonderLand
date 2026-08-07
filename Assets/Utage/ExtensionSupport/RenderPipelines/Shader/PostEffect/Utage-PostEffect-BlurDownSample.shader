Shader "Utage/PostEffect/BlurDownSample"
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				half2 uv0 : TEXCOORD0;
				half2 uv1 : TEXCOORD1;
				half2 uv2 : TEXCOORD2;
				half2 uv3 : TEXCOORD3;
            };
            
            v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos (v.vertex);
        		o.uv0 = UnityStereoScreenSpaceUVAdjust(v.uv + _MainTex_TexelSize.xy, _MainTex_ST);
				o.uv1 = UnityStereoScreenSpaceUVAdjust(v.uv + _MainTex_TexelSize.xy * half2(-0.5h,-0.5h), _MainTex_ST);
				o.uv2 = UnityStereoScreenSpaceUVAdjust(v.uv + _MainTex_TexelSize.xy * half2(0.5h,-0.5h), _MainTex_ST);
				o.uv3 = UnityStereoScreenSpaceUVAdjust(v.uv + _MainTex_TexelSize.xy * half2(-0.5h,0.5h), _MainTex_ST);

				return o; 
			}					
			
			fixed4 frag ( v2f i ) : SV_Target
			{				
				fixed4 color = tex2D (_MainTex, i.uv0);
				color += tex2D (_MainTex, i.uv1);
				color += tex2D (_MainTex, i.uv2);
				color += tex2D (_MainTex, i.uv3);
				return color / 4;
			}
            ENDCG
        }
    }
    FallBack Off
}
