// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//右寄せ、上詰めなどの配置用
	public enum Alignment
	{
		Center,
		TopLeft,
		TopCenter,
		TopRight,
		LeftCenter,
		RightCenter,
		BottomLeft,
		BottomCenter,
		BottomRight,
		Custom,
		None,
	}

	static public class AlignmentUtil
	{
		//アライメントの0～1の比率に置き換える（pivotなどの形式と同じ）
		static public Vector2 GetAlignmentValue(Alignment alignment)
		{
			switch (alignment)
			{
				case Alignment.TopLeft:
					return new Vector2(0.0f, 1.0f);
				case Alignment.LeftCenter:
					return new Vector2(0.0f, 0.5f);
				case Alignment.BottomLeft:
					return new Vector2(0.0f, 0.0f);
				case Alignment.TopCenter:
					return new Vector2(0.5f, 1.0f);
				case Alignment.Center:
					return new Vector2(0.5f, 0.5f);
				case Alignment.BottomCenter:
					return new Vector2(0.5f, 0.0f);
				case Alignment.TopRight:
					return new Vector2(1.0f, 1.0f);
				case Alignment.RightCenter:
					return new Vector2(1.0f, 0.5f);
				case Alignment.BottomRight:
					return new Vector2(1.0f, 0.0f);
				default:
					return new Vector2(0.5f, 0.5f);
			}
		}
	}

}
