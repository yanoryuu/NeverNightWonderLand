// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	//リップシンクのカスタムコンポーネントの共通インターフェース
	public interface IAdvLipSyncCustom
	{
		//テキスト送りやボイス再生中かを設定し、リップシンクが有効かチェック
		bool CheckLipSync(LipSynchBase lipSync, bool isVoice, bool isText);
	}
}
