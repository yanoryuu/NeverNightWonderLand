Shader "Utage/PostEffect/BlurSgxGauss"
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

            static const half4 curve4[7] = { half4(0.0205,0.0205,0.0205,0), half4(0.0855,0.0855,0.0855,0), half4(0.232,0.232,0.232,0),
			half4(0.324,0.324,0.324,1), half4(0.232,0.232,0.232,0), half4(0.0855,0.0855,0.0855,0), half4(0.0205,0.0205,0.0205,0) };


            struct v2f
            {
                float4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                half4 offs[3] : TEXCOORD1;
            };

            v2f vert(appdata_img v)
            {
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);				
				o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord.xy, _MainTex_ST);
            	
				const half offsetMagnitude = _MainTex_TexelSize.x * _Parameter;
				o.offs[0] = UnityStereoScreenSpaceUVAdjust(v.texcoord.xyxy + offsetMagnitude * -3*_UvOffset, _MainTex_ST);
				o.offs[1] = UnityStereoScreenSpaceUVAdjust(v.texcoord.xyxy + offsetMagnitude * -2*_UvOffset, _MainTex_ST);
				o.offs[2] = UnityStereoScreenSpaceUVAdjust(v.texcoord.xyxy + offsetMagnitude * -1*_UvOffset, _MainTex_ST);

                return o;
            }

            
            half4 frag(v2f i) : SV_Target
            {
				half4 color = tex2D(_MainTex, i.uv) * curve4[3];				
  				for( int l = 0; l < 3; l++ )  
  				{   
					const half4 tapA = tex2D(_MainTex, i.offs[l].xy);
					const half4 tapB = tex2D(_MainTex, i.offs[l].zw); 
					color += (tapA + tapB) * curve4[l];
  				}

				return color;
            }

            ENDCG
        }
    }

    FallBack Off
}
