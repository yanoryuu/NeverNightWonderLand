// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	//実際にUIとしてヒエラルキーに存在するメッセージウィンドウの管理オブジェクトのコンポーネントの共通インターフェース
	public interface IAdvMessageWindowManager
	{
		GameObject gameObject { get; }
		Dictionary<string, IAdvMessageWindow> AllWindows { get; }
	}
}