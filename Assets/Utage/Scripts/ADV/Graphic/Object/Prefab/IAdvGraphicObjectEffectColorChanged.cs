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
	public interface IAdvGraphicObject3DPrefabEffectColorChanged
	{
		//エフェクト用の色が変化したとき
		void OnEffectColorsChange(AdvEffectColor color);
	}
}
