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

	// 左右反転を処理の反映をカスタムするためのコンポーネントに共通のインターフェース
	public interface IAdvGraphicEventFlip
	{
		//左右反転処理
		void Flip(bool flipX, bool flipY);
	}
}
