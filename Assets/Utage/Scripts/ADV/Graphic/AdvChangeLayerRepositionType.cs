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

	//レイヤー変更時の座標の再設定のタイプ
	public enum AdvChangeLayerRepositionType
	{
		KeepGlobal,	//グローバル座標を保持
		KeepLocal,	//ローカル座標を保持
		ResetLocal,	//ローカル座標をリセット
	}
}
