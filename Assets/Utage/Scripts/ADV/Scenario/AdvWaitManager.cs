// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.Pool;

namespace Utage
{
	//シナリオスレッド内のコマンド待機処理のマネージャー
	internal class AdvWaitManager
	{
		//管理しているコマンドリスト
		readonly List<AdvCommandWaitBase> notWaitCommandList = new List<AdvCommandWaitBase>();
		
		readonly List<AdvCommandWaitBase> waitCommandList = new List<AdvCommandWaitBase>();
		
		//CheckWaitで削除対象となるコマンドリスト
		//Alloc回避にリストのインスタンスは保持する
		readonly List<AdvCommandWaitBase> removeCommandList = new List<AdvCommandWaitBase>();


		internal void Clear()
		{
			ClearCommandList(notWaitCommandList);
			ClearCommandList(waitCommandList);
		}

		void ClearCommandList(List<AdvCommandWaitBase> list)
		{
			foreach (var item in list)
			{
				FinalizeCommand(item);
			}
			list.Clear();
		}

		void FinalizeCommand(AdvCommandWaitBase command)
		{
			var effect = command as IAdvCommandEffect;
			if (effect != null)
			{
				effect.OnEffectFinalize();
			}
		}

		internal void StartCommand(AdvCommandWaitBase command)
		{
			//タイプによって管理リストから除外
			switch (command.WaitType)
			{
				case AdvCommandWaitType.NoWait:
				case AdvCommandWaitType.SkipOnInput:
				case AdvCommandWaitType.SkipOnBrPage:
					notWaitCommandList.Add(command);
					break;
				default:
					waitCommandList.Add(command);
					break;
			}
		}

		internal void CompleteCommand(AdvCommandWaitBase command)
		{
			FinalizeCommand(command);
			//タイプによって管理リストから除外
			switch (command.WaitType)
			{
				case AdvCommandWaitType.NoWait:
				case AdvCommandWaitType.SkipOnInput:
				case AdvCommandWaitType.SkipOnBrPage:
					notWaitCommandList.Remove(command);
					break;
				default:
					waitCommandList.Remove(command);
					break;
			}
		}

		//何らかの待機あり
		internal bool IsWaiting
		{
			get { return waitCommandList.Count > 0; }
		}


		//待機コマンドの場合のチェック
		internal bool IsWaitingDefault
		{
			get
			{
				UpdateCheckWait();
				return waitCommandList.Exists(x => x.WaitType.IsWaitingCommandType());
			}
		}

		//改行入力などを入力前にするエフェクトの終了待ち
		internal bool IsWaitingInputEffect
		{
			get
			{
				UpdateCheckWait();
				return waitCommandList.Exists(x => x.WaitType.IsWaitingInputType());
			}
		}

		//改ページ入力前にするエフェクトの終了待ち
		internal bool IsWaitingPageEndEffect
		{
			get
			{
				UpdateCheckWait();
				return waitCommandList.Exists(x => x.WaitType.IsWaitingPageEndEffect());
			}
		}

		internal bool IsWaitingOnThread
		{
			get
			{
				UpdateCheckWait();
				return waitCommandList.Exists(x => x.WaitType.IsWaitingOnThreadType());
			}
		}

		//コールバックでは実行されず、終わったかの終了チェックが必要なものをここで呼ぶ
		//対象のコマンドがWait待機中にしか呼ばれないので、毎フレームの時間加算などには使えない点に注意
		void UpdateCheckWait()
		{
			removeCommandList.Clear();
			foreach ( var command in waitCommandList)
			{
				if (command is IAdvCommandUpdateWait checkWait )
				{
					if (!checkWait.UpdateCheckWait())
					{
						removeCommandList.Add(command);
					}
				}
			}
			if(removeCommandList.Count>0)
			{
				foreach (var command in removeCommandList)
				{
					CompleteCommand(command);
				}
				removeCommandList.Clear();
			}
		}

		//コマンド待ちでのスキップ
		public void SkipEffectCommand()
		{
			SkipEffectSub(x => x.IsSkippableCommand());
		}

		//入力待ちでのスキップ
		public void SkipEffectInput()
		{
			SkipEffectSub(x => x.IsSkippableInput());
		}

		//改ページ待ちでのスキップ
		public void SkipEffectPageEnd()
		{
			SkipEffectSub(x => x.IsSkippable());
		}

		//WaitThreadコマンド中の演出スキップ
		public void SkipEffectCommandOnWaitThread()
		{
			SkipEffectSub(x => x.IsSkippableCommandOnWaitThread());
		}

		void SkipEffectSub( Func<AdvCommandWaitType,bool> checkSkip )
		{
			if (!waitCommandList.Exists(x=>checkSkip(x.WaitType)))
			{
				return;
			}

			var tmp = ListPool<AdvCommandWaitBase>.Get();
			tmp.AddRange(waitCommandList);
			foreach (AdvCommandWaitBase command in tmp)
			{
				if (!checkSkip(command.WaitType))
				{
//					Debug.LogWarning( command.ToErrorString("Not Skippable Wait type" + command.WaitType +" is added"));
					continue;
				}

				if (command is IAdvCommandEffect skip)
				{
					skip.OnEffectSkip();
				}
				else
				{
					Debug.LogErrorFormat("command {0} is not skippable effect", command.Id);
				}
			}
			ListPool<AdvCommandWaitBase>.Release(tmp);
		}

		//全エフェクトを強制スキップして終了する
		public void ForceSkipAllEffect(bool skipLoop)
		{
			ForceSkipCommandList(notWaitCommandList,skipLoop);
			ForceSkipCommandList(waitCommandList,skipLoop);
		}
		
		//NoWait系のエフェクトをすべて強制スキップして終了する
		public void ForceSkipNoWaitEffect(bool skipLoop)
		{
			ForceSkipCommandList(notWaitCommandList,skipLoop);
		}

		void ForceSkipCommandList(List<AdvCommandWaitBase> list, bool skipLoop )
		{
			var tmp = ListPool<AdvCommandWaitBase>.Get();
			tmp.AddRange(list);
			foreach (AdvCommandWaitBase command in tmp)
			{
				if (command is IAdvCommandEffect skip)
				{
					if (!skipLoop && skip is IAdvCommandEffectLoop loop && loop.IsLoopEffect())
					{
						continue;
					}
					skip.OnEffectSkip();
				}
				else
				{
					Debug.LogErrorFormat("command {0} is not skippable effect", command.Id);
				}
			}
			ListPool<AdvCommandWaitBase>.Release(tmp);
		}

		void ForceSkipNotWaitTypeSub(AdvCommandWaitType waitType)
		{
			var tmp = ListPool<AdvCommandWaitBase>.Get();
			tmp.AddRange(notWaitCommandList);
			foreach (AdvCommandWaitBase command in tmp)
			{
				if(command.WaitType != waitType ) continue;
				if (command is IAdvCommandEffect skip)
				{
					skip.OnEffectSkip();
				}
				else
				{
					Debug.LogErrorFormat("command {0} is not skippable effect", command.Id);
				}
			}
			ListPool<AdvCommandWaitBase>.Release(tmp);
		}

				
		public void OnEndInputWait()
		{
			ForceSkipNotWaitTypeSub(AdvCommandWaitType.SkipOnInput);
		}

		public void OnEndPage()
		{
			ForceSkipNotWaitTypeSub(AdvCommandWaitType.SkipOnBrPage);
			ForceSkipNotWaitTypeSub(AdvCommandWaitType.SkipOnInput);
		}
	}
}
