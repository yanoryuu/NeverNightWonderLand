// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{
	//二値の補完をする簡易クラス
	//フェードとかの簡易アニメーションに使う
	public class EaseValue
	{
		public float Duration { get; private set; }
		public EaseType EaseType { get; private set; }
		public bool UnscaledTime { get; private set; }

		float Goal { get; set; }
		float Start { get; set; }
		public float Current { get; private set; }
		float CurrentTime { get; set; }

		public EaseValue(float duration, EaseType easeType = EaseType.Linear)
		{
			this.Duration = duration;
			this.EaseType = easeType;
		}

		public EaseValue(float duration, bool unscaledTime, EaseType easeType = EaseType.Linear)
		{
			this.Duration = duration;
			this.EaseType = easeType;
			this.UnscaledTime = unscaledTime;
		}

		//値をリセットする
		public void Reset(float current)
		{
			this.Current = current;
			this.CurrentTime = 0;
		}

		//更新した補完値を取得
		public float Update(float value)
		{
			return Update(value, this.Duration, this.EaseType, TimeUtil.GetDeltaTime(UnscaledTime));
		}
		public float Update(float value, float duration, EaseType easeType, float deltaTime)
		{
			if (Current == value) return value;

			if (Goal != value)
			{
				CurrentTime = 0;
				Start = Current;
				Goal = value;
			}

			CurrentTime += deltaTime;
			if (CurrentTime >= duration || Current == Goal)
			{
				Current = Goal;
			}
			else
			{
				Current = Easing.GetCurve(Start, Goal, CurrentTime / duration, easeType);
			}
			return Current;
		}
	}
}