
// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	// クロスフェード処理用のインターフェース
	public interface IAdvCrossFadeImageObject
	{
		bool IsCrossFading { get; }
		void RestartCrossFade(float fadeTime, Action onComplete);
		void SkipCrossFade();
	}
}
