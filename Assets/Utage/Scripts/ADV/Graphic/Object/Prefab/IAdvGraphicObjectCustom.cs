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
	public interface IAdvGraphicObjectCustom
	{
		//描画時のリソース変更
		void ChangeResourceOnDrawSub(AdvGraphicInfo graphic);

		//エフェクト用の色が変化したとき
		void OnEffectColorsChange(AdvEffectColor color);
	}
}
