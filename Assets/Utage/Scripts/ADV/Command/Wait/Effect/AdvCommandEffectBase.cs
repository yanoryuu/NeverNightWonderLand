// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Utage
{

	/// <summary>
	/// エフェクトマネージャーで使うエフェクトデータの基底クラス
	/// </summary>
	public abstract class AdvCommandEffectBase : AdvCommandWaitBase
	{
		//ターゲット
		public AdvEffectManager.TargetType Target { get { return targetType; } }
		protected AdvEffectManager.TargetType targetType;

		//ターゲットとなる名前
		public string TargetName { get { return targetName ; } }
		protected string targetName ;

		//コンストラクタ
		protected AdvCommandEffectBase(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			OnParse(dataManager);
		}


		//解析必要に応じてオーバーライド
		protected virtual void OnParse(AdvSettingDataManager dataManager)
		{
			ParseEffectTarget(AdvColumnName.Arg1);
			ParseWait(AdvColumnName.WaitType);
		}


		//ウェイトタイプを解析
		protected virtual void ParseWait(AdvColumnName columnName)
		{
			//指定のcolumnName列にあるウェイトタイプを解析
			if (IsEmptyCell(columnName))
			{
				//設定なしの場合
				this.WaitType = AdvCommandWaitType.Default;
			}
			else
			{
				string waitString = ParseCell<string>(columnName);
				AdvCommandWaitType waitType;
				if (!ParserUtil.TryParaseEnum<AdvCommandWaitType>(waitString, out waitType))
				{
					//何のタイプか不明
					this.WaitType = AdvCommandWaitType.NoWait;
					Debug.LogError(ToErrorString("UNKNOWN WaitType"));
				}
				else
				{
					this.WaitType = waitType;
				}
			}
		}


		//エフェクト対象を解析
		protected virtual void ParseEffectTarget(AdvColumnName columnName)
		{
			//第1引数はターゲットの設定
			this.targetName = ParseCell<string>(columnName);
			if (!ParserUtil.TryParaseEnum<AdvEffectManager.TargetType>(this.targetName, out this.targetType))
			{
				this.targetType = AdvEffectManager.TargetType.Default;
			}
		}


		protected override void OnStart(AdvEngine engine, AdvScenarioThread thread)
		{
			GameObject go = engine.EffectManager.FindTarget(this);
			if (go == null)
			{
				Debug.LogError(RowData.ToErrorString(this.TargetName + " is not found"));
				OnComplete(thread);
				return;
			}
			OnStartEffect(go,engine,thread);
		}

		protected abstract void OnStartEffect(GameObject target, AdvEngine engine, AdvScenarioThread thread);
	}
}
