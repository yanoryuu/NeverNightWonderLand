// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// 画面管理コンポーネントの基本クラス（各画面制御はこれを継承する）
	/// </summary>
	public interface ITransition
	{
		//画面を開く処理を開始
		void Open();

		//画面を閉じる処理を開始
		void Close();

		//画面を閉じる処理をキャンセル
		void CancelClosing();

		bool IsPlaying { get; }
	}
}
