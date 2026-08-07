// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UtageExtensions;


namespace Utage
{

	/// <summary>
	/// アバタータイプのまばたき処理の基本クラス
	/// </summary>
	[RequireComponent(typeof(DicingImage))]
	[AddComponentMenu("Utage/Lib/UI/EyeBlinkDicing")]
	public class EyeBlinkDicing : EyeBlinkBase
	{
		DicingImage Dicing { get { return this.gameObject.GetComponentCache<DicingImage>(ref dicing); } }
		DicingImage dicing;

		protected override IEnumerator CoEyeBlink(Action onComplete)
		{
			foreach( var data in AnimationData.DataList)
			{
				Dicing.TryChangePatternWithOption(Dicing.MainPattern, EyeTag, data.ComvertNameSimple() );
				yield return TimeUtil.WaitForSeconds(UnscaledTime, data.Duration);
			}
			Dicing.TryChangePatternWithOption(Dicing.MainPattern, EyeTag, "");
			if (onComplete != null) onComplete();
			yield break;
		}
	}
}
