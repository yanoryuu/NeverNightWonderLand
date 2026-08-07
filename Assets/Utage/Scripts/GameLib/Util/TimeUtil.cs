// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// 時間処理
	/// </summary>
	public static class TimeUtil
	{
		static public float GetTime(bool unscaled)
		{
			return unscaled ? Time.unscaledTime : Time.time;
		}

		static public float GetDeltaTime(bool unscaled)
		{
			return unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
		}

		static public IEnumerator WaitForSeconds(bool unscaled, float time)
		{
			if (unscaled)
			{
				yield return new WaitForSecondsRealtime(time);
			}
			else
			{
				yield return new WaitForSeconds(time);
			}
		}
	}
}