// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Utage
{

	/// <summary>
	/// 第六引数でウェイト処理をする系のコマンドの基底クラス
	/// </summary>
	public abstract class AdvCommandWaitBase : AdvCommand
	{
		public AdvCommandWaitType WaitType { get; protected set; }

		protected AdvCommandWaitBase(StringGridRow row) : base(row)
		{
		}

		//コマンド実行
		public override void DoCommand(AdvEngine engine)
		{
			CurrentTread.WaitManager.StartCommand(this);
			OnStart(engine,CurrentTread);
		}

		//コマンド終了待ち
		public override bool Wait(AdvEngine engine)
		{
			//タイプによってウェイトチェック
			switch (WaitType)
			{
				case AdvCommandWaitType.Default:
					return CurrentTread.WaitManager.IsWaitingDefault;
				case AdvCommandWaitType.Skippable:
					if (!CurrentTread.WaitManager.IsWaitingDefault)
						return false;
					if (engine.Page.CheckSkip() || engine.UiManager.IsInputTrig)
					{
						//スキップさせるがこのフレームではまだ終了処理しない
						CurrentTread.WaitManager.SkipEffectCommand();
					}
					return true;
				case AdvCommandWaitType.SkippableOnWaitThread:
					if (!CurrentTread.WaitManager.IsWaitingOnThread)
						return false;
					
					//スレッド待機中にクリックされた
					if (CurrentTread.ParenetThread.IsWaitingSubTread(CurrentTread.ThreadName) && (engine.Page.CheckSkip() || engine.UiManager.IsInputTrig))
					{
						//スキップさせるがこのフレームではまだ終了処理しない
						CurrentTread.WaitManager.SkipEffectCommandOnWaitThread();
					}
					return true;
				default:
					return false;
			}
		}


		//開始時のコールバック
		protected abstract void OnStart(AdvEngine engine, AdvScenarioThread thread);

		//終了時のコールバック
		public virtual void OnComplete(AdvScenarioThread thread)
		{
			thread.WaitManager.CompleteCommand(this);
		}
	}
}
