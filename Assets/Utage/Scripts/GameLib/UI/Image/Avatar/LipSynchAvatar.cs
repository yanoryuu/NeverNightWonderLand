// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UtageExtensions;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Utage
{

	/// <summary>
	/// まばたき処理
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/LipSynchAvatar")]
	public class LipSynchAvatar : LipSynch2d
	{
		AvatarImage Avator { get { return this.gameObject.GetComponentCache<AvatarImage>(ref avator); } }
		AvatarImage avator;

		protected override IEnumerator CoUpdateLipSync()
		{
			while (IsPlaying)
			{
				string pattern;
				foreach (var data in AnimationData.DataList)
				{
					if (!TryGetCurrentPattern(data, out pattern))
					{
						break;
					}
					Avator.ChangePattern(LipTag, pattern);
					yield return TimeUtil.WaitForSeconds(UnscaledTime, data.Duration);
					while (IsPausing) yield return null;
				}
				if (!TryGetCurrentPattern(null, out pattern))
				{
					break;
				}
				Avator.ChangePattern(LipTag, pattern);
				if (!IsPlaying) break;
				yield return TimeUtil.WaitForSeconds(UnscaledTime, Interval);
				while (IsPausing) yield return null;
			}
			coLypSync = null;
			yield break;
		}
		//現在の状態に合わせた、口パクのパターン名を取得
		bool TryGetCurrentPattern(MiniAnimationData.Data data, out string pattern)
		{
			pattern = AvatarData.ToPatternName(Avator.AvatarPattern.GetOriginalPatternName(LipTag));
			if (string.IsNullOrEmpty(pattern)) return false;
			if (data != null)
			{
				pattern = data.ComvertName(pattern);
			}
			return true;
		}

		protected override void OnStopLipSync()
		{
			base.OnStopLipSync();
			string pattern = AvatarData.ToPatternName(Avator.AvatarPattern.GetOriginalPatternName(LipTag));
			if (string.IsNullOrEmpty(pattern)) return;
			Avator.ChangePattern(LipTag, pattern);
		}
	}
}
