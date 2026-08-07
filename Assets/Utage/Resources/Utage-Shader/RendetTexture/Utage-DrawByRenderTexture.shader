Shader "Utage/DrawByRenderTexture"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_FadeTex("Fade Texture", 2D) = "white" {}
		_Strength("Strength", Range(0.0, 1.0)) = 0.2

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend One OneMinusSrcAlpha, One One
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_ALPHACLIP

			#pragma multi_compile __ CROSS_FADE

			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
#ifdef CROSS_FADE
				float2 texcoord1 : TEXCOORD1;
#endif
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
#ifdef CROSS_FADE
				float2 texcoord1 : TEXCOORD1;
#endif
				float4 worldPosition : TEXCOORD2;
			};
			
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;

#ifdef CROSS_FADE
				OUT.texcoord1 = IN.texcoord1;
#endif

				
				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
				#endif
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			sampler2D _MainTex;
#ifdef CROSS_FADE
			sampler2D _FadeTex;
			fixed _Strength;
#endif

			fixed4 frag(v2f IN) : SV_Target
			{
#ifdef CROSS_FADE
				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);

				fixed4 detail = (tex2D(_FadeTex, IN.texcoord1) + _TextureSampleAdd);
				color.rgb = lerp(color.rgb, detail.rgb, _Strength);
				color.a = lerp(color.a, detail.a, _Strength);
#else

				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd);
#endif
				color.rgb *= IN.color.rgb*IN.color.a;
				color.a = (1 - color.a)*(IN.color.a);
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
}
