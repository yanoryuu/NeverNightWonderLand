// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// カスタム機能つきのオブジェクト表示のインターフェース
	/// </summary>
	public interface IAdvGraphicObjectCustomCommand
	{
		//********描画時の引数適用********//
		void SetCommandArg(AdvCommand command);
	}
}
