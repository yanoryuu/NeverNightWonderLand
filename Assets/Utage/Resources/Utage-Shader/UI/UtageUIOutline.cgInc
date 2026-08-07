#include "UnityCG.cginc"
#include "UnityUI.cginc"

#pragma multi_compile __ UNITY_UI_CLIP_RECT
#pragma multi_compile __ UNITY_UI_ALPHACLIP

struct appdata_t
{
	float4 vertex   : POSITION;
	float4 color    : COLOR;
	float2 texcoord : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
	float4 vertex   : SV_POSITION;
	fixed4 color : COLOR;
	float2 texcoord  : TEXCOORD0;
	float4 worldPosition : TEXCOORD1;
	UNITY_VERTEX_OUTPUT_STEREO
};


sampler2D _MainTex;
fixed4 _Color;
fixed4 _OutLineColor;
float _DistanceX;
float _DistanceY;
fixed4 _TextureSampleAdd;
float4 _ClipRect;
float4 _MainTex_ST;

v2f vertSub(appdata_t v, float x, float y, fixed4 color)
{
	v2f OUT;
	v.vertex.x += x;
	v.vertex.y += y;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

	OUT.worldPosition = v.vertex;
	OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

	OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

	OUT.color = color;
	return OUT;
}

v2f vertOutline(appdata_t v, float x, float y)
{
	fixed4 color = _OutLineColor;
	color.a *= v.color.a;
	return vertSub(v, x, y, color);
}

v2f vertDefault(appdata_t v, float x, float y )
{
	fixed4 color = v.color * _Color;
	return vertSub(v, x, y, color);
}



fixed4 frag(v2f IN) : SV_Target
{
	half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

	#ifdef UNITY_UI_CLIP_RECT
	color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
	#endif

	#ifdef UNITY_UI_ALPHACLIP
	clip(color.a - 0.001);
	#endif

	return color;
}
