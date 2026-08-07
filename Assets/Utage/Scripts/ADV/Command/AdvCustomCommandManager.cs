// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド拡張のための基底クラス。ヒエラルキー上ではAdvEngine以下にAddComponentすること
	/// </summary>
	public abstract class AdvCustomCommandManager : MonoBehaviour
	{
		//起動時に呼ばれる、カスタムコマンドの追加などを行う
		public virtual void OnBootInit()
		{
		}

		//AdvEnginのクリア処理のときに呼ばれる
		public virtual void OnClear()
		{
		}
	}
}
