// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace Utage
{
	//セーブデータを削除するための共通インターフェース
	public interface IAdvSaveDelete
	{
		//セーブデータを削除して終了するときに呼ぶ
		void OnDeleteAllSaveDataAndQuit();
	}
}
