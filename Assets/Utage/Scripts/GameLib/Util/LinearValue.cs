// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 一定時間で線形変化する値
	/// </summary>
	[System.Serializable]
	public class LinearValue
	{
		float time;
		float timeCurrent;
		float valueBegin;
		float valueEnd;
		float valueCurrent;
		bool unscaled;


		public LinearValue(bool unslaed = false)
		{
			this.unscaled = unslaed;
		}

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="time">変化する時間</param>
		/// <param name="valueBegin">開始の値</param>
		/// <param name="valueEnd">終了の値</param>
		public void Init(float time, float valueBegin, float valueEnd)
		{
			this.time = time;
			this.timeCurrent = 0;
			this.valueBegin = valueBegin;
			this.valueEnd = valueEnd;
			this.valueCurrent = valueBegin;
		}

		/// <summary>
		/// クリア
		/// </summary>
		public void Clear()
		{
			this.time = 0;
			this.timeCurrent = 0;
			this.valueBegin = 0;
			this.valueEnd = 0;
			this.valueCurrent = 0;
		}

		/// <summary>
		/// 時間経過を加算
		/// </summary>
		public void IncTime()
		{
			if (IsEnd()) return;

			timeCurrent = Mathf.Min(timeCurrent +TimeUtil.GetDeltaTime(unscaled), time);
			valueCurrent = Mathf.Lerp(valueBegin, valueEnd, timeCurrent / time);
		}

		/// <summary>
		/// フェード処理が終わったか
		/// </summary>
		/// <returns></returns>
		public bool IsEnd()
		{
			return (timeCurrent >= time);
		}

		/// <summary>
		/// 現在の値
		/// </summary>
		/// <returns></returns>
		public float GetValue()
		{
			return valueCurrent;
		}
	}

}