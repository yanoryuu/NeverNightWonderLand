// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace Utage
{
	//カスタムセーブデータの入出力用のインターフェース
	public interface IAdvSaveData : IBinaryIO
	{
		//クリアする
		void OnClear();
	}
}
