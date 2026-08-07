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
	[AddComponentMenu("Utage/Lib/UI/EyeBlinkAvatar")]
	[RequireComponent(typeof(AvatarImage))]
	public class EyeBlinkAvatar : EyeBlinkBase
	{
		AvatarImage Avator { get { return this.gameObject.GetComponentCache<AvatarImage>(ref avator); } }
		AvatarImage avator;

		protected override IEnumerator CoEyeBlink(Action onComplete)
		{
			string pattern;
			foreach (var data in AnimationData.DataList)
			{
				if (!TryGetCurrentPattern(data, out pattern))
				{
					if (onComplete != null) onComplete();
					yield break;
				}
				Avator.ChangePattern(EyeTag, pattern);
				yield return TimeUtil.WaitForSeconds(UnscaledTime, data.Duration);
			}
			if (!TryGetCurrentPattern(null, out pattern))
			{
				if (onComplete != null) onComplete();
				yield break;
			}
			Avator.ChangePattern(EyeTag, pattern);
			if (onComplete != null) onComplete();
			yield break;
		}

		//現在の状態に合わせた、目パチのパターン名を取得
		bool TryGetCurrentPattern(MiniAnimationData.Data data, out string pattern)
		{
			string eyePatternName = Avator.AvatarPattern.GetOriginalPatternName(EyeTag);
//			Debug.Log("eyePatternName="+eyePatternName);
			pattern = AvatarData.ToPatternName(eyePatternName);
			if (string.IsNullOrEmpty(pattern)) return false;
			if (data != null)
			{
				pattern = data.ComvertName(pattern);
			}
			return true;
		}
	}
}

