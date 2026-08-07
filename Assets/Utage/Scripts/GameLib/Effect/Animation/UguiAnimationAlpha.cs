// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	/// <summary>
	/// UI用の簡易アニメーション処理
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiAnimationAlpha")]
	public class UguiAnimationAlpha : UguiAnimation
	{
		public float From { get { return from; } set { from = value; } }
		[SerializeField]
		float from;

		public float To { get { return to; } set { to = value; } }
		[SerializeField]
		float to;

		public float By { get { return by; } set { by = value; } }
		[SerializeField]
		float by;

		float lerpFrom;
		float lerpTo;

		protected override void StartAnimation()
		{
			switch(Type)
			{
				case AnimationType.To:
					lerpFrom = TargetGraphic.color.a;
					lerpTo = To;
					break;
				case AnimationType.From:
					lerpFrom = From;
					lerpTo = TargetGraphic.color.a;
					break;
				case AnimationType.FromTo:
					lerpFrom = From;
					lerpTo = To;
					break;
				case AnimationType.By:
					lerpFrom = 0;
					lerpTo = By;
					break;
			}
			Color color = TargetGraphic.color;
			color.a = lerpFrom;
			TargetGraphic.color = color;
		}

		protected override void UpdateAnimation(float value)
		{
			Color color = TargetGraphic.color;
			float alpha = LerpValue(lerpFrom, lerpTo);
			switch (Type)
			{
				case AnimationType.By:
					color.a += alpha;
					break;
				default:
					color.a = alpha;
					break;
			}
			TargetGraphic.color = color;
		}
	}
}