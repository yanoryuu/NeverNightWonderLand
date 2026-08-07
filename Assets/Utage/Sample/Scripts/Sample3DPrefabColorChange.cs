// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	public class Sample3DPrefabColorChange 
		: MonoBehaviour
			, IAdvGraphicObject3DPrefabEffectColorChanged
	{
		public void OnEffectColorsChange(AdvEffectColor color)
		{
			//何もしない
		}
	}
}

