Shader "Utage/UI/Outline9Pass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
		_OutLineColor("OutLine", Color) = (0,0,0,1)
		_DistanceX("DistanceX", Float) = 1
		_DistanceY("DistanceY", Float) = 1

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]


		//左上
		Pass
		{
			Name "LeftTop"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,-_DistanceX,_DistanceY);
			}
		ENDCG
		}

		//上
		Pass
		{
			Name "Top"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,0,_DistanceY);
			}
		ENDCG
		}

	
		//右上
		Pass
		{
			Name "RightTop"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,_DistanceX,_DistanceY);
			}
		ENDCG
		}


		//左
		Pass
		{
			Name "Left"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,-_DistanceX,0);
			}
		ENDCG
		}

	
		//右
		Pass
		{
			Name "Right"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,_DistanceX,0);
			}
		ENDCG
		}

		//左下
		Pass
		{
			Name "LeftBottom"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,-_DistanceX,-_DistanceY);
			}
		ENDCG
		}

		//下
		Pass
		{
			Name "Bottom"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,0,-_DistanceY);
			}
		ENDCG
		}

	
		//右下
		Pass
		{
			Name "RightBottom"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertOutline(v,_DistanceX,-_DistanceY);
			}
		ENDCG
		}



		//最終描画
		Pass
		{
			Name "Default"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UtageUIOutline.cginc"

			v2f vert(appdata_t v)
			{
				return vertDefault(v,0,0);
			}
		ENDCG
		}
	}
}
