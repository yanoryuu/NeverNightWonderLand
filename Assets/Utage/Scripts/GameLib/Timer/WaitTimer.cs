// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	//指定した時間（秒）を待ちつづける
	//0～1補間の値を受けるコールバックを設定可能
	public class WaitTimer : CustomYieldInstruction
	{
		float duration; //待つ時間
		float delay;    //待機開始までの遅延時間
		bool unscaled;	//TimeScaleを無視するか
		float initTime; //初期化された時間
		bool isStarted; //開始処理

		public float Time { get; protected set; }       //経過時間
		public float Time01 { get; protected set; }     //経過時間の係数(0～１)

		//遅延時間を考慮した開始時間
		float StartTimeDelyed { get { return initTime + delay; } }
		//終了時間
		float EndTime { get { return StartTimeDelyed + duration; } }

		UnityAction<WaitTimer> onStart;
		UnityAction<WaitTimer> onUpdate;
		UnityAction<WaitTimer> onComplete;

		public WaitTimer(float duration, UnityAction<WaitTimer> onStart = null, UnityAction<WaitTimer> onUpdate = null, UnityAction<WaitTimer> onComplete = null)
		{
			Init(duration, 0, false, onStart, onUpdate, onComplete);
		}
		public WaitTimer(float duration, float delay, UnityAction<WaitTimer> onStart = null, UnityAction<WaitTimer> onUpdate = null, UnityAction<WaitTimer> onComplete = null)
		{
			Init(duration, delay, false, onStart, onUpdate, onComplete);
		}

		public WaitTimer(float duration, bool unscaled, UnityAction<WaitTimer> onStart = null, UnityAction<WaitTimer> onUpdate = null, UnityAction<WaitTimer> onComplete = null)
		{
			Init(duration, 0, unscaled, onStart, onUpdate, onComplete);
		}
		public WaitTimer(float duration, float delay, bool unscaled, UnityAction<WaitTimer> onStart = null, UnityAction<WaitTimer> onUpdate = null, UnityAction<WaitTimer> onComplete = null)
		{
			Init(duration, delay, unscaled, onStart, onUpdate, onComplete);
		}

		void Init(float duration, float delay, bool unscaled, UnityAction<WaitTimer> onStart, UnityAction<WaitTimer> onUpdate, UnityAction<WaitTimer> onComplete)
		{
			this.duration = duration;
			this.delay = delay;
			this.unscaled = unscaled;
			this.initTime = TimeUtil.GetTime(unscaled);
			this.onStart = onStart;
			this.onUpdate = onUpdate;
			this.onComplete = onComplete;
		}


		public override bool keepWaiting { get { return Waiting(); } }
		bool Waiting()
		{
			float time = TimeUtil.GetTime(unscaled);
			//開始遅延処理
			if (time < StartTimeDelyed) return true;

			//時間経過を更新
			this.Time = (time - StartTimeDelyed);
			//時間経過を更新
			if (duration == 0)
			{
				this.Time01 = 1.0f;
			}
			else
			{
				this.Time01 = Mathf.Clamp01(Time / duration);
			}


			//開始
			if (!isStarted)
			{
				//更新処理
				if (this.onStart != null)
				{
					this.onStart(this);
				}
				isStarted = true;
			}

			//更新処理
			if (this.onUpdate != null)
			{
				this.onUpdate(this);
			}

			//終了
			if (time >= EndTime)
			{
				if (this.onComplete != null)
				{
					this.onComplete(this);
				}
				return false;
			}
			return true;
		}
	}
}